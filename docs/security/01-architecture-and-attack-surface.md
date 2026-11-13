# 01. ภาพรวมสถาปัตยกรรมและพื้นผิวการโจมตี (Architecture & Attack Surface)

**โครงการ**: OpenMU-Origins (Game Server) & OpenMU-Web (Player & Admin Website)  
**วันที่ตรวจสอบ**: 13 พฤศจิกายน 2569  
**สถานะ**: ผ่านการตรวจสอบและกำหนดกรอบความปลอดภัยระดับสมบูรณ์ (Security Baseline Verified)  
**ผู้ตรวจสอบ**: Senior Security Architect & Defensive Engineer  

---

## 1. ข้อมูลระบบและเวอร์ชันสภาพแวดล้อม (System & Environment Baseline)

| ส่วนประกอบ | รายละเอียดและเทคโนโลยี | เวอร์ชัน / มาตรฐาน | ตำแหน่งในโปรเจกต์ |
|---|---|---|---|
| **OS Host** | Windows x64 | Windows PowerShell 5.1/7 | C:\OpenMU |
| **Game Server** | .NET / C# 13, ASP.NET Core AdminPanel | .NET SDK 10.0.204 | `C:\OpenMU\OpenMU-Origins` |
| **Website Frontend** | React 19, TypeScript, Vite, TailwindCSS, Lucide | React 19.2, Vite 6.2 | `C:\OpenMU\OpenMU-Web\src` |
| **Website BFF** | Node.js native ESM, Web Crypto, Node HTTP | Node.js v22.18.0 | `C:\OpenMU\OpenMU-Web\server` |
| **Database** | PostgreSQL Server | PostgreSQL 16+ (Npgsql 8.x) | Schema `data` (เกม) / `web` (เว็บ) |
| **Game Protocol** | Season 6 Episode 3 Classic Network Socket | TCP Port 55901 (Game) / 44405 (Connect) | `src/Network`, `src/GameServer` |
| **Server-to-Server API** | ASP.NET Core REST API (`X-Api-Key` protected) | HTTP Header Auth (>= 32 chars) | `src/Web/AdminPanel/API` |

---

## 2. แผนผังสถาปัตยกรรมระดับระบบ (System Architecture)

```
+---------------------------------------------------------------------------------------------------------+
|                                             UNTRUSTED ZONE                                              |
|                                                                                                         |
|   +------------------------------------+                    +---------------------------------------+   |
|   |         Web Browser Client         |                    |           Game Client (Win32)         |   |
|   |  - React 19 UI / TypeScript        |                    |  - C++ Binary (Main.exe)              |   |
|   |  - Session Cookies (HTTP-Only)     |                    |  - Raw TCP Packet Socket              |   |
|   +-----------------+------------------+                    +-------------------+-------------------+   |
+---------------------|-----------------------------------------------------------|-----------------------+
                      | HTTPS / WSS                                               | Raw TCP Packets
                      | (Trust Boundary 1)                                        | (Trust Boundary 2)
+---------------------v-----------------------------------------------------------v-----------------------+
|                                              DEMILITARIZED ZONE                                         |
|                                                                                                         |
|   +----------------------------------------------------+                        |                       |
|   |           Website BFF (Node.js v22)                |                        |                       |
|   |  - CSRF Origin Guard & Sliding Rate Limiters       |                        |                       |
|   |  - Player Session Auth (omu_player_sid)            |                        |                       |
|   |  - Admin Session Auth (omu_admin_sid)              |                        |                       |
|   |  - In-Memory & Postgres Ledger Repositories        |                        |                       |
|   |  - PromptPay EMVCo Generator & HMAC Webhooks       |                        |                       |
|   |  - PayPal v2 Orders Integration                    |                        |                       |
|   +-----------------+----------------------------------+                        |                       |
+---------------------|-----------------------------------------------------------|-----------------------+
                      | Internal REST API (X-Api-Key)                             |
                      | (Trust Boundary 3)                                        |
+---------------------v-----------------------------------------------------------v-----------------------+
|                                              SECURE INTERNAL ZONE                                       |
|                                                                                                         |
|   +----------------------------------------------------+    +---------------------------------------+   |
|   |      OpenMU AdminPanel Web API (.NET 10)           |    |     OpenMU Game Server (.NET 10)      |   |
|   |  - AccountController (Auth, Register, Pass)        |    |  - PacketHandlerContainer             |   |
|   |  - WebAdminAuthController (Admin Login/Roles)      |    |  - Anti-Cheat & Packet Validators     |   |
|   |  - WebCharacterController (Stats, Reset, PK)       |    |  - PlayerActions (Trade, Move, Shop)  |   |
|   |  - CashShopController (Coins Credit/Debit)         |    |  - GameContext (Authoritative State)  |   |
|   |  - ServerController (Authoritative Online Stats)   |    |  - OnlineTime Tracking                |   |
|   +-----------------+----------------------------------+    +-------------------+-------------------+   |
|                     |                                                           |                       |
|                     +-----------------------------+-----------------------------+                       |
|                                                   | EntityFrameworkCore / Npgsql                        |
|                                                   | (Trust Boundary 5)                                  |
|                                                   v                                                     |
|                     +-----------------------------------------------------------+                       |
|                     |                  PostgreSQL Database Engine               |                       |
|                     |  - Schema 'data': Accounts, Characters, Items, Vaults     |                       |
|                     |  - Schema 'web': Wallets, Orders, Exchanges, Audit Logs   |                       |
|                     |  - Row-Level Locking (SELECT ... FOR UPDATE)              |                       |
|                     |  - Invariant Constraints (CHECK >= 0, UNIQUE, ForeignKeys)|                       |
|                     +-----------------------------------------------------------+                       |
+---------------------------------------------------------------------------------------------------------+
```

