# 04. ตารางประเมินฟังก์ชันระบบเกมเพลย์แบบครอบคลุม (Comprehensive Gameplay Feature Matrix)

**โครงการ**: OpenMU-Origins & OpenMu-ClientMainS6E3  
**วันที่ตรวจสอบ**: 13 พฤศจิกายน 2569  
**เวอร์ชันเกม**: Season 6 Episode 3  
**เกณฑ์การจัดหมวดหมู่สถานะ**:
- `IMPLEMENTED_AND_TESTED`: มีฟังก์ชันในโค้ดและผ่านชุดทดสอบอัตโนมัติแล้ว
- `IMPLEMENTED_NOT_TESTED`: มีฟังก์ชันสมบูรณ์ในโค้ด แต่ยังไม่ได้เขียน Unit Test เฉพาะเจาะจง
- `PARTIALLY_IMPLEMENTED`: มีฟังก์ชันบางส่วน หรือเปิดใช้งานเฉพาะส่วนหลัก
- `NOT_IMPLEMENTED`: ยังไม่ได้พัฒนาฟังก์ชันนี้
- `BROKEN`: ฟังก์ชันขัดข้องหรือทำงานผิดพลาด
- `NOT_APPLICABLE`: ไม่เข้าเกณฑ์สำหรับ Season 6 Episode 3

---

## 1. ระบบบัญชีและตัวละคร (Account & Character System)

| ฟีเจอร์ (Feature) | ฝั่ง Client | ฝั่ง Server | สถานะการประเมิน | หมายเหตุและหลักฐานในโค้ด |
|---|---|---|---|---|
| **User Authentication / Login** | `LoginWin.cpp` | `LogInHandlerPlugIn.cs` | **IMPLEMENTED_AND_TESTED** | SimpleModulus + BCrypt Hash |
| **Character Creation** | `CharacterCreateWin.cpp` | `CharacterCreatePacketHandlerPlugIn` | **IMPLEMENTED_AND_TESTED** | รองรับ 7 อาชีพหลัก รวม Rage Fighter |
| **Character Deletion** | `CharacterSelectWin.cpp` | `CharacterDeletePacketHandlerPlugIn` | **IMPLEMENTED_AND_TESTED** | มีการยืนยันรหัสความปลอดภัย |
| **Character Selection & Loading** | `CharacterSelectWin.cpp` | `CharacterSelectPacketHandlerPlugIn` | **IMPLEMENTED_AND_TESTED** | โหลด AppearanceData และ Inventory |
| **Character Logout / Switch** | `GameMenu.cpp` | `LogOutHandlerPlugIn.cs` | **IMPLEMENTED_AND_TESTED** | คืนค่าสถานะสู่หน้าเลือกตัวละคร |
| **Auto-Reconnection** | `Connection.cpp` | `GameContext.cs` | **IMPLEMENTED_AND_TESTED** | ไคลเอนต์พยายามต่อสายใหม่เมื่อหลุด |
| **Server Switching** | `ConnectServer` | `ServerController.cs` | **IMPLEMENTED_AND_TESTED** | ย้ายห้อง / สลับ Channel ได้ |

---

## 2. ระบบการพัฒนาตัวละคร (Character Progression)

| ฟีเจอร์ (Feature) | ฝั่ง Client | ฝั่ง Server | สถานะการประเมิน | หมายเหตุและหลักฐานในโค้ด |
|---|---|---|---|---|
| **Leveling & Experience** | `CharacterInfo.cpp` | `GameContext.Experience` | **IMPLEMENTED_AND_TESTED** | เลเวล 1-400 คำนวณ EXP ถูกต้อง |
| **Master Level & Tree** | `MasterSkillTree.cpp` | `MasterSkillRoot` | **IMPLEMENTED_AND_TESTED** | เลเวล 401-800 รองรับ Master Points |
| **Stat Point Allocation** | `CharacterInfo.cpp` | `CharacterStatIncreasePacketHandler` | **IMPLEMENTED_AND_TESTED** | Str, Agi, Vit, Ene, Cmd (DL) |
| **Offline Stat / Reset Service**| `WebCharacterController` | `WebCharacterSelfService.cs` | **IMPLEMENTED_AND_TESTED** | ทำงานผ่าน Web API ขณะออฟไลน์ |
| **Character Reset System** | Web API / NPC | `ResetConfiguration.cs` | **IMPLEMENTED_AND_TESTED** | มี `ResetCharacterHonorsConfiguration` |
| **Grand Reset System** | Web API / NPC | Server Rules | **IMPLEMENTED_NOT_TESTED** | มีโครงสร้างรองรับในคอนฟิก |
| **Class Change Quest** | `QuestSystem.cpp` | `QuestClientActionHandlerPlugIn` | **IMPLEMENTED_AND_TESTED** | เควสเปลี่ยนคลาส 2 (Sebina) และ 3 |

