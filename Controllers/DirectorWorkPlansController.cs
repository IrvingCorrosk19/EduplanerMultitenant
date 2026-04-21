using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Dtos;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Controllers;

[Authorize(Roles = "director,Director")]
public class DirectorWorkPlansController : Controller
{
    private readonly IDirectorWorkPlanService _directorWorkPlanService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAcademicYearService _academicYearService;

    public DirectorWorkPlansController(
        IDirectorWorkPlanService directorWorkPlanService,
        ICurrentUserService currentUserService,
        IAcademicYearService academicYearService)
    {
        _directorWorkPlanService = directorWorkPlanService;
        _currentUserService = currentUserService;
        _academicYearService = academicYearService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        if (school == null)
        {
            TempData["Error"] = "No se pudo determinar su institución.";
            return RedirectToAction("Index", "Director");
        }
        ViewBag.SchoolId = school.Id;
        var years = await _academicYearService.GetAllBySchoolAsync(school.Id);
        ViewBag.AcademicYears = years.Select(ay => new { ay.Id, ay.Name }).ToList();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> FilterOptionsJson()
    {
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        if (school == null) return Json(new { success = false });
        var options = await _directorWorkPlanService.GetFilterOptionsAsync(school.Id);
        return Json(new { success = true, data = options });
    }

    [HttpGet]
    public async Task<IActionResult> ListJson([FromQuery] WorkPlanFiltersDto filters)
    {
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        if (school == null) return Json(new { success = false, message = "Sin institución." });
        filters.SchoolId = school.Id;
        var dashboard = await _directorWorkPlanService.GetDashboardAsync(filters);
        return Json(new
        {
            success = true,
            kpis = new
            {
                total = dashboard.TotalPlans,
                submitted = dashboard.SubmittedCount,
                approved = dashboard.ApprovedCount,
                rejected = dashboard.RejectedCount,
                draft = dashboard.DraftCount
            },
            items = dashboard.Items,
            totalCount = dashboard.TotalCount
        });
    }

    [HttpGet]
    public async Task<IActionResult> DetailsJson(Guid id)
    {
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        if (school == null) return Json(new { success = false });
        var detail = await _directorWorkPlanService.GetPlanByIdAsync(id, school.Id);
        if (detail == null) return Json(new { success = false });
        return Json(new { success = true, data = detail });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveJson(Guid id, [FromBody] ApproveRejectRequestDto dto)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null) return Json(new { success = false, message = "No autenticado." });
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        if (school == null) return Json(new { success = false, message = "Sin institución." });
        try
        {
            await _directorWorkPlanService.ApproveAsync(id, user.Id, dto?.Comment);
            return Json(new { success = true, message = "Plan aprobado." });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectJson(Guid id, [FromBody] ApproveRejectRequestDto dto)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null) return Json(new { success = false, message = "No autenticado." });
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        if (school == null) return Json(new { success = false, message = "Sin institución." });
        if (string.IsNullOrWhiteSpace(dto?.Comment) || dto.Comment.Trim().Length < 10)
            return Json(new { success = false, message = "El comentario de rechazo es obligatorio (mínimo 10 caracteres)." });
        try
        {
            await _directorWorkPlanService.RejectAsync(id, user.Id, dto.Comment.Trim());
            return Json(new { success = true, message = "Plan rechazado." });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(Guid id)
    {
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        if (school == null) return Forbid();
        try
        {
            var pdf = await _directorWorkPlanService.ExportPlanPdfAsync(id, school.Id);
            return File(pdf, "application/pdf", $"Plan_Trimestral_{id:N}.pdf");
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportConsolidatedPdf([FromQuery] WorkPlanFiltersDto filters)
    {
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        if (school == null) return Forbid();
        var pdf = await _directorWorkPlanService.ExportConsolidatedPdfAsync(filters, school.Id);
        return File(pdf, "application/pdf", $"Planes_Consolidado_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }
}
