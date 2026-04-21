using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Services.Interfaces;
using SchoolManager.ViewModels;

namespace SchoolManager.Controllers
{
    /// <summary>
    /// Controlador para gestionar el perfil de los estudiantes
    /// </summary>
    [Authorize(Roles = "student,estudiante")]
    public class StudentProfileController : Controller
    {
        private readonly IStudentProfileService _profileService;
        private readonly IUserPhotoService _userPhotoService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<StudentProfileController> _logger;

        public StudentProfileController(
            IStudentProfileService profileService,
            IUserPhotoService userPhotoService,
            ICurrentUserService currentUserService,
            ILogger<StudentProfileController> logger)
        {
            _profileService = profileService;
            _userPhotoService = userPhotoService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Muestra el perfil del estudiante actual
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var currentUserId = await _currentUserService.GetCurrentUserIdAsync();

                if (!currentUserId.HasValue)
                {
                    _logger.LogWarning("⚠️ No se pudo identificar al usuario actual");
                    return Unauthorized();
                }

                var profile = await _profileService.GetStudentProfileAsync(currentUserId.Value);

                if (profile == null)
                {
                    _logger.LogWarning("⚠️ No se encontró el perfil del estudiante: {UserId}", currentUserId.Value);
                    TempData["Error"] = "No se pudo cargar tu perfil. Por favor, contacta al administrador.";
                    return RedirectToAction("Index", "Home");
                }

                var currentRole = (await _currentUserService.GetCurrentUserRoleAsync() ?? "").Trim().ToLowerInvariant();
                profile.ShowEmergencyInfo = currentRole is "inspector" or "teacher" or "docente" or "admin" or "superadmin";

                return View(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cargando perfil del estudiante");
                TempData["Error"] = "Ocurrió un error al cargar tu perfil.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Actualiza el perfil del estudiante
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(StudentProfileViewModel model)
        {
            try
            {
                var currentUserId = await _currentUserService.GetCurrentUserIdAsync();

                if (!currentUserId.HasValue || currentUserId.Value != model.Id)
                {
                    _logger.LogWarning("⚠️ Intento de actualización no autorizado");
                    return Unauthorized();
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("⚠️ Modelo inválido al actualizar perfil");
                    TempData["Error"] = "Por favor, corrige los errores en el formulario.";
                    
                    // Recargar datos de solo lectura
                    var currentProfile = await _profileService.GetStudentProfileAsync(model.Id);
                    if (currentProfile != null)
                    {
                        model.Grade = currentProfile.Grade;
                        model.GroupName = currentProfile.GroupName;
                        model.SchoolName = currentProfile.SchoolName;
                        model.Role = currentProfile.Role;
                        model.PhotoUrl = currentProfile.PhotoUrl;
                    }
                    
                    return View("Index", model);
                }

                // Validar email único
                var emailAvailable = await _profileService.IsEmailAvailableAsync(model.Email, model.Id);
                if (!emailAvailable)
                {
                    _logger.LogWarning("⚠️ Email ya en uso: {Email}", model.Email);
                    ModelState.AddModelError("Email", "Este correo electrónico ya está registrado por otro usuario.");
                    TempData["Error"] = "El correo electrónico ya está en uso.";
                    
                    // Recargar datos de solo lectura
                    var currentProfile = await _profileService.GetStudentProfileAsync(model.Id);
                    if (currentProfile != null)
                    {
                        model.Grade = currentProfile.Grade;
                        model.GroupName = currentProfile.GroupName;
                        model.SchoolName = currentProfile.SchoolName;
                        model.Role = currentProfile.Role;
                        model.PhotoUrl = currentProfile.PhotoUrl;
                    }
                    
                    return View("Index", model);
                }

                // Validar documento único
                if (!string.IsNullOrEmpty(model.DocumentId))
                {
                    var documentAvailable = await _profileService.IsDocumentIdAvailableAsync(model.DocumentId, model.Id);
                    if (!documentAvailable)
                    {
                        _logger.LogWarning("⚠️ Documento ya en uso: {DocumentId}", model.DocumentId);
                        ModelState.AddModelError("DocumentId", "Este documento de identidad ya está registrado por otro usuario.");
                        TempData["Error"] = "El documento de identidad ya está en uso.";
                        
                        // Recargar datos de solo lectura
                        var currentProfile = await _profileService.GetStudentProfileAsync(model.Id);
                        if (currentProfile != null)
                        {
                            model.Grade = currentProfile.Grade;
                            model.GroupName = currentProfile.GroupName;
                            model.SchoolName = currentProfile.SchoolName;
                            model.Role = currentProfile.Role;
                            model.PhotoUrl = currentProfile.PhotoUrl;
                        }
                        
                        return View("Index", model);
                    }
                }

                // Actualizar perfil
                var success = await _profileService.UpdateStudentProfileAsync(model);

                if (success)
                {
                    _logger.LogInformation("✅ Perfil actualizado correctamente: {Name} {LastName}", model.Name, model.LastName);
                    TempData["Success"] = "Tu perfil ha sido actualizado correctamente.";
                    return RedirectToAction("Index");
                }
                else
                {
                    _logger.LogWarning("⚠️ No se pudo actualizar el perfil");
                    TempData["Error"] = "No se pudo actualizar tu perfil. Por favor, intenta nuevamente.";
                    return View("Index", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error actualizando perfil del estudiante");
                TempData["Error"] = "Ocurrió un error al actualizar tu perfil.";
                return View("Index", model);
            }
        }

        /// <summary>
        /// Verifica disponibilidad de email vía AJAX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckEmailAvailability(string email, Guid userId)
        {
            try
            {
                var available = await _profileService.IsEmailAvailableAsync(email, userId);
                return Json(new { available });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error verificando disponibilidad de email");
                return Json(new { available = false });
            }
        }

        /// <summary>
        /// Verifica disponibilidad de documento vía AJAX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckDocumentAvailability(string documentId, Guid userId)
        {
            try
            {
                var available = await _profileService.IsDocumentIdAvailableAsync(documentId, userId);
                return Json(new { available });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error verificando disponibilidad de documento");
                return Json(new { available = false });
            }
        }

        /// <summary>
        /// Actualiza la foto del estudiante (solo el propio usuario).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<IActionResult> UpdatePhoto(IFormFile? photo)
        {
            var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
            if (!currentUserId.HasValue)
                return Unauthorized();

            if (photo == null || photo.Length == 0)
            {
                TempData["Error"] = "Seleccione una imagen (JPEG o PNG; si supera 2 MB se comprimirá automáticamente, máx. de subida 12 MB).";
                return RedirectToAction("Index");
            }

            try
            {
                await _userPhotoService.UpdatePhotoAsync(currentUserId.Value, photo);
                TempData["Success"] = "Foto actualizada correctamente.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando foto del estudiante");
                TempData["Error"] = "No se pudo actualizar la foto. Use JPEG o PNG (máx. de subida 12 MB).";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Elimina la foto del estudiante.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePhoto()
        {
            var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
            if (!currentUserId.HasValue)
                return Unauthorized();

            try
            {
                await _userPhotoService.RemovePhotoAsync(currentUserId.Value);
                TempData["Success"] = "Foto eliminada.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando foto del estudiante");
                TempData["Error"] = "No se pudo eliminar la foto.";
            }

            return RedirectToAction("Index");
        }
    }
}

