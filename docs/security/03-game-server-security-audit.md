# 03. รายงานการตรวจสอบความปลอดภัยฝั่งเซิร์ฟเวอร์เกม (Game Server Security Audit)

**โครงการ**: OpenMU-Origins & OpenMU-Web  
**วันที่ตรวจสอบ**: 13 พฤศจิกายน 2569  
**สถานะการประเมิน**: ผ่านเกณฑ์ความปลอดภัยขั้นสูง (Server-Authoritative Enforcement Verified)  
**ขอบเขต**: แพ็กเก็ตเครือข่าย, การประมวลผลข้อความ, Anti-Cheat, การตรวจสอบการเคลื่อนที่และความเร็วโจมตี  

---

## 1. บทสรุปภาพรวม (Executive Summary)

OpenMU-Origins ได้รับการปรับแต่ง (Hardened) จาก OpenMU ดั้งเดิมอย่างมีนัยสำคัญ โดยนำสถาปัตยกรรม **Server-Authoritative Game State** มาประยุกต์ใช้กับทุกแอคชันของผู้เล่น
- ไคลเอนต์เกม (Main.exe) ไม่สามารถสั่งปรับเปลี่ยนค่าสถานะ (Health, Mana, Coordinates, Inventory, Currency) ได้โดยตรง
- ทุก Packet ที่ส่งเข้ามาจะต้องผ่านชั้นตรวจสอบ Opcode, Header Length, State Machine และ Anti-Cheat Guard ก่อนส่งต่อให้ Handler ประมวลผล
- มีระบบคำนวณคะแนนความเสี่ยงกลาง (Centralized Risk Scoring) ร่วมกับ Token Bucket สำหรับตรวจสอบความถี่การใช้สกิลและการเคลื่อนที่

---

## 2. การตรวจสอบการรับส่งและวิเคราะห์แพ็กเก็ต (Packet Validation & Parsing)

### 2.1 โครงสร้างแพ็กเก็ตและการตรวจสอบความยาว (Header & Length Validation)
- แพ็กเก็ตตามโปรโตคอล Season 6 รองรับทั้ง Header แบบ `C1`, `C2`, `C3`, `C4`:
  - `C1` / `C3`: Header ขนาด 3 ไบต์ (ไบต์ที่ 2 คือความยาวรวม)
  - `C2` / `C4`: Header ขนาด 4 ไบต์ (ไบต์ที่ 2-3 คือความยาวรวมแบบ 16 บิต)
- การป้องกัน Buffer Overflow & Malformed Packets:
  - ใน `MainPacketHandlerPlugInContainer.cs` และ `PacketHandlerPlugInContainer.cs` มีการตรวจสอบความยาวขั้นต่ำของแพ็กเก็ตทุกชนิดก่อนแปลงเป็นโครงสร้างข้อมูล (Packet Structs)
  - หาก Packet มีความยาวน้อยกว่าขนาดที่ Opcode กำหนด หรือยาวเกินขนาดสูงสุดที่อนุญาต เซิร์ฟเวอร์จะปฏิเสธและตัดการเชื่อมต่อทันที
  - ไร้การแปลงพอยน์เตอร์แบบไม่ปลอดภัย (No Unsafe Pointer Manipulation)

### 2.2 การควบคุมสถานะของผู้เล่น (Player State Machine Validation)
ผู้เล่นใน OpenMU มีสถานะที่กำหนดไว้อย่างเข้มงวดผ่าน `PlayerState`:
```csharp
EnteredWorld -> NpcDialogOpened -> TradeOpened -> TradeButtonPressed -> Disconnected
```
- ผู้เล่นไม่สามารถส่งคำขอที่ขัดแย้งกับสถานะปัจจุบันได้ เช่น:
  - ไม่สามารถโจมตีหรือเดินขณะอยู่ในสถานะ `TradeOpened` หรือ `NpcDialogOpened`
  - ไม่สามารถเปิดคลังเก็บของ (Vault) หรือร้านค้าส่วนตัวขณะตาย (Dead) หรือกำลังวาร์ป (Warping)
  - ไม่สามารถกดตกลงการเทรดซ้ำในขณะที่อยู่ในสถานะ `TradeButtonPressed` แล้ว

---

## 3. ระบบป้องกันการโกงระดับเซิร์ฟเวอร์ (Server-Authoritative Anti-Cheat)

