using SchoolManager.Dtos;

namespace SchoolManager.Services.Interfaces;

public interface ITeacherGradebookPdfService
{
    Task<byte[]> GenerateRegistroPdfAsync(
        Guid teacherId,
        Guid groupId,
        string trimester,
        Guid subjectId,
        Guid gradeLevelId,
        byte[]? logoBytes = null);
}
