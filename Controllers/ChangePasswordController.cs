using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using SchoolManager.Services.Interfaces;
using SchoolManager.ViewModels;

namespace SchoolManager.Controllers
{
    [Authorize]
    public class ChangePasswordController : Controller
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ChangePasswordController> _logger;

        public ChangePasswordController(IUserService userService, ICurrentUserService currentUserService, ILogger<ChangePasswordController> logger)
        {
            _userService = userService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
        {
            _logger.LogInformation("[ChangePassword] Iniciando cambio de contraseña");
            _logger.LogDebug("[ChangePassword] Model recibido: CurrentPassword={HasCurrent}, NewPassword={HasNew}, ConfirmPassword={HasConfirm}",
                !string.IsNullOrEmpty(model?.CurrentPassword), !string.IsNullOrEmpty(model?.NewPassword), !string.IsNullOrEmpty(model?.ConfirmPassword));

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("[ChangePassword] ModelState inválido: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(new { success = false, message = "Datos inválidos" });
            }

            var currentUser = await _currentUserService.GetCurrentUserAsync();
            _logger.LogDebug("[ChangePassword] Usuario actual: {HasUser}", currentUser != null ? $"UserId={currentUser.Id}" : "NULL");
            
            if (currentUser == null)
            {
                _logger.LogWarning("[ChangePassword] ERROR: Usuario no autenticado");
                return BadRequest(new { success = false, message = "Usuario no autenticado" });
            }

            _logger.LogInformation("[ChangePassword] Llamando a ChangePasswordAsync para UserId={UserId}", currentUser.Id);
            
            try
            {
                var (success, message) = await _userService.ChangePasswordAsync(
                    currentUser.Id,
                    model.CurrentPassword,
                    model.NewPassword
                );

                _logger.LogInformation("[ChangePassword] Resultado: Success={Success}", success);

                if (success)
                {
                    // Invalidate the current session so stolen cookies can't be reused
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    _logger.LogInformation("[ChangePassword] Sesión cerrada tras cambio exitoso UserId={UserId}", currentUser.Id);
                    return Json(new { success = true, message, redirect = "/Auth/Login" });
                }

                return Json(new { success, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ChangePassword] EXCEPCIÓN al cambiar contraseña para UserId={UserId}", currentUser.Id);
                return Json(new { success = false, message = "Error interno al cambiar la contraseña. Intente nuevamente." });
            }
        }
    }
}
