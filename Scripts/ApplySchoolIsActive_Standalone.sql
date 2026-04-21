-- Aplicar columna is_active para School Soft Delete (cuando la BD no está sincronizada con EF Migrations).
-- Ejecutar en la base de datos PostgreSQL que usa la aplicación.

-- 1) Añadir columna si no existe
ALTER TABLE schools
ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;

-- 2) Registrar la migración en el historial de EF para que "dotnet ef database update" no la vuelva a aplicar
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260217000736_AddSchoolIsActive', '9.0.3')
ON CONFLICT ("MigrationId") DO NOTHING;
