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
        private readonly ILogger<CurrentUserService> _logger;

        // Request-scoped cache: el DbContext es scoped, así que este campo vive exactamente
        // una request. Evita múltiples round-trips a DB por la misma identidad en la misma request.
        private User? _cachedUser;
        private bool _userLoaded;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor,
            SchoolDbContext context,
            ILogger<CurrentUserService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _logger = logger;
        }

        public async Task<Guid?> GetCurrentUserIdAsync()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return null;

            return Guid.Parse(userIdClaim.Value);
        }

        public Task<Guid?> GetCurrentSchoolIdAsync()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("school_id");
            if (claim != null && Guid.TryParse(claim.Value, out var schoolId) && schoolId != Guid.Empty)
                return Task.FromResult<Guid?>(schoolId);
            return Task.FromResult<Guid?>(null);
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            if (_userLoaded)
                return _cachedUser;

            var userId = await GetCurrentUserIdAsync();
            if (userId != null)
            {
                // IgnoreQueryFilters es intencional: cargamos al usuario actual por su propio ID
                // (proveniente del claim JWT firmado), independientemente de su school. El superadmin
                // no tiene school_id y sería invisible si se aplicara el GQF normal.
                _cachedUser = await _context.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.Id == userId.Value)
                    .FirstOrDefaultAsync();

                // I-07: detectar drift entre claim JWT y valor real en DB
                if (_cachedUser != null)
                {
                    var jwtClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("school_id")?.Value;
                    if (Guid.TryParse(jwtClaim, out var jwtSchool)
                        && _cachedUser.SchoolId.HasValue
                        && _cachedUser.SchoolId.Value != jwtSchool)
                    {
                        _logger.LogWarning(
                            "I-07 SchoolId drift: userId={UserId} JWT={JwtSchool} DB={DbSchool}. Token stale o school reasignada.",
                            userId, jwtSchool, _cachedUser.SchoolId.Value);
                    }
                }
            }

            _userLoaded = true;
            return _cachedUser;
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

            return await _context.Schools.Where(x => x.Id == user.SchoolId.Value).FirstOrDefaultAsync();
        }
    }
}
