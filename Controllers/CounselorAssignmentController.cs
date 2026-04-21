using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SchoolManager.Dtos;
using SchoolManager.Services.Interfaces;
using SchoolManager.Application.Interfaces;

namespace SchoolManager.Controllers
{
    [Authorize(Roles = "superadmin,admin")]
    public class CounselorAssignmentController : Controller
    {
        private readonly ICounselorAssignmentService _counselorAssignmentService;
        private readonly IUserService _userService;
        private readonly ISchoolService _schoolService;
        private readonly IGradeLevelService _gradeLevelService;
        private readonly IGroupService _groupService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CounselorAssignmentController> _logger;

        public CounselorAssignmentController(
            ICounselorAssignmentService counselorAssignmentService,
            IUserService userService,
            ISchoolService schoolService,
            IGradeLevelService gradeLevelService,
            IGroupService groupService,
            ICurrentUserService currentUserService,
            ILogger<CounselorAssignmentController> logger)
        {
            _counselorAssignmentService = counselorAssignmentService;
            _userService = userService;
            _schoolService = schoolService;
            _gradeLevelService = gradeLevelService;
            _groupService = groupService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        // GET: CounselorAssignment
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Accediendo a la vista Index de asignaciones de consejeros");
                
                var currentUser = await _currentUserService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    _logger.LogWarning("Usuario actual no encontrado");
                    return RedirectToAction("Login", "Auth");
                }

                var assignments = await _counselorAssignmentService.GetBySchoolIdAsync(currentUser.SchoolId.Value);
                
                // Obtener datos para el formulario
                var users = await _userService.GetAllWithAssignmentsByRoleAsync("teacher");
                var assignedCounselorIds = await _counselorAssignmentService.GetAssignedCounselorUserIdsAsync(currentUser.SchoolId.Value);
                var counselorUsers = users.Where(u => !assignedCounselorIds.Contains(u.Id)).ToList();
                var validCombinations = await _counselorAssignmentService.GetValidGradeGroupCombinationsAsync(currentUser.SchoolId.Value);
                
                ViewBag.Users = counselorUsers;
                ViewBag.ValidCombinations = validCombinations;
                ViewBag.SchoolId = currentUser.SchoolId;
                
                _logger.LogInformation("Se encontraron {Count} asignaciones de consejeros para la escuela {SchoolId}", 
                    assignments.Count(), currentUser.SchoolId.Value);

                return View(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la vista Index de asignaciones de consejeros");
                TempData["ErrorMessage"] = "Error al cargar las asignaciones de consejeros";
                return View(new List<CounselorAssignmentDto>());
            }
        }

        // GET: CounselorAssignment/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                _logger.LogInformation("Obteniendo detalles de asignación de consejero con ID: {Id}", id);
                
                var assignment = await _counselorAssignmentService.GetByIdAsync(id);
                if (assignment == null)
                {
                    _logger.LogWarning("Asignación de consejero no encontrada con ID: {Id}", id);
                    TempData["ErrorMessage"] = "Asignación de consejero no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Detalles obtenidos para asignación: {AssignmentType} - {UserFullName}", 
                    assignment.AssignmentType, assignment.UserFullName);

                return View(assignment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles de asignación con ID: {Id}", id);
                TempData["ErrorMessage"] = "Error al obtener los detalles de la asignación";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: CounselorAssignment/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                _logger.LogInformation("Accediendo a la vista Create de asignación de consejero");
                
                var currentUser = await _currentUserService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    _logger.LogWarning("Usuario actual no encontrado");
                    return RedirectToAction("Login", "Auth");
                }

                // Obtener usuarios que pueden ser consejeros (teachers, directors, admins)
                // EXCLUYENDO los que ya están asignados como consejeros
                var users = await _userService.GetAllWithAssignmentsByRoleAsync("teacher");
                var assignedCounselorIds = await _counselorAssignmentService.GetAssignedCounselorUserIdsAsync(currentUser.SchoolId.Value);
                var counselorUsers = users.Where(u => !assignedCounselorIds.Contains(u.Id)).ToList();

