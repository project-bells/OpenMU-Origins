BEGIN;

-- The Class 3 quest can be accepted and its maps can work even when this
-- plug-in configuration is missing. Without it, monster kills are not counted.
UPDATE config."PlugInConfiguration"
SET "IsActive" = TRUE
WHERE "TypeId" = '416c2231-d7fe-414a-9321-26622e262ef5';

INSERT INTO config."PlugInConfiguration"
(
    "Id",
    "TypeId",
    "IsActive",
    "GameConfigurationId",
    "CustomConfiguration",
    "CustomPlugInSource",
    "ExternalAssemblyName"
)
SELECT
    '416c2231-d7fe-414a-9321-26622e262ef5'::uuid,
    '416c2231-d7fe-414a-9321-26622e262ef5'::uuid,
    TRUE,
    server."GameConfigurationId",
    NULL,
    NULL,
    NULL
FROM config."GameServerDefinition" server
WHERE server."GameConfigurationId" IS NOT NULL
  AND NOT EXISTS
  (
      SELECT 1
      FROM config."PlugInConfiguration" existing
      WHERE existing."TypeId" = '416c2231-d7fe-414a-9321-26622e262ef5'
  )
ORDER BY server."ServerID"
LIMIT 1
ON CONFLICT ("Id") DO UPDATE
SET "IsActive" = TRUE,
    "TypeId" = EXCLUDED."TypeId",
    "GameConfigurationId" = EXCLUDED."GameConfigurationId";

DO $validation$
DECLARE
    missing_quests integer;
    invalid_requirements integer;
    missing_monsters integer;
    missing_spawns integer;
BEGIN
    SELECT COUNT(*) INTO missing_quests
    FROM (VALUES (5), (6)) expected(number)
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM config."QuestDefinition" quest
        WHERE quest."Group" = 0
          AND quest."Number" = expected.number
    );

    SELECT COUNT(*) INTO invalid_requirements
    FROM (VALUES (5, 409, 20), (5, 410, 20), (5, 411, 20), (6, 412, 1)) expected(quest_number, monster_number, kill_count)
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM config."QuestMonsterKillRequirement" requirement
        JOIN config."QuestDefinition" quest ON quest."Id" = requirement."QuestDefinitionId"
        JOIN config."MonsterDefinition" monster ON monster."Id" = requirement."MonsterId"
        WHERE quest."Group" = 0
          AND quest."Number" = expected.quest_number
          AND monster."Number" = expected.monster_number
          AND requirement."MinimumNumber" = expected.kill_count
    );

    SELECT COUNT(*) INTO missing_monsters
    FROM (VALUES (409), (410), (411), (412)) expected(number)
    WHERE NOT EXISTS
    (
        SELECT 1 FROM config."MonsterDefinition" monster
        WHERE monster."Number" = expected.number
    );

    SELECT COUNT(*) INTO missing_spawns
    FROM (VALUES (41, 409), (41, 410), (41, 411), (42, 412)) expected(map_number, monster_number)
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM config."MonsterSpawnArea" spawn
        JOIN config."GameMapDefinition" map ON map."Id" = spawn."GameMapId"
        JOIN config."MonsterDefinition" monster ON monster."Id" = spawn."MonsterDefinitionId"
        WHERE map."Number" = expected.map_number
          AND monster."Number" = expected.monster_number
          AND spawn."Quantity" > 0
    );

    IF missing_quests > 0 OR invalid_requirements > 0 OR missing_monsters > 0 OR missing_spawns > 0 THEN
        RAISE EXCEPTION
            'Class 3 quest validation failed: missing quests %, invalid requirements %, missing monsters %, missing spawns %',
            missing_quests, invalid_requirements, missing_monsters, missing_spawns;
    END IF;
END
$validation$;

COMMIT;

SELECT
    plugin."TypeId",
    plugin."IsActive",
    plugin."GameConfigurationId"
FROM config."PlugInConfiguration" plugin
WHERE plugin."TypeId" = '416c2231-d7fe-414a-9321-26622e262ef5';

SELECT
    quest."Number" AS "QuestNumber",
    monster."Number" AS "MonsterNumber",
    requirement."MinimumNumber" AS "RequiredKills"
FROM config."QuestMonsterKillRequirement" requirement
JOIN config."QuestDefinition" quest ON quest."Id" = requirement."QuestDefinitionId"
JOIN config."MonsterDefinition" monster ON monster."Id" = requirement."MonsterId"
WHERE quest."Group" = 0
  AND quest."Number" IN (5, 6)
ORDER BY quest."Number", monster."Number";
