# 06. รายงานการตรวจสอบระบบป้องกันการโกงและการรักษาความมั่นคงปลอดภัย (Anti-Cheat Security Audit)

**โครงการ**: OpenMU-Origins & OpenMu-ClientMainS6E3  
**วันที่ตรวจสอบ**: 13 พฤศจิกายน 2569  
**สถานะการประเมิน**: **ผ่านเกณฑ์ระดับสูง (Layered Defense-in-Depth Verified)**  
**ขอบเขต**: Client Anti-Cheat (C++), Launcher Guard (C# .NET), Server Anti-Cheat Plugin (.NET 10)  
**ผู้ตรวจสอบ**: Senior Game Security & Anti-Cheat Engineer  

---

## 1. ปรัชญาและสถาปัตยกรรมการป้องกันการโกง (Anti-Cheat Philosophy & Architecture)

ระบบป้องกันการโกงของ OpenMU ได้รับการออกแบบตามหลักการ **Layered Defense-in-Depth** โดยตระหนักถึงความเป็นจริงทางวิศวกรรมความมั่นคงปลอดภัยว่า:
> *"ไม่มีระบบป้องกันการโกงใดในโลกที่สามารถรับประกันการป้องกันได้ 100% ตราบใดที่โปรแกรมทำงานบนเครื่องลูกข่ายของผู้เล่น ความปลอดภัยที่แท้จริงต้องตั้งอยู่บน Server-Authoritative Validation เป็นแกนหลัก โดยมี Client Integrity เป็นเกราะป้องกันชั้นนอก"*

### แผนผังสถาปัตยกรรม 3 ชั้น (3-Tier Layered Protection):

```
+------------------------------------------------------------------------------------+
| TIER 1: CLIENT RUNTIME INTEGRITY (User-Mode Process Boundary)                      |
|                                                                                    |
|  [Main.exe / ClientAntiCheat.cpp]                                                  |
|  - IntegrityManager: ตรวจสอบ Text Section Hash และ SHA-256 ของ Executable          |
|  - ModuleScanner: สแกน DLL ที่โหลดเข้า Process ป้องกัน Unsigned / Injected Modules  |
|  - AntiDebugManager: ตรวจจับ Debugger API Hooks และ Hardware Breakpoints            |
|  - SpeedValidation: ตรวจจับการบิดเบือนเวลา (steady_clock vs system_clock drift)   |
+------------------------------------------+-----------------------------------------+
                                           | Local Named Pipe (OriginsGuard)
                                           | Signed with Session HMAC-SHA256
+------------------------------------------v-----------------------------------------+
| TIER 2: LAUNCHER INTEGRITY & SUPERVISOR (OriginsLauncher.exe)                      |
|                                                                                    |
|  [GuardPipeServer.cs]                                                              |
|  - รับ Heartbeat จาก Main.exe ทุกๆ 2.5 วินาที พร้อมตรวจสอบลำดับ Sequence Number   |
|  - ตรวจสอบความถูกต้องของ SessionId, BuildId และลายเซ็น HMAC                        |
|  - ตัดการทำงานของเกมทันทีหาก Heartbeat ขาดหายหรือมีการปิด Launcher                |
+------------------------------------------+-----------------------------------------+
                                           | Encrypted TCP Game Socket (55901)
+------------------------------------------v-----------------------------------------+
| TIER 3: SERVER-AUTHORITATIVE ENGINE (OpenMU-Origins GameServer)                    |
|                                                                                    |
|  [ServerAuthoritativeAntiCheatPlugIn.cs]                                           |
|  - Central Risk Scoring: คำนวณคะแนนความเสี่ยงสะสมแยกตามเซสชัน (Half-Life Decay)    |
|  - SkillCastAdmissionGuard: Token Bucket ควบคุมอัตราการใช้สกิลตาม Attack Speed    |
|  - EconomicTransactionState: Single-Flight Lock ป้องกันคำขอซื้อขายซ้ำซ้อน         |
|  - Movement Validator: ตรวจสอบความเร็วเดินและดึงตำแหน่งกลับเมื่อเดินข้ามพิกัด     |
+------------------------------------------------------------------------------------+
```

---

## 2. การวิเคราะห์การป้องกันภัยคุกคามรายประเภท (Threat-Specific Defense Analysis)

### 2.1 การป้องกันการเร่งความเร็วเดิน (Speed Hack & Movement Manipulation)
- **ฝั่ง Client (`SpeedValidation`)**:
  - เปรียบเทียบความต่างระหว่าง `std::chrono::steady_clock` (ฮาร์ดแวร์ monotonic timer) กับ `std::chrono::system_clock`
  - หากโปรแกรมโกงปรับแต่ง `timeGetTime` หรือ `GetTickCount` จะเกิดความคลาดเคลื่อน (Clock Drift) เกินเกณฑ์ปกติ และถูกรายงานเป็น `speed.clock_drift`
- **ฝั่ง Server (`CharacterWalkHandlerPlugIn.cs`)**:
  - คำนวณระยะทางแบบ Euclidean Distance บนแผนที่
  - เปรียบเทียบกับความเร็วเดินสูงสุดที่เป็นไปได้ตามอาชีพ, รองเท้า, ปีก, และสัตว์ขี่ (Fenrir / Dinorant)
  - หากก้าวเกินระยะใน 1 Tick เซิร์ฟเวอร์จะปฏิเสธตำแหน่งใหม่และดึงกลับมาที่เดิม (Rubber-banding)

### 2.2 การป้องกันการเร่งความเร็วโจมตีและการใช้สกิลรัว (Attack Speed & Skill Spam)
- **ฝั่ง Server (`SkillCastAdmissionGuard`)**:
  - คำนวณช่วงเวลาขั้นต่ำระหว่างการร่ายสกิล (`MinimumSkillIntervalMilliseconds`) จากค่า Agility ของตัวละครจริงบนเซิร์ฟเวอร์
  - มี Burst Capacity เล็กน้อยเพื่อรองรับความแปรปรวนของ Network Jitter
  - หากผู้เล่นส่งแพ็กเกจโจมตีเร็วกว่าที่ Stat อนุญาต คำขอจะถูก Reject และบันทึกคะแนนความเสี่ยง `combat.skill_cast.rate_exceeded`

### 2.3 การป้องกันการวาร์ปและข้ามกำแพง (Teleport & Range Hacks)
- **ฝั่ง Server (`WarpGateHandlerPlugIn.cs` / `AttackActionValidator.cs`)**:
  - คำนวณระยะห่างระหว่างผู้เล่นกับเป้าหมาย (Distance <= MaxSkillRange)
  - คำนวณแนวสายตา (Line of Sight) และสิ่งกีดขวางบน Map Grid
  - การวาร์ปหรือเคลื่อนที่ข้ามพิกัดทำได้เฉพาะเมื่อผ่าน Gatekeeper หรือสกิลวาร์ปที่ถูกกฎหมายเท่านั้น

### 2.4 การป้องกันการฉีดโค้ดและแก้ไขหน่วยความจำ (DLL Injection & Memory Editing)
- **ฝั่ง Client (`IntegrityManager` & `ModuleScanner`)**:
  - คำนวณ Text Section Hash ของ `Main.exe` เมื่อเริ่มเปิดเกม
  - สแกนรายชื่อ DLL ที่ถูกโหลดเข้ามาใน Process เพื่อตรวจจับ Hook ยอดนิยม (เช่น Speed Gear, Cheat Engine DLLs)
  - ตรวจจับ Debugger ผ่าน `IsDebuggerPresent`, `CheckRemoteDebuggerPresent`, และ Thread Context Flags

### 2.5 การป้องกันธุรกรรมเศรษฐกิจซ้ำซ้อน (Economic Concurrency Abuse)
- **ฝั่ง Server (`EconomicTransactionState`)**:
  - บังคับใช้สัญญา **Single-Flight Lease**: ผู้เล่น 1 คนสามารถทำธุรกรรมเคลื่อนย้ายไอเทมหรือซื้อขายได้ทีละ 1 รายการเท่านั้น
  - หากส่งคำขอซื้อของหรือเทรดซ้อนกันในเวลาเสี้ยววินาที คำขอที่สองจะถูกตัดทิ้งทันที และบันทึกหลักฐาน `economy.transaction.concurrent`

---

## 3. นโยบายการตอบสนองและการลงโทษแบบขั้นบันได (Tiered Sanctions Policy)

เพื่อป้องกันปัญหาผลบวกลวง (False Positives) ที่อาจเกิดจากความล่าช้าของอินเทอร์เน็ต ระบบใช้คะแนนความเสี่ยงสะสมที่มีการลดทอนตามเวลา (Half-Life Decay):

| ระดับคะแนนความเสี่ยง (Risk Score) | สถานะความเสี่ยง (Risk Level) | การดำเนินการของเซิร์ฟเวอร์ (Action Taken) |
|---|---|---|
| **0 – 24 คะแนน** | ปกติ (Normal) | อนุญาตให้เล่นตามปกติ |
| **25 – 49 คะแนน** | น่าสงสัย (Suspicious) | บันทึกคำเตือนในเซิร์ฟเวอร์ล็อก (Observe Only) |
| **50 – 99 คะแนน** | เสี่ยงสูง (High Risk) | ปฏิเสธแอคชันที่ผิดปกติ (Reject Action) และแจ้งเตือนผู้เล่น |
| **100 คะแนนขึ้นไป** | วิกฤต (Critical) | ตัดการเชื่อมต่อทันที (Disconnect) และส่งเข้าระบบตรวจทานของผู้ดูแลระบบ |

---

## 4. รายงานความเสี่ยงคงเหลือตามความเป็นจริง (Explicit Residual Risk Disclosure)

ตามมาตรฐานวิศวกรรมความมั่นคงปลอดภัย ขอเปิดเผยความเสี่ยงคงเหลือที่อยู่นอกเหนือขอบเขตการควบคุมของ User-Mode Anti-Cheat:
1. **DMA (Direct Memory Access) Hardware Cards**: การใช้อุปกรณ์ฮาร์ดแวร์ภายนอกอ่านหน่วยความจำโดยตรงผ่านบัส PCIe
   - *มาตรการลดความเสี่ยง*: บังคับใช้ Server-Authoritative Logic อย่างสมบูรณ์ ทำให้แม้ผู้โกงจะอ่านค่าในหน่วยความจำได้ ก็ไม่สามารถแก้ไขค่าบนเซิร์ฟเวอร์ได้
2. **AI-driven Vision Aim / Mouse Automation**: บอทที่จับภาพหน้าจอและจำลองการคลิกเมาส์เสมือนผู้เล่นจริง
   - *มาตรการลดความเสี่ยง*: ตรวจสอบพฤติกรรมการเล่นและระยะเวลาออนไลน์ที่ต่อเนื่องยาวนานผิดปกติผ่าน Audit Log
3. **Modified OS / Hypervisor Virtualization**: การรันเกมบน VM ที่ปรับแต่ง Clock Timing
   - *มาตรการลดความเสี่ยง*: การตรวจสอบเวลาแบบสองแกน (Client vs Server Latency Delta)

---

## 5. บทสรุปการประเมิน
ระบบ Anti-Cheat ของ OpenMU Ecosystem มีความสมดุลระหว่างความเข้มงวดในการป้องกันและความเป็นมิตรต่อผู้ใช้ โดยไม่ติดตั้งไดรเวอร์ระดับเคอร์เนลที่เป็นอันตรายต่อเครื่องผู้เล่น และเน้นการชี้ขาดบนเซิร์ฟเวอร์เป็นหลัก ผ่านการประเมินระดับพร้อมใช้งานจริง

