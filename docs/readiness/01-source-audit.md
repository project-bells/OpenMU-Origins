# 01. รายงานการตรวจสอบซอร์สโค้ดและแผนผังความเชื่อมโยง (Source Code Audit & Dependency Map)

**โครงการ**: OpenMU Full Ecosystem (Client, Server & Web)  
**วันที่ตรวจสอบ**: 13 พฤศจิกายน 2569  
**สถานะการตรวจสอบ**: สมบูรณ์ (Baseline Verified across 3 Repositories)  
**ผู้ตรวจสอบ**: Senior Game Engine & Security Architect  

---

## 1. บทสรุปภาพรวมของ 3 Repositories (Repository Baseline)

| คลังโค้ด (Repository) | เทคโนโลยีหลัก | สาขา (Branch) | บทบาทและหน้าที่หลัก | ตำแหน่งในเครื่อง |
|---|---|---|---|---|
| **OpenMu-ClientMainS6E3** | C++20, CMake, SDL3 GPU, RmlUi, .NET 10 Native AOT, C# WinForms | `main` | ไคลเอนต์เกม Season 6 Episode 3, เรนเดอร์กราฟิก, UI, และ Launcher | `C:\OpenMU\OpenMu-ClientMainS6E3` |
| **OpenMU-Origins** | .NET 10, C# 13, ASP.NET Core, EF Core, PostgreSQL | `integration/upstream-2026-11` | เซิร์ฟเวอร์เกมหลัก, GameLogic, LoginServer, ConnectServer, AdminPanel API | `C:\OpenMU\OpenMU-Origins` |
| **OpenMU-Web** | React 19, Vite, Node.js v22 BFF, PostgreSQL | `main` | เว็บไซต์ผู้เล่น, ระบบจัดการตัวละคร, กระเป๋าเงินเครดิต, การเติมเงิน | `C:\OpenMU\OpenMU-Web` |

---

## 2. แผนผังความเชื่อมโยงระดับระบบ (Cross-Repository Dependency Map)

```
+--------------------------------------------------------------------------------------------------+
|                                      GAME CLIENT SUBSYSTEM                                       |
|                                                                                                  |
|   +------------------------------------+               +-------------------------------------+   |
|   |    OriginsLauncher.exe (.NET 10)   |               |         Main.exe (C++20 Native)     |   |
|   |  - WinForms / ClientUpdater        |               |  - SDL3 GPU Renderer / Modern RmlUi |   |
|   |  - GuardPipeServer (Named Pipe)    |<==============|  - ClientAntiCheat.cpp (HMAC Heart) |   |
|   |  - ServerStatusChecker (TCP Ping)  |  Named Pipe   |  - GameLogic / Audio / World Scenes |   |
|   +-----------------+------------------+  OriginsGuard |  - Dotnet/Connection.cpp Bridge    |   |
|                     |                                  +------------------+------------------+   |
|                     |                                                     |                      |
|                     |                                  +------------------v------------------+   |
|                     |                                  |   ClientLibrary.dll (Native AOT)    |   |
|                     |                                  |  - MUnique.OpenMU.Network.Packets   |   |
|                     |                                  |  - Season 6 Packet Serializers      |   |
|                     |                                  +------------------+------------------+   |
+---------------------|-----------------------------------------------------|----------------------+
                      |                                                     | Raw TCP Packets
                      | Web API / Status Ping                               | (Season 6 Protocol)
+---------------------v-----------------------------------------------------v----------------------+
|                                      GAME SERVER SUBSYSTEM                                       |
|                                                                                                  |
|   +----------------------------------------------------+    +--------------------------------+   |
|   |           ConnectServer & ChatServer               |    |     OpenMU GameServer (.NET 10)|   |
|   |  - TCP Port 44405 (ConnectServer ServerList)       |    |  - TCP Port 55901 (Game)       |   |
|   |  - TCP Port 55980 (ExDB / Chat Room Server)        |    |  - PacketHandlerContainer      |   |
|   +----------------------------------------------------+    |  - PlayerState State Machine   |   |
|                                                             |  - ServerAntiCheatPlugIn       |   |
|   +----------------------------------------------------+    |  - PlayerActions (Trade/Vault) |   |
|   |           AdminPanel Web API (.NET 10)             |    +----------------+---------------+   |
|   |  - AccountController (Register, Auth, Pass)        |                     |                   |
|   |  - WebCharacterController (Stats, Reset, PK)       |                     | EF Core / Npgsql  |
|   |  - CashShopController (WCoinC Grants/Exchanges)    |                     |                   |
|   |  - ServerController (Authoritative Online Stats)   |                     v                   |
|   +-------------------------+--------------------------+    +--------------------------------+   |
+-----------------------------|-------------------------------|   PostgreSQL Engine (data)     |   |
                              | Internal REST API             |  - Accounts, Characters, Items |   |
                              | (X-Api-Key >= 32 chars)       +--------------------------------+   |
+-----------------------------v--------------------------------------------------------------------+
|                                      WEBSITE SUBSYSTEM (BFF)                                     |
|                                                                                                  |
|   +----------------------------------------------------+    +--------------------------------+   |
|   |             Website BFF (Node.js v22)              |    |   PostgreSQL Engine (web)      |   |
|   |  - CSRF Origin Guard & Sliding Rate Limiters       |--->|  - Wallets, Exchanges, Orders  |   |
|   |  - Player Session Auth (omu_player_sid)            |    |  - Row Locking (FOR UPDATE)    |   |
|   |  - P0-A Online Rewards, P0-B Exchange, P0-C Top-up |    |  - Check Constraints (>= 0)    |   |
|   +----------------------------------------------------+    +--------------------------------+   |
+--------------------------------------------------------------------------------------------------+
```

