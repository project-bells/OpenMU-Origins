# 09. รายงานการตรวจสอบความมั่นคงปลอดภัยของการเชื่อมโยงระบบเว็บไซต์ (Website Integration Security)

**โครงการ**: OpenMU-Web & OpenMU-Origins  
**วันที่ตรวจสอบ**: 13 พฤศจิกายน 2569  
**สถานะการประเมิน**: **ผ่านเกณฑ์ระดับสมบูรณ์ (Integration Verified & Zero Vulnerabilities)**  
**ขอบเขต**: Web-to-Game Authentication, Character Self-Service, P0 Workflows, API Key  
**ผู้ตรวจสอบ**: Senior Full-Stack & Application Security Engineer  

---

## 1. ขอบเขตความปลอดภัยระหว่างเว็บไซต์และเซิร์ฟเวอร์เกม (Web-to-Game Trust Boundary)

```
[Web Browser Client]
        |
        | HTTPS (Cookie: omu_player_sid, SameSite=Lax, HttpOnly)
        v
[Website BFF Server (Node.js v22)]
        |
        | Private Server-to-Server REST Call
        | Header: X-Api-Key (ความยาว >= 32 ตัวอักษร, SHA-256 Hashed)
        v
[OpenMU AdminPanel Web API (.NET 10)]
        |
        | In-Memory Game Context & EF Core
        v
[Game Database (PostgreSQL)]
```

### การควบคุมความมั่นคงปลอดภัยที่จุดเชื่อมโยง:
1. **API Key Isolation**: กุญแจ `X-Api-Key` ถูกจัดเก็บเป็นความลับในสภาพแวดล้อมฝั่งเซิร์ฟเวอร์ (`.env`) เบราว์เซอร์ของผู้เล่นไม่มีทางเข้าถึงหรือเห็นกุญแจนี้ได้
2. **Session Cookie Isolation**: แบ่งแยกคุกกี้ผู้เล่น (`omu_player_sid`) และผู้ดูแลระบบ (`omu_admin_sid`) ไม่สามารถใช้แทนกันได้
3. **การป้องกัน IDOR 100%**: ทุกคำขอด้านตัวละคร (Reset, PK Clear, Add Stats) จะใช้ `session.loginName` จากคุกกี้เซสชันที่ตรวจสอบแล้วเท่านั้น ไคลเอนต์ไม่สามารถส่งชื่อบัญชีของผู้อื่นมาสั่งแก้ไขข้อมูลได้

---

## 2. การตรวจสอบการทำงานของฟังก์ชันระดับ P0 (P0 Business Workflows)

### 2.1 P0-A: รางวัลเวลาออนไลน์ (Online-Time Rewards)
- **เงื่อนไขทางธุรกิจ**: เล่นเกมครบ **1 ชั่วโมง (3,600 วินาที) = 10 เครดิต**
- **ความถูกต้องของแหล่งข้อมูล**: ดึงค่าเวลาสะสม `onlineTimeSeconds` จาก `GameContext.cs` บนเซิร์ฟเวอร์เกมโดยตรง
- **การหักลบและจองสิทธิ์**: หักลบกับ `redeemed_online_seconds` ใน `web.online_time_ledgers` ล็อกด้วย Idempotency Key ไม่สามารถกดรับซ้ำซ้อนได้

### 2.2 P0-B: การแลกเครดิตเป็นเหรียญเกม (Credit Exchange to WCoinC)
- **เงื่อนไขทางธุรกิจ**: **100 เครดิต = 100 WCoinC**
- **สถาปัตยกรรม Two-Phase Outbox**:
  1. ล็อกและหักเครดิตใน `web.wallets` พร้อมบันทึกสถานะ `PENDING` ใน `web.credit_exchanges` ภายใน Database Transaction
  2. ยิงคำขอเติมเหรียญไปยัง OpenMU CashShop API ด้วย Unique Reference (`webex:<id>`)
  3. **Auto-Refund Semantics**: หาก OpenMU ตอบกลับ 4xx (เช่น บัญชีไม่พบบนเกม) ระบบจะคืนเครดิตเข้ากระเป๋าผู้ใช้อัตโนมัติ (`status = 'REVERSED'`)
  4. หากเกิด Timeout หรือ 5xx ระบบจะคงสถานะ `PENDING_RETRY` และมี Worker ทำการ Reconcile ซ้ำ ไม่ตัดเงินสูญเปล่าและไม่สร้างเหรียญเบิ้ล

