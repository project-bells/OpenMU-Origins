-- Restores the Season 6 tier pools of Box of Kundun +1 through +5.
-- It also guarantees an excellent item drop and removes the money fallback.
BEGIN;

CREATE TEMP TABLE kundun_tier_items
(
    "Tier" smallint NOT NULL,
    "Group" smallint NOT NULL,
    "Number" smallint NOT NULL,
    PRIMARY KEY ("Tier", "Group", "Number")
) ON COMMIT DROP;

INSERT INTO kundun_tier_items ("Tier", "Group", "Number") VALUES
    (1, 0, 0),
    (1, 0, 1),
    (1, 0, 2),
    (1, 0, 3),
    (1, 0, 4),
    (1, 1, 0),
    (1, 1, 1),
    (1, 1, 2),
    (1, 1, 3),
    (1, 2, 0),
    (1, 2, 1),
    (1, 2, 2),
    (1, 3, 1),
    (1, 3, 2),
    (1, 3, 5),
    (1, 3, 6),
    (1, 3, 7),
    (1, 4, 0),
    (1, 4, 1),
    (1, 4, 2),
    (1, 4, 8),
    (1, 4, 9),
    (1, 5, 0),
    (1, 5, 1),
    (1, 5, 2),
    (1, 5, 14),
    (1, 5, 15),
    (1, 6, 0),
    (1, 6, 1),
    (1, 6, 2),
    (1, 6, 3),
    (1, 6, 4),
    (1, 7, 5),
    (1, 8, 5),
    (1, 9, 5),
    (1, 10, 5),
    (1, 11, 5),
    (1, 7, 2),
    (1, 8, 2),
    (1, 9, 2),
    (1, 10, 2),
    (1, 11, 2),
    (1, 7, 10),
    (1, 8, 10),
    (1, 9, 10),
    (1, 10, 10),
    (1, 11, 10),
    (1, 7, 0),
    (1, 8, 0),
    (1, 9, 0),
    (1, 10, 0),
    (1, 11, 0),
    (1, 7, 11),
    (1, 8, 11),
    (1, 9, 11),
    (1, 10, 11),
    (1, 11, 11),
    (1, 7, 39),
    (1, 8, 39),
    (1, 9, 39),
    (1, 10, 39),
    (1, 11, 39),
    (1, 7, 40),
    (1, 8, 40),
    (1, 9, 40),
    (1, 10, 40),
    (1, 11, 40),
    (2, 0, 5),
    (2, 0, 6),
    (2, 0, 7),
    (2, 0, 8),
    (2, 0, 9),
    (2, 0, 10),
    (2, 1, 4),
    (2, 1, 5),
    (2, 1, 6),
    (2, 1, 7),
    (2, 2, 3),
    (2, 2, 4),
    (2, 2, 8),
    (2, 2, 9),
    (2, 3, 0),
    (2, 3, 3),
    (2, 3, 4),
    (2, 3, 8),
    (2, 4, 3),
    (2, 4, 4),
    (2, 4, 10),
    (2, 4, 11),
    (2, 4, 12),
    (2, 5, 3),
    (2, 5, 4),
    (2, 5, 16),
    (2, 6, 5),
    (2, 6, 6),
    (2, 6, 7),
    (2, 6, 8),
    (2, 13, 8),
    (2, 13, 9),
    (2, 13, 12),
    (2, 13, 13),
    (2, 7, 6),
    (2, 8, 6),
    (2, 9, 6),
    (2, 10, 6),
    (2, 11, 6),
    (2, 7, 8),
    (2, 8, 8),
    (2, 9, 8),
    (2, 10, 8),
    (2, 11, 8),
    (2, 7, 4),
    (2, 8, 4),
    (2, 9, 4),
    (2, 10, 4),
    (2, 11, 4),
    (2, 7, 7),
    (2, 8, 7),
    (2, 9, 7),
    (2, 10, 7),
    (2, 11, 7),
    (2, 7, 12),
    (2, 8, 12),
    (2, 9, 12),
    (2, 10, 12),
    (2, 11, 12),
    (2, 7, 13),
    (2, 8, 13),
    (2, 9, 13),
    (2, 10, 13),
    (2, 11, 13),
    (2, 7, 25),
    (2, 8, 25),
    (2, 9, 25),
    (2, 10, 25),
    (2, 11, 25),
    (2, 7, 41),
    (2, 8, 41),
    (2, 9, 41),
    (2, 10, 41),
    (2, 11, 41),
    (3, 0, 11),
    (3, 0, 12),
    (3, 0, 13),
    (3, 0, 14),
    (3, 0, 15),
    (3, 0, 32),
    (3, 1, 8),
    (3, 2, 5),
    (3, 2, 6),
    (3, 2, 7),
    (3, 2, 10),
    (3, 3, 9),
    (3, 4, 5),
    (3, 4, 6),
    (3, 4, 13),
    (3, 4, 14),
    (3, 5, 3),
    (3, 5, 5),
    (3, 5, 7),
    (3, 5, 17),
    (3, 5, 18),
    (3, 6, 9),
    (3, 6, 10),
    (3, 6, 11),
    (3, 6, 12),
    (3, 6, 14),
    (3, 13, 21),
    (3, 13, 22),
    (3, 13, 23),
    (3, 13, 24),
    (3, 13, 25),
    (3, 13, 26),
    (3, 13, 27),
    (3, 13, 28),
    (3, 7, 16),
    (3, 8, 16),
    (3, 9, 16),
    (3, 10, 16),
    (3, 11, 16),
    (3, 7, 17),
    (3, 8, 17),
    (3, 9, 17),
    (3, 10, 17),
    (3, 11, 17),
    (3, 7, 18),
    (3, 8, 18),
    (3, 9, 18),
    (3, 10, 18),
    (3, 11, 18),
    (3, 7, 19),
    (3, 8, 19),
    (3, 9, 19),
    (3, 10, 19),
    (3, 11, 19),
    (3, 7, 20),
    (3, 8, 20),
    (3, 9, 20),
    (3, 10, 20),
    (3, 11, 20),
    (3, 7, 27),
    (3, 8, 27),
    (3, 9, 27),
    (3, 10, 27),
    (3, 11, 27),
    (3, 7, 43),
    (3, 8, 43),
    (3, 9, 43),
    (3, 10, 43),
    (3, 11, 43),
    (3, 7, 60),
    (3, 8, 60),
    (3, 9, 60),
    (3, 10, 60),
    (3, 11, 60),
    (4, 0, 16),
    (4, 0, 17),
    (4, 0, 18),
    (4, 0, 19),
    (4, 0, 33),
    (4, 2, 11),
    (4, 2, 12),
    (4, 2, 13),
    (4, 3, 10),
    (4, 4, 16),
    (4, 4, 17),
    (4, 4, 18),
    (4, 4, 19),
    (4, 5, 8),
    (4, 5, 9),
    (4, 5, 10),
    (4, 5, 19),
    (4, 6, 13),
    (4, 6, 15),
    (4, 6, 16),
    (4, 7, 16),
    (4, 8, 16),
    (4, 9, 16),
    (4, 10, 16),
    (4, 11, 16),
    (4, 7, 17),
    (4, 8, 17),
    (4, 9, 17),
    (4, 10, 17),
    (4, 11, 17),
    (4, 7, 18),
    (4, 8, 18),
    (4, 9, 18),
    (4, 10, 18),
    (4, 11, 18),
    (4, 7, 19),
    (4, 8, 19),
    (4, 9, 19),
    (4, 10, 19),
    (4, 11, 19),
    (4, 7, 20),
    (4, 8, 20),
    (4, 9, 20),
    (4, 10, 20),
    (4, 11, 20),
    (4, 7, 27),
    (4, 8, 27),
    (4, 9, 27),
    (4, 10, 27),
    (4, 11, 27),
    (4, 7, 43),
    (4, 8, 43),
    (4, 9, 43),
    (4, 10, 43),
    (4, 11, 43),
    (4, 7, 60),
    (4, 8, 60),
    (4, 9, 60),
    (4, 10, 60),
    (4, 11, 60),
    (5, 0, 20),
    (5, 0, 21),
    (5, 0, 31),
    (5, 0, 34),
    (5, 2, 15),
    (5, 4, 20),
    (5, 5, 11),
    (5, 5, 13),
    (5, 7, 21),
    (5, 8, 21),
    (5, 9, 21),
    (5, 10, 21),
    (5, 11, 21),
    (5, 7, 22),
    (5, 8, 22),
    (5, 9, 22),
    (5, 10, 22),
    (5, 11, 22),
    (5, 7, 23),
    (5, 8, 23),
    (5, 9, 23),
    (5, 10, 23),
    (5, 11, 23),
    (5, 7, 24),
    (5, 8, 24),
    (5, 9, 24),
    (5, 10, 24),
    (5, 11, 24),
    (5, 7, 28),
    (5, 8, 28),
    (5, 9, 28),
    (5, 10, 28),
    (5, 11, 28),
    (5, 7, 34),
    (5, 8, 34),
    (5, 9, 34),
    (5, 10, 34),
    (5, 11, 34),
    (5, 7, 35),
    (5, 8, 35),
    (5, 9, 35),
    (5, 10, 35),
    (5, 11, 35),
    (5, 7, 36),
    (5, 8, 36),
    (5, 9, 36),
    (5, 10, 36),
    (5, 11, 36),
    (5, 7, 37),
    (5, 8, 37),
    (5, 9, 37),
    (5, 10, 37),
    (5, 11, 37),
    (5, 7, 38),
    (5, 8, 38),
    (5, 9, 38),
    (5, 10, 38),
    (5, 11, 38),
    (5, 7, 61),
    (5, 8, 61),
    (5, 9, 61),
    (5, 10, 61),
    (5, 11, 61);