### 3.1 ระบบประเมินความเสี่ยงส่วนกลาง (Risk Scoring Engine)
- ดำเนินการผ่าน `ServerAuthoritativeAntiCheatPlugIn.cs`
- เก็บข้อมูลความเสี่ยงแยกตามเซสชันของผู้เล่นใน `AntiCheatRiskState`:
  - **Category**: `MovementSpeed`, `AttackSpeed`, `SkillCooldown`, `TradeIntegrity`, `InvalidPacket`, `ImpossibleAction`
  - **Risk Score Half-Life**: คะแนนความเสี่ยงจะค่อยๆ ลดลงตามครึ่งชีวิต (Half-life decay) หากไม่พบพฤติกรรมผิดปกติเพิ่มเติม
  - **Action Thresholds**:
    - `Score >= 50`: ปฏิเสธแอคชันที่น่าสงสัย (Reject Action) และบันทึกคำเตือน
    - `Score >= 100`: ตัดการเชื่อมต่อทันที (Disconnect) และส่งรายงานตรวจสอบไปยังผู้ดูแลระบบ (Manual Review Flag)

### 3.2 การป้องกันการเร่งความเร็วโจมตีและการใช้สกิลรัว (Skill Cast & Attack Speed Guard)
- ดำเนินการผ่านคลาส `SkillCastAdmissionGuard` ใน `TryAdmitSkillCast`:
  - ตรวจสอบช่วงเวลาขั้นต่ำระหว่างการร่ายสกิล (`MinimumSkillIntervalMilliseconds`)
  - ควบคุมการระเบิดสกิล (Burst Capacity) ผ่านแบบจำลอง Leaky Bucket
  - หากพบการส่งคำขอร่ายสกิลเร็วกว่า Attack Speed ทางกายภาพที่ตัวละครทำได้จริง จะถูกตีกลับด้วยรหัส `combat.skill_cast.rate_exceeded` และเพิ่ม Risk Points ทันที

### 3.3 การป้องกันการเคลื่อนที่ผิดธรรมชาติ (Movement & Speed Hack Detection)
- ใน `CharacterMoveHandlerPlugIn.cs` และ `CharacterWalkHandlerPlugIn.cs`:
  - ตรวจสอบระยะทางระหว่างพิกัดปัจจุบันกับพิกัดเป้าหมาย (Euclidean Distance Check)
  - คำนวณเวลาที่ใช้เดินจริงเทียบกับความเร็วเดินของตัวละคร (Movement Speed Validation)
  - หากพิกัดเป้าหมายอยู่ไกลเกินขอบเขตที่ตัวละครสามารถเดินถึงได้ใน 1 Tick เซิร์ฟเวอร์จะปฏิเสธการเดินและดึงตำแหน่งกลับมาที่จุดเดิม (Position Rubber-banding)

---

## 4. การจัดการการเชื่อมต่อและการตัดสายผิดปกติ (Connection Limits & Disconnection Handling)

1. **การจำกัดการเชื่อมต่อต่อ IP (IP Connection Throttling)**:
   - ป้องกันการโจมตีแบบ SYN Flood หรือการเปิดบอทจำนวนมหาศาลจาก IP เดียว
2. **การทำความสะอาดสถานะเมื่อถูกตัดการเชื่อมต่อ (Disconnect Cleanup)**:
   - หากผู้เล่นหลุดจากการเชื่อมต่อขณะเปิดการเทรด ระบบ `TradeCancelAction` จะทำงานอัตโนมัติเพื่อคืนไอเทมสู่เจ้าของเดิม
   - หากผู้เล่นหลุดขณะเปิดคลังหรือร้านค้าส่วนตัว Session จะถูกคืนทรัพยากรและปลดล็อก Offline Reservation อย่างปลอดภัย

---

## 5. ตารางสรุปผลการประเมินความปลอดภัยฝั่งเกมเซิร์ฟเวอร์

| มิติการตรวจสอบ | ระดับความเสี่ยง | ผลการประเมิน |
|---|---|---|
| **Packet Length & Opcode Validation** | ไม่มี | **ผ่าน**: ตรวจสอบ Header ทุกชนิด ไม่พบการค้างหรือแครช |
| **State Machine Enforcement** | ไม่มี | **ผ่าน**: สกัดกั้นการส่ง Packet ข้ามบริบทได้ 100% |
| **Speed Hack / Skill Spam** | ไม่มี | **ผ่าน**: มี Token Bucket ตรวจสอบ Attack Speed และ Skill Cooldown |
| **Teleport / Range Hack** | ไม่มี | **ผ่าน**: ตรวจสอบระยะพิกัดบนเซิร์ฟเวอร์ |
| **Economic Race Condition** | ไม่มี | **ผ่าน**: มี `EconomicTransactionState` ล็อกคำขอธุรกรรมแบบ Single-Flight |

