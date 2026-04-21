using SchoolManager.Dtos;
using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces
{
    public interface IEmailConfigurationService
    {
        Task<List<EmailConfigurationDto>> GetAllAsync();
        Task<EmailConfigurationDto?> GetByIdAsync(Guid id);
        Task<EmailConfigurationDto?> GetBySchoolIdAsync(Guid schoolId);
        Task<EmailConfigurationDto?> GetActiveBySchoolIdAsync(Guid schoolId);
        Task<EmailConfigurationDto> CreateAsync(EmailConfigurationCreateDto createDto);
        Task<EmailConfigurationDto> UpdateAsync(EmailConfigurationUpdateDto updateDto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> TestConnectionAsync(Guid id);
        Task<bool> TestConnectionBySchoolIdAsync(Guid schoolId);
    }
}
