using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces;

public interface IScheduleConfigurationService
{
    /// <summary>Obtiene la configuración de la escuela; null si no existe.</summary>
    Task<SchoolScheduleConfiguration?> GetBySchoolIdAsync(Guid schoolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda la configuración y regenera los TimeSlots.
    /// Si hay ScheduleEntries y forceRegenerate es false, no permite y devuelve error.
    /// Si forceRegenerate es true, elimina todas las asignaciones de horario de esta escuela y luego regenera.
    /// </summary>
    Task<(bool Success, string Message)> SaveAndGenerateBlocksAsync(SchoolScheduleConfiguration model, Guid schoolId, bool forceRegenerate = false, CancellationToken cancellationToken = default);
}
