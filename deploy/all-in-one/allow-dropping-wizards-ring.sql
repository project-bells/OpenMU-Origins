BEGIN;

-- Wizard's Ring (group 13, number 20) is character-bound, but players must
-- still be able to discard it when they no longer need it.
UPDATE config."ItemDefinition"
SET "IsDroppable" = TRUE
WHERE "Group" = 13
  AND "Number" = 20;

COMMIT;
