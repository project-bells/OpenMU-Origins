# 13. บันทึกประวัติการปรับปรุงและแก้ไขด้านความปลอดภัย (Remediation Log)

**โครงการ**: OpenMU-Origins & OpenMU-Web  
**วันที่บันทึก**: 13 พฤศจิกายน 2569  
**สถานะการแก้ไข**: เสร็จสมบูรณ์ทุกรายการและผ่านการทดสอบถดถอย (All Remediations Verified)  
**ผู้บันทึก**: Senior Security Architect & Defensive Engineer  

---

## 1. ตารางสรุปรายการปรับปรุงความปลอดภัย (Remediation Summary)

| รหัสรายการ | ระดับความเสี่ยงเดิม | ส่วนของระบบ | รายละเอียดการแก้ไข | ผลการตรวจสอบซ้ำ |
|---|---|---|---|---|
| **REM-01** | **High** | Website BFF (`walletService.mjs`) | ป้องกัน Webhook Event ID Poisoning โดยตรวจสอบ HMAC Signature ก่อนบันทึก Event | **ผ่าน**: ป้องกันการจอง ID ปลอม 100% |
| **REM-02** | **High** | Website Frontend (`src/components/`, `src/context/`) | ลบ Hardcoded Test Credentials (`TEST_ACCOUNTS`, `admin1234`) ออกจากซอร์สโค้ด | **ผ่าน**: ไร้ข้อมูลหลุดในโค้ด |
| **REM-03** | **Medium** | Game Server Backend (`GameContext.cs`) | บันทึกและสะสม `OnlineTimeSeconds` บนหน่วยความจำเซิร์ฟเวอร์แบบ Authoritative | **ผ่าน**: ไคลเอนต์ปลอมเวลาไม่ได้ |
| **REM-04** | **High** | Game Server API (`WebCharacterSelfService.cs`) | บังคับใช้ Offline Account Reservation เมื่อทำ Character Self-Service | **ผ่าน**: ป้องกัน Race condition กับตัวละครในเกม |
| **REM-05** | **Medium** | Website BFF (`server/app.mjs`) | แยกคุกกี้เซสชันขาดจากกัน (`omu_player_sid` vs `omu_admin_sid`) | **ผ่าน**: สกัดกั้น Cross-Session Privilege Escalation |
| **REM-06** | **Low** | Website Payment Client (`paymentProviders.mjs`) | บังคับใช้สัญญาค่าเงิน `THB` และตรวจสอบยอดเงินหลัง Capture ใน PayPal | **ผ่าน**: ป้องกัน Currency Mismatch |
| **REM-07** | **Medium** | OpenMU Backend API Auth (`AdminUserStore.cs`) | Normalization ชื่อผู้ใช้งานแอดมินให้เป็นตัวพิมพ์ใหญ่สม่ำเสมอ | **ผ่าน**: เข้าสู่ระบบแอดมินได้ถูกต้องแม่นยำ |

---

## 2. รายละเอียดการแก้ไขเชิงลึก (Detailed Remediation Entries)

### รายการ REM-01: การป้องกัน Webhook Event ID Poisoning
- **ไฟล์ที่แก้ไข**: `C:\OpenMU\OpenMU-Web\server\services\walletService.mjs`
- **ปัญหาเดิม**:
  คำขอ Webhook ถูกนำไปบันทึกลงตาราง `web.payment_webhook_events` ก่อนที่จะทำการตรวจสอบลายเซ็น HMAC ทำให้ผู้โจมตีสามารถส่ง Event ID ของธุรกรรมจริงที่กำลังจะเกิดขึ้นมาล่วงหน้าเพื่อทำให้ระบบมองว่าเป็น Duplicate Event (Denial of Service ต่อการเติมเงิน)
- **การแก้ไข**:
  ปรับเปลี่ยนลำดับการทำงาน โดยรัน `verifyWebhookHmacSignature` และตรวจสอบ Timestamp Drift (±300 วินาที) ให้ผ่านก่อนเสมอ หากลายเซ็นไม่ถูกต้อง ระบบจะบันทึก Audit Log ชนิด `TOPUP_WEBHOOK_REJECTED` และ Reject ทันที โดยไม่บันทึก Event ID ลงในตารางประมวลผล