### 2.3 P0-C: การเติมเงินและประมวลผลการชำระเงิน (Top-Up & Payment Processing)
- **การควบคุมราคาแบบเด็ดขาด**: ดึงข้อมูลแพ็กเกจและราคาจากแคตตาล็อกเซิร์ฟเวอร์ (`TOPUP_PACKAGES`) เสมอ ปฏิเสธการส่งราคาจากฝั่งเบราว์เซอร์
- **PromptPay Dynamic QR**: มาตรฐาน EMVCo พร้อมการคำนวณ Checksum ด้วย CRC16-CCITT (Polynomial `0x1021`)
- **การลงนาม HMAC-SHA256**: Webhook แจ้งยอดเงินชำระต้องมีลายเซ็นถูกต้อง และตรวจสอบ Timestamp Drift ไม่เกิน ±300 วินาที ป้องกัน Replay Attack
- **การป้องกัน Event ID Poisoning**: สลับขั้นตอนตรวจสอบลายเซ็น HMAC ให้ผ่านก่อนเสมอ ก่อนจะบันทึก Event ID ลงในตารางตรวจสอบความซ้ำซ้อน

---

## 3. การจองสถานะบัญชีขณะให้บริการตนเอง (Offline Account Reservation)

เพื่อป้องกันการเกิด Race Condition เมื่อผู้เล่นเปิดเกมเล่นอยู่ แล้วมากดรีเซ็ตเลเวลบนหน้าเว็บ:
```csharp
// WebCharacterSelfService.cs
var reservationId = WebCharacterMapper.CreateReservationId();
if (!await this._loginServer.TryReserveAccountAsync(normalizedLogin, reservationId).ConfigureAwait(false))
{
    return new WebCharacterOperationResult(WebCharacterOperationStatus.AccountOnline, "Account is currently logged in.");
}
```
- ระบบจะตรวจสอบว่าบัญชีกำลังออนไลน์อยู่ใน Game Server หรือไม่
- หากกำลังออนไลน์ เซิร์ฟเวอร์จะคืนสถานะ `AccountOnline` และระงับการทำรายการจนกว่าผู้เล่นจะออกจากเกม ป้องกัน State Desynchronization ระหว่างหน่วยความจำกับฐานข้อมูล

---

## 4. ตารางสรุปผลการประเมินการเชื่อมโยงระบบเว็บไซต์

| จุดทดสอบ | รายละเอียดการประเมิน | สถานะความปลอดภัย |
|---|---|---|
| **Credential Storage** | รหัสผ่านเก็บด้วย BCrypt ทั้งหมด | **VERIFIED_SAFE** |
| **Session Fixation** | สร้าง Session ID ด้วย Crypto Random 256 บิต | **VERIFIED_SAFE** |
| **CSRF Protection** | Origin Guard บังคับใช้ในทุก Mutating API | **VERIFIED_SAFE** |
| **Rate Limiting** | Sliding-window ป้องกัน Brute-force ทุกจุด | **VERIFIED_SAFE** |
| **Cross-Origin Leak** | Security Headers (CSP, X-Frame-Options DENY) | **VERIFIED_SAFE** |
| **P0-A / P0-B / P0-C** | ผ่านการทดสอบอัตโนมัติครบทุกเงื่อนไข | **VERIFIED_WORKING** |

