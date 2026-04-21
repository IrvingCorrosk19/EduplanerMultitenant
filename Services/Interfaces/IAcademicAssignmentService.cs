using SchoolManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoolManager.Application.Interfaces
{
    public interface IAcademicAssignmentService
    {
        // Nueva versión usando SubjectAssignmentId
        Task<bool> AssignTeacherAsync(Guid teacherId, Guid subjectAssignmentId);

        // Obtener todas las asignaciones por docente
        Task<List<TeacherAssignmentRequest>> GetAssignmentsByTeacherAsync(Guid teacherId);

        // Verifica si existe una asignación combinada (usado para carga desde Excel)
        Task<bool> ExisteAsignacionAsync(Guid specialtyId, Guid areaId, Guid subjectId, Guid gradeLevelId, Guid groupId, Guid? schoolId);

        // Crea la asignación académica (en subject_assignments)
        Task CreateAsignacionAsync(Guid specialtyId, Guid areaId, Guid subjectId, Guid gradeLevelId, Guid groupId, Guid? schoolId);

        // Nuevo: buscar subject_assignment_id desde componentes
        Task<Guid?> GetSubjectAssignmentIdAsync(Guid specialtyId, Guid areaId, Guid subjectId, Guid gradeLevelId, Guid groupId, Guid? schoolId);
    }
}