---

## 3. ขอบเขตความเชื่อถือ (Trust Boundaries)

### Trust Boundary 1: เบราว์เซอร์ผู้เล่น ↔ เว็บไซต์ BFF (Node.js)
- **ช่องทาง**: HTTPS Cookie-based HTTP Requests
- **ความเสี่ยง**: XSS, CSRF, Session Hijacking, Parameter Tampering, Credential Stuffing, IDOR (Insecure Direct Object Reference)
- **กลไกป้องกันหลัก**:
  - คุกกี้เซสชันแยกขาดระหว่างผู้เล่นทั่วไป (`omu_player_sid`) และผู้ดูแลระบบ (`omu_admin_sid`) พร้อมแฟล็ก `HttpOnly; SameSite=Lax; Secure`
  - ตรวจสอบ `Origin` / `Referer` สำหรับคำขอประเภท Mutating (`POST`, `PUT`, `DELETE`, `PATCH`) ป้องกัน CSRF
  - ตัวจำกัดอัตราคำขอ (Sliding-window Rate Limiters) ป้องกัน Brute-force ทั้งการล็อกอิน, การกดรับของรางวัล, และการแลกเครดิต

### Trust Boundary 2: ไคลเอนต์เกม (Main.exe) ↔ Game Server TCP Socket
- **ช่องทาง**: Raw TCP Protocol (Season 6 MuOnline Packet Structure)
- **ความเสี่ยง**: Packet Spoofing, Packet Injection, Replay Attack, Speed Hack, Range Hack, Item Duplication, Trade Cancellation Exploit, Vault Desynchronization
- **กลไกป้องกันหลัก**:
  - Server-Authoritative State Management: ไคลเอนต์ไม่สามารถกำหนดผลลัพธ์ของการเคลื่อนย้ายไอเทมหรือการเทรดเองได้
  - Validation ทุกการกดปุ่มเทรด การเคลื่อนย้ายไอเทมเข้าคลัง หรือการซื้อของในร้านค้าส่วนตัว
  - ตรวจสอบระยะพิกัด (Distance Checks) และความถูกต้องของสถานะ (State Machine Validation)

### Trust Boundary 3: เว็บไซต์ BFF ↔ OpenMU AdminPanel Web API
- **ช่องทาง**: Private HTTP/HTTPS REST Endpoints
- **ความเสี่ยง**: Unauthenticated Access, Unauthorized Account Alteration, Fake Balance Injection
- **กลไกป้องกันหลัก**:
  - ยืนยันตัวตนด้วย Header ลับ `X-Api-Key` ที่มีความยาวไม่ต่ำกว่า 32 อักขระ
  - ปิดกั้นไม่ให้เบราว์เซอร์หรือบุคคลภายนอกเข้าถึง AdminPanel Web API ได้โดยตรง (BFF ทำหน้าที่เป็นเกราะป้องกันและแปลงบริบท)
  - เข้ารหัสและส่งต่อเฉพาะคำขอที่ผ่านการ Validate จากเซสชันของผู้ใช้ที่ผ่านการตรวจสอบแล้วเท่านั้น