CREATE TEMP TABLE kundun_groups ON COMMIT DROP AS
SELECT
    drops."Id" AS "DropGroupId",
    drops."SourceItemLevel" - 7 AS "Tier",
    box."GameConfigurationId"
FROM config."ItemDropItemGroup" drops
JOIN config."ItemDefinition" box
    ON box."Id" = drops."ItemDefinitionId"
WHERE box."Group" = 14
  AND box."Number" = 11
  AND drops."SourceItemLevel" BETWEEN 8 AND 12
  AND drops."ItemType" = 2;

DO $check$
DECLARE
    group_count integer;
    missing_count integer;
BEGIN
    SELECT COUNT(*) INTO group_count FROM kundun_groups;
    IF group_count <> 5 THEN
        RAISE EXCEPTION 'Expected 5 excellent Kundun groups, found %', group_count;
    END IF;

    SELECT COUNT(*) INTO missing_count
    FROM kundun_tier_items requested
    JOIN kundun_groups groups ON groups."Tier" = requested."Tier"
    LEFT JOIN config."ItemDefinition" item
      ON item."GameConfigurationId" = groups."GameConfigurationId"
     AND item."Group" = requested."Group"
     AND item."Number" = requested."Number"
    WHERE item."Id" IS NULL;

    IF missing_count > 0 THEN
        RAISE WARNING 'Tier configuration references % missing item definitions; these entries will be skipped', missing_count;
    END IF;
