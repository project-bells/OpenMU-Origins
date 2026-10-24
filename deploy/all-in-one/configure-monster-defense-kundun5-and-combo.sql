-- Applies the Origins monster-defense and Box of Kundun +4/+5 rebalance.
-- Safe to execute repeatedly; the one-time changes are tracked in the database.
BEGIN;

CREATE TABLE IF NOT EXISTS config."OriginsAppliedChange"
(
    "Key" text PRIMARY KEY,
    "AppliedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

DO $changes$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1 FROM config."OriginsAppliedChange"
        WHERE "Key" = '2026-10-24-monster-defense-minus-30'
    ) THEN
        UPDATE config."MonsterAttribute" attribute
        SET "Value" = attribute."Value" * 0.70
        FROM config."MonsterDefinition" monster
        WHERE monster."Id" = attribute."MonsterDefinitionId"
          AND monster."NpcWindow" = 0
          AND attribute."AttributeDefinitionId" = 'eb098c46-60d4-4ca6-bbd4-5b6270a1407b';

        INSERT INTO config."OriginsAppliedChange" ("Key")
        VALUES ('2026-10-24-monster-defense-minus-30');
    END IF;
END
$changes$;

CREATE TEMP TABLE kundun_groups ON COMMIT DROP AS
SELECT drops."Id", drops."SourceItemLevel"
FROM config."ItemDropItemGroup" drops
JOIN config."ItemDefinition" box ON box."Id" = drops."ItemDefinitionId"
WHERE box."Group" = 14
  AND box."Number" = 11
  AND drops."SourceItemLevel" IN (11, 12)
  AND drops."ItemType" = 2;

DO $changes$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1 FROM config."OriginsAppliedChange"
        WHERE "Key" = '2026-10-24-kundun-plus5-level380'
    ) THEN
        -- Preserve the previous +5 pool by adding it to +4 first.
        INSERT INTO config."ItemDropItemGroupItemDefinition"
            ("ItemDropItemGroupId", "ItemDefinitionId")
        SELECT plus4."Id", links."ItemDefinitionId"
        FROM kundun_groups plus4
        CROSS JOIN kundun_groups plus5
        JOIN config."ItemDropItemGroupItemDefinition" links
          ON links."ItemDropItemGroupId" = plus5."Id"
        WHERE plus4."SourceItemLevel" = 11
          AND plus5."SourceItemLevel" = 12
        ON CONFLICT DO NOTHING;

        DELETE FROM config."ItemDropItemGroupItemDefinition" links
        USING kundun_groups plus5
        WHERE plus5."SourceItemLevel" = 12
          AND links."ItemDropItemGroupId" = plus5."Id";

        INSERT INTO config."ItemDropItemGroupItemDefinition"
            ("ItemDropItemGroupId", "ItemDefinitionId")
        SELECT plus5."Id", item."Id"
        FROM kundun_groups plus5
        CROSS JOIN config."ItemDefinition" item
        WHERE plus5."SourceItemLevel" = 12
          AND
          (
              (item."Group", item."Number") IN
                  ((0,22), (0,23), (0,26), (0,27), (0,28), (0,35),
                   (2,14), (4,21),
                   (5,12), (5,19), (5,20), (5,30), (5,31))
              OR
              (item."Group" BETWEEN 7 AND 11
               AND item."Number" IN (29,30,31,32,33,43,73))
          )
        ON CONFLICT DO NOTHING;

        INSERT INTO config."OriginsAppliedChange" ("Key")
        VALUES ('2026-10-24-kundun-plus5-level380');
    END IF;
END
$changes$;

UPDATE config."ItemDropItemGroup" drops
SET "Chance" = 1.0,
    "ItemType" = 2
FROM kundun_groups selected
WHERE selected."Id" = drops."Id";

COMMIT;

SELECT COUNT(*) AS "ReducedMonsterDefenseRows"
FROM config."MonsterAttribute"
WHERE "AttributeDefinitionId" = 'eb098c46-60d4-4ca6-bbd4-5b6270a1407b';

SELECT drops."SourceItemLevel" - 7 AS "KundunPlus",
       COUNT(links."ItemDefinitionId") AS "PossibleItems"
FROM config."ItemDropItemGroup" drops
JOIN config."ItemDefinition" box ON box."Id" = drops."ItemDefinitionId"
LEFT JOIN config."ItemDropItemGroupItemDefinition" links
  ON links."ItemDropItemGroupId" = drops."Id"
WHERE box."Group" = 14
  AND box."Number" = 11
  AND drops."SourceItemLevel" IN (11, 12)
  AND drops."ItemType" = 2
GROUP BY drops."SourceItemLevel"
ORDER BY drops."SourceItemLevel";