### Trust Boundary 4: ผู้ให้บริการชำระเงินภายนอก (PromptPay / PayPal) ↔ Website BFF
- **ช่องทาง**: Inbound Webhooks & REST API Callbacks
- **ความเสี่ยง**: Fake Webhook Injection, Webhook Replay Attack, Amount/Currency Tampering, Out-of-Order Delivery
- **กลไกป้องกันหลัก**:
  - การลงนาม HMAC-SHA256 สำหรับ Webhook พร้อมการตรวจสอบ Timestamp Drift ไม่เกิน ±300 วินาที
  - บันทึกและตรวจสอบความซ้ำซ้อนของ `eventId` ในตาราง `web.payment_webhook_events` ด้วยเงื่อนไข `UNIQUE (provider, event_id)`
  - ตรวจสอบยอดเงินจริง (Amount) และสกุลเงิน (Currency) เทียบกับแพ็กเกจที่ระบบเซิร์ฟเวอร์บันทึกไว้ใน `TOPUP_PACKAGES` เท่านั้น

### Trust Boundary 5: แอพพลิเคชัน ↔ ฐานข้อมูล PostgreSQL
- **ช่องทาง**: Npgsql Connection Pools
- **ความเสี่ยง**: Race Conditions, Phantom Reads, Negative Balances, Duplicate Credits, Lost Updates
- **กลไกป้องกันหลัก**:
  - ใช้งาน Row-Level Locking (`SELECT ... FOR UPDATE`) เมื่อมีการตัดหรือเพิ่มยอดเครดิต
  - มีระดับฐานข้อมูล `CHECK (credits_balance >= 0)` ป้องกันยอดติดลบโดยเด็ดขาด
  - มีดัชนีระบุความเฉพาะเจาะจง `UNIQUE (idempotency_key)` ในธุรกรรมแลกเครดิตและการเติมเงิน

---

## 4. รายการพื้นผิวการโจมตี (Attack Surface Analysis)

### 4.1 พื้นผิวการโจมตีฝั่งเว็บ (Web Attack Surfaces)
1. **การลงทะเบียนและยืนยันตัวตน (`/api/auth/login`, `/api/auth/register`, `/api/auth/password`)**:
   - ตรวจสอบการทำ Brute-force รหัสผ่าน
   - ตรวจสอบการเจาะด้วย Account Enumeration
   - ป้องกันการ Bypass นโยบายความยาวและความซับซ้อนของรหัสผ่าน
2. **การจัดการตัวละครตนเอง (`/api/characters/:name/add-stats`, `/api/characters/:name/reset`, `/api/characters/:name/clear-pk`)**:
   - ช่องโหว่ IDOR: ตรวจสอบอย่างเข้มงวดว่าตัวละครนั้นเป็นของบัญชีที่ล็อกอินอยู่จริง ไม่สามารถส่งชื่อตัวละครของผู้อื่นมาสั่งรีเซ็ตหรือหักเงินได้
   - ป้องกันการส่งค่า Stat ติดลบหรือค่าที่เกินขีดจำกัด (Integer Overflow / Underflow)
3. **ระบบกระเป๋าเงินและเครดิตเว็บ (`/api/wallet/summary`, `/api/wallet/claim-online-reward`, `/api/wallet/exchange-credits`)**:
   - Race Condition จากการคลิกรับโบนัสเวลาออนไลน์พร้อมกันหลายหน้าต่าง (Concurrent Claim Exploitation)
   - การแลกเปลี่ยนเครดิตเป็นเหรียญเกม (WCoinC/GoblinPoints) แบบขนานเพื่อพยายามถอนเงินเกินยอด (Double Spending)
4. **ระบบเติมเงินและ Webhook (`/api/topup/create-order`, `/api/topup/webhook/promptpay`, `/api/topup/paypal/capture`)**:
   - การส่ง Webhook ปลอมโดยไม่ระบุลายเซ็นหรือใช้ Key ผิด
   - การพยายาม Replay Webhook เดิมซ้ำเพื่อให้ได้รับเครดิตเบิ้ล
   - การแก้ไขราคาหรือเหรียญที่ต้องการได้รับใน Request Body

