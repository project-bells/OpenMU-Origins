# 07. รายงานความปลอดภัยระบบรางวัลเวลาออนไลน์ (P0-A: Online-Time Reward Security)

**โครงการ**: OpenMU-Origins & OpenMU-Web  
**วันที่ตรวจสอบ**: 13 พฤศจิกายน 2569  
**สถานะการประเมิน**: ผ่านเกณฑ์ความปลอดภัยสมบูรณ์ (Authoritative Tracking & Anti-Double Claim Verified)  
**ขอบเขต**: GameContext Online Tracking, Web Wallet Ledger, Concurrent Claim Defense  

---

## 1. กฎทางธุรกิจและเงื่อนไขของระบบ (Business Logic Contract)

- **อัตราแลกเปลี่ยน**: เล่นเกมครบทุกๆ **1 ชั่วโมง (3,600 วินาที)** จะได้รับสิทธิ์แลก **10 เครดิต** เข้ากระเป๋าเงินเว็บ
- **ความถูกต้องของเวลา**: ไคลเอนต์เบราว์เซอร์ **ไม่มีสิทธิ์** ส่งตัวเลขชั่วโมงหรือวินาทีที่ตนเองอ้างว่าเล่นเข้ามาเพื่อขอเครดิต
- **Server-Authoritative Tracking**: ตัวเลขเวลาออนไลน์รวม (`OnlineTimeSeconds`) ถูกคำนวณและสะสมโดยตรงบนเซิร์ฟเวอร์เกม (`GameContext.cs`) เมื่อผู้เล่นเชื่อมต่ออยู่ในเซิร์ฟเวอร์เท่านั้น

---

## 2. โครงสร้างข้อมูลและการคำนวณสิทธิ์ (Data Model & Ledger Invariants)

ระบบใช้ตารางบัญชีแยกประเภทใน PostgreSQL:
- **`web.online_time_ledgers`**:
  - `account_id` (PK)
  - `authoritative_online_seconds`: เวลาออนไลน์สูงสุดที่เซิร์ฟเวอร์เกมรายงานล่าสุด
  - `redeemed_online_seconds`: วินาทีที่ถูกนำไปเคลมเป็นเครดิตแล้ว
  - `available_seconds`: คำนวณจาก `authoritative_online_seconds - redeemed_online_seconds`
  - `CONSTRAINT ck_redeemed_not_exceed_authoritative CHECK (redeemed_online_seconds <= authoritative_online_seconds)`

---

## 3. ขั้นตอนการทำงานที่ป้องกันการโจมตี (Secure Claim Sequence)

```
[User Browser]             [BFF Service]               [OpenMU Backend]          [PostgreSQL]
      |                          |                            |                       |
      |-- POST /redeem --------->|                            |                       |
      |   (Idempotency-Key)      |-- GET /api/accounts/:id -->|                       |
      |                          |<-- { onlineTimeSeconds } --|                       |
      |                          |                                                    |
      |                          |-- BEGIN TRANSACTION ------------------------------>|
      |                          |-- SELECT ... FOR UPDATE (Lock Wallet & Ledger) --->|
      |                          |   (คำนวณ: ClaimableHours = Floor(Available / 3600))|
      |                          |   (ตรวจสอบ: ClaimableHours >= 1)                    |
      |                          |-- UPDATE web.wallets (+ Credits, + RedeemedSec) -->|
      |                          |-- INSERT INTO web.online_reward_redemptions ------>|
      |                          |-- INSERT INTO web.credit_transactions ------------>|
      |                          |-- COMMIT TRANSACTION ----------------------------->|
      |<-- 200 OK (Credits Added)|
```

---

## 4. ผลการทดสอบกรณีการโจมตี (Exploit Scenario Verification)

### 4.1 การกดเคลมพร้อมกันหลายหน้าต่าง (Concurrent Double Claim)
- **การทดสอบ**: ส่งคำขอเคลมรางวัล 5 คำขอพร้อมกันในมิลลิวินาทีเดียวกัน โดยมีเวลาออนไลน์คงเหลือเพียงพอสำหรับแลก 1 ครั้ง (3,600 วินาที = 10 เครดิต)
- **ผลลัพธ์**:
  - คำขอแรกสามารถครอบครอง Row Lock ได้สำเร็จ ได้รับ 10 เครดิต และอัปเดต `redeemed_online_seconds` เพิ่ม 3,600 วินาที
  - คำขอที่ 2 ถึง 5 ถูกบล็อกรอจนคำขอแรกเสร็จ เมื่อเข้าอ่านข้อมูลพบว่า `available_seconds = 0` ส่งผลให้ถูกปฏิเสธด้วยรหัส `INSUFFICIENT_ONLINE_TIME`
  - **สรุป**: ไม่เกิดการรับเครดิตซ้ำซ้อน ยอดเครดิตเพิ่มถูกต้องตามเวลาจริง 100%

### 4.2 การ Replay คำขอด้วย Idempotency Key เดิม
- **การทดสอบ**: ส่งคำขอเคลมเดิมพร้อม `Idempotency-Key` เดิมซ้ำอีกครั้ง
- **ผลลัพธ์**: ระบบตรวจพบรายการใน `web.online_reward_redemptions` ที่ผูกกับคีย์ดังกล่าว และคืนค่าสถานะความสำเร็จเดิมโดยไม่บวกเครดิตซ้ำ

### 4.3 การปลอมแปลงเวลาจากฝั่งเบราว์เซอร์ (Client-side Time Tampering)
- **การทดสอบ**: ผู้เล่นแก้ไข Local Clock ของเครื่องตนเอง หรือส่งตัวเลขเวลาปลอมมาใน Request Body
- **ผลลัพธ์**: BFF ละเลยค่าที่ส่งมาจากเบราว์เซอร์ทั้งหมด และดึงค่าเวลาจาก OpenMU Game API ที่มี Header `X-Api-Key` เท่านั้น การแกล้งปรับเวลาจึงไม่มีผลใดๆ ทั้งสิ้น

---

## 5. สรุปผลการประเมิน
ระบบ P0-A: Online-Time Rewards มีความปลอดภัยสูงสุด สอดคล้องตามหลักการ Server-Authoritative และ ACID Transaction

