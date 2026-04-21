using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Jornada fija: 8 bloques de 45 minutos. Siempre existen para cada escuela si no tiene ninguno.
/// No es dinámico: cada jornada tiene exactamente 8 bloques de 45 min.
/// </summary>
public static class EnsureDefaultTimeSlots
{
    /// <summary>
    /// 8 bloques de 45 min: 07:00-07:45, 07:45-08:30, 08:30-09:15, 09:15-10:00, 10:00-10:45, 10:45-11:30, 11:30-12:15, 12:15-13:00
    /// </summary>
    public static readonly (TimeOnly Start, TimeOnly End)[] DefaultBlocks =
    {
        (new TimeOnly(7, 0), new TimeOnly(7, 45)),
        (new TimeOnly(7, 45), new TimeOnly(8, 30)),
        (new TimeOnly(8, 30), new TimeOnly(9, 15)),
        (new TimeOnly(9, 15), new TimeOnly(10, 0)),
        (new TimeOnly(10, 0), new TimeOnly(10, 45)),
        (new TimeOnly(10, 45), new TimeOnly(11, 30)),
        (new TimeOnly(11, 30), new TimeOnly(12, 15)),
        (new TimeOnly(12, 15), new TimeOnly(13, 0))
    };

    public const int JornadaBlockCount = 8;
    public const int BlockDurationMinutes = 45;

    /// <summary>
    /// Si la escuela no tiene TimeSlots, crea exactamente 8 bloques de 45 min (jornada estándar).
    /// </summary>
    public static async Task EnsureForSchoolAsync(SchoolDbContext context, Guid schoolId)
    {
        var hasAny = await context.TimeSlots.AnyAsync(t => t.SchoolId == schoolId).ConfigureAwait(false);
        if (hasAny)
            return;

        for (var i = 0; i < DefaultBlocks.Length; i++)
        {
            var (start, end) = DefaultBlocks[i];
            context.TimeSlots.Add(new TimeSlot
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                Name = $"Bloque {i + 1}",
                StartTime = start,
                EndTime = end,
                DisplayOrder = i,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
