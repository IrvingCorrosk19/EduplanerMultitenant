using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net;

namespace SchoolManager.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly SchoolDbContext _context;

        public AuthController(IAuthService authService, IUserService userService, SchoolDbContext context)
        {
            _authService = authService;
            _userService = userService;
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null, string schoolInactive = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!string.IsNullOrEmpty(schoolInactive))
                TempData["Error"] = "La institución se encuentra inactiva. Contacte al administrador.";
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]  // Evita fallo en Render: Data Protection keys no persisten en contenedor
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Por favor, corrija los errores en el formulario.";
                return View(model);
            }

            var (success, message, user) = await _authService.LoginAsync(model.Email, model.Password);

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
        public async Task<IActionResult> ApiLogin([FromBody] LoginApiRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Email y contraseña son requeridos" });
            }

            var (success, message, user) = await _authService.LoginAsync(request.Email, request.Password);

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

            // Generar token simple (GUID + timestamp en base64)
            // En producción, usar JWT real con System.IdentityModel.Tokens.Jwt
            var tokenData = $"{user.Id}:{user.Email}:{DateTime.UtcNow:yyyyMMddHHmmss}";
            var tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenData);
            var token = Convert.ToBase64String(tokenBytes);

            return Ok(new { 
                token = token,
                userId = user.Id,
                email = user.Email,
                name = user.Name,
                role = user.Role
            });
        }

        // Método temporal para arreglar contraseñas no hasheadas
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> FixPasswords()
        {
            try
            {
                var users = await _userService.GetAllAsync();
                int fixedCount = 0;
                var results = new List<string>();

                foreach (var user in users)
                {
                    if (!_authService.IsPasswordHashed(user.PasswordHash))
                    {
                        // La contraseña no está hasheada, hashearla
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                        await _userService.UpdateAsync(user);
                        fixedCount++;
                        results.Add($"Usuario {user.Email}: contraseña hasheada");
                    }
                }

                return Json(new { 
                    success = true, 
                    message = $"Se arreglaron {fixedCount} contraseñas", 
                    results = results 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Error: {ex.Message}" 
                });
            }
        }
    }

    public class LoginApiRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        /// <summary>Cuando es "scanner", el login solo se permite para roles Inspector y Teacher.</summary>
        public string? Client { get; set; }
    }
} 