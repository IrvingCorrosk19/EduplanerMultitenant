-- Columnas de la migración ExtendIdCardUserAndSettings que pueden faltar en Render.
-- Ejecutar en la BD donde falla (ej. Render). Idempotente: no falla si ya existen.

-- users: allergies, emergency_contact_*
ALTER TABLE users ADD COLUMN IF NOT EXISTS allergies character varying(500) NULL;
ALTER TABLE users ADD COLUMN IF NOT EXISTS emergency_contact_name character varying(200) NULL;
ALTER TABLE users ADD COLUMN IF NOT EXISTS emergency_contact_phone character varying(30) NULL;
ALTER TABLE users ADD COLUMN IF NOT EXISTS emergency_relationship character varying(50) NULL;

-- school_id_card_settings: show_allergies, show_emergency_contact, show_school_phone
ALTER TABLE school_id_card_settings ADD COLUMN IF NOT EXISTS show_allergies boolean NOT NULL DEFAULT false;
ALTER TABLE school_id_card_settings ADD COLUMN IF NOT EXISTS show_emergency_contact boolean NOT NULL DEFAULT false;
ALTER TABLE school_id_card_settings ADD COLUMN IF NOT EXISTS show_school_phone boolean NOT NULL DEFAULT true;
