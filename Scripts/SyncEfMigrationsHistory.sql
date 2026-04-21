-- Sincronizar __EFMigrationsHistory: insertar migraciones que ya están aplicadas en el esquema
-- pero no registradas en la tabla. Ejecutar en LOCAL y en RENDER (psql o cliente SQL).
-- Uso: psql "connection_string" -f Scripts/SyncEfMigrationsHistory.sql

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES
  ('20251102175646_AddPaymentModuleComplete', '9.0.3'),
  ('20251115111847_CompletePrematriculationModule', '9.0.3'),
  ('20251115115232_AddAcademicYearSupport', '9.0.3'),
  ('20260117093532_AddStudentIdModule', '9.0.3'),
  ('20260117095203_AddIdCardSettingsAndTemplates', '9.0.3'),
  ('20260216194827_AddScheduleModule', '9.0.3'),
  ('20260216225855_AddSchoolScheduleConfiguration', '9.0.3'),
  ('20260217000736_AddSchoolIsActive', '9.0.3'),
  ('20260217134353_AddUserPhotoUrl', '9.0.3'),
  ('20260217142501_AddTeacherWorkPlanModule', '9.0.3'),
  ('20260315084229_AddStudentPaymentAccessAndClubRoles', '9.0.3'),
  ('20260315223827_ExtendIdCardUserAndSettings', '9.0.3')
ON CONFLICT ("MigrationId") DO NOTHING;
