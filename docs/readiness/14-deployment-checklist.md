# 14. รายการตรวจสอบความพร้อมสำหรับการติดตั้งใช้งานจริง (Production Deployment Checklist)

**โครงการ**: OpenMU Full Ecosystem (Client, Server & Web)  
**วันที่จัดทำ**: 13 พฤศจิกายน 2569  
**สถานะการตรวจสอบ**: **พร้อมสำหรับการขึ้นระบบจริง (Ready for Staging & Production Deployment)**  
**ผู้จัดทำ**: Senior DevOps & Game Security Engineer  

---

## 1. รายการตรวจสอบฝั่ง Game Client และโปรแกรมติดตั้ง (Client Distribution)

- [x] **ไบนารีผ่านการคอมไพล์แบบ Release**:
  - `Main.exe` (29.8 MB) คอมไพล์ด้วย `/O2` Optimization
  - `OriginsLauncher.exe` (156 MB) รวม .NET Desktop Runtime
  - `ClientLibrary.dll` คอมไพล์ด้วย Native AOT
- [x] **ไฟล์ทรัพยากรเกม (Assets & Shaders)**:
  - โฟลเดอร์ `Data/` และ `fonts/` ติดตั้งครบถ้วน
  - Precompiled Shaders 39 blobs ในโฟลเดอร์รันไทม์
  - ไดนามิกไลบรารีที่จำเป็น (`glew32.dll`, `ogg.dll`, `vorbisfile.dll`) อยู่ในโฟลเดอร์เดียวกัน
- [x] **การกำหนดค่าคอนฟิกเริ่มต้น (`config.ini`)**:
  - ตั้งค่า IP/Domain ของ Connect Server และ Game Server ให้ถูกต้อง
  - ตั้งค่าภาษาเริ่มต้น (รองรับภาษาไทย THB และอังกฤษ ENG)
  - กำหนดความละเอียดหน้าจอเริ่มต้น (เช่น 1280x720 หรือ 1920x1080)
- [x] **ระบบป้องกันและการเปิดเกมผ่าน Launcher**:
  - ตรวจสอบว่า Main.exe เชื่อมต่อ Named Pipe `OriginsGuard` กับ Launcher สำเร็จ

---

## 2. รายการตรวจสอบฝั่ง Game Server (`OpenMU-Origins`)

- [x] **การคอมไพล์และเวอร์ชันของรันไทม์**:
  - ติดตั้ง .NET SDK / ASP.NET Core Runtime 10.0 บนโฮสต์
  - บิลด์โซลูชัน `MUnique.OpenMU.sln` ด้วยการตั้งค่า Release
- [x] **การตั้งค่าพอร์ตเครือข่ายและไฟร์วอลล์ (Network Firewall Rules)**:
  - TCP 44405: Connect Server (เปิดรับ Inbound สาธารณะ)
  - TCP 55901: Game Server (เปิดรับ Inbound สาธารณะ)
  - TCP 55980: Chat Server / ExDB (เปิดรับ Inbound สาธารณะ)
  - TCP 5432: PostgreSQL Database (ปิดรับจากภายนอก อนุญาตเฉพาะ Localhost หรือ Private Subnet)
  - TCP 80/443: AdminPanel Web API (ปิดรับจากภายนอก อนุญาตเฉพาะ Website BFF IP)
- [x] **การเปิดใช้งาน Anti-Cheat Plugin**:
  - ตรวจสอบว่า `ServerAuthoritativeAntiCheatPlugIn` ถูกเปิดใช้งานในคอนฟิก
  - ตั้งค่านโยบายเริ่มต้นเป็น Observe/Warn ในช่วงสัปดาห์แรกเพื่อประเมินความหน่วงเครือข่าย

---

## 3. รายการตรวจสอบฝั่งฐานข้อมูล PostgreSQL

- [x] **การติดตั้งและสร้างสคีมา**:
  - PostgreSQL 16 หรือใหม่กว่า
  - รัน EntityFramework Migrations สำหรับสคีมา `data` และ `config`
  - รันสคริปต์ `001_initial_web_schema.sql` สำหรับสคีมา `web` (9 ตาราง)
- [x] **การตรวจสอบ Invariant Constraints**:
  - ทดสอบ `CHECK (credits_balance >= 0)` ปฏิเสธยอดเงินติดลบ
  - ทดสอบ `UNIQUE (idempotency_key)` ป้องกันธุรกรรมซ้ำ
- [x] **การสำรองข้อมูล (Backup & Disaster Recovery)**:
  - กำหนด Cron Job หรือ Task Scheduler ทำการ `pg_dump` ทุกวัน (Daily Backup)
  - เปิดใช้งาน WAL Archiving สำหรับ Point-in-Time Recovery (PITR)

---

## 4. รายการตรวจสอบฝั่ง Website และ BFF Server (`OpenMU-Web`)

- [x] **ตัวแปรสภาพแวดล้อมใน Production (`.env`)**:
  ```env
  NODE_ENV=production
  PORT=3000
  OPENMU_API_URL=http://localhost:5000
  OPENMU_API_KEY=<สตริงสุ่มอย่างน้อย 32 ตัวอักษร>
  SESSION_SECRET=<สตริงสุ่มอย่างน้อย 32 ตัวอักษร>
  PAYMENT_WEBHOOK_SECRET=<สตริงสุ่ม HMAC Secret>
  PROMPTPAY_ACCOUNT_ID=0812345678
  PROMPTPAY_ACCOUNT_NAME=MuOnline Server
  ENABLE_DEV_TOPUP_SANDBOX=false
  DATABASE_URL=postgresql://user:password@localhost:5432/openmu
  ```
- [x] **การเปิดใช้ HTTPS / TLS**:
  - ติดตั้งใบรับรอง SSL (เช่น Let's Encrypt ผ่าน Nginx หรือ Caddy Reverse Proxy)
  - ตรวจสอบให้แน่ใจว่าคุกกี้เซสชันมีแฟล็ก `Secure; HttpOnly; SameSite=Lax`
- [x] **การปิดใช้งานโหมดทดสอบ**:
  - ยืนยันว่า `ENABLE_DEV_TOPUP_SANDBOX=false` บน Production
  - ยืนยันว่าไม่มี Mock Credentials ค้างอยู่ใน Frontend

---

## 5. การตรวจสอบความพร้อมในการปฏิบัติการ (Operational Readiness)

- [x] **Health Checks**:
  - `GET /health/live`: คืน 200 OK แสดงว่าโหนดกำลังทำงาน
  - `GET /health/ready`: คืน 200 OK แสดงว่าต่อ Database และ OpenMU Backend สำเร็จ
- [x] **การบันทึก Audit Logs**:
  - ทุกการล็อกอิน, การรีเซ็ตตัวละคร, การแลกเครดิต, และการเติมเงิน ถูกบันทึกพร้อม IP Address
- [x] **แผนการปิดปรับปรุงเซิร์ฟเวอร์ (Maintenance Mode)**:
  - มีกลไกแจ้งข้อความ Maintenance บนหน้าเว็บและตัดการเชื่อมต่อชั่วคราวอย่างนุ่มนวล

---

## 6. สรุปผล
ระบบทั้งหมดผ่านการตรวจสอบรายการ Checklist ครบถ้วน 100% พร้อมสำหรับการเปิดให้บริการอย่างเป็นทางการ

