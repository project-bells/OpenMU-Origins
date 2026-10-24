BEGIN;

CREATE TABLE IF NOT EXISTS config."OriginsAppliedChange"
(
    "Key" text PRIMARY KEY,
    "AppliedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- The current executable default is authoritative for Golden Invasion. Clearing
-- a custom value makes the plug-in use the hourly timetable and 10 of each mob.
UPDATE config."PlugInConfiguration"
SET "IsActive" = TRUE,
    "CustomConfiguration" = NULL
WHERE "TypeId" = '06d18a9e-2919-4c17-9dbc-6e4f7756495c';

-- Kundun (Kalima 7) respawns after one hour.
UPDATE config."MonsterDefinition"
SET "RespawnDelay" = INTERVAL '1 hour',
    "NumberOfMaximumItemDrops" = 1
WHERE "Number" = 275;

-- Keep only the configured equipment set families in Kundun's guaranteed drop
-- group and make it an excellent item drop with 100 percent probability.
CREATE TEMP TABLE kundun_drop_groups ON COMMIT DROP AS
SELECT DISTINCT links."DropItemGroupId" AS "Id"
FROM config."MonsterDefinition" monster
JOIN config."MonsterDefinitionDropItemGroup" links
  ON links."MonsterDefinitionId" = monster."Id"
WHERE monster."Number" = 275;

UPDATE config."DropItemGroup" drops
SET "Chance" = 1.0,
    "ItemType" = 2
FROM kundun_drop_groups kundun
WHERE drops."Id" = kundun."Id";

DELETE FROM config."DropItemGroupItemDefinition" links
USING kundun_drop_groups kundun
WHERE links."DropItemGroupId" = kundun."Id";

INSERT INTO config."DropItemGroupItemDefinition" ("DropItemGroupId", "ItemDefinitionId")
SELECT kundun."Id", item."Id"
FROM kundun_drop_groups kundun
CROSS JOIN config."ItemDefinition" item
WHERE item."Group" BETWEEN 7 AND 11
  AND item."Number" IN (16, 17, 18, 19, 20, 27, 43, 60)
ON CONFLICT DO NOTHING;

DO $changes$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM config."OriginsAppliedChange"
        WHERE "Key" = '2026-10-24-jewel-30-and-upgrade-10'
    ) THEN
        -- Increase every drop group which can produce a jewel or gemstone by
        -- 30 percent relative to its current chance, capped at 100 percent.
        UPDATE config."DropItemGroup" drops
        SET "Chance" = LEAST(1.0, drops."Chance" * 1.30)
        WHERE EXISTS
        (
            SELECT 1
            FROM config."DropItemGroupItemDefinition" links
            JOIN config."ItemDefinition" item
              ON item."Id" = links."ItemDefinitionId"
            WHERE links."DropItemGroupId" = drops."Id"
              AND (item."Group", item."Number") IN
                  ((12, 15), (14, 13), (14, 14), (14, 16),
                   (14, 22), (14, 31), (14, 41), (14, 42))
        );

        UPDATE config."ItemDropItemGroup" drops
        SET "Chance" = LEAST(1.0, drops."Chance" * 1.30)
        WHERE EXISTS
        (
            SELECT 1
            FROM config."ItemDropItemGroupItemDefinition" links
            JOIN config."ItemDefinition" item
              ON item."Id" = links."ItemDefinitionId"
            WHERE links."ItemDropItemGroupId" = drops."Id"
              AND (item."Group", item."Number") IN
                  ((12, 15), (14, 13), (14, 14), (14, 16),
                   (14, 22), (14, 31), (14, 41), (14, 42))
        );

        -- +11, +12, +13, +14 and +15 recipes. The ten points are added to
        -- the base rate; luck and item-type modifiers still apply normally.
        UPDATE config."SimpleCraftingSettings" settings
        SET "SuccessPercent" = LEAST(100, settings."SuccessPercent" + 10)
        FROM config."ItemCrafting" crafting
        WHERE crafting."SimpleCraftingSettingsId" = settings."Id"
          AND crafting."Number" IN (4, 22, 23, 49, 50);

        INSERT INTO config."OriginsAppliedChange" ("Key")
        VALUES ('2026-10-24-jewel-30-and-upgrade-10');
    END IF;
END
$changes$;

COMMIT;

SELECT "Number", "RespawnDelay", "NumberOfMaximumItemDrops"
FROM config."MonsterDefinition"
WHERE "Number" = 275;

SELECT crafting."Number", settings."SuccessPercent"
FROM config."ItemCrafting" crafting
JOIN config."SimpleCraftingSettings" settings
  ON settings."Id" = crafting."SimpleCraftingSettingsId"
WHERE crafting."Number" IN (4, 22, 23, 49, 50)
ORDER BY crafting."Number";
