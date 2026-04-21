-- Agregar columna orientation a school_id_card_settings (Vertical / Horizontal).
-- Ejecutar en local y Render si no se aplica la migración EF.
-- Idempotente.

ALTER TABLE school_id_card_settings
ADD COLUMN IF NOT EXISTS orientation character varying(20) NOT NULL DEFAULT 'Vertical';
