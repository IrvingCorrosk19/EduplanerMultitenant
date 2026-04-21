using SchoolManager.Models;

public interface ISecuritySettingService
{
    Task<List<SecuritySetting>> GetAllAsync();
    Task<SecuritySetting?> GetBySchoolIdAsync(Guid schoolId);
    Task CreateAsync(SecuritySetting setting);
    Task UpdateAsync(SecuritySetting setting);
}
