# 02. รายงานการตรวจสอบความปลอดภัยฝั่งเว็บ (Web Security Audit)

**โครงการ**: OpenMU-Origins & OpenMU-Web  
**วันที่ตรวจสอบ**: 13 พฤศจิกายน 2569  
**สถานะการประเมิน**: ผ่านเกณฑ์ความปลอดภัยมาตรฐานสูง (Passed - High Security Posture)  
**ขอบเขต**: React 19 Frontend, Node.js BFF (Backend-for-Frontend), REST API Endpoints  

---

## 1. บทสรุปผู้บริหาร (Executive Summary)

จากการตรวจสอบซอร์สโค้ดเชิงลึกของฝั่งเว็บ (`OpenMU-Web`) และ API ที่เชื่อมโยงกับเซิร์ฟเวอร์เกม (`OpenMU-Origins`) ไม่พบช่องโหว่ระดับวิกฤต (Critical Vulnerabilities) หรือช่องโหว่ระดับสูง (High Severity Flaws) ระบบได้รับการออกแบบสถาปัตยกรรมความปลอดภัยตามหลักการ **Defense-in-Depth** และ **Zero Trust** ต่ออินพุตจากเบราว์เซอร์

จุดเด่นสำคัญของการป้องกัน:
1. **การแบ่งแยก Session Cookie โดยสมบูรณ์**: แยกคุกกี้ผู้เล่น (`omu_player_sid`) และผู้ดูแลระบบ (`omu_admin_sid`) ไม่สามารถใช้แทนกันได้
2. **การป้องกัน IDOR 100%**: การจัดการตัวละครทุกคำขอ (Add Stats, Reset, PK Clear) ผูกติดกับ `session.loginName` ที่ได้จากคุกกี้เซสชันที่ผ่านการยืนยันแล้ว และมี Server-Authoritative Check ซ้ำบน OpenMU Backend
3. **การป้องกัน CSRF และ Clickjacking**: มี Origin Guard ตรวจสอบคำขอ Mutating ทุกประเภท และกำหนด `X-Frame-Options: DENY` ควบคู่กับ CSP `frame-ancestors 'none'`
4. **ตัวจำกัดอัตราคำขอ (Sliding-Window Rate Limiting)**: ป้องกัน Brute-force และ DoS ทุกจุดเสี่ยง (Login, Register, Redeem, Exchange, Top-up)

---

## 2. ผลการตรวจสอบความปลอดภัยแยกตามมิติ (Security Domain Analysis)

### 2.1 การจัดการเซสชันและคุกกี้ (Session Management)
- **การสร้างเซสชัน**: ใช้ `crypto.randomBytes(32).toString("hex")` ในการสร้าง Session ID ขนาด 256 บิต ป้องกันการคาดเดา (Session Fixation / Brute-force ID)
- **การจัดเก็บ**:
  - รองรับทั้ง `InMemoryWebStore` (โหมดพัฒนา) และ `PostgresStore` (โหมดการใช้งานจริง) ตาราง `web.player_sessions` และ `web.admin_sessions`
  - มีการกำหนดเวลาหมดอายุชัดเจน (`expires_at`) และอัปเดตแบบ Sliding Session อย่างปลอดภัย
- **แอตทริบิวต์ของคุกกี้**:
  - `HttpOnly`: ไม่อนุญาตให้ JavaScript ในเบราว์เซอร์เข้าถึงคุกกี้เซสชัน ป้องกันการขโมยเซสชันผ่าน XSS
  - `SameSite=Lax`: ป้องกันการส่งคุกกี้ข้ามโดเมนในคำขอ Third-party ป้องกันการโจมตีประเภท CSRF
  - `Secure`: เปิดใช้งานอัตโนมัติในสภาพแวดล้อม Production

### 2.2 การป้องกัน CSRF (Cross-Site Request Forgery)
- ฟังก์ชัน `isCsrfOriginValid(request, config)` ใน `server/services/authService.mjs`:
  ```javascript
  // ตรวจสอบ Origin / Referer เทียบกับ ALLOWED_ORIGINS ในคำขอ state-changing (POST, PUT, DELETE, PATCH)
  if (pathname.startsWith("/api/") && pathname !== "/api/payments/webhook" && !isCsrfOriginValid(request, config)) {
    return json(response, 403, { success: false, code: "CSRF_ORIGIN_REJECTED", message: "Cross-origin request rejected." });
  }
  ```
- ยกเว้นเฉพาะ `/api/payments/webhook` เนื่องจากเป็น Server-to-Server Webhook จากผู้ให้บริการชำระเงินภายนอก ซึ่งได้รับการป้องกันด้วยลายเซ็น HMAC-SHA256 แทน

### 2.3 การป้องกัน IDOR (Insecure Direct Object Reference)
- **จุดตรวจสอบ**: `/api/characters/reset`, `/api/characters/pkclear`, `/api/characters/addstats`, `/api/characters/resetStats`
- **การวิเคราะห์**:
  1. เบราว์เซอร์ส่งเฉพาะ `characterName` เข้ามา
  2. BFF ดึง `loginName` จาก `session.loginName` (มาจาก Session Cookie ที่ถูกต้อง) เท่านั้น
  3. BFF เรียกไปยัง OpenMU AdminPanel API โดยส่งทั้ง `loginName` และ `characterName`
  4. เซิร์ฟเวอร์ `WebCharacterSelfService.cs` ดำเนินการตรวจสอบ:
     ```csharp
     if (probeAccount.Characters.All(c => !c.Name.Equals(normalizedChar, StringComparison.Ordinal)))
     {
         return new WebCharacterOperationResult(WebCharacterOperationStatus.CharacterNotFound, "Character not found on this account.");
     }
     ```
  5. ผลลัพธ์: ผู้ใช้ A **ไม่สามารถ** แกล้งส่งชื่อตัวละครของผู้ใช้ B เพื่อสั่งรีเซ็ต stat หรือหักเงินของผู้อื่นได้ การทดสอบความพยายามเจาะ IDOR ถูกสกัดกั้นทั้งในชั้น BFF และ Game Logic