---

## 3. การตรวจสอบโครงสร้างซอร์สโค้ดเชิงลึก (Detailed Source Code Audit)

### 3.1 ฝั่ง Game Client (`C:\OpenMU\OpenMu-ClientMainS6E3`)
- **มาตรฐานภาษาและ Toolchain**: C++20, CMake 3.25+, MSVC v143 (VS 2022 Build Tools), .NET 10 Native AOT
- **การเรนเดอร์ (Renderer)**:
  - ใช้งาน **SDL3 GPU API** พร้อมระบบ Deferred Draw Batching และ Bone Matrix Palettes
  - เชเดอร์ (Shaders) ถูกคอมไพล์ล่วงหน้า (Precompiled SPIR-V / DXIL) ใน `out/build/windows-x64/shaders` รวม 39 Shader Blobs
  - รองรับ VSync และ Uncapped Frame Rate โดยคง Timing การคำนวณฟิสิกส์และการเคลื่อนไหวไว้ที่ 25 FPS ตามมาตรฐานคลาสสิกของ MuOnline
- **ระบบติดต่อผู้ใช้ (UI System)**:
  - พัฒนาด้วย **RmlUi (RML / RCSS)** ทั้งหมด แทนที่โค้ด UI รุ่นเก่า โดยมีระบบตรวจจับ Theme Drift และ Syntax Balance ในกระบวนการ Build
- **ระบบเน็ตเวิร์กและการเชื่อมต่อ (Networking Bridge)**:
  - โฟลเดอร์ `ClientLibrary/`: โปรเจกต์ C# .NET 10 ซึ่งถูก Publish แบบ Native AOT ออกมาเป็น Dynamic Link Library
  - เชื่อมโยงกับ `src/source/Dotnet/Connection.cpp` และ `PacketFunctions_ClientToServer.cpp`
  - ใช้ชุดคลาสแพ็กเก็ตเดียวกับ OpenMU Game Server (`MUnique.OpenMU.Network.Packets` v0.9.11) ทำให้โครงสร้างแพ็กเก็ตทุกฟิลด์ตรงกันในระดับไบต์
- **ระบบความปลอดภัยในเครื่องลูกข่าย (Client Anti-Cheat)**:
  - `src/source/AntiCheat/ClientAntiCheat.cpp`: ตรวจสอบ Text Section Hash, ตรวจสอบโมดูล DLL แปลกปลอม, ตรวจจับ Debugger และ Timing Tampering
  - ส่งสถานะตรวจสอบ (Heartbeat) ผ่าน Named Pipe แบบลงนาม HMAC ไปยัง Launcher

### 3.2 ฝั่ง Game Server (`C:\OpenMU\OpenMU-Origins`)
- **มาตรฐานภาษาและ Toolchain**: C# 13, .NET SDK 10.0.204, ASP.NET Core, EntityFrameworkCore Npgsql
- **สถาปัตยกรรม Game Logic**:
  - `src/GameLogic/`: โค้ดหลักในการคำนวณ State ทั้งหมด เป็นอิสระจากการเชื่อมต่อเครือข่าย
  - `src/GameServer/`: Network Listener Socket และ Packet Handlers (`PacketHandlerPlugInContainer.cs`)
  - `src/GameLogic/AntiCheat/ServerAuthoritativeAntiCheatPlugIn.cs`: ระบบคำนวณ Risk Score กลาง, ตรวจสอบ Attack Speed และ Skill Cooldown ด้วย Token Bucket
  - `src/GameLogic/PlayerActions/`: ตรรกะการเทรด, การย้ายไอเทม, คลังเก็บของ, ร้านค้าส่วนตัว ซึ่งมีการล็อก State ป้องกันการปั๊มไอเทม
