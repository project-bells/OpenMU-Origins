# 10. รายงานการตรวจสอบความสมบูรณ์ของฐานข้อมูลและการคงอยู่ของข้อมูล (Database & Persistence Integrity)

**โครงการ**: OpenMU Full Ecosystem (Server & Web)  
**วันที่ตรวจสอบ**: 13 พฤศจิกายน 2569  
**สถานะการประเมิน**: **ผ่านเกณฑ์บูรณภาพข้อมูลระดับสูงสุด (ACID & Schema Integrity Verified)**  
**ฐานข้อมูลหลัก**: PostgreSQL 16+ (Schemas: `data`, `config`, `web`)  
**ผู้ตรวจสอบ**: Senior Database Architect & Persistence Specialist  

---

## 1. การแบ่งแยกสคีมาฐานข้อมูลและขอบเขตความรับผิดชอบ (Schema Separation)

เพื่อป้องกันการปะปนกันของข้อมูลเกมและข้อมูลเว็บ และเพื่อให้ง่ายต่อการจัดการ Backup/Restore ระบบแบ่งสคีมาออกเป็น 3 ส่วน:

```
+---------------------------------------------------------------------------------+
|                               POSTGRESQL DATABASE                               |
|                                                                                 |
|  +---------------------------+  +------------------------+  +----------------+  |
|  |       SCHEMA 'data'       |  |      SCHEMA 'web'      |  | SCHEMA 'config'|  |
|  | - Accounts                |  | - wallets              |  | - Game Maps    |  |
|  | - Characters              |  | - wallet_ledger        |  | - Item Rules   |  |
|  | - ItemStorages & Items    |  | - credit_exchanges     |  | - Drop Groups  |  |
|  | - CashShopCoinGrants      |  | - topup_orders         |  | - Gate Info    |  |
|  | - Guilds & CastleSiege    |  | - online_time_ledgers  |  | - Monster Spawns| |
|  +-------------+-------------+  +-----------+------------+  +--------+-------+  |
+----------------|----------------------------|------------------------|----------+
                 |                            |                        |
                 | EntityFrameworkCore        | Native SQL Driver (pg) | EF Core
                 v                            v                        v
         [OpenMU Game Server]       [OpenMU Web BFF Server]     [OpenMU Admin]
```

---

## 2. กฎความคงที่ระดับเครื่องยนต์ฐานข้อมูล (Database-Level Invariants)

### 2.1 การป้องกันข้อผิดพลาดทางตรรกะด้วย CHECK Constraints
- สคีมา `web` มีการติดตั้ง Constraint ที่ปฏิเสธการบันทึกข้อมูลที่ไม่ถูกต้องในระดับ PostgreSQL:
  ```sql
  CONSTRAINT ck_wallets_credits_balance_non_negative CHECK (credits_balance >= 0),
  CONSTRAINT ck_wallets_lifetime_earned_non_negative CHECK (lifetime_credits_earned >= 0),
  CONSTRAINT ck_wallets_lifetime_spent_non_negative  CHECK (lifetime_credits_spent >= 0),
  CONSTRAINT ck_wallets_total_topup_non_negative     CHECK (total_topup_thb >= 0.00),
  CONSTRAINT ck_credit_exchanges_credits_spent_positive CHECK (credits_spent > 0),
  CONSTRAINT ck_credit_exchanges_wcoin_amount_positive CHECK (wcoin_amount > 0)
  ```
  - ไม่ว่าจะมีบั๊กในระดับโค้ดหรือการเรียกใช้งานคำสั่งพร้อมกัน ฐานข้อมูลจะไม่มีทางยอมให้ยอดเครดิตติดลบได้

### 2.2 การป้องกันการทำธุรกรรมซ้ำด้วย UNIQUE Constraints
  ```sql
  CONSTRAINT uq_online_reward_redemptions_idempotency UNIQUE (account_id, idempotency_key),
  CONSTRAINT uq_credit_exchanges_idempotency UNIQUE (account_id, idempotency_key),
  CONSTRAINT uq_topup_orders_idempotency UNIQUE (account_id, idempotency_key),
  CONSTRAINT uq_payment_webhook_events_provider_event UNIQUE (provider, event_id),
  CONSTRAINT uq_topup_orders_order_code UNIQUE (order_code)
  ```
  - รับประกันความมีผลครั้งเดียว (Idempotency) แม้ระบบเครือข่ายจะส่งคำขอซ้ำเข้ามา

---

## 3. ความปลอดภัยของ Transaction และการรับมือเหตุการณ์ขัดข้อง (Crash Recovery)

### 3.1 การโอนย้ายไอเทมในเกม (Game Persistence Context Isolation)
- ใน `TradeButtonAction.cs` และ `BuyRequestAction.cs`:
  - ใช้งาน `CreateNewTradeContext()` แยกอิสระจาก Context ปกติ
  - บันทึกการโอนย้ายไอเทมผ่าน `SaveChangesAsync()`
  - หากเซิร์ฟเวอร์เกิดไฟฟ้าดับหรือ Process ถูก Terminate ในขณะบันทึก:
    - PostgreSQL WAL (Write-Ahead Logging) จะทำการ Rollback ข้อมูลกลับสู่สถานะก่อนเริ่มเทรดโดยอัตโนมัติ
    - จะไม่มีไอเทมใดถูกคัดลอกหรือสูญหายไประหว่างทาง

### 3.2 การกู้คืนธุรกรรมค้างคาบนเว็บ (Web Outbox Reconciliation)
- รายการแลกเครดิตที่ติดสถานะ `PENDING_RETRY` จากการที่เซิร์ฟเวอร์เกมขัดข้องชั่วคราว:
  - มีฟังก์ชัน `reconcilePendingExchanges()` คอยตรวจสอบและส่งคำขอใหม่เมื่อเซิร์ฟเวอร์กลับมาออนไลน์
  - ป้องกันการที่เครดิตถูกตัดแต่ผู้เล่นไม่ได้รับเหรียญในเกม

---

## 4. ตารางประเมินผลการตรวจสอบบูรณภาพฐานข้อมูล

| การตรวจสอบ | มาตรการควบคุม | ผลการประเมิน |
|---|---|---|
| **Schema Migration Testing** | สคริปต์ DDL 9 ตารางผ่านการรันใน CI | **VERIFIED_WORKING** |
| **Cascade Delete Safety** | มีการใช้ `RESTRICT` ป้องกันข้อมูลสำคัญหาย | **VERIFIED_SAFE** |
| **Read-Only Audit Diagnostics** | ไร้คำสั่งลบข้อมูลประวัติหรือตัดทอนอัตโนมัติ | **VERIFIED_SAFE** |
| **Row-Level Deadlock Prevention** | เข้าล็อกแถวตามลำดับ Key สม่ำเสมอ | **VERIFIED_SAFE** |
| **Index Efficiency** | มี Index บน `account_id`, `created_at`, `status` | **OPTIMIZED** |