### 2.4 การป้องกันการโจมตีแบบ Brute-Force (Rate Limiting)
ระบบใช้ตัวจำกัดอัตราคำขอแบบ Sliding-Window ในหน่วยความจำหรือฐานข้อมูล โดยมีขีดจำกัดดังนี้:
- **Registration**: 5 ครั้งต่อนาที ต่อ IP
- **Player Login**: 12 ครั้งต่อนาที ต่อ IP และ 6 ครั้งต่อนาที ต่อชื่อบัญชี
- **Admin Login**: 8 ครั้งต่อนาที ต่อ `IP:LoginName`
- **Password Change**: 5 ครั้งต่อนาที ต่อบัญชี
- **Online Reward Redeem**: 10 ครั้งต่อนาที ต่อบัญชี
- **Credit Exchange**: 10 ครั้งต่อนาที ต่อบัญชี
- **Top-up Order Creation**: 10 ครั้งต่อนาที ต่อบัญชี

เมื่อเกินเกณฑ์ที่กำหนด ระบบตอบกลับด้วย HTTP 429 พร้อมรหัสข้อผิดพลาด `RATE_LIMITED`

### 2.5 Security Headers
ทุกการตอบกลับจาก BFF มีการแทรก Security Headers ตามมาตรฐาน OWASP ในฟังก์ชัน `securityHeaders(response)`:
```http
Content-Security-Policy: default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'; connect-src 'self'; font-src 'self' data:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: camera=(), microphone=(), geolocation=()
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Cache-Control: no-store
```

### 2.6 การตรวจสอบขนาดและรูปแบบข้อมูลนำเข้า (Input Validation & Body Limiting)
- จำกัดขนาด Payload สูงสุดที่ 16 KB สำหรับ API ทั่วไป และ 64 KB สำหรับ Webhook ด้วยฟังก์ชัน `readRawBody(request, maximum)` ป้องกันการโจมตี Memory Exhaustion DoS
- ป้องกัน JSON Injection ด้วยการครอบ `JSON.parse` ภายใต้ try-catch และคืนสถานะ 400 Bad Request เมื่อข้อมูลผิดรูปแบบ
- กรองตัวเลขอินพุตใน `addstats` ด้วย `Math.max(0, Number.parseInt(...))` ป้องกันค่าติดลบหรือตัวเลขทศนิยมที่ผิดปกติ

---

## 3. สรุปตารางผลการประเมินความปลอดภัยฝั่งเว็บ

| หัวข้อการตรวจสอบ | สถานะ | รายละเอียดการประเมิน |
|---|---|---|
| **A1: Broken Access Control** | **ผ่าน (Safe)** | ตรวจสอบ Session และ IDOR ทั้งสองชั้น ไม่พบช่องโหว่ |
| **A2: Cryptographic Failures** | **ผ่าน (Safe)** | ใช้ BCrypt สำหรับรหัสผ่าน, SHA-256 สำหรับ API Key, HMAC-SHA256 สำหรับ Webhook |
| **A3: Injection (SQL / Command)**| **ผ่าน (Safe)** | ใช้งาน Parameterized Query (`$1, $2, ...`) และ EF Core LINQ 100% |
| **A4: Insecure Design** | **ผ่าน (Safe)** | สถาปัตยกรรมแยก BFF และ Game API ชัดเจน พร้อม Offline Reservation Lock |
| **A5: Security Misconfiguration**| **ผ่าน (Safe)** | ลบ Mock credentials และ Test Accounts ออกหมดแล้ว มี Strict Environment Checks |
| **A6: Vulnerable Dependencies** | **ผ่าน (Safe)** | ไร้ช่องโหว่ระดับ High/Critical ในชุดโมดูลหลัก |
| **A7: Identification & Auth** | **ผ่าน (Safe)** | Session Isolation สมบูรณ์, ป้องกัน Brute-force ด้วย Rate Limiting |
| **A8: Software & Data Integrity**| **ผ่าน (Safe)** | ลายเซ็น HMAC ตรวจสอบ Webhook ไม่สามารถแก้ไข Payload กลางทางได้ |
| **A9: Logging & Monitoring** | **ผ่าน (Safe)** | บันทึก Audit Log ทุกเหตุการณ์สำคัญ (Login, Reset, Exchange, Topup) |
| **A10: SSRF** | **ผ่าน (Safe)** | BFF สื่อสารเฉพาะ URL ภายในที่กำหนดไว้ใน Config ไม่รับ URL จากผู้ใช้ |

---

## 4. แนะนำข้อปรับปรุงเพิ่มเติมเพื่อความแข็งแกร่ง (Hardening Recommendations)
1. **การบันทึก IP ล้มเหลวต่อเนื่อง (Fail2ban Integration)**: สำหรับสภาพแวดล้อมที่เปิดสาธารณะ แนะนำให้บันทึกเหตุการณ์ 429 ลงใน syslog เพื่อให้ reverse proxy (เช่น Cloudflare หรือ Nginx Fail2ban) บล็อก IP ตั้งแต่ระดับ Network Firewall
2. **การบังคับ 2FA สำหรับ Admin ทุกบัญชี**: สนับสนุนให้ผู้ดูแลระบบเปิดใช้งาน TOTP Authenticator เสมอ ซึ่งระบบ `WebAdminAuthController` รองรับเรียบร้อยแล้ว

