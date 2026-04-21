-- Ejecutar en la BD que usa la app (local / Render).
-- Corrige: column u.password_email_sent_at does not exist

ALTER TABLE users ADD COLUMN IF NOT EXISTS password_email_sent_at timestamp with time zone;
ALTER TABLE users ADD COLUMN IF NOT EXISTS password_email_status character varying(20);

-- Opcional: registrar migración EF si usas historial (evita que EF intente volver a crear columnas)
-- INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
-- SELECT '20260318015555_AddPasswordEmailAndResendSettings', '9.0.3'
-- WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318015555_AddPasswordEmailAndResendSettings');
