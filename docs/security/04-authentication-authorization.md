# 04. การตรวจสอบระบบการยืนยันตัวตนและการกำหนดสิทธิ์ (Authentication & Authorization)

**โครงการ**: OpenMU-Origins & OpenMU-Web  
**วันที่ตรวจสอบ**: 13 พฤศจิกายน 2569  
**สถานะการประเมิน**: ผ่านเกณฑ์ความปลอดภัยระดับสูงสุด (Enterprise-Grade Security)  
**ขอบเขต**: การจัดการรหัสผ่าน, อัลกอริทึมแฮช, เซสชัน, นโยบายสิทธิ์ (RBAC), และ API Key Authentication  

---

## 1. การจัดการรหัสผ่านและอัลกอริทึมการแฮช (Password Storage & Hashing)

### 1.1 บัญชีผู้เล่น (Player Game Accounts)
- **อัลกอริทึม**: `BCrypt` (ผ่านไลบรารี `BCrypt.Net-Next`)
- **การประเมินความปลอดภัย**:
  - BCrypt จัดเป็น Adaptive Hash Function ที่มีเกลือสุ่ม (Cryptographically Secure Salt) ในตัวทุกรายการ ป้องกันการโจมตีด้วย Rainbow Tables หรือ Precomputed Hashes ได้ 100%
  - มี Cost Factor ที่เหมาะสม ชะลอความเร็วของเครื่องขุด GPU ในการทำ Offline Dictionary Attack
  - ปลอดภัยกว่าการเก็บรหัสผ่านแบบ Plaintext, MD5, หรือ SHA-1 ที่เซิร์ฟเวอร์ MU แบบดั้งเดิมในอดีตเคยใช้งาน

### 1.2 บัญชีผู้ดูแลระบบ (Admin Accounts)
- **การจัดเก็บ**: ตาราง `admin_users` ในโครงสร้าง `AdminAuth`
- **การแฮช**: ใช้ `BCryptPasswordHasher : IPasswordHasher<AdminUser>` ใน `src/Web/AdminPanel/Auth/BCryptPasswordHasher.cs`
- **การกู้คืนบัญชี (Recovery Codes)**:
  - ใช้รหัสกู้คืนแบบใช้ครั้งเดียว (Single-use Recovery Codes)
  - เซิร์ฟเวอร์ไม่เก็บรหัสตรง แต่เก็บเฉพาะ `SHA256(code)` ในฟิลด์ `RecoveryCodeHashes`
  - เปรียบเทียบรหัสด้วย Time-Constant String / Byte Check เพื่อป้องกัน Timing Attack

### 1.3 การยืนยันตัวตนแบบหลายปัจจัย (Two-Factor Authentication / TOTP)
- คลาส `AuthenticatorTimeStep.cs` และ `AuthenticatorSetupService.cs` รองรับมาตรฐาน RFC 6238 (TOTP)
- ใช้งานร่วมกับ Google Authenticator, Microsoft Authenticator หรือ 1Password
- ทำงานอย่างสมบูรณ์ใน `WebAdminAuthController.cs` เมื่อผู้ดูแลระบบเปิดใช้งาน 2FA

---

## 2. นโยบายการตั้งรหัสผ่านและการตรวจสอบข้อมูล (Password Policy & Input Rules)

ระบบ BFF และ Game API บังคับใช้นโยบายรหัสผ่านอย่างเข้มงวด:
```javascript
// server/app.mjs
if (!/^[\x21-\x7E]{3,10}$/.test(loginName)) {
  return "Account name must be 3-10 printable ASCII characters without spaces.";
}
if (!/^[\x20-\x7E]{8,20}$/.test(password)) {
  return "Password must be 8-20 printable ASCII characters.";
}
if (email.length > 254 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
  return "A valid email address is required.";
}
```
- **Login Name**: 3-10 ตัวอักษร ไม่มีช่องว่าง เพื่อความเข้ากันได้กับ MuOnline Season 6 Protocol
- **Password**: 8-20 ตัวอักษร ไม่อนุญาตให้ใช้รหัสผ่านสั้นเกินไปหรือยาวเกินขนาดบัฟเฟอร์ของเกม
- **Email**: RFC 5322 Basic Format สำหรับการยืนยันตัวตนและการกู้คืนบัญชี

---

## 3. วงจรชีวิตของเซสชันและการแยกสิทธิ์ (Session Lifecycle & Isolation)

