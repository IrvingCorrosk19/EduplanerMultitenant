-- Agregar columna show_watermark a school_id_card_settings (marca de agua con logo del colegio).
-- Idempotente. Ejecutar en local y Render si no se aplica la migración EF.

ALTER TABLE school_id_card_settings
ADD COLUMN IF NOT EXISTS show_watermark boolean NOT NULL DEFAULT true;
