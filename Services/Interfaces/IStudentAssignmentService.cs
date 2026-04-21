using System.Collections.Generic;
using SchoolManager.Models;

public interface IStudentAssignmentService
{
    Task<List<StudentAssignment>> GetAssignmentsByStudentIdAsync(Guid studentId, bool activeOnly = true);

    /// <summary>
    /// Una sola consulta con JOIN por escuela del usuario actual (evita IN masivo de UUIDs).
    /// Mismo criterio que GetAllStudentsAsync: school_id + rol student/estudiante, solo asignaciones activas.
    /// </summary>
    Task<Dictionary<Guid, List<StudentAssignment>>> GetActiveAssignmentsForCurrentSchoolAsync();

    Task AssignAsync(Guid studentId, List<(Guid SubjectId, Guid GradeId, Guid GroupId)> assignments);

    Task<bool> AssignStudentAsync(Guid studentId, Guid subjectId, Guid gradeId, Guid groupId); // ← NUEVO

    Task RemoveAssignmentsAsync(Guid studentId);

    Task BulkAssignFromFileAsync(List<(string StudentEmail, string SubjectCode, string GradeName, string GroupName)> rows);
    Task<bool> ExistsAsync(Guid studentId, Guid gradeId, Guid groupId);
    Task InsertAsync(StudentAssignment assignment);


}