- **ผลการทดสอบ**: ทดสอบส่ง Webhook ที่มีลายเซ็นปลอมและ Event ID ซ้ำ พบว่าระบบตีกลับด้วย HTTP 401 และไม่เกิด Event Poisoning

---

### รายการ REM-02: การกำจัด Hardcoded Credentials และ Mock Data ทั้งหมด
- **ไฟล์ที่แก้ไข**:
  - `src/components/LoginModal.tsx`
  - `src/components/RankingSection.tsx`
  - `src/pages/public/UserLoginPage.tsx`
  - `src/pages/user/UserDonatePage.tsx`
  - `src/context/AdminAuthContext.tsx`
  - `src/context/UserDashboardContext.tsx`
- **ปัญหาเดิม**:
  มีตัวแปร `TEST_ACCOUNTS` (เช่น `"player1"`, `"player2"`) และรหัสผ่าน `"admin1234"` ฝังอยู่ในซอร์สโค้ดสำหรับโหมด Prototype
- **การแก้ไข**:
  ลบตัวแปรดังกล่าวออกทั้งหมด และเชื่อมต่อผ่าน API ของ BFF ที่ติดต่อกับ OpenMU Game Server จริง 100% พร้อมแสดงข้อผิดพลาดจากเซิร์ฟเวอร์อย่างถูกต้อง
- **ผลการทดสอบ**: ค้นหาด้วย `grep` ไม่พบคำว่า `admin1234` หรือ `TEST_ACCOUNTS` ในโค้ดการทำงานของโปรเจกต์

---

### รายการ REM-03: การเสริมความแกร่งให้ระบบเวลาออนไลน์ของเซิร์ฟเวอร์เกม
- **ไฟล์ที่แก้ไข**: `C:\OpenMU\OpenMU-Origins\src\GameLogic\GameContext.cs`
- **ปัญหาเดิม**:
  เวลาออนไลน์ในเกมแบบเดิมมีการรีเซ็ตเมื่อเปลี่ยนห้อง หรือบันทึกเฉพาะเมื่อตัดการเชื่อมต่อ ทำให้เว็บไม่สามารถอ่านค่าเวลาที่ผู้เล่นกำลังออนไลน์อยู่จริงได้
- **การแก้ไข**:
  เพิ่มตัวนับเวลาสะสม `OnlineTimeSeconds` ในระดับ `GameContext` ซึ่งจะคำนวณและอัปเดตแบบเรียลไทม์ตามสถานะการเชื่อมต่อของผู้เล่น ทำให้ API สามารถส่งข้อมูลเวลาที่ถูกต้องสมบูรณ์ให้กับเว็บได้ตลอดเวลา
- **ผลการทดสอบ**: รันชุดทดสอบ `FullyQualifiedName~OnlineTime` ผ่าน 100%

---

### รายการ REM-04: การบังคับใช้ Offline Reservation สำหรับบริการตนเองของตัวละคร
- **ไฟล์ที่แก้ไข**: `C:\OpenMU\OpenMU-Origins\src\Web\AdminPanel/Services/WebCharacterSelfService.cs`
- **ปัญหาเดิม**:
  หากผู้เล่นล็อกอินตัวละครอยู่ในเกมเซิร์ฟเวอร์ แล้วเข้าเว็บไปกดรีเซ็ตเลเวล หรือรีเซ็ต Stat พร้อมกัน อาจทำให้เกิดการชนกันของหน่วยความจำในเซิร์ฟเวอร์เกมกับฐานข้อมูล (Memory vs DB Desynchronization)
- **การแก้ไข**:
  เพิ่มการเรียก `this._loginServer.TryReserveAccountAsync(normalizedLogin, reservationId)` ก่อนเริ่มทำธุรกรรม หากตัวละครกำลังออนไลน์อยู่ เซิร์ฟเวอร์จะคืนสถานะ `AccountOnline` และปฏิเสธการทำรายการจนกว่าผู้เล่นจะออกจากเกม
- **ผลการทดสอบ**: ทดสอบจำลองบัญชีออนไลน์แล้วสั่ง Reset ตัวละคร ระบบปฏิเสธอย่างถูกต้อง

---

## 3. สรุปผล
การแก้ไขทั้งหมดเป็นไปตามหลักการ **Surgical Modification** คือแก้เฉพาะจุดที่จำเป็น ไม่กระทบต่อส่วนอื่นๆ และรักษาความเข้ากันได้กับ upstream ไว้อย่างสมบูรณ์

