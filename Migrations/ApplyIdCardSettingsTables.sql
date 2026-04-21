-- Crea las tablas del módulo de carnets (school_id_card_settings, id_card_template_fields)
-- cuando la BD ya existe pero esta migración no se aplicó.
-- Ejecutar en la base de datos PostgreSQL conectada por la app.

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Tabla de campos de plantilla (depende de schools)
CREATE TABLE IF NOT EXISTS id_card_template_fields (
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    school_id uuid NOT NULL,
    field_key character varying(50) NOT NULL,
    is_enabled boolean NOT NULL DEFAULT true,
    x_mm numeric(6,2) NOT NULL DEFAULT 0,
    y_mm numeric(6,2) NOT NULL DEFAULT 0,
    w_mm numeric(6,2) NOT NULL DEFAULT 0,
    h_mm numeric(6,2) NOT NULL DEFAULT 0,
    font_size numeric(4,2) NOT NULL DEFAULT 10,
    font_weight character varying(20) NOT NULL DEFAULT 'Normal',
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT id_card_template_fields_pkey PRIMARY KEY (id),
    CONSTRAINT id_card_template_fields_school_id_fkey FOREIGN KEY (school_id)
        REFERENCES schools (id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_id_card_template_fields_field ON id_card_template_fields (field_key);
CREATE INDEX IF NOT EXISTS ix_id_card_template_fields_school ON id_card_template_fields (school_id);

-- Tabla de configuración de carnets por escuela
CREATE TABLE IF NOT EXISTS school_id_card_settings (
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    school_id uuid NOT NULL,
    template_key character varying(50) NOT NULL DEFAULT 'default_v1',
    page_width_mm integer NOT NULL DEFAULT 54,
    page_height_mm integer NOT NULL DEFAULT 86,
    bleed_mm integer NOT NULL DEFAULT 0,
    background_color character varying(20) NOT NULL DEFAULT '#FFFFFF',
    primary_color character varying(20) NOT NULL DEFAULT '#0D6EFD',
    text_color character varying(20) NOT NULL DEFAULT '#111111',
    show_qr boolean NOT NULL DEFAULT true,
    show_photo boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT school_id_card_settings_pkey PRIMARY KEY (id),
    CONSTRAINT school_id_card_settings_school_id_fkey FOREIGN KEY (school_id)
        REFERENCES schools (id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_school_id_card_settings_school_id"
    ON school_id_card_settings (school_id);

-- Sincronizar historial de migraciones (la BD ya tenía el resto del esquema)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES
    ('20251102175646_AddPaymentModuleComplete', '9.0.3'),
    ('20251115111847_CompletePrematriculationModule', '9.0.3'),
    ('20251115115232_AddAcademicYearSupport', '9.0.3'),
    ('20260117093532_AddStudentIdModule', '9.0.3'),
    ('20260117095203_AddIdCardSettingsAndTemplates', '9.0.3')
ON CONFLICT ("MigrationId") DO NOTHING;