ระบบมีการแบ่งแยก Principal และ Session Cookie ออกจากกันโดยเด็ดขาด:

| มิติ | Player Session | Admin Session |
|---|---|---|
| **ชื่อคุกกี้** | `omu_player_sid` | `omu_admin_sid` |
| **ความยาวโทเค็น** | 64 ตัวอักษร Hex (256-bit entropy) | 64 ตัวอักษร Hex (256-bit entropy) |
| **ตารางฐานข้อมูล** | `web.player_sessions` | `web.admin_sessions` |
| **อายุเซสชัน** | 7 วัน (Sliding Expiration) | 12 ชั่วโมง (Strict Expiration) |
| **การคุ้มครอง** | `HttpOnly; SameSite=Lax; Secure` | `HttpOnly; SameSite=Lax; Secure` |
| **สิทธิ์การเข้าถึง** | สิทธิ์เฉพาะ `/api/characters/*`, `/api/wallet/*` | สิทธิ์เฉพาะ `/api/admin/*` |

### ผลการทดสอบ Cross-Session Intrusion
- การนำคุกกี้ `omu_player_sid` ไปเรียก API ฝั่งผู้ดูแลระบบ (`/api/admin/*`): ได้รับผลลัพธ์ **401 ADMIN_UNAUTHORIZED** ทันที
- การนำคุกกี้ `omu_admin_sid` ไปเรียก API ของผู้เล่นโดยตรงโดยไม่มี player session: ได้รับผลลัพธ์ **401 UNAUTHORIZED**
- การล็อกเอาต์ (`/api/account/logout` หรือ `/api/admin/logout`): ลบเรคอร์ดออกจากฐานข้อมูลทันที และส่ง Header `Set-Cookie` เคลียร์ค่าคุกกี้ในเบราว์เซอร์

---

## 4. การรักษาความปลอดภัย API ระหว่างเซิร์ฟเวอร์ (Server-to-Server X-Api-Key)

AdminPanel API ของ OpenMU เปิดรับคำขอจาก BFF โดยได้รับการคุ้มครองด้วย API Key Authentication:
- **Header Name**: `X-Api-Key`
- **ข้อกำหนดความยาว**: อย่างน้อย 32 อักขระ
- **การจัดเก็บและการจับคู่**:
  - ฝั่ง OpenMU ไม่เก็บ Key เป็น Plaintext แต่จะ Hash ด้วย SHA-256 ก่อนเก็บใน `ApiKey.KeyHash`
  - เมื่อได้รับ Header `X-Api-Key` มา Handler จะคำนวณ `SHA256(presentedKey)` แล้วค้นหาในฐานข้อมูล
  - มีการตรวจสอบสถานะ `IsEnabled` และช่วงเวลาหมดอายุ `ExpiresAt`
- **ความปลอดภัยเชิงเครือข่าย**:
  - เบราว์เซอร์ไม่มีสิทธิ์เข้าถึงหรือเห็น `X-Api-Key` นี้
  - กุญแจถูกเก็บเป็นความลับในสภาพแวดล้อมฝั่งเซิร์ฟเวอร์ (BFF Environment Variable: `OPENMU_API_KEY`)

---

## 5. การควบคุมการเข้าถึงตามบทบาท (Role-Based Access Control - RBAC)

ในฝั่ง OpenMU AdminPanel มีการกำหนด Policy ชัดเจน:
- `AdminPolicies.AuthenticatedOperator`: ต้องเป็นผู้ใช้ที่ผ่านการยืนยันตัวตนด้วย API Key หรือ Admin Session ที่มีสิทธิ์
- `AdminRoles.GameMaster`: สิทธิ์ในการดูข้อมูลเซิร์ฟเวอร์, สถานะผู้เล่น
- `AdminRoles.Administrator`: สิทธิ์ระดับสูงในการจัดการบัญชี, ปรับแต่งคอนฟิก และออก API Key

BFF มีการตรวจสอบ Role ในเซสชันผู้ดูแลระบบก่อนส่งคำขอไปยังฟังก์ชันที่สำคัญ

---

## 6. สรุปผลการประเมิน
ระบบ Authentication และ Authorization มีความรัดกุม ปลอดภัยตามมาตรฐานสากล ปราศจาก Hardcoded Credentials, Mock Fallbacks หรือ Bypass Vectors

