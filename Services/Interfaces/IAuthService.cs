using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces
{
    public interface IAuthService
    {
        Task<(bool success, string message, User? user)> LoginAsync(string email, string password);
        Task LogoutAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<User?> GetCurrentUserAsync();
        bool IsPasswordHashed(string passwordHash);
    }
} 