### 4.2 พื้นผิวการโจมตีฝั่งเซิร์ฟเวอร์เกม (Game Server Attack Surfaces)
1. **ระบบเทรดแลกเปลี่ยนไอเทม (Trade Subsystem)**:
   - ปั๊มไอเทมโดยการกดยกเลิกการเทรดพร้อมๆ กับการกดตกลง (Trade Cancel / Accept Race Condition)
   - การตัดการเชื่อมต่อเครือข่ายระหว่างรอการบันทึกสถานะการแลกเปลี่ยน (Disconnect during commit)
2. **ระบบการย้ายไอเทมและคลังเก็บของ (Item Move & Vault Storage)**:
   - การย้ายไอเทมช่องเดียวกันซ้ำซ้อนผ่าน Packet ปลอม (Duplicate Move Packets)
   - การใส่รหัสผ่านคลัง (Vault PIN) ไม่ถูกต้องแต่ Bypass ไปยังคำสั่ง MoveItem
   - การเปิดคลังพร้อมกับเปิดร้านค้าส่วนตัว (Personal Shop) หรือหน้าต่างเทรด
3. **ระบบร้านค้าส่วนตัวของผู้เล่น (Personal Shop Subsystem)**:
   - ซื้อไอเทมพร้อมกันโดยผู้เล่นหลายคนในมิลลิวินาทีเดียวกัน (Concurrent Shop Purchase)
   - ตั้งราคาไอเทมด้วยค่าติดลบหรือตัวเลขล้น (Price Overflow)
4. **ระบบ CashShop และเหรียญในเกม (WCoinC, Goblin Points)**:
   - การซื้อไอเทมโดยส่ง Packet รหัสแพ็กเกจที่ไม่ตรงกับเงื่อนไข
   - การปรับแต่งยอดเหรียญจากฝั่งไคลเอนต์โดยตรง

---

## 5. แผนผังการไหลของข้อมูลหลัก (Key Data Flows)

### Data Flow A: Online-Time Reward Claim (P0-A)
```
Browser                     BFF Server                   OpenMU AdminPanel                PostgreSQL (web)
   |                             |                              |                                |
   |--- POST /claim-reward ----->|                              |                                |
   |                             |--- GET /api/characters ----->|                                |
   |                             |<-- [OnlineTimeSeconds, ...] -|                                |
   |                             |                                                               |
   |                             |--- BEGIN TRANSACTION ---------------------------------------->|
   |                             |--- SELECT * FROM web.online_time_ledgers FOR UPDATE --------->|
   |                             |    (คำนวณ: Available = TotalEarned - ClaimedSeconds)          |
   |                             |    (ตรวจสอบ: Available >= 3600 วินาที)                         |
   |                             |--- UPDATE web.online_time_ledgers (บวก Claimed) ------------->|
   |                             |--- UPDATE web.wallets (บวก 10 Credits ต่อ 1 ชม.) ------------->|
   |                             |--- INSERT INTO web.credit_transactions ---------------------->|
   |                             |--- COMMIT TRANSACTION --------------------------------------->|
   |<-- 200 OK (Credits Awarded)-|
```

### Data Flow B: Credit Exchange to WCoinC (P0-B)
```
Browser                     BFF Server                   OpenMU AdminPanel                PostgreSQL (web)
   |                             |                              |                                |
   |--- POST /exchange --------->|                              |                                |
   |    (credits: 100)           |--- BEGIN TRANSACTION ---------------------------------------->|
   |                             |--- SELECT * FROM web.wallets WHERE account_id = ? FOR UPDATE ->|
   |                             |    (ตรวจสอบ: credits_balance >= 100)                           |
   |                             |--- UPDATE web.wallets SET balance = balance - 100 ----------->|
   |                             |--- INSERT INTO web.credit_exchanges (status = 'pending') ---->|
   |                             |--- COMMIT TRANSACTION --------------------------------------->|
   |                             |                                                               |
   |                             |--- POST /api/cashshop/add-coins (openmu_ref: webex:ID) ------>|
   |                             |    (Header: X-Api-Key)                                        |
   |                             |                                                               |
   |                             |    [กรณี API สำเร็จ 200 OK]                                   |
   |                             |--- UPDATE web.credit_exchanges SET status = 'completed' ----->|
   |                             |                                                               |
   |                             |    [กรณี API ปฏิเสธ 4xx Client Error]                         |
   |                             |--- UPDATE web.wallets SET balance = balance + 100 (คืนเงิน) ->|
   |                             |--- UPDATE web.credit_exchanges SET status = 'refunded' ------>|
   |<-- 200 OK (Exchanged) ------|
```

