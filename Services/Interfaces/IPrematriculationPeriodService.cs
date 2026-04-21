using SchoolManager.Dtos;
using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces;

public interface IPrematriculationPeriodService
{
    Task<PrematriculationPeriod?> GetActivePeriodAsync(Guid schoolId);
    Task<PrematriculationPeriod?> GetByIdAsync(Guid id);
    Task<List<PrematriculationPeriodDto>> GetAllAsync(Guid schoolId);
    Task<PrematriculationPeriod> CreateAsync(PrematriculationPeriod period, Guid createdBy);
    Task<PrematriculationPeriod> UpdateAsync(PrematriculationPeriod period, Guid updatedBy);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> IsPeriodActiveAsync(Guid periodId);
}

