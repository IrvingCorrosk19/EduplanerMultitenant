using SchoolManager.Models;
using SchoolManager.Dtos;

namespace SchoolManager.Services.Interfaces
{
    public interface ICounselorAssignmentService
    {
        // Obtener todas las asignaciones de consejeros
        Task<List<CounselorAssignmentDto>> GetAllAsync();
        
        // Obtener asignación por ID
        Task<CounselorAssignmentDto?> GetByIdAsync(Guid id);
        
        // Obtener asignaciones por escuela
        Task<List<CounselorAssignmentDto>> GetBySchoolIdAsync(Guid schoolId);
        
        // Obtener asignaciones por usuario (consejero)
        Task<List<CounselorAssignmentDto>> GetByUserIdAsync(Guid userId);
        
        // Obtener asignaciones por grado
        Task<List<CounselorAssignmentDto>> GetByGradeIdAsync(Guid gradeId);
        
        // Obtener asignaciones por grupo
        Task<List<CounselorAssignmentDto>> GetByGroupIdAsync(Guid groupId);
        
        // Obtener consejero asignado a un grado específico
        Task<CounselorAssignmentDto?> GetCounselorByGradeAsync(Guid schoolId, Guid gradeId);
        
        // Obtener consejero asignado a un grupo específico
        Task<CounselorAssignmentDto?> GetCounselorByGroupAsync(Guid schoolId, Guid groupId);
        
        // Obtener consejero general de una escuela
        Task<CounselorAssignmentDto?> GetGeneralCounselorAsync(Guid schoolId);
        
        // Crear nueva asignación
        Task<CounselorAssignmentDto> CreateAsync(CounselorAssignmentCreateDto dto);
        
        // Actualizar asignación existente
        Task<CounselorAssignmentDto> UpdateAsync(Guid id, CounselorAssignmentUpdateDto dto);
        
        // Eliminar asignación
        Task<bool> DeleteAsync(Guid id);
        
        // Activar/Desactivar asignación
        Task<bool> ToggleActiveAsync(Guid id);
        
        // Verificar si un usuario es consejero en una escuela
        Task<bool> IsUserCounselorAsync(Guid userId, Guid schoolId);
        
        // Obtener estadísticas de asignaciones
        Task<CounselorAssignmentStatsDto> GetStatsAsync(Guid schoolId);
        
        // Obtener combinaciones válidas de grado-grupo desde student_assignments
        Task<List<GradeGroupCombinationDto>> GetValidGradeGroupCombinationsAsync(Guid schoolId);
        Task<List<GradeGroupCombinationDto>> GetValidGradeGroupCombinationsForEditAsync(Guid schoolId, Guid? excludeAssignmentId = null);
        Task<List<Guid>> GetAssignedCounselorUserIdsAsync(Guid schoolId);
        
        // Obtener grupos asignados a un profesor como consejero
        Task<List<CounselorGroupDto>> GetCounselorGroupsAsync(Guid teacherId);
    }
}
