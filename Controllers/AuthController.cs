using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

        public AuthController(IAuthService authService, IUserService userService, SchoolDbContext context, IConfiguration configuration)
        {
            _authService = authService;
            _userService = userService;
            _context = context;
            _configuration = configuration;
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
        public async Task<IActionResult> Login(string returnUrl = null, string schoolInactive = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!string.IsNullOrEmpty(schoolInactive))
                TempData["Error"] = "La institución se encuentra inactiva. Contacte al administrador.";

            var schoolItems = await _context.Schools.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                .ToListAsync();
            ViewBag.TenantSchools = schoolItems;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]  // Evita fallo en Render: Data Protection keys no persisten en contenedor
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Por favor, corrija los errores en el formulario.";
                return View(model);
            }

            Guid? schoolId = null;
            if (!string.IsNullOrWhiteSpace(model.SchoolId) && Guid.TryParse(model.SchoolId, out var parsedSchool))
                schoolId = parsedSchool;

            var (success, message, user) = await _authService.LoginAsync(model.Email, model.Password, schoolId);

            Console.WriteLine($"[Login] Intento de login para {model.Email} - Éxito: {success}");

            if (!success)
            {
                TempData["Error"] = message;
                return View(model);
            }

            TempData["Success"] = "¡Bienvenido " + user.Name + "!";

            // Redirigir según el rol del usuario
            if (user.Role.ToLower() == "superadmin")
            {
                return RedirectToAction("Index", "SuperAdmin");
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
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

        // Endpoint para crear superadmin inicial (solo si no existe ninguno). Uso: GET /api/auth/create-superadmin
        [HttpGet("api/auth/create-superadmin")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateSuperAdmin()
        {
            if (await _context.Users.AnyAsync(u => u.Role == "superadmin"))
                return Ok(new { success = false, message = "Ya existe un superadmin" });

            var u = new User
            {
                Id = Guid.NewGuid(),
                Name = "Super", LastName = "Administrador",
                Email = "superadmin@schoolmanager.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "superadmin", Status = "active", SchoolId = null,
                DocumentId = "8-000-0000", DateOfBirth = new DateTime(1990, 1, 1),
                CellphonePrimary = "+507 0000 0000", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(u);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Superadmin creado: superadmin@schoolmanager.com / Admin123!" });
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