                // Obtener combinaciones válidas de grado-grupo desde student_assignments
                var validCombinations = await _counselorAssignmentService.GetValidGradeGroupCombinationsAsync(currentUser.SchoolId.Value);

                ViewBag.Users = counselorUsers;
                ViewBag.ValidCombinations = validCombinations;
                ViewBag.SchoolId = currentUser.SchoolId;

                _logger.LogInformation("Vista Create preparada con {UserCount} usuarios, {CombinationCount} combinaciones válidas", 
                    counselorUsers.Count, validCombinations.Count);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al preparar la vista Create de asignación de consejero");
                TempData["ErrorMessage"] = "Error al cargar el formulario de creación";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CounselorAssignment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CounselorAssignmentCreateDto dto)
        {
            try
            {
                _logger.LogInformation("Creando nueva asignación de consejero para usuario {UserId} en escuela {SchoolId}", 
                    dto.UserId, dto.SchoolId);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Modelo inválido al crear asignación de consejero");
                    TempData["ErrorMessage"] = "Por favor, complete todos los campos requeridos";
                    return RedirectToAction(nameof(Create));
                }

                var assignment = await _counselorAssignmentService.CreateAsync(dto);
                
                _logger.LogInformation("Asignación de consejero creada exitosamente con ID: {Id}", assignment.Id);
                TempData["SuccessMessage"] = "Asignación de consejero creada exitosamente";

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operación inválida al crear asignación de consejero: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear asignación de consejero");
                TempData["ErrorMessage"] = "Error al crear la asignación de consejero";
                return RedirectToAction(nameof(Create));
            }
        }

        // GET: CounselorAssignment/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                _logger.LogInformation("Accediendo a la vista Edit de asignación de consejero con ID: {Id}", id);
                
                var assignment = await _counselorAssignmentService.GetByIdAsync(id);
                if (assignment == null)
                {
                    _logger.LogWarning("Asignación de consejero no encontrada con ID: {Id}", id);
                    TempData["ErrorMessage"] = "Asignación de consejero no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                var currentUser = await _currentUserService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    _logger.LogWarning("Usuario actual no encontrado");
                    return RedirectToAction("Login", "Auth");
                }

                // Obtener usuarios que pueden ser consejeros
                // EXCLUYENDO los que ya están asignados como consejeros (excepto el actual)
                var users = await _userService.GetAllWithAssignmentsByRoleAsync("teacher");
                var assignedCounselorIds = await _counselorAssignmentService.GetAssignedCounselorUserIdsAsync(currentUser.SchoolId.Value);
                var counselorUsers = users.Where(u => !assignedCounselorIds.Contains(u.Id) || u.Id == assignment.UserId).ToList();

                // Obtener combinaciones válidas de grado-grupo desde student_assignments
                // Para edición, excluimos la asignación actual para que pueda aparecer en el dropdown
                var validCombinations = await _counselorAssignmentService.GetValidGradeGroupCombinationsForEditAsync(currentUser.SchoolId.Value, assignment.Id);

                ViewBag.Users = counselorUsers;
                ViewBag.ValidCombinations = validCombinations;
                ViewBag.SchoolId = currentUser.SchoolId;

                _logger.LogInformation("Vista Edit preparada para asignación: {UserFullName}", 
                    assignment.UserFullName);

                return View(assignment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al preparar la vista Edit de asignación con ID: {Id}", id);
                TempData["ErrorMessage"] = "Error al cargar el formulario de edición";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CounselorAssignment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CounselorAssignmentUpdateDto dto)
        {
            try
            {
                _logger.LogInformation("Actualizando asignación de consejero con ID: {Id}", id);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Modelo inválido al actualizar asignación de consejero");
                    TempData["ErrorMessage"] = "Por favor, complete todos los campos requeridos";
                    return RedirectToAction(nameof(Edit), new { id });
                }

                var assignment = await _counselorAssignmentService.UpdateAsync(id, dto);
                
                _logger.LogInformation("Asignación de consejero actualizada exitosamente con ID: {Id}", id);
                TempData["SuccessMessage"] = "Asignación de consejero actualizada exitosamente";

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operación inválida al actualizar asignación de consejero: {Message}", ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar asignación de consejero con ID: {Id}", id);
                TempData["ErrorMessage"] = "Error al actualizar la asignación de consejero";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        // POST: CounselorAssignment/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Delete method called with ID: {id}");
                _logger.LogInformation("Eliminando asignación de consejero con ID: {Id}", id);

                // Verificar si el ID es válido
                if (id == Guid.Empty)
                {
                    Console.WriteLine("[ERROR] ID is empty or invalid");
                    _logger.LogError("ID de asignación de consejero inválido: {Id}", id);
                    TempData["ErrorMessage"] = "ID de asignación inválido";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si la asignación existe antes de eliminar
                var assignment = await _counselorAssignmentService.GetByIdAsync(id);
                if (assignment == null)
                {
                    Console.WriteLine($"[ERROR] Assignment not found with ID: {id}");
                    _logger.LogWarning("Asignación de consejero no encontrada con ID: {Id}", id);
                    TempData["ErrorMessage"] = "Asignación de consejero no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                Console.WriteLine($"[DEBUG] Assignment found: {assignment.UserFullName} - {assignment.AssignmentType}");
                _logger.LogInformation("Asignación encontrada: {UserFullName} - {AssignmentType}", assignment.UserFullName, assignment.AssignmentType);

                var result = await _counselorAssignmentService.DeleteAsync(id);
                Console.WriteLine($"[DEBUG] Delete result: {result}");
                
                if (result)
                {
                    Console.WriteLine($"[SUCCESS] Assignment deleted successfully with ID: {id}");
                    _logger.LogInformation("Asignación de consejero eliminada exitosamente con ID: {Id}", id);
                    TempData["SuccessMessage"] = "Asignación de consejero eliminada exitosamente";
                }
                else
                {
                    Console.WriteLine($"[WARNING] Could not delete assignment with ID: {id}");
                    _logger.LogWarning("No se pudo eliminar la asignación de consejero con ID: {Id}", id);
                    TempData["ErrorMessage"] = "No se pudo eliminar la asignación de consejero";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in Delete method: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                _logger.LogError(ex, "Error al eliminar asignación de consejero con ID: {Id}", id);
                TempData["ErrorMessage"] = $"Error al eliminar la asignación de consejero: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CounselorAssignment/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(Guid id)
        {
            try
            {
                _logger.LogInformation("Cambiando estado de asignación de consejero con ID: {Id}", id);

                var result = await _counselorAssignmentService.ToggleActiveAsync(id);
                if (result)
                {
                    _logger.LogInformation("Estado de asignación cambiado exitosamente para ID: {Id}", id);
                    TempData["SuccessMessage"] = "Estado de asignación actualizado exitosamente";
                }
                else
                {
                    _logger.LogWarning("No se pudo cambiar el estado de la asignación con ID: {Id}", id);
                    TempData["ErrorMessage"] = "No se pudo cambiar el estado de la asignación";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado de asignación con ID: {Id}", id);
                TempData["ErrorMessage"] = "Error al cambiar el estado de la asignación";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: CounselorAssignment/Stats
        public async Task<IActionResult> Stats()
        {
            try
            {
                _logger.LogInformation("Obteniendo estadísticas de asignaciones de consejeros");
                
                var currentUser = await _currentUserService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    _logger.LogWarning("Usuario actual no encontrado");
                    return RedirectToAction("Login", "Auth");
                }

                var stats = await _counselorAssignmentService.GetStatsAsync(currentUser.SchoolId.Value);
                
                _logger.LogInformation("Estadísticas obtenidas para la escuela {SchoolId}", currentUser.SchoolId.Value);

                return View(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de asignaciones de consejeros");
                TempData["ErrorMessage"] = "Error al obtener las estadísticas";
                return RedirectToAction(nameof(Index));
            }
        }

        // AJAX Endpoints para operaciones CRUD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax([FromBody] CounselorAssignmentCreateDto dto)
        {
            try
            {
                Console.WriteLine($"[AJAX DEBUG] CreateAjax called with UserId: {dto.UserId}, GradeId: {dto.GradeId}, GroupId: {dto.GroupId}");
                _logger.LogInformation("Creando asignación de consejero via AJAX para usuario {UserId}", dto.UserId);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Modelo inválido al crear asignación de consejero via AJAX");
                    return Json(new { success = false, message = "Datos inválidos" });
                }

                var assignment = await _counselorAssignmentService.CreateAsync(dto);
                
                _logger.LogInformation("Asignación de consejero creada exitosamente via AJAX con ID: {Id}", assignment.Id);
                return Json(new { success = true, message = "Asignación creada exitosamente", data = assignment });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operación inválida al crear asignación de consejero via AJAX: {Message}", ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear asignación de consejero via AJAX");
                return Json(new { success = false, message = "Error al crear la asignación" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAjax([FromBody] CounselorAssignmentUpdateDto dto)
        {
            try
            {
                Console.WriteLine($"[AJAX DEBUG] UpdateAjax called with ID: {dto.Id}");
                _logger.LogInformation("Actualizando asignación de consejero via AJAX con ID: {Id}", dto.Id);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Modelo inválido al actualizar asignación de consejero via AJAX");
                    return Json(new { success = false, message = "Datos inválidos" });
                }

                var assignment = await _counselorAssignmentService.UpdateAsync(dto.Id, dto);
                
                _logger.LogInformation("Asignación de consejero actualizada exitosamente via AJAX con ID: {Id}", assignment.Id);
                return Json(new { success = true, message = "Asignación actualizada exitosamente", data = assignment });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operación inválida al actualizar asignación de consejero via AJAX: {Message}", ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar asignación de consejero via AJAX");
                return Json(new { success = false, message = "Error al actualizar la asignación" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax([FromBody] Guid id)
        {
            try
            {
                Console.WriteLine($"[AJAX DEBUG] DeleteAjax called with ID: {id}");
                _logger.LogInformation("Eliminando asignación de consejero via AJAX con ID: {Id}", id);

                if (id == Guid.Empty)
                {
                    _logger.LogError("ID de asignación de consejero inválido via AJAX: {Id}", id);
                    return Json(new { success = false, message = "ID inválido" });
                }

                var result = await _counselorAssignmentService.DeleteAsync(id);
                
                if (result)
                {
                    _logger.LogInformation("Asignación de consejero eliminada exitosamente via AJAX con ID: {Id}", id);
                    return Json(new { success = true, message = "Asignación eliminada exitosamente" });
                }
                else
                {
                    _logger.LogWarning("No se pudo eliminar la asignación de consejero via AJAX con ID: {Id}", id);
                    return Json(new { success = false, message = "No se pudo eliminar la asignación" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar asignación de consejero via AJAX con ID: {Id}", id);
                return Json(new { success = false, message = "Error al eliminar la asignación" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetByIdAjax(Guid id)
        {
            try
            {
                Console.WriteLine($"[AJAX DEBUG] GetByIdAjax called with ID: {id}");
                _logger.LogInformation("Obteniendo asignación de consejero via AJAX con ID: {Id}", id);

                var assignment = await _counselorAssignmentService.GetByIdAsync(id);
                if (assignment == null)
                {
                    _logger.LogWarning("Asignación de consejero no encontrada via AJAX con ID: {Id}", id);
                    return Json(new { success = false, message = "Asignación no encontrada" });
                }

                _logger.LogInformation("Asignación de consejero obtenida exitosamente via AJAX: {UserFullName}", assignment.UserFullName);
                return Json(new { success = true, data = assignment });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener asignación de consejero via AJAX con ID: {Id}", id);
                return Json(new { success = false, message = "Error al obtener la asignación" });
            }
        }
    }
}
