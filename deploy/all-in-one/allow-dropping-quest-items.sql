BEGIN;

UPDATE config."ItemDefinition"
SET "IsDroppable" = TRUE
WHERE "Group" = 14
  AND "Number" IN (23, 24, 25, 26, 65, 66, 67, 68)
  AND "IsQuestItem" = TRUE;

COMMIT;
