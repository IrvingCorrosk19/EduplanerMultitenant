using SchoolManager.Dtos;

namespace SchoolManager.Services.Interfaces;

/// <summary>
/// Servicio del plan de trabajo trimestral. Solo planes del docente actual (Teacher); Admin puede listar todos de la escuela.
/// </summary>
public interface ITeacherWorkPlanService
{
    /// <summary>Crear plan. Valida que la combinación materia/grado/grupo esté asignada al docente y que no exista duplicado mismo trimestre+materia+grupo.</summary>
    Task<TeacherWorkPlanDto> CreateAsync(Guid teacherId, CreateTeacherWorkPlanDto dto, Guid? schoolId);

    /// <summary>Actualizar plan. Solo si pertenece al docente.</summary>
    Task<TeacherWorkPlanDto> UpdateAsync(Guid planId, Guid teacherId, CreateTeacherWorkPlanDto dto);

    /// <summary>Listar planes del docente (Teacher) o de la escuela (Admin).</summary>
    Task<List<TeacherWorkPlanListDto>> GetByTeacherAsync(Guid? teacherId, Guid? schoolId, bool adminSeesAll);

    /// <summary>Obtener plan con detalles. Valida acceso (docente dueño o admin misma escuela).</summary>
    Task<TeacherWorkPlanDto?> GetByIdAsync(Guid planId, Guid? teacherId, Guid? schoolId, bool isAdmin);

    /// <summary>Opciones para el formulario: años académicos y asignaciones del docente (materia, grado, grupo).</summary>
    Task<TeacherWorkPlanFormOptionsDto> GetFormOptionsAsync(Guid teacherId, Guid? schoolId);

    /// <summary>Eliminar plan. Solo si pertenece al docente.</summary>
    Task DeleteAsync(Guid planId, Guid teacherId);

    /// <summary>Cambiar estado a Submitted (futuro flujo de aprobación).</summary>
    Task SubmitAsync(Guid planId, Guid teacherId);
}
