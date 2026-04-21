using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Añade la columna is_active a schools y registra la migración en __EFMigrationsHistory.
/// Útil cuando la BD no está sincronizada con EF (p. ej. tablas creadas a mano o historial distinto).
/// </summary>
public static class ApplySchoolIsActive
{
    public static async Task RunAsync(SchoolDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE schools
ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;");

        await context.Database.ExecuteSqlRawAsync(@"
INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
VALUES ('20260217000736_AddSchoolIsActive', '9.0.3')
ON CONFLICT (""MigrationId"") DO NOTHING;");
    }
}