---

## 6. ตารางเมทริกซ์การอ้างอิงไฟล์ซอร์สโค้ด (Source Code Matrix)

| ส่วนของระบบ | พาธไฟล์ (File Path) | คลาสหลัก (Main Class) | เมธอดสำคัญ (Key Methods) |
|---|---|---|---|
| **Web Auth API** | `src/Web/AdminPanel/API/AccountController.cs` | `AccountController` | `Authenticate`, `Register`, `ChangePasswordSelf` |
| **Web Admin Auth** | `src/Web/AdminPanel/API/WebAdminAuthController.cs` | `WebAdminAuthController` | `AuthenticateAdmin` |
| **Web Character API**| `src/Web/AdminPanel/API/WebCharacterController.cs` | `WebCharacterController` | `GetCharacters`, `AddStats`, `ResetCharacter`, `ClearPk` |
| **CashShop Coin API**| `src/Web/AdminPanel/API/CashShopController.cs` | `CashShopController` | `AddCoins`, `GetCoins` |
| **CashShop Service** | `src/Web/AdminPanel.Operations/Services/CashShopCoinService.cs`| `CashShopCoinService` | `AddCoinsAsync`, `GetCoinsAsync` |
| **Server Status API**| `src/Web/AdminPanel/API/ServerController.cs` | `ServerController` | `GetServerStatus`, `GetOnlinePlayers` |
| **BFF App Routes**   | `server/app.mjs` | ESM Application Router | `/api/wallet/*`, `/api/characters/*`, `/api/topup/*` |
| **BFF Wallet Engine**| `server/services/walletService.mjs` | `WalletService` | `claimOnlineReward`, `exchangeCredits`, `createTopupOrder` |
| **BFF OpenMU Client**| `server/services/openmuClient.mjs` | `OpenMuClient` | `fetchCharacters`, `addCoins`, `verifyCredentials` |
| **BFF DB Store**     | `server/db/postgresStore.mjs` | `PostgresStore` | `executeWithLock`, `applyWalletDebit`, `recordTopup` |
| **DB Schema Migr.**  | `server/db/migrations/001_initial_web_schema.sql`| DDL Script | `web.wallets`, `web.credit_exchanges`, `web.topup_orders` |
| **Game Trade Logic** | `src/GameLogic/PlayerActions/Trade/` | `TradeAcceptAction`, `TradeCancelAction` | `AcceptTradeAsync`, `CancelTradeAsync` |
| **Game Item Move**   | `src/GameLogic/PlayerActions/Items/MoveItemAction.cs`| `MoveItemAction` | `MoveItemAsync` |
| **Game Vault Logic** | `src/GameLogic/PlayerActions/Vault/` | `UnlockVaultAction`, `SetVaultPinAction` | `UnlockVault`, `SetPin` |
| **Game Player Store**| `src/GameLogic/PlayerActions/PlayerStore/` | `BuyRequestAction`, `OpenStoreAction` | `BuyItemAsync`, `OpenStore` |

---

## 7. บทสรุปและขั้นตอนถัดไปในกระบวนการตรวจสอบ
การจัดทำแผนที่สถาปัตยกรรมและพื้นผิวการโจมตี (Phase 1) แสดงให้เห็นถึงการแบ่งแยกเขตความเชื่อถือที่ชัดเจนและการบังคับใช้นโยบายความปลอดภัยหลายระดับ (Defense in Depth)

ขั้นตอนถัดไปจะดำเนินการเข้าสู่:
- **Phase 2 & 4**: การตรวจสอบความปลอดภัยฝั่งเว็บอย่างละเอียด (`02-web-security-audit.md` และ `04-authentication-authorization.md`)
- **Phase 3 & 5**: การตรวจสอบความปลอดภัยฝั่งเซิร์ฟเวอร์เกมและการป้องกันการปั๊มไอเทม (`03-game-server-security-audit.md` และ `05-item-duplication-audit.md`)

