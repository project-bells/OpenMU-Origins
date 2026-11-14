# 13. บันทึกการปรับปรุง แก้ไข และเสริมความมั่นคงปลอดภัย (Remediation & Hardening Log)

**โครงการ**: OpenMU Full Ecosystem (Client, Server & Web)  
**วันที่บันทึก**: 13 พฤศจิกายน 2569  
**สถานะการแก้ไข**: **แก้ไขเสร็จสิ้น 100% และผ่านการทดสอบถดถอยทั้งหมด (All Remediations Verified)**  
**หลักการแก้ไข**: Surgical, Safe & Minimal Changes (คงความเข้ากันได้ ไม่กระทบฟังก์ชันเดิม)  
**ผู้ตรวจสอบ**: Senior Security & Systems Architect  

---

## 1. ตารางสรุปรายการแก้ไขและปรับปรุงความปลอดภัย (Remediation Summary Table)

| รหัสรายการ | ระดับความเสี่ยงเดิม | ระบบที่ได้รับผลกระทบ | คำอธิบายสั้นของการแก้ไข | สถานะการตรวจสอบ |
|---|---|---|---|---|
| **REM-01** | **High** | Website BFF (`walletService.mjs`) | ป้องกัน Webhook Event ID Poisoning โดยตรวจสอบ HMAC Signature ก่อนบันทึก | **FIXED_AND_VERIFIED** |
| **REM-02** | **High** | Website Frontend (`src/`) | ลบ Mock Credentials (`TEST_ACCOUNTS`, `admin1234`) ออกจากซอร์สโค้ดทั้งหมด | **FIXED_AND_VERIFIED** |
| **REM-03** | **Medium** | Game Server (`GameContext.cs`) | ปรับปรุงระบบบันทึกเวลาออนไลน์ `OnlineTimeSeconds` บนหน่วยความจำเซิร์ฟเวอร์ | **FIXED_AND_VERIFIED** |
| **REM-04** | **High** | Game Server API (`WebCharacterSelfService.cs`) | บังคับใช้ Offline Account Reservation เมื่อจัดการตัวละครผ่านหน้าเว็บ | **FIXED_AND_VERIFIED** |
| **REM-05** | **Medium** | Website BFF (`server/app.mjs`) | แบ่งแยก Cookie Session ขาดจากกัน (`omu_player_sid` vs `omu_admin_sid`) | **FIXED_AND_VERIFIED** |
| **REM-06** | **Low** | Website Payment (`paymentProviders.mjs`) | บังคับใช้สัญญาค่าเงิน `THB` และตรวจสอบยอดเงินหลัง Capture ใน PayPal | **FIXED_AND_VERIFIED** |
| **REM-07** | **Medium** | Game Server API (`AdminUserStore.cs`) | บังคับใช้ Normalization ชื่อผู้ใช้งานแอดมินเป็นตัวพิมพ์ใหญ่ | **FIXED_AND_VERIFIED** |
| **REM-08** | **Medium** | Client Library (`Windows874Wire.cs`) | แปลงสายข้อมูลตัวอักษรระหว่าง C# UTF-16 กับสาย Network เป็น Windows-874 (ไทย) | **FIXED_AND_VERIFIED** |
| **REM-09** | **High** | Game Server (`ServerAuthoritativeAntiCheatPlugIn.cs`)| บังคับใช้ Single-Flight Economic Lease ป้องกันการซื้อขายหรือเทรดซ้อน | **FIXED_AND_VERIFIED** |

---

## 2. รายละเอียดการวิเคราะห์สาเหตุและวิธีแก้ไข (Root Cause Analysis & Fixes)

### รายการ REM-01: การป้องกัน Webhook Event ID Poisoning
- **สาเหตุ (Root Cause)**: ในโค้ดเดิม ข้อมูล Webhook ถูกบันทึกเข้าสู่ตาราง `web.payment_webhook_events` ก่อนที่จะทำการตรวจสอบลายเซ็น HMAC ส่งผลให้ผู้โจมตีสามารถส่ง Event ID ปลอมมาจองคิวไว้ล่วงหน้า ทำให้ Webhook จริงที่จะมาถึงในภายหลังถูกมองว่าเป็น Duplicate Event (Denial of Service)
- **ไฟล์ที่แก้ไข**: `C:\OpenMU\OpenMU-Web\server\services\walletService.mjs`
- **แนวทางแก้ไข**: ปรับเปลี่ยนลำดับโค้ด โดยให้รันฟังก์ชัน `verifyWebhookHmacSignature` และตรวจสอบ Timestamp Drift (±300 วินาที) ให้ผ่านก่อนเสมอ หากลายเซ็นไม่ถูกต้อง จะบันทึก Audit Log ชนิด `TOPUP_WEBHOOK_REJECTED` และ Reject ทันที โดยไม่บันทึก Event ID ลงในตารางประมวลผล
- **ผลการทดสอบ**: ทดสอบส่ง Webhook ที่ลายเซ็นผิดและ Event ID ซ้ำ ระบบตีกลับด้วย HTTP 401 และไม่เกิด Event Poisoning

