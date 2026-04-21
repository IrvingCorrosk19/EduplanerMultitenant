using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces;

public interface IScheduleService
{
    /// <summary>
    /// Crea una entrada de horario. Valida conflictos docente/grupo y que el usuario tenga permiso.
    /// </summary>
    Task<ScheduleEntry> CreateEntryAsync(
        Guid teacherAssignmentId,
        Guid timeSlotId,
        byte dayOfWeek,
        Guid academicYearId,
        Guid currentUserId);

    /// <summary>
    /// Elimina una entrada de horario. El docente solo puede eliminar entradas propias.
    /// </summary>
    Task DeleteEntryAsync(Guid id, Guid currentUserId);

    /// <summary>
    /// Obtiene las entradas de horario de un docente para un año académico.
    /// </summary>
    Task<List<ScheduleEntry>> GetByTeacherAsync(Guid teacherId, Guid academicYearId);

    /// <summary>
    /// Obtiene las entradas de horario de un grupo para un año académico.
    /// </summary>
    Task<List<ScheduleEntry>> GetByGroupAsync(Guid groupId, Guid academicYearId);

    /// <summary>
    /// Obtiene las entradas de horario del estudiante (User) para un año académico.
    /// Resuelve GroupId vía StudentAssignment activa; valida que el grupo pertenezca a la escuela del usuario.
    /// Solo lectura; no modifica datos.
    /// </summary>
    Task<List<ScheduleEntry>> GetByStudentUserAsync(Guid studentUserId, Guid academicYearId);
}
