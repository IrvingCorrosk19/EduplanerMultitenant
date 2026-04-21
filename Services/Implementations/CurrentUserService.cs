using Microsoft.AspNetCore.Http;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace SchoolManager.Services.Implementations
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SchoolDbContext _context;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, SchoolDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public async Task<Guid?> GetCurrentUserIdAsync()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return null;

            return Guid.Parse(userIdClaim.Value);
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return null;

            return await _context.Users.FindAsync(userId.Value);
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }

        public async Task<string?> GetCurrentUserRoleAsync()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
        }

        public async Task<School?> GetCurrentUserSchoolAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null || user.SchoolId == null)
                return null;

            return await _context.Schools.FindAsync(user.SchoolId.Value);
        }
    }
} 