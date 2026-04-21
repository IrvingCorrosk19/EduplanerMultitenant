using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Dtos;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Controllers;

[Authorize(Roles = "Admin,Teacher,admin,teacher,docente")]
public class TeacherWorkPlanController : Controller
{
    private readonly ITeacherWorkPlanService _workPlanService;
    private readonly ITeacherWorkPlanPdfService _pdfService;
    private readonly ICurrentUserService _currentUserService;

    public TeacherWorkPlanController(
        ITeacherWorkPlanService workPlanService,
        ITeacherWorkPlanPdfService pdfService,
        ICurrentUserService currentUserService)
    {
        _workPlanService = workPlanService;
        _pdfService = pdfService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Auth");
        var role = (await _currentUserService.GetCurrentUserRoleAsync())?.ToLower() ?? "";
        var isAdmin = role == "admin";
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        Guid? teacherId = isAdmin ? null : user.Id;
        var list = await _workPlanService.GetByTeacherAsync(teacherId, school?.Id, adminSeesAll: isAdmin);
        ViewBag.IsAdmin = isAdmin;
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Auth");
        var role = (await _currentUserService.GetCurrentUserRoleAsync())?.ToLower() ?? "";
        if (role == "admin") return RedirectToAction(nameof(Index));
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        var options = await _workPlanService.GetFormOptionsAsync(user.Id, school?.Id);
        if (options.AssignmentOptions.Count == 0)
        {
            TempData["Warning"] = "No tiene materias asignadas. Contacte al administrador para asignar materia, grado y grupo.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Options = options;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateTeacherWorkPlanDto dto)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        var role = (await _currentUserService.GetCurrentUserRoleAsync())?.ToLower() ?? "";
        if (role == "admin") return Forbid();
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        try
        {
            var result = await _workPlanService.CreateAsync(user.Id, dto, school?.Id);
            return Json(new { success = true, id = result.Id, message = "Plan guardado correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Auth");
        var role = (await _currentUserService.GetCurrentUserRoleAsync())?.ToLower() ?? "";
        var isAdmin = role == "admin";
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        var plan = await _workPlanService.GetByIdAsync(id, isAdmin ? null : user.Id, school?.Id, isAdmin);
        if (plan == null) return NotFound();
        if (!isAdmin && plan.TeacherId != user.Id) return Forbid();
        if (isAdmin) return RedirectToAction(nameof(Index));
        var options = await _workPlanService.GetFormOptionsAsync(user.Id, school?.Id);
        ViewBag.Options = options;
        return View(plan);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [FromBody] CreateTeacherWorkPlanDto dto)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        var role = (await _currentUserService.GetCurrentUserRoleAsync())?.ToLower() ?? "";
        if (role == "admin") return Forbid();
        try
        {
            var result = await _workPlanService.UpdateAsync(id, user.Id, dto);
            return Json(new { success = true, message = "Plan actualizado correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        var role = (await _currentUserService.GetCurrentUserRoleAsync())?.ToLower() ?? "";
        if (role == "admin") return Forbid();
        try
        {
            await _workPlanService.DeleteAsync(id, user.Id);
            TempData["SuccessMessage"] = "Plan eliminado.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        var role = (await _currentUserService.GetCurrentUserRoleAsync())?.ToLower() ?? "";
        if (role == "admin") return Forbid();
        try
        {
            await _workPlanService.SubmitAsync(id, user.Id);
            TempData["SuccessMessage"] = "Plan enviado a revisión.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Auth");
        var role = (await _currentUserService.GetCurrentUserRoleAsync())?.ToLower() ?? "";
        var isAdmin = role == "admin";
        try
        {
            var pdf = await _pdfService.GeneratePdfAsync(id, user.Id, isAdmin);
            var plan = await _workPlanService.GetByIdAsync(id, isAdmin ? null : user.Id, user.SchoolId, isAdmin);
            var fileName = plan != null
                ? $"Plan_Trim_{plan.Trimester}_{plan.SubjectName?.Replace(" ", "_")}_{plan.GroupName}.pdf"
                : "Plan_Trabajo_Trimestral.pdf";
            return File(pdf, "application/pdf", fileName);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetFormOptions()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        var role = (await _currentUserService.GetCurrentUserRoleAsync())?.ToLower() ?? "";
        if (role == "admin") return Forbid();
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        var options = await _workPlanService.GetFormOptionsAsync(user.Id, school?.Id);
        return Json(options);
    }
}