END
$check$;

-- Print definitions which don't exist in the currently installed game configuration.
-- Database versions can legitimately differ, so missing entries are skipped below.
SELECT DISTINCT
    requested."Tier" AS "KundunPlus",
    requested."Group" AS "MissingItemGroup",
    requested."Number" AS "MissingItemNumber"
FROM kundun_tier_items requested
JOIN kundun_groups groups ON groups."Tier" = requested."Tier"
LEFT JOIN config."ItemDefinition" item
  ON item."GameConfigurationId" = groups."GameConfigurationId"
 AND item."Group" = requested."Group"
 AND item."Number" = requested."Number"
WHERE item."Id" IS NULL
ORDER BY requested."Tier", requested."Group", requested."Number";

-- Every Kundun box selects its excellent group with 100 percent probability.
UPDATE config."ItemDropItemGroup" drops
SET "Chance" = 1.0,
    "ItemType" = 2,
    "MoneyAmount" = 0
FROM config."ItemDefinition" box
WHERE box."Id" = drops."ItemDefinitionId"
  AND box."Group" = 14
  AND box."Number" = 11
  AND drops."SourceItemLevel" BETWEEN 8 AND 12
  AND drops."ItemType" = 2;

-- Remove only the money fallbacks of Box of Kundun +1 through +5.
DELETE FROM config."ItemDropItemGroup" drops
USING config."ItemDefinition" box
WHERE box."Id" = drops."ItemDefinitionId"
  AND box."Group" = 14
  AND box."Number" = 11
  AND drops."SourceItemLevel" BETWEEN 8 AND 12
  AND drops."ItemType" = 5;

-- Remove the accidental all-items pool, then restore the intended tier pool.
DELETE FROM config."ItemDropItemGroupItemDefinition" links
USING kundun_groups groups
WHERE links."ItemDropItemGroupId" = groups."DropGroupId";

INSERT INTO config."ItemDropItemGroupItemDefinition"
(
    "ItemDropItemGroupId",
    "ItemDefinitionId"
)
SELECT
    groups."DropGroupId",
    item."Id"
FROM kundun_groups groups
JOIN kundun_tier_items requested
    ON requested."Tier" = groups."Tier"
JOIN config."ItemDefinition" item
    ON item."GameConfigurationId" = groups."GameConfigurationId"
   AND item."Group" = requested."Group"
   AND item."Number" = requested."Number"
ON CONFLICT DO NOTHING;

COMMIT;

