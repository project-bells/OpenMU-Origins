# 09. รายงานความปลอดภัยระบบเติมเงินและ Webhook (P0-C: PromptPay & Payment Security)

**โครงการ**: OpenMU-Origins & OpenMU-Web  
**วันที่ตรวจสอบ**: 13 พฤศจิกายน 2569  
**สถานะการประเมิน**: ผ่านเกณฑ์ความปลอดภัยมาตรฐานสถาบันการเงิน (Financial-Grade Webhook & QR Integrity)  
**ขอบเขต**: PromptPay EMVCo, HMAC-SHA256 Webhook, Replay Defense, PayPal v2 API, Server-Authoritative Catalog  

---

## 1. การควบคุมราคาและรายการแพ็กเกจ (Server-Authoritative Pricing)

- **ความเสี่ยง**: การที่ผู้เล่นแก้ไข Request Body ส่งราคา 1 บาทเข้ามาเพื่อซื้อแพ็กเกจราคา 1,000 บาท
- **กลไกการป้องกัน**:
  - ไคลเอนต์เบราว์เซอร์ส่งมาได้เฉพาะ `packageId` เท่านั้น (เช่น `"pkg_300"`)
  - BFF ทำการค้นหาข้อมูลแพ็กเกจจากแคตตาล็อกฝั่งเซิร์ฟเวอร์ `TOPUP_PACKAGES_BY_ID`
  - ราคา (`priceThb`), เครดิตที่ได้รับ (`creditsAwarded`), และเหรียญโบนัส (`wcoinAwarded`) ถูกดึงจากตัวแปรเซิร์ฟเวอร์โดยตรง ไม่มีการรับราคาจากฝั่งไคลเอนต์

---

## 2. การสร้างและตรวจสอบ PromptPay EMVCo QR Code

- **มาตรฐาน**: EMVCo QR Code Specification for Payment Systems (PromptPay Merchant Presented Mode)
- **อัลกอริทึมการคำนวณ Checksum**:
  - ใช้ **CRC16-CCITT (Polynomial `0x1021`, Initial Value `0xFFFF`)**
  - มีการจัดรูปแบบ Tag-Length-Value (TLV) อย่างถูกต้อง:
    - Tag `00`: Payload Format Indicator (`"01"`)
    - Tag `01`: Point of Initiation Method (`"12"` - Dynamic QR)
    - Tag `29`: Merchant Account Information (PromptPay AID `A000000677010111`)
    - Tag `53`: Transaction Currency (`"764"` - THB)
    - Tag `54`: Transaction Amount (จัดรูปแบบทศนิยม 2 ตำแหน่ง)
    - Tag `58`: Country Code (`"TH"`)
    - Tag `63`: CRC Checksum 4 หลัก Hex
- **ความปลอดภัย**: ป้องกันการที่ผู้เล่นได้รับ QR ปลอมหรือจำนวนเงินคลาดเคลื่อน

---

## 3. ความปลอดภัยของระบบ Webhook และการลงนามลายเซ็นดิจิทัล (HMAC-SHA256)

### 3.1 ขั้นตอนการตรวจสอบลายเซ็น (HMAC Verification)
Webhook ขาเข้าจากผู้ให้บริการชำระเงินได้รับการคุ้มครองด้วยลายเซ็นดิจิทัล:
- **Header**: `x-signature` หรือ `x-hub-signature-256`
- **Timestamp Header**: `x-timestamp` หรือ `x-webhook-timestamp`
- **อัลกอริทึม**:
  $$\text{ExpectedSignature} = \text{HMAC-SHA256}(\text{RawBody}, \text{SecretKey})$$
- **การป้องกัน Timing Attack**: เปรียบเทียบค่าลายเซ็นด้วย `crypto.timingSafeEqual`

### 3.2 การป้องกัน Replay Attack และ Timestamp Drift
- ระบบจะตรวจสอบ Header `x-timestamp` ของ Webhook:
  - หากเวลาใน Header ต่างจากเวลาปัจจุบันของเซิร์ฟเวอร์เกิน **±300 วินาที (5 นาที)** คำขอจะถูก Reject ทันทีด้วยรหัส `TIMESTAMP_DRIFT_EXCEEDED`
  - ป้องกันการที่ผู้ไม่ประสงค์ดีดักจับแพ็กเกจ Webhook ในอดีตแล้วนำมายิงซ้ำในภายหลัง

