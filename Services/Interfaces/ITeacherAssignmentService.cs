using SchoolManager.Models;
using SchoolManager.ViewModels;

public interface ITeacherAssignmentService
{
    Task<List<TeacherAssignment>> GetByTeacherIdAsync(Guid teacherId);

    Task CreateAsync(Guid teacherId, Guid subjectId, Guid groupId, Guid gradeLevelId, Guid areaId, Guid specialtyId);

    Task UpdateAsync(Guid assignmentId, Guid subjectId, Guid groupId, Guid gradeLevelId, Guid areaId, Guid specialtyId);

    Task DeleteAsync(Guid assignmentId);

    Task<TeacherAssignment?> GetByIdAsync(Guid id);
    Task<List<TeacherAssignment>> GetAllWithIncludesAsync();

    Task<List<TeacherAssignment>> GetAssignmentsForModalByTeacherIdAsync(Guid teacherId);
    Task<(bool Success, List<Guid>? SubjectAssignmentIds, AssignmentDto? FailedAssignment)> GetSubjectAssignmentIdsAsync(SaveTeacherAssignmentsRequest request);

    Task DeleteAllAssignmentsByTeacherIdAsync(Guid teacherId);

    Task AddAssignmentAsync(Guid teacherId, Guid subjectAssignmentId);

    Task<TeacherAssignment?> GetExistingAssignmentAsync(
        Guid currentTeacherId,
        Guid specialtyId,
        Guid areaId,
        Guid subjectId,
        Guid gradeLevelId,
        Guid groupId);
}