---

## 3. ระบบการต่อสู้ (Combat Subsystem)

| ฟีเจอร์ (Feature) | ฝั่ง Client | ฝั่ง Server | สถานะการประเมิน | หมายเหตุและหลักฐานในโค้ด |
|---|---|---|---|---|
| **Normal Attack** | `CombatSystem.cpp` | `HitHandlerPlugIn.cs` | **IMPLEMENTED_AND_TESTED** | โจมตีประชิดและระยะไกล |
| **Targeted Skill** | `SkillSystem.cpp` | `TargetedSkillHandlerPlugIn.cs` | **IMPLEMENTED_AND_TESTED** | สกิลเป้าหมายเดี่ยว ตรวจระยะจริง |
| **Area Attack Skill (AoE)** | `SkillSystem.cpp` | `AreaSkillAttackHandlerPlugIn.cs` | **IMPLEMENTED_AND_TESTED** | Evil Spirit, Twisting Slash, Nova |
| **Combo Skill System** | `CombatSystem.cpp` | `SkillComboStep` | **IMPLEMENTED_AND_TESTED** | คอมโบ 3 สเต็ปของ Blade Knight |
| **Attack Speed Enforcement** | `ClientAntiCheat.cpp` | `SkillCastAdmissionGuard.cs` | **IMPLEMENTED_AND_TESTED** | Token bucket ควบคุมความถี่สกิล |
| **PvP & PvE Damage Formulas** | `DamageDisplay.cpp` | `AttributeSystem` | **IMPLEMENTED_AND_TESTED** | คำนวณเกราะ, คริติคอล, เอ็กเซเลนต์ |
| **Monster HP Display Bar** | `MonsterHpBar.cpp` | `ShowMonsterHealthBarPlugIn.cs`| **IMPLEMENTED_AND_TESTED** | แสดงหลอดเลือดหลังโจมตี (Origins) |
| **Buffs & Debuffs** | `BuffSystem.cpp` | `MagicEffectDefinition` | **IMPLEMENTED_AND_TESTED** | Greater Fortitude, Soul Barrier, Mana Shield |

---

## 4. ระบบไอเทมและเศรษฐกิจ (Inventory, Storage & Economy)

| ฟีเจอร์ (Feature) | ฝั่ง Client | ฝั่ง Server | สถานะการประเมิน | หมายเหตุและหลักฐานในโค้ด |
|---|---|---|---|---|
| **Inventory & Equipment** | `InventorySystem.cpp`| `InventoryConstants` | **IMPLEMENTED_AND_TESTED** | กระเป๋า 8x8 + ขยาย (Expanded) |
| **Item Move & Context Lock** | `InventorySystem.cpp`| `MoveItemAction.cs` | **IMPLEMENTED_AND_TESTED** | ตรวจสอบสถานะ NPC และหน้าต่าง |
| **Item Stacking & Split** | `InventorySystem.cpp`| `ItemStackAction.cs` | **IMPLEMENTED_AND_TESTED** | ซ้อน Jewel, Potions ได้ |
| **Item Durability & Repair** | `InventorySystem.cpp`| `ItemRepairAction.cs` | **IMPLEMENTED_AND_TESTED** | ซ่อมไอเทมรายชิ้น / ซ่อมทั้งหมด |
| **Chaos Machine Crafting** | `ChaosMachineWin.cpp` | `BaseItemCraftingHandler.cs` | **IMPLEMENTED_AND_TESTED** | ตีบวก +10 ถึง +15, ผสมปีก 1-3 |
| **Talisman of Luck / Chaos** | `ChaosMachineWin.cpp` | `CraftingTalismansTest.cs` | **IMPLEMENTED_AND_TESTED** | ป้องกันของแตกและเพิ่มอัตราติด |
| **Player-to-Player Trading** | `TradeSystem.cpp` | `TradeButtonAction.cs` | **IMPLEMENTED_AND_TESTED** | ป้องกัน Duping ด้วย Async Lock |
| **Personal Shop (Store)** | `PersonalShop.cpp` | `BuyRequestAction.cs` | **IMPLEMENTED_AND_TESTED** | Single-flight Economic Lease |
| **Vault (Khloko) Storage** | `VaultSystem.cpp` | `UnlockVaultPlugIn.cs` | **IMPLEMENTED_AND_TESTED** | คลังเก็บของและรหัสผ่าน PIN |
| **CashShop (WCoinC/Points)** | `GameShop.cpp` | `CashShopController.cs` | **IMPLEMENTED_AND_TESTED** | เชื่อมโยงกับระบบกระเป๋าเงินเว็บ |
| **Lucky Items System** | `InventorySystem.cpp`| `LuckyItemTicketExchange` | **PARTIALLY_IMPLEMENTED** | รองรับโครงสร้าง แต่รอการเปิดเต็มตัว |

---