---

### รายการ REM-04: การบังคับใช้ Offline Reservation สำหรับตัวละคร
- **สาเหตุ (Root Cause)**: หากผู้เล่นกำลังออนไลน์อยู่ในเกมเซิร์ฟเวอร์ แล้วเข้าหน้าเว็บไปกดรีเซ็ตเลเวล หรือรีเซ็ต Stat พร้อมกัน อาจทำให้เกิดการชนกันของ State ระหว่างหน่วยความจำในเกมเซิร์ฟเวอร์กับฐานข้อมูล (Memory vs DB Desynchronization)
- **ไฟล์ที่แก้ไข**: `C:\OpenMU\OpenMU-Origins\src\Web\AdminPanel\Services\WebCharacterSelfService.cs`
- **แนวทางแก้ไข**: เพิ่มคำสั่ง `this._loginServer.TryReserveAccountAsync(normalizedLogin, reservationId)` ก่อนเริ่มทำธุรกรรม หากตัวละครกำลังออนไลน์อยู่ เซิร์ฟเวอร์จะคืนสถานะ `AccountOnline` และระงับการทำรายการจนกว่าผู้เล่นจะออกจากเกม
- **ผลการทดสอบ**: ทดสอบส่งคำขอรีเซ็ตขณะตัวละครออนไลน์ เซิร์ฟเวอร์ปฏิเสธอย่างถูกต้อง

---

### รายการ REM-08: การรองรับภาษาไทยในสายสัญญาณเครือข่าย (Windows-874 Wire Format)
- **สาเหตุ (Root Cause)**: ในโปรโตคอล MuOnline Season 6 ข้อความในแพ็กเก็ตที่เป็นภาษาไทย (Thai Ansi) ใช้ Code Page 874 แต่ในโปรเจกต์ C# .NET 10 ค่าเริ่มต้นของสตริงเป็น UTF-16 หากไม่มีการแปลงที่ถูกต้อง ชื่อตัวละครหรือข้อความแชทภาษาไทยจะกลายเป็นเครื่องหมายคำถาม (`???`) หรือตัวอักษรขยะ
- **ไฟล์ที่แก้ไข**: `C:\OpenMU\OpenMu-ClientMainS6E3\ClientLibrary\Windows874Wire.cs`
- **แนวทางแก้ไข**: นำเข้า Encoding Provider `CodePagesEncodingProvider.Instance` และสร้าง Helper ในการอ่าน/เขียนไบต์ของสายข้อมูลเน็ตเวิร์กด้วย `Encoding.GetEncoding(874)`
- **ผลการทดสอบ**: ทดสอบพิมพ์ข้อความและตั้งชื่อตัวละครภาษาไทย แสดงผลถูกต้องทั้งในไคลเอนต์และเซิร์ฟเวอร์

---

### รายการ REM-09: Single-Flight Economic Transaction Lease
- **สาเหตุ (Root Cause)**: ความเสี่ยงในการส่งแพ็กเกจซื้อไอเทมในร้านค้าส่วนตัว หรือการเทรดซ้ำซ้อนกันในเวลาเสี้ยววินาทีเพื่อพยายามคัดลอกไอเทม (Race Condition)
- **ไฟล์ที่แก้ไข**: `C:\OpenMU\OpenMU-Origins\src\GameLogic\AntiCheat\ServerAuthoritativeAntiCheatPlugIn.cs`
- **แนวทางแก้ไข**: เพิ่มคลาส `EconomicTransactionState` และเมทอด `TryBeginEconomicTransaction` เมื่อมีคำขอธุรกรรมเศรษฐกิจเข้ามา ผู้เล่นจะได้รับ Transaction Lease ซึ่งหากมีคำขอที่สองส่งเข้ามาซ้อนขณะคำขอแรกยังไม่เสร็จสิ้น คำขอที่สองจะถูกปฏิเสธทันที
- **ผลการทดสอบ**: ทดสอบยิงคำขอซื้อไอเทมพร้อมกัน 2 เธรด คำขอแรกสำเร็จ คำขอที่สองถูกบล็อก ไม่เกิดไอเทมคู่แฝด

---

## 3. สรุปผล
การปรับปรุงแก้ไขทั้งหมดได้ดำเนินการเสร็จสิ้น ไร้ผลข้างเคียงต่อระบบ และผ่านการทดสอบ Regression ครบทุกรายการ

