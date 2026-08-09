using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.ViewModels;

namespace SchoolManager.Services.Helpers;

/// <summary>
/// Carga masiva read-only para informes institucionales (elimina N+1 por estudiante/celda).
/// </summary>
public sealed class ReportesGrupoBulkData
{
    public List<InformeEstudianteFilaDto> Estudiantes { get; init; } = new();
    public Dictionary<string, Guid> TrimesterNameToId { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<ReportesActivityRow> Activities { get; init; } = new();
    public Dictionary<(Guid StudentId, Guid ActivityId), decimal?> Scores { get; init; } = new();
    public Dictionary<(Guid StudentId, Guid TrimesterId), (int Ausencias, int Tardanzas)> Attendance { get; init; } = new();
    public IReadOnlyList<Trimester> TrimesterEntities { get; init; } = Array.Empty<Trimester>();
}

public sealed class ReportesActivityRow
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "";
    public string? Trimester { get; init; }
    public Guid? TrimesterId { get; init; }
    public Guid? SubjectId { get; init; }
    public string SubjectName { get; init; } = "";
}

public static class ReportesInstitucionalesBulkLoader
{
    public static async Task<ReportesGrupoBulkData> LoadAsync(
        SchoolDbContext context,
        Guid schoolId,
        Guid groupId,
        Guid gradeLevelId)
    {
        var estudiantesRaw = await context.StudentAssignments
            .AsNoTracking()
            .Where(sa =>
                sa.GroupId == groupId &&
                sa.GradeId == gradeLevelId &&
                sa.IsActive &&
                sa.Group != null &&
                sa.Group.SchoolId == schoolId)
            .Join(context.Users.AsNoTracking().Where(u => u.SchoolId == null || u.SchoolId == schoolId),
                sa => sa.StudentId,
                u => u.Id,
                (sa, u) => new { u.Id, u.Name, u.LastName })
            .OrderBy(x => x.LastName).ThenBy(x => x.Name)
            .ToListAsync();

        var estudiantes = estudiantesRaw.Select((e, i) => new InformeEstudianteFilaDto
        {
            Numero = i + 1,
            StudentId = e.Id,
            Nombre = $"{e.Name} {e.LastName}".Trim()
        }).ToList();

        var studentIds = estudiantes.Select(e => e.StudentId).ToList();

        var trimesterEntities = await context.Trimesters
            .AsNoTracking()
            .Where(t => t.SchoolId == schoolId)
            .ToListAsync();

        var trimesterNameToId = trimesterEntities
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var trimesterIds = trimesterEntities.Select(t => t.Id).ToList();
        var trimesterNames = trimesterEntities.Select(t => t.Name).Where(n => n != null).Cast<string>().ToList();

        var activities = await context.Activities
            .AsNoTracking()
            .Where(a =>
                a.GroupId == groupId &&
                a.GradeLevelId == gradeLevelId &&
                (a.SchoolId == schoolId || a.SchoolId == null) &&
                (trimesterNames.Contains(a.Trimester!) ||
                 (a.TrimesterId.HasValue && trimesterIds.Contains(a.TrimesterId.Value))))
            .Select(a => new ReportesActivityRow
            {
                Id = a.Id,
                Type = a.Type,
                Trimester = a.Trimester,
                TrimesterId = a.TrimesterId,
                SubjectId = a.SubjectId,
                SubjectName = a.Subject!.Name
            })
            .ToListAsync();

        var activityIds = activities.Select(a => a.Id).ToList();

        var scores = activityIds.Count == 0 || studentIds.Count == 0
            ? new Dictionary<(Guid, Guid), decimal?>()
            : (await context.StudentActivityScores
                .AsNoTracking()
                .Where(s =>
                    s.SchoolId == schoolId &&
                    studentIds.Contains(s.StudentId) &&
                    activityIds.Contains(s.ActivityId))
                .Select(s => new { s.StudentId, s.ActivityId, s.Score })
                .ToListAsync())
                .ToDictionary(s => (s.StudentId, s.ActivityId), s => s.Score);

        var attendance = await LoadAttendanceAsync(context, schoolId, groupId, studentIds, trimesterEntities);

        return new ReportesGrupoBulkData
        {
            Estudiantes = estudiantes,
            TrimesterNameToId = trimesterNameToId,
            Activities = activities,
            Scores = scores,
            Attendance = attendance,
            TrimesterEntities = trimesterEntities
        };
    }

