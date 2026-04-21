-- Agregar columna id_card_policy a schools (política del carnet, única por escuela).
-- Ejecutar en local y Render si no se aplica la migración EF.
-- Idempotente.

ALTER TABLE schools ADD COLUMN IF NOT EXISTS id_card_policy text NULL;
