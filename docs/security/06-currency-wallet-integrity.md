# 06. รายงานความสมบูรณ์ของระบบเงินและกระเป๋าเงินเว็บ (Currency & Wallet Integrity)

**โครงการ**: OpenMU-Origins & OpenMU-Web  
**วันที่ตรวจสอบ**: 13 พฤศจิกายน 2569  
**สถานะการประเมิน**: ผ่านเกณฑ์ความถูกต้องทางบัญชีระดับสมบูรณ์ (Double-Entry Ledger Verified)  
**ขอบเขต**: Zen, WCoinC, Goblin Points, Website Credits, Ledger Constraints, Row-Level Locking  

---

## 1. ภาพรวมสถาปัตยกรรมระบบการเงิน (Financial Architecture Overview)

ระบบ OpenMU มีการจำแนกสกุลเงินออกเป็น 2 ชั้นอย่างชัดเจน:
1. **สกุลเงินในเกม (Game Currencies)**:
   - **Zen**: เงินพื้นฐานสำหรับซื้อขายไอเทม NPC, ซ่อมแซม, วาร์ป, และตั้งร้านค้าส่วนตัว (จำกัดสูงสุด 2,000,000,000 Zen ต่อตัวละคร / คลัง)
   - **WCoinC (Cash Shop Coins)**: เหรียญพรีเมียมสำหรับซื้อแพ็กเกจใน Cash Shop บันทึกอยู่ในสคีมาเกม (`BasicModel.CashShopCoinGrant`)
   - **Goblin Points / WCoinP**: แต้มพิเศษจากกิจกรรมหรือเวลาออนไลน์
2. **สกุลเงินบนเว็บ (Website Credits)**:
   - บันทึกอยู่ในสคีมาฐานข้อมูล PostgreSQL `web.wallets`
   - แหล่งที่มา: เติมเงินผ่าน PromptPay/PayPal (P0-C) หรือ แลกรับจากเวลาออนไลน์ (P0-A)
   - การใช้งาน: แลกเปลี่ยนเป็น WCoinC ในเกม (P0-B)

---

## 2. กฎความคงที่ระดับฐานข้อมูล (Database Financial Invariants)

เพื่อรับประกันว่าจะไม่มีทางเกิดยอดเงินติดลบ หรือเงินงอกขึ้นมาลอยๆ ระบบฐานข้อมูลได้กำหนดข้อจำกัดระดับ DDL ใน `server/db/migrations/001_initial_web_schema.sql`:

```sql
CREATE TABLE web.wallets (
    account_id              VARCHAR(32) PRIMARY KEY,
    credits_balance         BIGINT NOT NULL DEFAULT 0,
    lifetime_credits_earned BIGINT NOT NULL DEFAULT 0,
    lifetime_credits_spent  BIGINT NOT NULL DEFAULT 0,
    total_topup_thb         NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    redeemed_online_seconds BIGINT NOT NULL DEFAULT 0,
    version                 BIGINT NOT NULL DEFAULT 0,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_wallets_credits_balance_non_negative CHECK (credits_balance >= 0),
    CONSTRAINT ck_wallets_lifetime_earned_non_negative CHECK (lifetime_credits_earned >= 0),
    CONSTRAINT ck_wallets_lifetime_spent_non_negative  CHECK (lifetime_credits_spent >= 0),
    CONSTRAINT ck_wallets_total_topup_non_negative     CHECK (total_topup_thb >= 0.00),
    CONSTRAINT ck_wallets_redeemed_seconds_non_negative CHECK (redeemed_online_seconds >= 0)
);
```

### การป้องกันยอดเงินติดลบ (No Negative Balances)
- ข้อกำหนด `CHECK (credits_balance >= 0)` จะทำการแทรกแซงในระดับ PostgreSQL Engine
- หากมีบั๊กในโค้ดหรือการเรียกใช้งานพร้อมกันที่พยายามหักเครดิตเกินกว่ายอดคงเหลือ คำสั่ง SQL จะถูก Reject ด้วย Constraint Violation ทันที ธุรกรรมทั้งหมดจะ Rollback อัตโนมัติ

---

## 3. การล็อกแถวเพื่อป้องกันการแย่งใช้ทรัพยากร (Row-Level Locking)

ใน `server/db/postgresStore.mjs` ทุกการเปลี่ยนแปลงยอดเครดิต (Debit หรือ Credit) ต้องดำเนินการผ่าน `SELECT ... FOR UPDATE`:

```javascript
const { rows: [wallet] } = await client.query(
  `SELECT * FROM web.wallets WHERE account_id = $1 FOR UPDATE`,
  [loginName]
);

if (wallet.credits_balance < creditsCost) {
  throw new StoreError(400, "INSUFFICIENT_CREDITS", "Insufficient credit balance.");
}
```

### ผลกระทบด้านความปลอดภัย:
- หากผู้ใช้เปิด 2 แท็บพร้อมกัน แล้วกดแลกเครดิต 100 เครดิตในเวลาเดียวกัน
- คำขอที่ 1 จะล็อกแถวบัญชีไว้ในฐานข้อมูล คำขอที่ 2 จะต้องหยุดรอจนกว่าคำขอที่ 1 จะ Commit เสร็จสิ้น
- เมื่อคำขอที่ 2 อ่านข้อมูลใหม่ ยอดเงินจะเหลือไม่พอ และถูกตีกลับด้วยรหัส `INSUFFICIENT_CREDITS`
- ป้องกันการใช้จ่ายซ้ำซ้อน (Double Spending) ได้ 100%

---

## 4. บันทึกบัญชีแยกประเภทแบบหักลบได้ (Double-Entry Ledger & Audit Log)

ทุกการเปลี่ยนแปลงของยอดเครดิตจะต้องมีเรคอร์ดคู่ขนานในตารางประวัติธุรกรรมเสมอ:
- **`web.credit_transactions`**:
  - บันทึก `delta_credits` (ค่าบวกสำหรับการเติมเงิน/รับโบนัส, ค่าลบสำหรับการแลกของ)
  - บันทึก `balance_after` เพื่อให้สามารถทำ Audit Reconcile ย้อนหลังได้
  - บันทึก `reference_type` (`ONLINE_REWARD`, `CREDIT_EXCHANGE`, `TOPUP_ORDER`) และ `reference_id`
- **`web.audit_logs`**:
  - บันทึก Actor Type, Actor Name, IP Address, Action, Outcome, Timestamp
  - ไม่สามารถแก้ไขหรือลบได้ (Append-only)

---

## 5. ตารางสรุปการประเมินความสมบูรณ์ของระบบเงิน

| หัวข้อตรวจสอบ | กลไกควบคุม | ผลการทดสอบ |
|---|---|---|
| **Negative Balance Prevention** | SQL CHECK Constraint (`credits_balance >= 0`) | **ผ่าน**: ไม่มียอดติดลบ |
| **Concurrent Double Debit** | `SELECT ... FOR UPDATE` Row Locking | **ผ่าน**: ป้องกันการกดซ้อนได้ 100% |
| **Audit Traceability** | `web.credit_transactions` & `web.audit_logs` | **ผ่าน**: ตรวจสอบย้อนกลับได้ทุกบาท |
| **Game Zen Overflow** | `MoneyOverflowOrNotEnoughSpace` check | **ผ่าน**: ป้องกัน Zen ทะลุ 2,000,000,000 |
| **Idempotent Spending** | `UNIQUE (idempotency_key)` ในธุรกรรมแลกเครดิต | **ผ่าน**: สั่งซ้ำไม่หักเงินเพิ่ม |

