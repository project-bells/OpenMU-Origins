BEGIN;

-- Ten times the original value (100), so equipped pets and mounts lose
-- durability about ten times more slowly without affecting armor or weapons.
UPDATE config."GameConfiguration"
SET "DamagePerOnePetDurability" = 1000
WHERE "Id" IN
(
    SELECT DISTINCT "GameConfigurationId"
    FROM config."GameServerDefinition"
    WHERE "GameConfigurationId" IS NOT NULL
);

-- Repair the Crywolf warp entry (warp index 26) when map 34 and its spawn gate
-- still exist. The initializer creates this gate at x 229..239, y 37..46.
WITH crywolf_target AS
(
    SELECT
        map."GameConfigurationId",
        gate."Id" AS "GateId"
    FROM config."GameMapDefinition" map
    JOIN config."ExitGate" gate
      ON gate."MapId" = map."Id"
    WHERE map."Number" = 34
      AND gate."IsSpawnGate" = TRUE
      AND gate."X1" = 229
      AND gate."Y1" = 37
)
UPDATE config."WarpInfo" warp
SET "Name" = 'Crywolf',
    "Costs" = 10000,
    "LevelRequirement" = 190,
    "GateId" = target."GateId"
FROM crywolf_target target
WHERE warp."GameConfigurationId" = target."GameConfigurationId"
  AND warp."Index" = 26;

WITH crywolf_target AS
(
    SELECT
        map."GameConfigurationId",
        gate."Id" AS "GateId"
    FROM config."GameMapDefinition" map
    JOIN config."ExitGate" gate
      ON gate."MapId" = map."Id"
    WHERE map."Number" = 34
      AND gate."IsSpawnGate" = TRUE
      AND gate."X1" = 229
      AND gate."Y1" = 37
)
INSERT INTO config."WarpInfo"
(
    "Id",
    "GameConfigurationId",
    "GateId",
    "Index",
    "Name",
    "Costs",
    "LevelRequirement"
)
SELECT
    gen_random_uuid(),
    target."GameConfigurationId",
    target."GateId",
    26,
    'Crywolf',
    10000,
    190
FROM crywolf_target target
WHERE NOT EXISTS
(
    SELECT 1
    FROM config."WarpInfo" warp
    WHERE warp."GameConfigurationId" = target."GameConfigurationId"
      AND warp."Index" = 26
);

COMMIT;

SELECT
    game."DamagePerOnePetDurability",
    map."Number" AS "CrywolfMap",
    warp."Index" AS "WarpIndex",
    warp."Name" AS "WarpName",
    warp."LevelRequirement"
FROM config."GameConfiguration" game
LEFT JOIN config."GameMapDefinition" map
  ON map."GameConfigurationId" = game."Id"
 AND map."Number" = 34
LEFT JOIN config."WarpInfo" warp
  ON warp."GameConfigurationId" = game."Id"
 AND warp."Index" = 26
WHERE game."Id" IN
(
    SELECT DISTINCT "GameConfigurationId"
    FROM config."GameServerDefinition"
    WHERE "GameConfigurationId" IS NOT NULL
);
