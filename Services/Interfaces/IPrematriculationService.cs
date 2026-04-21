using SchoolManager.Dtos;
using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces;

public interface IPrematriculationService
{
    Task<Prematriculation?> GetByIdAsync(Guid id);
    Task<Prematriculation?> GetByCodeAsync(string code);
    Task<List<PrematriculationDto>> GetByStudentAsync(Guid studentId);
    Task<List<PrematriculationDto>> GetByParentAsync(Guid parentId);
    Task<List<PrematriculationDto>> GetByGroupAsync(Guid groupId);
    Task<List<PrematriculationDto>> GetByPeriodAsync(Guid periodId);
    Task<List<AvailableGroupsDto>> GetAvailableGroupsAsync(Guid schoolId, Guid? gradeId);
    Task<int> GetFailedSubjectsCountAsync(Guid studentId);
    Task<Prematriculation> CreatePrematriculationAsync(PrematriculationCreateDto dto, Guid? parentId);
    Task<Prematriculation> AutoAssignGroupAsync(Guid prematriculationId);
    Task<bool> ValidateAcademicConditionAsync(Guid studentId);
    Task<Prematriculation> ConfirmMatriculationAsync(Guid prematriculationId, Guid? confirmedBy = null);
    Task<bool> CheckGroupCapacityAsync(Guid groupId, Guid? excludePrematriculationId = null);
    Task<string> GeneratePrematriculationCodeAsync();
    Task<bool> IsNewStudentAsync(Guid studentId);
    Task<bool> ValidateRequiredDocumentsAsync(Guid studentId);
    Task<bool> ValidateAgeForGradeAsync(Guid? gradeId, DateTime? dateOfBirth);
    Task<bool> ValidateParentRequiredAsync(Guid studentId);
    Task<Prematriculation> RejectPrematriculationAsync(Guid prematriculationId, string reason, Guid? rejectedBy = null);
    Task<Prematriculation> CancelPrematriculationAsync(Guid prematriculationId, string? reason = null, Guid? cancelledBy = null);
}