- **Web API สำหรับส่วนขยาย (AdminPanel)**:
  - `AccountController.cs`: สมัครสมาชิก, ยืนยันรหัสผ่านด้วย BCrypt, เปลี่ยนรหัสผ่านตนเอง
  - `WebAdminAuthController.cs`: การเข้าสู่ระบบแอดมิน, สิทธิ์ RBAC, TOTP 2FA
  - `WebCharacterController.cs`: รีเซ็ตเลเวล, ล้างหัวแดง (PK), อัพ Stat ขณะออฟไลน์ พร้อมระบบจองสถานะบัญชี (`TryReserveAccountAsync`)
  - `CashShopController.cs`: เติมเหรียญ WCoinC / WCoinP พร้อมรองรับ Unique Reference

### 3.3 ฝั่ง Website (`C:\OpenMU\OpenMU-Web`)
- **เทคโนโลยี**: React 19 Frontend + Node.js v22 BFF + PostgreSQL
- **ความปลอดภัยและการเงิน**:
  - ปราศจาก Mock Credentials (ลบ `TEST_ACCOUNTS` และ `"admin1234"` ออกหมดแล้ว)
  - แบ่งแยก Session Cookie อย่างเด็ดขาดระหว่าง `omu_player_sid` และ `omu_admin_sid`
  - ตรวจสอบ Origin ทุกคำขอ Mutating ป้องกัน CSRF
  - ระบบกระเป๋าเงินใช้ ACID Row-Level Locking (`FOR UPDATE`) และ Check Constraints

---

## 4. เมทริกซ์การพึ่งพาของไลบรารีและเวอร์ชัน (Dependencies Matrix)

| โปรเจกต์ | ไลบรารี / แพ็กเกจสำคัญ | เวอร์ชัน | แหล่งที่มา / วิธีติดตั้ง |
|---|---|---|---|
| **Client C++** | SDL3 | 3.x (FetchContent) | CMake Auto Fetch |
| **Client C++** | RmlUi | 5.x / Modernized | `src/ThirdParty/RmlUi` |
| **Client C++** | OpenSSL | 3.x | vcpkg manifest (`vcpkg.json`) |
| **Client C++** | curl (with SSL) | 8.x | vcpkg manifest |
| **Client C++** | directx-dxc / spirv-cross | Latest | vcpkg manifest |
| **Client .NET** | MUnique.OpenMU.Network.Packets | 0.9.11 | NuGet Package |
| **Client .NET** | Pipelines.Sockets.Unofficial | 2.2.8 | NuGet Package |
| **Server** | BCrypt.Net-Next | 4.0.3 | NuGet Package |
| **Server** | Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.x / 9.0 | NuGet Package |
| **Server** | Microsoft.AspNetCore.Identity | 10.0.x | .NET 10 Framework |
| **Website** | pg (node-postgres) | 8.13.3 | npm package |
| **Website** | React & React-DOM | 19.2.0 | npm package |
| **Website** | Vite | 6.2.0 | npm package |

---

## 5. ตารางวิเคราะห์ฟังก์ชันที่ขาดหรือต้องเสริมความแกร่ง (Gap Analysis)

| ส่วนประกอบ | สถานะปัจจุบัน | ความเสี่ยงที่พบ | แผนการจัดการและแก้ไข |
|---|---|---|---|
| **Client-Server Handshake** | มี Native AOT Serializer ตรงกัน | ไคลเอนต์รุ่นเก่าที่ไม่มี AOT อาจส่ง Packet ไม่ครบ | บังคับใช้โปรโตคอล Season 6 ผ่าน `ClientLibrary` |
| **Monster HP Bar** | มีการส่งข้อมูล HP หลังโจมตี | ปริมาณ Packet อาจสูงขึ้นเมื่อมอนสเตอร์หนาแน่น | มี Rate Limit ในการ Broadcast HP Bar Update |
| **Auto Reconnect** | มีระบบ Reconnect อัตโนมัติใน Client | อาจเกิด Reconnect Race กับ Session เก่า | ใช้ Session Reservation ID ยืนยันการเคลียร์ State เดิม |
| **Lucky Items** | ยังไม่สมบูรณ์ใน Season 6 | ฟีเจอร์อยู่ระหว่างการพัฒนา | ปิดการใช้งาน Lucky Item Drop ชั่วคราวบนเซิร์ฟเวอร์ |

---

## 6. บทสรุปการตรวจสอบใน Phase 1
สถาปัตยกรรมของทั้ง 3 Repositories มีการแบ่งชั้นการทำงานที่ชัดเจน (Well-Architected) จุดเชื่อมต่อด้าน Network มีความเข้ากันได้สูงเนื่องจากใช้โมเดลข้อมูลจากแหล่งเดียวกัน (`MUnique.OpenMU.Network`) พร้อมก้าวเข้าสู่ Phase 2 และ Phase 3 ต่อไป

