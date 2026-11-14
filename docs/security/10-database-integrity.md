# 10. รายงานความสมบูรณ์และการรักษาบูรณภาพของฐานข้อมูล (Database Integrity)

**โครงการ**: OpenMU-Origins & OpenMU-Web  
**วันที่ตรวจสอบ**: 13 พฤศจิกายน 2569  
**สถานะการประเมิน**: ผ่านเกณฑ์บูรณภาพข้อมูลระดับสูงสุด (ACID & Invariant Integrity Verified)  
**ขอบเขต**: PostgreSQL Database, Schema `data` (Game), Schema `web` (Website/Wallet), Migrations, Constraints  

---

## 1. การแบ่งแยกสคีมาฐานข้อมูล (Schema Separation & Architecture)

ฐานข้อมูล PostgreSQL ของระบบได้รับการออกแบบให้แยกเขตความรับผิดชอบอย่างชัดเจน:
1. **Schema `data`**:
   - ควบคุมสถานะเกม: `Account`, `Character`, `Item`, `ItemStorage`, `CashShopCoinGrant`
   - จัดการผ่าน EntityFramework Core Migrations ใน `src/Persistence/EntityFramework/Migrations/`
2. **Schema `web`**:
   - ควบคุมระบบเว็บและการเงิน: `wallets`, `wallet_ledger`, `credit_exchanges`, `topup_orders`, `online_time_ledgers`, `payment_webhook_events`, `web_sessions`, `web_audit_logs`
   - จัดการผ่าน DDL Migration Scripts ใน `server/db/migrations/001_initial_web_schema.sql`
3. **Schema `config`**:
   - ควบคุมการตั้งค่าระบบและ Game Rules

---

## 2. การควบคุมข้อกำหนดความคงที่ (Database Invariants & Constraints)

### 2.1 ข้อกำหนดตรวจเช็คค่าไม่ติดลบ (Non-Negative CHECK Constraints)
ในสคีมา `web` มีการบังคับใช้ Constraint ในระดับ Engine:
- `CONSTRAINT ck_wallets_credits_balance_non_negative CHECK (credits_balance >= 0)`
- `CONSTRAINT ck_wallets_lifetime_earned_non_negative CHECK (lifetime_credits_earned >= 0)`
- `CONSTRAINT ck_wallets_lifetime_spent_non_negative CHECK (lifetime_credits_spent >= 0)`
- `CONSTRAINT ck_wallets_total_topup_non_negative CHECK (total_topup_thb >= 0.00)`
- `CONSTRAINT ck_wallets_redeemed_seconds_non_negative CHECK (redeemed_online_seconds >= 0)`
- `CONSTRAINT ck_credit_exchanges_credits_spent_positive CHECK (credits_spent > 0)`
- `CONSTRAINT ck_credit_exchanges_wcoin_amount_positive CHECK (wcoin_amount > 0)`

**ประโยชน์**: ป้องกันข้อผิดพลาดทางตรรกะในระดับโค้ด ไม่ให้สามารถทำให้ยอดเงินของผู้เล่นติดลบได้ในทุกกรณี

### 2.2 ข้อกำหนดความเฉพาะเจาะจง (Unique Constraints & Idempotency)
- `CONSTRAINT uq_online_reward_redemptions_idempotency UNIQUE (account_id, idempotency_key)`
- `CONSTRAINT uq_credit_exchanges_idempotency UNIQUE (account_id, idempotency_key)`
- `CONSTRAINT uq_topup_orders_idempotency UNIQUE (account_id, idempotency_key)`
- `CONSTRAINT uq_payment_webhook_events_provider_event UNIQUE (provider, event_id)`
- `CONSTRAINT uq_topup_orders_order_code UNIQUE (order_code)`

**ประโยชน์**: ป้องกันการประมวลผลซ้ำซ้อนระดับฮาร์ดแวร์/เครือข่าย คำขอที่มี Idempotency Key ซ้ำจะถูกระงับทันที

### 2.3 ข้อกำหนดความสัมพันธ์ของข้อมูล (Foreign Key Constraints)
- ข้อมูลธุรกรรม (`credit_transactions`, `online_reward_redemptions`, `credit_exchanges`, `topup_orders`) ผูกเข้ากับ `account_id` ใน `web.wallets`
- มีการกำหนด `ON DELETE RESTRICT` หรือ Cascade ที่ปลอดภัย ป้องกันข้อมูลกำพร้า (Orphaned Records)

---

## 3. การทำธุรกรรมแบบอะตอมิกและการล็อกแถว (Atomic Transactions & Row Locking)

### 3.1 การทำธุรกรรมทางการเงิน (Financial ACID Isolation)
- ทุกกระบวนการหักหรือเพิ่มยอดเงินทำงานภายใต้ PostgreSQL Transaction:
  ```sql
  BEGIN;
  SELECT * FROM web.wallets WHERE account_id = $1 FOR UPDATE;
  -- คำนวณและตรวจสอบยอดคงเหลือ
  UPDATE web.wallets SET credits_balance = credits_balance - $2, ... WHERE account_id = $1;
  INSERT INTO web.credit_transactions (...);
  COMMIT;
  ```
- **Read Committed with Exclusive Row Lock**: ป้องกัน Dirty Reads, Non-Repeatable Reads, และ Phantom Updates อย่างเบ็ดเสร็จ

### 3.2 ความปลอดภัยของการจัดเก็บข้อมูลเกม (Game State Persistence Context)
- ฝั่งเซิร์ฟเวอร์เกมใช้งาน `IPersistenceContextProvider` ร่วมกับ EntityFramework Core
- การโอนย้ายไอเทมระหว่างการเทรดหรือร้านค้าส่วนตัวจะสร้าง `CreateNewTradeContext()` แยกต่างหาก
- หากกระบวนการบันทึกฐานข้อมูลล้มเหลว Transaction จะถูก Rollback และไม่มีการเปลี่ยนมือของไอเทม

---

## 4. ผลการตรวจสอบการโยกย้ายฐานข้อมูล (Migration Integrity)

- สคริปต์ `001_initial_web_schema.sql` ถูกทดสอบผ่านชุดทดสอบอัตโนมัติใน `app.test.mjs` (Subtest 2)
- ผลการทดสอบ: ตรวจสอบความถูกต้องของตารางทั้ง 9 ตาราง, Foreign Keys, Unique Keys, และ Check Constraints ทั้งหมด **ผ่าน 100% (Passed)**
- ไม่มีคำสั่ง DDL ที่เป็นอันตรายต่อข้อมูล (No Unsafe Drops or Truncates)