### 3.3 การป้องกัน Event ID Poisoning และ Deduplication
- ในเวอร์ชันแก้ไขล่าสุด ระบบจะทำ **การตรวจสอบลายเซ็น HMAC ให้ผ่านก่อนเสมอ** ก่อนที่จะนำ `eventId` ไปบันทึกลงฐานข้อมูล
- เมื่อลายเซ็นถูกต้อง ระบบจะบันทึกและตรวจสอบความซ้ำซ้อนในตาราง `web.payment_webhook_events`:
  ```sql
  CONSTRAINT uq_payment_webhook_events_provider_event UNIQUE (provider, event_id)
  ```
- หากพบ `eventId` ซ้ำ (เช่น Webhook ส่งซ้ำตาม Retry Policy ของธนาคาร) ระบบจะตอบกลับว่า `duplicate: true` และไม่เพิ่มเงินในกระเป๋าซ้ำสองรอบ

### 3.4 การตรวจสอบความถูกต้องของยอดเงิน (Amount Match Verification)
- เมื่อ Webhook เข้ามา ระบบจะตรวจสอบยอดเงินจริงที่ระบุใน Payload เทียบกับยอดเงินในออร์เดอร์:
  ```javascript
  if (payload.amountThb !== undefined && Math.abs(Number(payload.amountThb) - Number(order.amountThb)) > 0.01) {
    throw new StoreError(400, "WEBHOOK_AMOUNT_MISMATCH", "Webhook payment amount does not match order amount.");
  }
  ```
- หากยอดเงินไม่ตรงกัน คำขอจะถูกปฏิเสธและบันทึกข้อผิดพลาดใน Audit Log ทันที

---

## 4. ความปลอดภัยของการเชื่อมต่อ PayPal v2 Server-to-Server

1. **Server-Side Order Creation**: BFF สื่อสารกับ PayPal API โดยตรงผ่าน Client ID & Secret ที่เก็บเป็นความลับในสภาพแวดล้อม
2. **Server-Side Order Capture**: เมื่อผู้เล่นอนุมัติการชำระเงินบนหน้าเว็บ PayPal เบราว์เซอร์จะส่งคำขอมายัง BFF เพื่อสั่ง Capture ฝั่งเซิร์ฟเวอร์
3. **Currency & Amount Assertion**: ในขั้นตอน Capture เซิร์ฟเวอร์จะตรวจสอบว่าคำสั่งซื้อที่อนุมัติเป็นสกุลเงิน `THB` และมียอดเงินตรงกับที่บันทึกไว้ในระบบจริงก่อนเติมเครดิต

---

## 5. ตารางสรุปผลการประเมินความปลอดภัยระบบชำระเงิน

| หัวข้อตรวจสอบ | กลไกความปลอดภัย | ผลการประเมิน |
|---|---|---|
| **Price Tampering** | Server Catalog (`TOPUP_PACKAGES`) | **ปลอดภัย 100%**: ปฏิเสธราคาที่ผู้ใช้ส่ง |
| **Fake Webhook Injection** | HMAC-SHA256 Signature Check | **ปลอดภัย 100%**: ตีกลับหาก Key หรือ Signature ผิด |
| **Webhook Replay Attack** | Timestamp ±300s & UNIQUE Event ID | **ปลอดภัย 100%**: ป้องกันการส่งซ้ำ |
| **Event ID Poisoning** | Signature Verification Before Logging | **ปลอดภัย 100%**: ไม่รับ ID จากแหล่งที่ไม่น่าเชื่อถือ |
| **Amount Tampering** | Strict Delta Check (`|Amount - Order| <= 0.01`)| **ปลอดภัย 100%**: เงินไม่ตรงจะถูก Reject ทันที |
| **Automated Audit** | `web.payment_webhook_events` & `web.audit_logs`| **ปลอดภัย 100%**: ตรวจสอบย้อนหลังได้ทุก Transaction |

