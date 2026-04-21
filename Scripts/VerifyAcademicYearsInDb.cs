using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Verifica en la BD si existe la tabla academic_years y si tiene datos.
/// Se ejecuta al arranque para diagnosticar el desplegable "Año académico" en Schedule/ByTeacher.
/// </summary>
public static class VerifyAcademicYearsInDb
{
    public static async Task RunAsync(SchoolDbContext context, ILogger logger)
    {
        try
        {
            var tableExists = await context.Database
                .SqlQueryRaw<int>(
                    "SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'academic_years' LIMIT 1")
                .ToListAsync();

            if (tableExists.Count == 0)
            {
                logger.LogWarning("La tabla 'academic_years' NO EXISTE en la BD. Cree la tabla (migración o dotnet run -- --apply-academic-year).");
                return;
            }

            var total = await context.AcademicYears.CountAsync();
            if (total == 0)
            {
                logger.LogWarning("La tabla 'academic_years' existe pero está VACÍA. Inserte al menos un año por escuela (Prematrícula/Administración).");
                return;
            }

            var bySchool = await context.AcademicYears
                .GroupBy(a => a.SchoolId)
                .Select(g => new { SchoolId = g.Key, Cnt = g.Count() })
                .ToListAsync();
            var msg = string.Join(", ", bySchool.Select(r => $"{r.SchoolId}={r.Cnt}"));
            logger.LogInformation("Tabla 'academic_years' OK. Total: {Total}. Por escuela: {BySchool}", total, msg);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al verificar tabla academic_years en la BD");
        }
    }
}
