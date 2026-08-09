using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using BCrypt.Net;
using System.Security.Cryptography;
using System.Text;

namespace SchoolManager.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly SchoolDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IAuditLogService _auditLog;

        // Lockout: 10 intentos fallidos → bloqueo de 15 minutos
        private const int MaxFailedAttempts = 10;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public AuthController(
            IAuthService authService,
            IUserService userService,
            SchoolDbContext context,
            IConfiguration configuration,
            ILogger<AuthController> logger,
            IMemoryCache cache,
            IAuditLogService auditLog)
        {
            _authService = authService;
            _userService = userService;
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _cache = cache;
            _auditLog = auditLog;
        }

        private async Task WriteAuditAsync(Guid? userId, Guid? schoolId, string? userName, string? userRole, string action, string resource, string details)
        {
            try
            {
                await _auditLog.LogActionAsync(new Models.AuditLog
                {
                    Id = Guid.NewGuid(),
                    SchoolId = schoolId,
                    UserId = userId,
                    UserName = userName,
                    UserRole = userRole,
                    Action = action,
                    Resource = resource,
                    Details = details,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo escribir audit log: {Action}", action);
            }
        }

        private string BuildApiToken(Guid userId, Guid? schoolId, string role)
        {
            var secretKey = _configuration["ApiToken:SecretKey"]
                ?? throw new InvalidOperationException("ApiToken:SecretKey no está configurado. Defina la variable de entorno o la clave en appsettings.");
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var safeRole = (role ?? "").Replace(":", "_"); // role cannot contain ':'
            var payload = $"{userId}:{schoolId}:{safeRole}:{timestamp}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var sig = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{payload}:{sig}"));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null, string schoolInactive = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!string.IsNullOrEmpty(schoolInactive))
                TempData["Error"] = "La institución se encuentra inactiva. Contacte al administrador.";

            // No cargar todos los colegios: el selector se resuelve por correo (AJAX).
            return View(new LoginViewModel());
        }

        /// <summary>
        /// Resuelve instituciones asociadas a un correo. Solo revela escuelas si hay 2+.
        /// Si 0 o 1 coincidencia → lista vacía (anti-enumeración / UX sin selector).
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> ResolveLoginSchools(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return Json(new { requiresSelection = false, schools = Array.Empty<object>() });
            }

            var schools = await _userService.GetLoginSchoolsByEmailAsync(email);
            if (schools.Count <= 1)
            {
                return Json(new { requiresSelection = false, schools = Array.Empty<object>() });
            }

            return Json(new
            {
                requiresSelection = true,
                schools = schools.Select(s => new { id = s.Id, name = s.Name })
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Por favor, corrija los errores en el formulario.";
                await PopulateSchoolSelectorIfNeededAsync(model);
                return View(model);
            }

            // Account lockout: bloquear tras MaxFailedAttempts intentos fallidos consecutivos
            var lockoutKey  = $"lockout:{model.Email?.ToLowerInvariant()}";
            var attemptsKey = $"attempts:{model.Email?.ToLowerInvariant()}";

            if (_cache.TryGetValue(lockoutKey, out _))
            {
                _logger.LogWarning("Cuenta bloqueada temporalmente. EmailHash: {Hash}", model.Email?.GetHashCode());
                TempData["Error"] = $"Cuenta bloqueada por múltiples intentos fallidos. Intente en {(int)LockoutDuration.TotalMinutes} minutos.";
                await PopulateSchoolSelectorIfNeededAsync(model);
                return View(model);
            }

            Guid? schoolId = null;
            if (!string.IsNullOrWhiteSpace(model.SchoolId) && Guid.TryParse(model.SchoolId, out var parsedSchool))
                schoolId = parsedSchool;

            var (success, message, user) = await _authService.LoginAsync(model.Email, model.Password, schoolId);

            if (!success)
            {
                // Incrementar contador de intentos fallidos
                var attempts = _cache.GetOrCreate(attemptsKey, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = LockoutDuration;
                    entry.Size = 1;
                    return 0;
                });
                attempts++;
                var setOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = LockoutDuration,
                    Size = 1
                };
                _cache.Set(attemptsKey, attempts, setOptions);

                if (attempts >= MaxFailedAttempts)
                {
                    _cache.Set(lockoutKey, true, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = LockoutDuration, Size = 1 });
                    _logger.LogWarning("Cuenta bloqueada tras {Attempts} intentos fallidos. EmailHash: {Hash}",
                        attempts, model.Email?.GetHashCode());
                    await WriteAuditAsync(null, schoolId, null, null,
                        "LOGIN_BLOQUEADO", "Auth", $"Cuenta bloqueada tras {attempts} intentos fallidos. Email hash: {model.Email?.GetHashCode()}");
                    TempData["Error"] = $"Cuenta bloqueada por {MaxFailedAttempts} intentos fallidos. Intente en {(int)LockoutDuration.TotalMinutes} minutos.";
                }
                else
                {
                    _logger.LogInformation("Intento fallido #{Attempts}/{Max}", attempts, MaxFailedAttempts);
                    await WriteAuditAsync(null, schoolId, null, null,
                        "LOGIN_FALLIDO", "Auth", $"Intento {attempts}/{MaxFailedAttempts}. Email hash: {model.Email?.GetHashCode()}");
                    TempData["Error"] = message;
                }

                await PopulateSchoolSelectorIfNeededAsync(model);
                return View(model);
            }

            // Login exitoso: limpiar contadores
            _cache.Remove(attemptsKey);
            _cache.Remove(lockoutKey);
            _logger.LogInformation("Login exitoso UserId: {UserId}, Role: {Role}", user.Id, user.Role);
            await WriteAuditAsync(user.Id, user.SchoolId, $"{user.Name} {user.LastName}", user.Role,
                "LOGIN_EXITOSO", "Auth", $"Email hash: {model.Email?.GetHashCode()}");

            TempData["Success"] = "¡Bienvenido " + user.Name + "!";

            if (user.Role.ToLower() == "superadmin")
                return RedirectToAction("Index", "SuperAdmin");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Solo rellena el selector cuando el correo tiene 2+ escuelas activas.
        /// </summary>
        private async Task PopulateSchoolSelectorIfNeededAsync(LoginViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
                return;

            var schools = await _userService.GetLoginSchoolsByEmailAsync(model.Email);
            if (schools.Count <= 1)
                return;

            model.IsMultiSchool = true;
            model.AvailableSchools = schools.ToList();
            ViewBag.TenantSchools = schools
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name,
                    Selected = string.Equals(model.SchoolId, s.Id.ToString(), StringComparison.OrdinalIgnoreCase)
                })
                .ToList();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Capture current user context before signing out
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName    = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var userRole    = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            Guid? userId    = Guid.TryParse(userIdClaim, out var uid) ? uid : null;

            await _authService.LogoutAsync();

            await WriteAuditAsync(userId, null, userName, userRole,
                "LOGOUT", "Auth", "Sesión cerrada correctamente");

            TempData["Success"] = "Sesión cerrada correctamente";
            return RedirectToAction("Login");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // Endpoint de bootstrap para crear superadmin inicial.
        // REQUIERE variable de entorno BOOTSTRAP_SUPERADMIN_TOKEN configurada en Render.
        // Uso: GET /api/auth/create-superadmin?token=<BOOTSTRAP_SUPERADMIN_TOKEN>
        // Una vez creado el superadmin, eliminar o deshabilitar esta variable en Render.
        [HttpGet("api/auth/create-superadmin")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateSuperAdmin([FromQuery] string token)
        {
            // Verificar token de bootstrap desde variable de entorno
            var bootstrapToken = Environment.GetEnvironmentVariable("BOOTSTRAP_SUPERADMIN_TOKEN");

            // Si la variable de entorno no está configurada, el endpoint está deshabilitado
            if (string.IsNullOrWhiteSpace(bootstrapToken))
                return NotFound();

            // Verificar que el token proporcionado coincide con el de la variable de entorno
            if (string.IsNullOrWhiteSpace(token) || token != bootstrapToken)
                return Forbid();

            if (await _context.Users.AnyAsync(u => u.Role == "superadmin"))
                return Ok(new { success = false, message = "Ya existe un superadmin" });

            // Generar contraseña temporal aleatoria segura
            var tempPassword = GenerateSecureTemporaryPassword();

            var u = new User
            {
                Id = Guid.NewGuid(),
                Name = "Super", LastName = "Administrador",
                Email = "superadmin@schoolmanager.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                Role = "superadmin", Status = "active", SchoolId = null,
                DocumentId = "8-000-0000", DateOfBirth = new DateTime(1990, 1, 1),
                CellphonePrimary = "+507 0000 0000", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(u);
            await _context.SaveChangesAsync();

            // Retornar la contraseña temporal UNA SOLA VEZ — debe cambiarse inmediatamente
            return Ok(new
            {
                success = true,
                message = "Superadmin creado. Cambia la contraseña inmediatamente después del primer login.",
                email = "superadmin@schoolmanager.com",
                temporaryPassword = tempPassword,
                warning = "Elimina la variable BOOTSTRAP_SUPERADMIN_TOKEN de Render una vez completado el bootstrap."
            });
        }

        private static string GenerateSecureTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";
            var bytes = new byte[16];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        }

        // API endpoint para login desde app móvil (devuelve token simple)
        [HttpPost("api/auth/login")]
        [AllowAnonymous]
        [EnableRateLimiting("ApiLoginPolicy")]
        public async Task<IActionResult> ApiLogin([FromBody] LoginApiRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Email y contraseña son requeridos" });
            }

            var (success, message, user) = await _authService.LoginAsync(request.Email, request.Password, request.SchoolId);

            if (!success)
            {
                return Unauthorized(new { message = message });
            }

            // Cliente "scanner" (app escaneo de carnets): solo inspector y teacher/docente (cualquier casing)
            if (string.Equals(request.Client, "scanner", StringComparison.OrdinalIgnoreCase))
            {
                var role = (user.Role ?? "").Trim().ToLowerInvariant();
                var allowedForScanner = new[] { "inspector", "teacher", "docente" };
                if (!allowedForScanner.Contains(role))
                {
                    return StatusCode(403, new { message = "Solo usuarios con rol Inspector o Docente pueden usar la aplicación de escaneo de carnets." });
                }
            }

            var token = BuildApiToken(user.Id, user.SchoolId, user.Role);

            return Ok(new {
                token = token,
                userId = user.Id,
                schoolId = user.SchoolId,
                email = user.Email,
                name = user.Name,
                role = user.Role
            });
        }

    }

    public class LoginApiRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        /// <summary>Opcional: institución cuando el correo existe en más de un colegio.</summary>
        public Guid? SchoolId { get; set; }
        /// <summary>Cuando es "scanner", el login solo se permite para roles Inspector y Teacher.</summary>
        public string? Client { get; set; }
    }
} 