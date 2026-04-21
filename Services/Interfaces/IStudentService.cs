using SchoolManager.Dtos;
using SchoolManager.Models;

public interface IStudentService
{
    Task<List<Student>> GetAllAsync();
    Task<Student?> GetByIdAsync(Guid id);
    Task CreateAsync(Student student);
    Task UpdateAsync(Student student);
    Task DeleteAsync(Guid id);
    Task<List<Student>> GetByGroupAsync(string groupName);
    Task<IEnumerable<StudentBasicDto>> GetByGroupAndGradeAsync(Guid groupId, Guid gradeId);
    Task<IEnumerable<StudentBasicDto>> GetBySubjectGroupAndGradeAsync(Guid subjectId, Guid groupId, Guid gradeId);
}
