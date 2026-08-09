using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using BCrypt.Net;

namespace SchoolManager.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserService _userService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SchoolDbContext _context;

        public AuthService(IUserService userService, IHttpContextAccessor httpContextAccessor, SchoolDbContext context)
        {
            _userService = userService;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public async Task<(bool success, string message, User? user)> LoginAsync(string email, string password, Guid? schoolId = null)
        {
            const string genericFailure = "Correo o contraseña incorrectos.";

            var user = await _userService.GetByEmailForLoginAsync(email, schoolId);

            if (user == null)
            {
                var schools = await _userService.GetLoginSchoolsByEmailAsync(email);
                if (!schoolId.HasValue && schools.Count > 1)
                {
                    return (false, "Existen varias cuentas con este correo. Seleccione la institución e intente de nuevo.", null);
                }

                return (false, genericFailure, null);
            }

            bool passwordValid = false;

            // Verificar si la contraseña está hasheada
            if (IsPasswordHashed(user.PasswordHash))
            {
                passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            else
            {
                passwordValid = password == user.PasswordHash;

                if (passwordValid)
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                    await _userService.UpdateAsync(user);
                }
            }

            if (!passwordValid)
            {
                return (false, genericFailure, null);
            }

            if (user.Status?.ToLower() != "active")
            {
                return (false, "Usuario inactivo", null);
            }

            if (user.SchoolId.HasValue)
            {
                var school = await _context.Schools
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(s => s.Id == user.SchoolId.Value);
                if (school != null && !school.IsActive)
                {
                    return (false, "La institución se encuentra inactiva. Contacte al administrador.", null);
                }
            }

            // Actualizar último login
            user.LastLogin = DateTime.UtcNow;
            await _userService.UpdateAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("school_id", user.SchoolId?.ToString() ?? "")
            };
            foreach (var roleClaim in BuildRoleClaims(user.Role))
                claims.Add(roleClaim);

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
            };

            await _httpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return (true, "Login exitoso", user);
        }

        public async Task LogoutAsync()
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            if (!await IsAuthenticatedAsync())
                return null;

            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return null;

            return await _userService.GetByIdWithRelationsAsync(Guid.Parse(userIdClaim.Value));
        }

        /// <summary>Claims ClaimTypes.Role para el valor persistido en users.role (típicamente minúsculas).</summary>
        internal static IEnumerable<Claim> BuildRoleClaims(string? roleFromDb)
        {
            if (string.IsNullOrWhiteSpace(roleFromDb))
                yield break;

            var raw = roleFromDb.Trim();
            yield return new Claim(ClaimTypes.Role, raw);

            switch (raw.ToLowerInvariant())
            {
                case "director":
                    yield return new Claim(ClaimTypes.Role, "Director");
                    break;
                case "inspector":
                    yield return new Claim(ClaimTypes.Role, "Inspector");
                    break;
                case "teacher":
                    yield return new Claim(ClaimTypes.Role, "Teacher");
                    yield return new Claim(ClaimTypes.Role, "Docente");
                    break;
                case "admin":
                    yield return new Claim(ClaimTypes.Role, "Admin");
                    break;
                case "secretaria":
                    yield return new Claim(ClaimTypes.Role, "Secretaria");
                    break;
                case "superadmin":
                    yield return new Claim(ClaimTypes.Role, "SuperAdmin");
                    break;
                case "clubparentsadmin":
                    yield return new Claim(ClaimTypes.Role, "ClubParentsAdmin");
                    break;
                case "estudiante":
                    yield return new Claim(ClaimTypes.Role, "student");
                    yield return new Claim(ClaimTypes.Role, "Student");
                    break;
                case "acudiente":
                case "parent":
                    yield return new Claim(ClaimTypes.Role, "Parent");
                    yield return new Claim(ClaimTypes.Role, "Acudiente");
                    break;
                case "qlservices":
                    yield return new Claim(ClaimTypes.Role, "QlServices");
                    break;
                case "contable":
                    yield return new Claim(ClaimTypes.Role, "Contabilidad");
                    break;
            }
        }

        public bool IsPasswordHashed(string passwordHash)
        {
            // Verificar si la contraseña está hasheada con BCrypt
            // Los hashes de BCrypt comienzan con $2a$, $2b$, $2x$, $2y$ o $2$
            return !string.IsNullOrEmpty(passwordHash) && 
                   (passwordHash.StartsWith("$2a$") || 
                    passwordHash.StartsWith("$2b$") || 
                    passwordHash.StartsWith("$2x$") || 
                    passwordHash.StartsWith("$2y$") || 
                    passwordHash.StartsWith("$2$"));
        }
    }
} 