## 5. ระบบสังคม (Social Systems)

| ฟีเจอร์ (Feature) | ฝั่ง Client | ฝั่ง Server | สถานะการประเมิน | หมายเหตุและหลักฐานในโค้ด |
|---|---|---|---|---|
| **Chat (Normal / Whisper)** | `ChatWin.cpp` | `ChatMessageHandlerPlugIn.cs` | **IMPLEMENTED_AND_TESTED** | รองรับภาษาไทย (Windows-874) |
| **Chat Commands** | `ChatWin.cpp` | `ChatCommandGroupHandlerPlugIn` | **IMPLEMENTED_AND_TESTED** | `/post`, `/warp`, `/addstr`, ฯลฯ |
| **Party System** | `PartyWin.cpp` | `PartyRequestHandlerPlugIn.cs` | **IMPLEMENTED_AND_TESTED** | ปาร์ตี้สูงสุด 5 คน, แชร์ EXP |
| **Guild System** | `GuildWin.cpp` | `GuildCreateHandlerPlugIn.cs` | **IMPLEMENTED_AND_TESTED** | สร้างกิลด์, โลโก้กิลด์, พันธมิตร |
| **Friend & Letter Messenger**| `MessengerWin.cpp` | `AddFriendHandlerPlugIn.cs` | **IMPLEMENTED_AND_TESTED** | เพื่อนและกล่องจดหมาย |
| **Duel System (Vulcanus)** | `DuelWin.cpp` | `DuelActions.cs` | **IMPLEMENTED_AND_TESTED** | ระบบดวล 1-on-1 มีสกอร์บอร์ด |

---

## 6. โลกและกิจกรรมพิเศษ (World & Scheduled Events)

| ฟีเจอร์ (Feature) | ฝั่ง Client | ฝั่ง Server | สถานะการประเมิน | หมายเหตุและหลักฐานในโค้ด |
|---|---|---|---|---|
| **Map Navigation & Gates** | `World.cpp` | `WarpGateHandlerPlugIn.cs` | **IMPLEMENTED_AND_TESTED** | วาร์ปตามเกทและระดับเลเวล |
| **Blood Castle (BC 1-8)** | `EventWindow.cpp` | `BloodCastleEnterHandlerPlugIn` | **IMPLEMENTED_AND_TESTED** | เควสส่งดาบ Archangel รับของรางวัล |
| **Devil Square (DS 1-7)** | `EventWindow.cpp` | `DevilSquareEnterHandlerPlugIn` | **IMPLEMENTED_AND_TESTED** | เอาชีวิตรอดและเก็บแต้มอันดับ |
| **Chaos Castle (CC 1-7)** | `EventWindow.cpp` | `ChaosCastleEnterHandlerPlugIn` | **IMPLEMENTED_AND_TESTED** | แบทเทิลรอยัล พื้นตก รับ Jewel |
| **Crywolf Event** | `Crywolf.cpp` | `CrywolfGroupHandlerPlugIn.cs` | **IMPLEMENTED_AND_TESTED** | ปกป้องแท่นหิน Balgass |
| **Castle Siege** | `CastleSiege.cpp` | `CastleSiegeGroupHandlerPlugIn` | **IMPLEMENTED_AND_TESTED** | สงครามปราสาท Valley of Loren |
| **Kanturu / Maya / Nightmare**| `World.cpp` | `KanturuGroupHandlerPlugIn.cs` | **IMPLEMENTED_AND_TESTED** | ดันเจี้ยนหอคอย Refine Tower |
| **Raklion (Selupan Boss)** | `World.cpp` | `RaklionStateInfoRequestHandler` | **IMPLEMENTED_AND_TESTED** | สังหารบอส Selupan ฟักไข่ |
| **Imperial Guardian** | `EventWindow.cpp` | `ImperialGuardianEnterHandler` | **IMPLEMENTED_AND_TESTED** | ดันเจี้ยนประจำวันเป็นรอบ |
| **Doppelganger** | `EventWindow.cpp` | `DoppelgangerEnterRequestHandler` | **IMPLEMENTED_AND_TESTED** | ดันเจี้ยนเลียนแบบตัวละคร |
| **MU Helper (Auto-Hunt)** | `MUHelper.cpp` | `MuHelperGroupHandler.cs` | **IMPLEMENTED_AND_TESTED** | ระบบบอทแท้ในเกม (ออโต้สกิล/บัฟ) |

---

## 7. บทสรุปการประเมินฟังก์ชันเกมเพลย์
ระบบเกมเพลย์หลักของ Season 6 Episode 3 ได้รับการพัฒนาอย่างสมบูรณ์แบบทั้งสองฝั่ง ฟังก์ชันหลักกว่า 95% ได้รับการยืนยันสถานะ `IMPLEMENTED_AND_TESTED` พร้อมเปิดให้บริการแก่ผู้เล่นทั่วไป

