# 08. รายงานความปลอดภัยระบบแลกเครดิตเป็นเหรียญเกม (P0-B: Credit Exchange Security)

**โครงการ**: OpenMU-Origins & OpenMU-Web  
**วันที่ตรวจสอบ**: 13 พฤศจิกายน 2569  
**สถานะการประเมิน**: ผ่านเกณฑ์ความปลอดภัยขั้นสูง (Two-Phase Outbox & Auto-Refund Verified)  
**ขอบเขต**: Credit-to-WCoinC Exchange, Idempotency, Network Failure Recovery, Replay Defense  

---

## 1. ข้อกำหนดทางธุรกิจและการแลกเปลี่ยน (Business Rule Contract)

- **อัตราการแลกเปลี่ยน**: 100 Website Credits = 100 WCoinC (แลกได้เป็นทวีคูณของ 100 เครดิต เช่น 100, 200, 500, 1000)
- **หลักประกันความถูกต้อง (Core Invariant)**:
  - เครดิตต้องถูกหักออกจากกระเป๋าเงินเว็บก่อนที่จะทำการมอบเหรียญในเกม
  - หากการมอบเหรียญในเกมล้มเหลวด้วยข้อผิดพลาดถาวร (4xx) เครดิตต้องถูกคืนกลับเข้ากระเป๋าผู้ใช้อัตโนมัติ (Automated Refund)
  - หากระบบเกมเซิร์ฟเวอร์ขัดข้องชั่วคราว (5xx / Timeout) ระบบต้องไม่คืนเงินและไม่ตัดเงินซ้ำ แต่จะเข้าสู่คิวรอทำรายการซ้ำ (Retry Outbox)

---

## 2. การออกแบบรูปแบบ Two-Phase Outbox Pattern

```
[Browser]             [BFF Wallet Service]           [PostgreSQL (web)]           [OpenMU CashShop API]
    |                         |                               |                             |
    |-- POST /exchange ------>|                               |                             |
    |   (credits: 100)        |-- BEGIN TRANSACTION --------->|                             |
    |   (idempotencyKey)      |-- SELECT ... FOR UPDATE ----->|                             |
    |                         |   (ตรวจ: balance >= 100)      |                             |
    |                         |-- UPDATE wallets (-100) ----->|                             |
    |                         |-- INSERT exchanges (PENDING)->|                             |
    |                         |-- COMMIT TRANSACTION -------->|                             |
    |                         |                                                             |
    |                         |-- POST /api/cashshop/add-coins (ref: webex:ID) ------------>|
    |                         |                                                             |
    |                         |   [กรณีที่ 1: ตอบกลับสำเร็จ 200 OK]                          |
    |                         |<-- { success: true, grantId } ------------------------------|
    |                         |-- UPDATE exchanges (COMPLETED) |                             |
    |                         |                                                             |
    |                         |   [กรณีที่ 2: ตอบกลับข้อผิดพลาด 4xx บัญชีไม่ถูกต้อง]         |
    |                         |<-- 400 Bad Request -----------------------------------------|
    |                         |-- UPDATE wallets (+100 คืน) ->|                             |
    |                         |-- UPDATE exchanges (REVERSED)->|                             |
    |                         |                                                             |
    |                         |   [กรณีที่ 3: เครือข่ายหลุด / Timeout 5xx]                   |
    |                         |<-- Timeout / 503 Service Unavailable -----------------------|
    |                         |-- UPDATE exchanges (PENDING_RETRY) ------------------------>|
    |                         |   (รอ Worker: reconcilePendingExchanges รันซ้ำ)             |
    |<-- 200 OK / Error ------|
```

---

## 3. การป้องกันความเสี่ยงในแต่ละจุด (Defensive Controls)

### 3.1 การป้องกัน Double Spending ด้วย Row-Level Locking
- คำขอแลกเครดิตจะรัน `SELECT * FROM web.wallets WHERE account_id = $1 FOR UPDATE` ภายใน Transaction
- หากผู้ใช้พยายามแลกเครดิตยอดเดียวกันพร้อมกัน 10 เธรด ทุกเธรดจะถูกจัดลำดับคิวและดำเนินการทีละรายการ เมื่อยอดเงินไม่พอ เธรดที่เหลือจะถูกปฏิเสธทันที

### 3.2 การอ้างอิงรหัสเฉพาะ (Unique Idempotency & OpenMU Reference)
- ทุกรายการแลกเครดิตมีรหัสเฉพาะ `openmuReference = 'webex:' + exchange.id`
- ฝั่ง OpenMU Backend มีการบันทึก Reference นี้ลงในตาราง `CashShopCoinGrant`
- หากมีการส่งคำขอซ้ำเนื่องจาก Network Timeout ฝั่ง OpenMU จะสามารถตรวจจับได้ว่าเป็นธุรกรรมเดิมและไม่เติมเหรียญซ้ำสองครั้ง

### 3.3 การจัดการข้อผิดพลาดระดับ 4xx (Auto-Refund)
- หากบัญชีผู้เล่นถูกแบนหรือไม่อยู่ในระบบ OpenMU จะตอบกลับด้วย HTTP 400/404
- BFF จะดักจับ `UpstreamError` และเรียกฟังก์ชัน `reverseExchangeTransaction`:
  - คืนยอดเครดิต 100 กลับเข้า `web.wallets`
  - ปรับปรุงสถานะเป็น `REVERSED` พร้อมบันทึกเหตุผลใน `web.audit_logs`
  - ตอบกลับผู้เล่นอย่างชัดเจนว่าเครดิตได้รับการคืนเข้ากระเป๋าเรียบร้อยแล้ว

### 3.4 การจัดการข้อผิดพลาดระดับ 5xx (Safe Outbox Retry)
- หาก OpenMU อยู่ระหว่าง Restart หรือ Network ขัดข้อง
- BFF จะไม่ทำการ Rollback ทันที เพราะคำขออาจไปถึง OpenMU แล้วแต่ Response ส่งกลับมาไม่ทัน (In-Flight State)
- ระบบจะทำเครื่องหมายเป็น `PENDING_RETRY` และมีฟังก์ชัน `reconcilePendingExchanges()` คอยประสานงานเมื่อระบบกลับมาออนไลน์

---

## 4. ผลการทดสอบเชิงรุก (Automated Test Verification)

การทดสอบใน `server/app.test.mjs` (Subtest 4):
1. **การแลกปกติ (Happy Path)**: ตัด 100 Credits -> ได้รับ 100 WCoinC -> ยอดคงเหลือหักถูกต้อง
2. **การป้องกันเงินไม่พอ (Insufficient Balance)**: ส่งคำขอเมื่อเงินมี 50 Credits -> ถูกปฏิเสธด้วยรหัส `INSUFFICIENT_CREDITS` (HTTP 400)
3. **การคืนเงินอัตโนมัติ (4xx Upstream Rejection)**: จำลอง OpenMU คืนสถานะ 404 บัญชีไม่พบ -> ระบบคืนเงิน 100 Credits กลับเข้ากระเป๋า และตั้งสถานะ `REVERSED` ทันที
4. **Idempotency Replay**: ส่งคำขอเดิมพร้อม `Idempotency-Key` เดิมซ้ำ -> คืนผลลัพธ์เดิม ไม่หักเงินเพิ่ม

---

## 5. สรุปผลการประเมิน
ระบบ P0-B: Credit Exchange มีความแข็งแกร่ง ป้องกันการสูญหายของเงินและการปั๊มเหรียญได้อย่างมีประสิทธิภาพสูงสุด

