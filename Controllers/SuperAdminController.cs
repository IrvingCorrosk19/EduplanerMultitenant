using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Helpers;
using SchoolManager.Models;
using SchoolManager.Services;
using SchoolManager.ViewModels;
using BCrypt.Net;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Controllers;

[Authorize(Roles = "superadmin")]
public class SuperAdminController : Controller
{
    private static readonly Guid DefaultEmailApiConfigId = Guid.Parse("b2222222-2222-2222-2222-222222222222");

    private readonly ISuperAdminService _superAdminService;
    private readonly IUserPhotoService _userPhotoService;
    private readonly IFileStorageService _fileStorage;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly SchoolDbContext _db;
    private readonly ILogger<SuperAdminController> _logger;

    public SuperAdminController(
        ISuperAdminService superAdminService,
        IUserPhotoService userPhotoService,
        IFileStorageService fileStorage,
        IWebHostEnvironment webHostEnvironment,
        SchoolDbContext db,
        ILogger<SuperAdminController> logger)
    {
        _superAdminService = superAdminService;
        _userPhotoService = userPhotoService;
        _fileStorage = fileStorage;
        _webHostEnvironment = webHostEnvironment;
        _db = db;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    // GET: SuperAdmin/CreateSchoolWithAdmin
    public IActionResult CreateSchoolWithAdmin()
    {
        return View();
    }

    // POST: SuperAdmin/CreateSchoolWithAdmin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSchoolWithAdmin(SchoolAdminViewModel model, IFormFile? logoFile)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                var success = await _superAdminService.CreateSchoolWithAdminAsync(model, logoFile, uploadsPath);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "Escuela y administrador creados exitosamente.";
                    return RedirectToAction(nameof(ListSchools));
                }
                else
                {
                    ModelState.AddModelError("", "Error al crear la escuela y el administrador.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al crear la escuela y el administrador. Intente nuevamente.");
                _logger.LogError(ex, "Error al crear escuela y administrador");
            }
        }

        return View(model);
    }

    // GET: SuperAdmin/ListSchools
    public async Task<IActionResult> ListSchools(string searchString)
    {
        _logger.LogInformation("[ListSchools] Cargando lista de escuelas con filtro");

        try
        {
            var schools = await _superAdminService.GetAllSchoolsAsync(searchString);
            ViewData["CurrentFilter"] = searchString;
            return View(schools);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar lista de escuelas");

            ViewData["CurrentFilter"] = searchString;
            return View(new List<SchoolListViewModel>());
        }
    }

    // GET: SuperAdmin/ListAdmins
    public async Task<IActionResult> ListAdmins()
    {
        var admins = await _superAdminService.GetAllAdminsAsync();

        return View(admins);
    }

    // GET: SuperAdmin/StudentDirectory
    [HttpGet]
    public async Task<IActionResult> StudentDirectory([FromQuery] SuperAdminStudentDirectoryFilterVm? filter)
    {
        filter ??= new SuperAdminStudentDirectoryFilterVm();
        var page = await _superAdminService.GetStudentDirectoryPageAsync(filter);
        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> StudentDirectoryUpdatePhoto(Guid userId, IFormFile? photo)
    {
        if (photo == null || photo.Length == 0)
            return Json(new { success = false, message = "Seleccione una imagen (JPEG o PNG; si supera 2 MB se comprimirá automáticamente, máx. de subida 12 MB)." });

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return Json(new { success = false, message = "Estudiante no encontrado." });

        var role = (user.Role ?? "").ToLowerInvariant();
        if (role != "student" && role != "estudiante" && role != "alumno")
            return Json(new { success = false, message = "El usuario no es estudiante." });

        try
        {
            await _userPhotoService.UpdatePhotoAsync(userId, photo);
            var updated = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            return Json(new { success = true, photoUrl = updated?.PhotoUrl });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = "Error interno. Intente nuevamente." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando foto de estudiante {UserId} desde StudentDirectory", userId);
            return Json(new { success = false, message = "No se pudo actualizar la foto." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StudentDirectoryRemovePhoto(Guid userId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return Json(new { success = false, message = "Estudiante no encontrado." });

        var role = (user.Role ?? "").ToLowerInvariant();
        if (role != "student" && role != "estudiante" && role != "alumno")
            return Json(new { success = false, message = "El usuario no es estudiante." });

        try
        {
            await _userPhotoService.RemovePhotoAsync(userId);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando foto de estudiante {UserId} desde StudentDirectory", userId);
            return Json(new { success = false, message = "No se pudo eliminar la foto." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> StudentDirectoryDownloadPhoto(Guid userId)
    {
        var user = await _db.Users.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return NotFound();

        var role = (user.Role ?? "").ToLowerInvariant();
        if (role is not ("student" or "estudiante" or "alumno"))
            return NotFound();

        if (string.IsNullOrWhiteSpace(user.PhotoUrl))
            return NotFound("El estudiante no tiene foto.");

        try
        {
            var bytes = await _fileStorage.GetUserPhotoBytesAsync(user.PhotoUrl.Trim());
            if (bytes == null || bytes.Length == 0)
                return NotFound("No se pudo obtener la foto.");

            var ext = user.PhotoUrl.Contains(".png", StringComparison.OrdinalIgnoreCase) ? "png" : "jpg";
            var contentType = ext == "png" ? "image/png" : "image/jpeg";
            var fileName = BuildStudentPhotoDownloadFileName(user.Name, user.LastName, user.DocumentId, ext);
            return File(bytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error descargando foto de estudiante {UserId} desde StudentDirectory", userId);
            return NotFound("No se pudo descargar la foto.");
        }
    }

    private static string BuildStudentPhotoDownloadFileName(string? name, string? lastName, string? documentId, string ext)
    {
        static string Sanitize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var chars = value.Trim().Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_').ToArray();
            var s = new string(chars).Trim();
            return string.IsNullOrWhiteSpace(s) ? "" : s.Replace(' ', '-');
        }

        var parts = new[] { Sanitize(name), Sanitize(lastName), Sanitize(documentId) }
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();
        var baseName = parts.Count > 0 ? string.Join("_", parts) : "estudiante";
        return $"{baseName}.{ext}";
    }

    [HttpGet]
    public async Task<IActionResult> StaffDirectory([FromQuery] SuperAdminStaffDirectoryFilterVm? filter)
    {
        filter ??= new SuperAdminStaffDirectoryFilterVm();
        var page = await _superAdminService.GetStaffDirectoryPageAsync(filter);
        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> StaffDirectoryUpdatePhoto(Guid userId, IFormFile? photo)
    {
        if (photo == null || photo.Length == 0)
            return Json(new { success = false, message = "Seleccione una imagen (JPEG o PNG)." });

        var user = await _db.Users.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return Json(new { success = false, message = "Usuario no encontrado." });

        if (!StaffInstitutionalProfileAccess.IsStaffDirectoryEligibleRole(user.Role))
            return Json(new { success = false, message = "El usuario no es personal institucional elegible para el directorio." });

        try
        {
            await _userPhotoService.UpdatePhotoAsync(userId, photo);
            var updated = await _db.Users.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
            return Json(new { success = true, photoUrl = updated?.PhotoUrl });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando foto de personal {UserId}", userId);
            return Json(new { success = false, message = "No se pudo actualizar la foto." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StaffDirectoryRemovePhoto(Guid userId)
    {
        var user = await _db.Users.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return Json(new { success = false, message = "Usuario no encontrado." });

        if (!StaffInstitutionalProfileAccess.IsStaffDirectoryEligibleRole(user.Role))
            return Json(new { success = false, message = "El usuario no es personal institucional elegible para el directorio." });

        try
        {
            await _userPhotoService.RemovePhotoAsync(userId);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando foto de personal {UserId}", userId);
            return Json(new { success = false, message = "No se pudo eliminar la foto." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StaffDirectorySaveProfile(
        Guid userId,
        string? jobTitle,
        string? department,
        string? employeeCode)
    {
        var user = await _db.Users.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return Json(new { success = false, message = "Usuario no encontrado." });

        if (!StaffInstitutionalProfileAccess.IsStaffDirectoryEligibleRole(user.Role))
            return Json(new { success = false, message = "El usuario no es personal institucional elegible para el directorio." });

        if (!user.SchoolId.HasValue)
            return Json(new { success = false, message = "El usuario no tiene escuela asignada." });

        try
        {
            var profile = await _db.StaffInstitutionalProfiles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                profile = new StaffInstitutionalProfile { UserId = userId, SchoolId = user.SchoolId.Value };
                _db.StaffInstitutionalProfiles.Add(profile);
            }
            else if (profile.SchoolId == Guid.Empty)
            {
                profile.SchoolId = user.SchoolId.Value;
            }

            profile.JobTitle = string.IsNullOrWhiteSpace(jobTitle) ? null : jobTitle.Trim();
            profile.Department = string.IsNullOrWhiteSpace(department) ? null : department.Trim();
            profile.EmployeeCode = string.IsNullOrWhiteSpace(employeeCode) ? null : employeeCode.Trim();
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando perfil laboral {UserId}", userId);
            return Json(new { success = false, message = "No se pudo guardar el perfil laboral." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSchool(Guid id)
    {
        _logger.LogInformation("[DeleteSchool] Iniciando eliminación de escuela con ID: {SchoolId}", id);

        try
        {
            var success = await _superAdminService.DeleteSchoolAsync(id);

            if (success)
            {
                _logger.LogInformation("[DeleteSchool] Institución desactivada correctamente: {SchoolId}", id);
                TempData["SuccessMessage"] = "Institución desactivada correctamente.";
            }
            else
            {
                _logger.LogWarning("[DeleteSchool] No se pudo desactivar la institución: {SchoolId}", id);
                TempData["ErrorMessage"] = "No se pudo desactivar la institución.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando escuela: {SchoolId}", id);
            TempData["ErrorMessage"] = "Error al eliminar la escuela. Intente nuevamente.";
        }

        return RedirectToAction(nameof(ListSchools));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        _logger.LogInformation("[DeleteUser] Iniciando eliminación de usuario con ID: {UserId}", id);

        try
        {
            var success = await _superAdminService.DeleteUserAsync(id);

            if (success)
            {
                _logger.LogInformation("[DeleteUser] Usuario eliminado exitosamente: {UserId}", id);
                TempData["SuccessMessage"] = "Usuario eliminado exitosamente.";
            }
            else
            {
                _logger.LogWarning("[DeleteUser] No se pudo eliminar el usuario: {UserId}", id);
                TempData["ErrorMessage"] = "No se pudo eliminar el usuario.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando usuario: {UserId}", id);
            TempData["ErrorMessage"] = "Error al eliminar el usuario. Intente nuevamente.";
        }

        return RedirectToAction(nameof(ListSchools));
    }

    [HttpGet]
    public async Task<IActionResult> EditSchool(Guid id)
    {
        var viewModel = await _superAdminService.GetSchoolForEditWithAdminAsync(id);
        
        if (viewModel == null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSchool(SchoolAdminEditViewModel model, IFormFile? logoFile)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                var success = await _superAdminService.UpdateSchoolAsync(model, logoFile, uploadsPath);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "Escuela actualizada exitosamente.";
                    return RedirectToAction(nameof(ListSchools));
                }
                else
                {
                    ModelState.AddModelError("", "Error al actualizar la escuela.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar escuela");
                ModelState.AddModelError("", "Error al actualizar la escuela. Intente nuevamente.");
            }
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(Guid id)
    {
        var viewModel = await _superAdminService.GetUserForEditAsync(id);
        
        if (viewModel == null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(UserEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var success = await _superAdminService.UpdateUserAsync(model);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "Usuario actualizado exitosamente.";
                    return RedirectToAction(nameof(ListSchools));
                }
                else
                {
                    ModelState.AddModelError("", "Error al actualizar el usuario.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario");
                ModelState.AddModelError("", "Error al actualizar el usuario. Intente nuevamente.");
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> UpdateUserPhoto(Guid id, IFormFile? photo)
    {
        if (photo == null || photo.Length == 0)
        {
            TempData["ErrorMessage"] = "Seleccione una imagen (JPEG o PNG; si supera 2 MB se comprimirá automáticamente, máx. de subida 12 MB).";
            return RedirectToAction(nameof(EditUser), new { id });
        }
        try
        {
            await _userPhotoService.UpdatePhotoAsync(id, photo);
            TempData["SuccessMessage"] = "Foto actualizada correctamente.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = "Error interno. Intente nuevamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando foto del usuario {UserId}", id);
            TempData["ErrorMessage"] = "No se pudo actualizar la foto. Use JPEG o PNG (máx. de subida 12 MB).";
        }
        return RedirectToAction(nameof(EditUser), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveUserPhoto(Guid id)
    {
        try
        {
            await _userPhotoService.RemovePhotoAsync(id);
            TempData["SuccessMessage"] = "Foto eliminada.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando foto del usuario {UserId}", id);
            TempData["ErrorMessage"] = "No se pudo eliminar la foto.";
        }
        return RedirectToAction(nameof(EditUser), new { id });
    }


    // Método para diagnosticar problemas de eliminación
    [HttpGet]
    public async Task<IActionResult> DiagnoseSchool(Guid id)
    {
        _logger.LogInformation("[DiagnoseSchool] Diagnosticando escuela con ID: {SchoolId}", id);

        try
        {
            var result = await _superAdminService.DiagnoseSchoolAsync(id);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DiagnoseSchool] Error al diagnosticar escuela: {SchoolId}", id);
            return Json(new { success = false, message = "Error interno. Intente nuevamente." });
        }
    }

    // Método temporal para crear el superadmin inicial
    [HttpGet]
    public async Task<IActionResult> CreateInitialSuperAdmin()
    {
        try
        {
            // Verificar si ya existe un superadmin
            var existingSuperAdmin = await _superAdminService.GetAllAdminsAsync();
            if (existingSuperAdmin.Any(u => u.Role == "superadmin"))
            {
                return Json(new { 
                    success = false, 
                    message = "Ya existe un superadmin en el sistema" 
                });
            }

            // Crear el superadmin
            var superAdmin = new User
            {
                Id = Guid.NewGuid(),
                Name = "Super",
                LastName = "Administrador",
                Email = "superadmin@schoolmanager.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "superadmin",
                Status = "active",
                SchoolId = null, // Sin SchoolId para superadmin
                DocumentId = "8-000-0000",
                DateOfBirth = new DateTime(1990, 1, 1),
                CellphonePrimary = "+507 0000 0000",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Usar el contexto directamente para insertar
            using var context = new SchoolDbContext();
            context.Users.Add(superAdmin);
            await context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "Superadmin creado exitosamente",
                user = new {
                    id = superAdmin.Id,
                    name = superAdmin.Name,
                    lastName = superAdmin.LastName,
                    email = superAdmin.Email,
                    role = superAdmin.Role
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateInitialSuperAdmin] Error al crear superadmin inicial");
            return Json(new {
                success = false,
                message = "Error al crear superadmin. Intente nuevamente."
            });
        }
    }

    // GET: SuperAdmin/SystemSettings
    [HttpGet]
    public async Task<IActionResult> SystemSettings()
    {
        try
        {
            var stats = await _superAdminService.GetSystemStatsAsync();
            return View(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando configuración del sistema");
            TempData["ErrorMessage"] = "Error al cargar la configuración.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: SuperAdmin/Backup
    [HttpGet]
    public IActionResult Backup()
    {
        return View();
    }

    // POST: SuperAdmin/ExecuteBackup
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteBackup()
    {
        try
        {
            TempData["InfoMessage"] = "El respaldo debe realizarse desde pgAdmin o mediante comandos pg_dump. " +
                "Consulte la documentación para más información.";
            return RedirectToAction(nameof(Backup));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando respaldo");
            TempData["ErrorMessage"] = "Error al ejecutar el respaldo.";
            return RedirectToAction(nameof(Backup));
        }
    }

    // GET: SuperAdmin/SystemStats
    [HttpGet]
    public async Task<IActionResult> SystemStats()
    {
        try
        {
            var stats = await _superAdminService.GetSystemStatsAsync();
            return View(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando estadísticas");
            TempData["ErrorMessage"] = "Error al cargar las estadísticas.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: SuperAdmin/ActivityLog
    [HttpGet]
    public async Task<IActionResult> ActivityLog(int page = 1, int pageSize = 50)
    {
        try
        {
            var logs = await _superAdminService.GetActivityLogsAsync(page, pageSize);
            return View(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando registro de actividad");
            TempData["ErrorMessage"] = "Error al cargar el registro.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>Configuración Resend para envío masivo de contraseñas (UserPasswordManagement).</summary>
    [HttpGet]
    public async Task<IActionResult> EmailApiSettings()
    {
        try
        {
            await EnsureEmailApiConfigurationRowAsync();
            var row = await _db.EmailApiConfigurations
                .AsNoTracking()
                .OrderByDescending(x => x.IsActive)
                .ThenByDescending(x => x.CreatedAt)
                .FirstAsync();
            var vm = new EmailApiSettingsViewModel
            {
                FromEmail = row.FromEmail,
                FromName = row.FromName,
                IsActive = row.IsActive,
                Provider = row.Provider,
                HasStoredApiKey = !string.IsNullOrWhiteSpace(row.ApiKey)
            };
            return View(vm);
        }
        catch (Exception ex) when (ex.Message.Contains("email_api_configurations", StringComparison.OrdinalIgnoreCase) ||
                                   ex.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(ex, "Tabla email_api_configurations no existe");
            TempData["ErrorMessage"] =
                "Falta la tabla email_api_configurations en la base de datos. Aplique la migración EF o el SQL del proyecto.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmailApiSettings(EmailApiSettingsViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await EnsureEmailApiConfigurationRowAsync();
            var row = await _db.EmailApiConfigurations.FirstOrDefaultAsync(x => x.Id == DefaultEmailApiConfigId)
                      ?? await _db.EmailApiConfigurations.OrderByDescending(x => x.CreatedAt).FirstAsync();

            if (!string.IsNullOrWhiteSpace(model.NewApiKey))
                row.ApiKey = model.NewApiKey.Trim();

            row.FromEmail = model.FromEmail.Trim();
            row.FromName = string.IsNullOrWhiteSpace(model.FromName) ? "SchoolManager" : model.FromName.Trim();
            row.Provider = "Resend";
            row.IsActive = model.IsActive;
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Configuración de correo API guardada.";
            return RedirectToAction(nameof(EmailApiSettings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EmailApiSettings save failed");
            ModelState.AddModelError("", "No se pudo guardar. Verifique que exista la tabla email_api_configurations.");
            model.HasStoredApiKey = await _db.EmailApiConfigurations.AnyAsync(x => !string.IsNullOrEmpty(x.ApiKey));
            return View(model);
        }
    }

    private async Task EnsureEmailApiConfigurationRowAsync()
    {
        if (await _db.EmailApiConfigurations.AnyAsync())
            return;
        _db.EmailApiConfigurations.Add(new EmailApiConfiguration
        {
            Id = DefaultEmailApiConfigId,
            Provider = "Resend",
            ApiKey = string.Empty,
            FromEmail = "noreply@tusistema.com",
            FromName = "SchoolManager",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}