    private static async Task<Dictionary<(Guid, Guid), (int, int)>> LoadAttendanceAsync(
        SchoolDbContext context,
        Guid schoolId,
        Guid groupId,
        List<Guid> studentIds,
        List<Trimester> trimesterEntities)
    {
        var result = new Dictionary<(Guid, Guid), (int, int)>();
        if (studentIds.Count == 0 || trimesterEntities.Count == 0)
            return result;

        var minDate = trimesterEntities.Min(t => DateOnly.FromDateTime(t.StartDate));
        var maxDate = trimesterEntities.Max(t => DateOnly.FromDateTime(t.EndDate));

        // MultiTenant Attendance no tiene TrimesterId/AcademicYearId: se agrupa por rango de fechas del trimestre.
        var registros = await context.Attendances
            .AsNoTracking()
            .Where(a =>
                a.SchoolId == schoolId &&
                a.GroupId == groupId &&
                a.StudentId.HasValue &&
                studentIds.Contains(a.StudentId.Value) &&
                a.Date >= minDate &&
                a.Date <= maxDate)
            .Select(a => new
            {
                StudentId = a.StudentId!.Value,
                a.Date,
                a.Status
            })
            .ToListAsync();

        // Agrupar una sola vez (O(n)) en lugar de filtrar por estudiante×trimestre (O(n·s·t))
        var byStudent = registros.GroupBy(r => r.StudentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var trim in trimesterEntities)
        {
            var start = DateOnly.FromDateTime(trim.StartDate);
            var end = DateOnly.FromDateTime(trim.EndDate);

            foreach (var studentId in studentIds)
            {
                if (!byStudent.TryGetValue(studentId, out var delEstudianteAll))
                {
                    result[(studentId, trim.Id)] = (0, 0);
                    continue;
                }

                var ausencias = 0;
                var tardanzas = 0;
                foreach (var r in delEstudianteAll)
                {
                    if (r.Date < start || r.Date > end)
                        continue;
                    if (string.Equals(r.Status, "absent", StringComparison.OrdinalIgnoreCase))
                        ausencias++;
                    else if (string.Equals(r.Status, "late", StringComparison.OrdinalIgnoreCase))
                        tardanzas++;
                }

                result[(studentId, trim.Id)] = (ausencias, tardanzas);
            }
        }

        return result;
    }

    public static decimal? CalcularNotaFinal(
        ReportesGrupoBulkData bulk,
        Guid studentId,
        string trimestre,
        IEnumerable<string> palabrasClave)
    {
        bulk.TrimesterNameToId.TryGetValue(trimestre, out var trimesterId);

        var actividadesMateria = bulk.Activities
            .Where(a =>
                ActivityEnTrimestre(a, trimestre, trimesterId) &&
                PalabrasClaveCoinciden(a.SubjectName, palabrasClave))
            .ToList();

        if (actividadesMateria.Count == 0)
            return null;

        var scoreDict = actividadesMateria.ToDictionary(
            a => a.Id,
            a => bulk.Scores.TryGetValue((studentId, a.Id), out var s) ? s : (decimal?)null);

        var acts = actividadesMateria.Select(a => new Activity
        {
            Id = a.Id,
            Type = a.Type
        }).ToList();

        return GradebookFinalGradeCalculator.CalcularNotaFinal(acts, scoreDict);
    }

    public static (int Ausencias, int Tardanzas) ContarAsistencia(
        ReportesGrupoBulkData bulk,
        Guid studentId,
        Trimester? trimester)
    {
        if (trimester == null)
            return (0, 0);

        return bulk.Attendance.TryGetValue((studentId, trimester.Id), out var v)
            ? v
            : (0, 0);
    }

    private static bool ActivityEnTrimestre(ReportesActivityRow a, string trimestre, Guid trimesterId)
    {
        if (string.Equals(a.Trimester, trimestre, StringComparison.OrdinalIgnoreCase))
            return true;
        return trimesterId != Guid.Empty && a.TrimesterId.HasValue && a.TrimesterId.Value == trimesterId;
    }

    private static bool PalabrasClaveCoinciden(string subjectName, IEnumerable<string> palabrasClave) =>
        palabrasClave.Any(p => subjectName.Contains(p, StringComparison.OrdinalIgnoreCase));
}
