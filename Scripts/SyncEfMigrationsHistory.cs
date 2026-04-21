using Npgsql;

namespace SchoolManager.Scripts;

/// <summary>
/// Inserta en __EFMigrationsHistory las migraciones que faltan (el esquema ya fue aplicado por scripts).
/// Así EF considera todas las migraciones aplicadas y no intenta crear tablas que ya existen.
/// Ejecutar: dotnet run -- --sync-ef-migrations-history (usa conexión configurada)
/// </summary>
public static class SyncEfMigrationsHistory
{
    private const string ProductVersion = "9.0.3";

    private static readonly string[] AllMigrationIds =
    {
        "20251102175646_AddPaymentModuleComplete",
        "20251115111847_CompletePrematriculationModule",
        "20251115115232_AddAcademicYearSupport",
        "20260117093532_AddStudentIdModule",
        "20260117095203_AddIdCardSettingsAndTemplates",
        "20260216194827_AddScheduleModule",
        "20260216225855_AddSchoolScheduleConfiguration",
        "20260217000736_AddSchoolIsActive",
        "20260217134353_AddUserPhotoUrl",
        "20260217142501_AddTeacherWorkPlanModule",
        "20260315084229_AddStudentPaymentAccessAndClubRoles",
        "20260315223827_ExtendIdCardUserAndSettings"
    };

    public static async Task RunAsync(string connectionString, string label = "DB")
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        // Asegurar que la tabla existe
        await using (var cmd = new NpgsqlCommand(@"
CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
    ""MigrationId"" character varying(150) NOT NULL,
    ""ProductVersion"" character varying(32) NOT NULL,
    CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
);", conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        var inserted = 0;
        foreach (var migrationId in AllMigrationIds)
        {
            await using var cmd = new NpgsqlCommand(@"
INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
VALUES (@p1, @p2)
ON CONFLICT (""MigrationId"") DO NOTHING;", conn);
            cmd.Parameters.AddWithValue("p1", migrationId);
            cmd.Parameters.AddWithValue("p2", ProductVersion);
            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0)
            {
                inserted++;
                Console.WriteLine($"   [{label}] Registrada: {migrationId}");
            }
        }

        Console.WriteLine($"\n[{label}] Total migraciones ya presentes o insertadas: {AllMigrationIds.Length}. Nuevas insertadas: {inserted}");
    }
}
