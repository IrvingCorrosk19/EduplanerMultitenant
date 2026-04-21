using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SchoolManager.Services.Interfaces;
using SchoolManager.ViewModels;

namespace SchoolManager.Controllers
{
    [Authorize]
    public class ChangePasswordController : Controller
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;

        public ChangePasswordController(IUserService userService, ICurrentUserService currentUserService)
        {
            _userService = userService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
        {
            Console.WriteLine($"[ChangePassword] Iniciando cambio de contraseña");
            Console.WriteLine($"[ChangePassword] Model recibido: CurrentPassword={!string.IsNullOrEmpty(model?.CurrentPassword)}, NewPassword={!string.IsNullOrEmpty(model?.NewPassword)}, ConfirmPassword={!string.IsNullOrEmpty(model?.ConfirmPassword)}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine($"[ChangePassword] ModelState inválido: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(new { success = false, message = "Datos inválidos" });
            }

            var currentUser = await _currentUserService.GetCurrentUserAsync();
            Console.WriteLine($"[ChangePassword] Usuario actual: {(currentUser != null ? $"ID={currentUser.Id}, Email={currentUser.Email}" : "NULL")}");
            
            if (currentUser == null)
            {
                Console.WriteLine($"[ChangePassword] ERROR: Usuario no autenticado");
                return BadRequest(new { success = false, message = "Usuario no autenticado" });
            }

            Console.WriteLine($"[ChangePassword] Llamando a ChangePasswordAsync para usuario {currentUser.Id}");
            
            try
            {
                var (success, message) = await _userService.ChangePasswordAsync(
                    currentUser.Id, 
                    model.CurrentPassword, 
                    model.NewPassword
                );

                Console.WriteLine($"[ChangePassword] Resultado: Success={success}, Message={message}");
                return Json(new { success, message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChangePassword] EXCEPCIÓN: {ex.Message}");
                Console.WriteLine($"[ChangePassword] StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error interno: {ex.Message}" });
            }
        }
    }
}
