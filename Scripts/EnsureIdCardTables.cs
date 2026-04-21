using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Crea las tablas del módulo de carnets si no existen (útil cuando la BD
/// no tiene aplicada la migración AddIdCardSettingsAndTemplates).
/// </summary>
public static class EnsureIdCardTables
{
    public static async Task EnsureAsync(SchoolDbContext context)
    {
        try
        {
            var exists = await context.Database.SqlQueryRaw<int>(
                @"SELECT 1 FROM information_schema.tables 
                  WHERE table_schema = 'public' AND table_name = 'school_id_card_settings' 
                  LIMIT 1").ToListAsync();
            if (exists.Count == 0)
            {
                await context.Database.ExecuteSqlRawAsync(@"
CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";

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

CREATE TABLE IF NOT EXISTS school_id_card_settings (
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    school_id uuid NOT NULL,
    template_key character varying(50) NOT NULL DEFAULT 'default_v1',
    page_width_mm integer NOT NULL DEFAULT 55,
    page_height_mm integer NOT NULL DEFAULT 85,
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
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_school_id_card_settings_school_id"" ON school_id_card_settings (school_id);

INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
VALUES ('20260117095203_AddIdCardSettingsAndTemplates', '9.0.3')
ON CONFLICT (""MigrationId"") DO NOTHING;
");
            }

            // Tablas del módulo de carné (QR tokens, scan logs, id cards) - siempre ejecutar (IF NOT EXISTS)
            await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS scan_logs (
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    student_id uuid NULL,
    scan_type character varying(50) NOT NULL,
    result character varying(50) NOT NULL,
    scanned_by uuid NOT NULL,
    scanned_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT scan_logs_pkey PRIMARY KEY (id),
    CONSTRAINT scan_logs_student_id_fkey FOREIGN KEY (student_id) REFERENCES users (id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS IX_scan_logs_student_id ON scan_logs (student_id);
CREATE INDEX IF NOT EXISTS IX_scan_logs_scanned_at ON scan_logs (scanned_at);

CREATE TABLE IF NOT EXISTS student_id_cards (
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    student_id uuid NOT NULL,
    card_number character varying(50) NOT NULL,
    issued_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at timestamp with time zone NULL,
    status character varying(20) NOT NULL DEFAULT 'active',
    is_printed boolean NOT NULL DEFAULT false,
    printed_at timestamp with time zone NULL,
    CONSTRAINT student_id_cards_pkey PRIMARY KEY (id),
    CONSTRAINT student_id_cards_student_id_fkey FOREIGN KEY (student_id) REFERENCES users (id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_student_id_cards_card_number ON student_id_cards (card_number);
CREATE INDEX IF NOT EXISTS IX_student_id_cards_student_id ON student_id_cards (student_id);

CREATE TABLE IF NOT EXISTS student_qr_tokens (
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    student_id uuid NOT NULL,
    token character varying(500) NOT NULL,
    expires_at timestamp with time zone NULL,
    is_revoked boolean NOT NULL DEFAULT false,
    CONSTRAINT student_qr_tokens_pkey PRIMARY KEY (id),
    CONSTRAINT student_qr_tokens_student_id_fkey FOREIGN KEY (student_id) REFERENCES users (id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_student_qr_tokens_token ON student_qr_tokens (token);
CREATE INDEX IF NOT EXISTS IX_student_qr_tokens_student_id ON student_qr_tokens (student_id);
");
            try
            {
                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE scan_logs ALTER COLUMN student_id DROP NOT NULL;");
            }
            catch { /* Ignorar si ya es nullable o no existe */ }

            // Compatibilidad: agregar columnas de estado de impresión en BDs existentes.
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE student_id_cards ADD COLUMN IF NOT EXISTS is_printed boolean NOT NULL DEFAULT false;");
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE student_id_cards ADD COLUMN IF NOT EXISTS printed_at timestamp with time zone NULL;");

            // Columna shift_id en student_assignments si no existe (compatibilidad con BDs antiguas)
            var hasShiftId = await context.Database.SqlQueryRaw<int>(
                @"SELECT 1 FROM information_schema.columns 
                  WHERE table_schema='public' AND table_name='student_assignments' AND column_name='shift_id' 
                  LIMIT 1").ToListAsync();
            if (hasShiftId.Count == 0)
            {
                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE student_assignments ADD COLUMN IF NOT EXISTS shift_id uuid NULL;");
            }
        }
        catch (Exception ex)
        {
            // Log but do not fail startup (e.g. connection issues, permissions)
            System.Diagnostics.Debug.WriteLine($"[EnsureIdCardTables] {ex.Message}");
        }
    }
}
