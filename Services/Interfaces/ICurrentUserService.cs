using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces
{
    public interface ICurrentUserService
    {
        Task<Guid?> GetCurrentUserIdAsync();
        Task<User?> GetCurrentUserAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<string?> GetCurrentUserRoleAsync();
        Task<School?> GetCurrentUserSchoolAsync();
    }
} 