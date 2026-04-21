using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Dtos;
using SchoolManager.Repositories.Interfaces;
using SchoolManager.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace SchoolManager.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,superadmin,Admin,admin")]
[Route("Admin/EmailJobs")]
public class EmailJobsController : Controller
{
    private const int PageSize = 50;

    private readonly IEmailJobService        _emailJobService;
    private readonly ICurrentUserService     _currentUserService;
    private readonly IEmailQueueRepository   _emailQueueRepository;

    public EmailJobsController(
        IEmailJobService      emailJobService,
        ICurrentUserService   currentUserService,
        IEmailQueueRepository emailQueueRepository)
    {
        _emailJobService      = emailJobService;
        _currentUserService   = currentUserService;
        _emailQueueRepository = emailQueueRepository;
    }

    [HttpGet]
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index([FromQuery] int page = 1)
    {
        var me        = await _currentUserService.GetCurrentUserAsync();
        var isSuper   = string.Equals(me?.Role, "superadmin", StringComparison.OrdinalIgnoreCase);
        var schoolId  = isSuper ? (Guid?)null : me?.SchoolId;

        var summary = await _emailJobService.GetSummaryAsync(schoolId);
        var jobs    = await _emailJobService.GetJobsAsync(schoolId, page, PageSize);

        ViewBag.Summary  = summary;
        ViewBag.Page     = page;
        ViewBag.PageSize = PageSize;
        ViewBag.IsSuper  = isSuper;

        return View("~/Views/Admin/EmailJobs/Index.cshtml", jobs);
    }

    [HttpGet]
    [Route("Details/{jobId:guid}")]
    public async Task<IActionResult> Details(Guid jobId)
    {
        var me       = await _currentUserService.GetCurrentUserAsync();
        var isSuper  = string.Equals(me?.Role, "superadmin", StringComparison.OrdinalIgnoreCase);
        var schoolId = isSuper ? (Guid?)null : me?.SchoolId;

        var vm = await _emailJobService.GetJobDetailsAsync(jobId, schoolId);
        if (vm == null) return NotFound();

        return View("~/Views/Admin/EmailJobs/Details.cshtml", vm);
    }

    /// <summary>JSON para DataTable en Index.</summary>
    [HttpGet]
    [Route("ListJson")]
    public async Task<IActionResult> ListJson([FromQuery] int page = 1)
    {
        var me       = await _currentUserService.GetCurrentUserAsync();
        var isSuper  = string.Equals(me?.Role, "superadmin", StringComparison.OrdinalIgnoreCase);
        var schoolId = isSuper ? (Guid?)null : me?.SchoolId;

        var jobs = await _emailJobService.GetJobsAsync(schoolId, page, PageSize);
        return Json(jobs);
    }

    /// <summary>JSON de detalle de ítems para polling.</summary>
    [HttpGet]
    [Route("DetailsJson/{jobId:guid}")]
    public async Task<IActionResult> DetailsJson(Guid jobId)
    {
        var me       = await _currentUserService.GetCurrentUserAsync();
        var isSuper  = string.Equals(me?.Role, "superadmin", StringComparison.OrdinalIgnoreCase);
        var schoolId = isSuper ? (Guid?)null : me?.SchoolId;

        // Refresh counters before responding
        await _emailQueueRepository.RefreshJobCountersAsync(jobId);

        var vm = await _emailJobService.GetJobDetailsAsync(jobId, schoolId);
        if (vm == null) return NotFound(new { message = "Job no encontrado.", jobId });

        return Json(new JobStatusDto
        {
            JobId         = vm.JobId,
            CorrelationId = vm.CorrelationId,
            Status        = vm.Status,
            TotalItems    = vm.TotalItems,
            SentCount     = vm.SentCount,
            FailedCount   = vm.FailedCount,
            RejectedCount = vm.RejectedCount,
            RequestedAt   = vm.RequestedAt,
            StartedAt     = vm.StartedAt,
            CompletedAt   = vm.CompletedAt
        });
    }

    /// <summary>Métricas de resumen (tarjetas).</summary>
    [HttpGet]
    [Route("SummaryJson")]
    public async Task<IActionResult> SummaryJson()
    {
        var me       = await _currentUserService.GetCurrentUserAsync();
        var isSuper  = string.Equals(me?.Role, "superadmin", StringComparison.OrdinalIgnoreCase);
        var schoolId = isSuper ? (Guid?)null : me?.SchoolId;

        var summary = await _emailJobService.GetSummaryAsync(schoolId);
        return Json(summary);
    }

    // ── Reintentos administrativos ─────────────────────────────────────────────

    /// <summary>
    /// Reintenta todos los ítems Failed/DeadLetter del job.
    /// Antiforgery validado vía header RequestVerificationToken.
    /// </summary>
    [HttpPost]
    [Route("RetryJobFailed/{jobId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryJobFailed(Guid jobId)
    {
        var me = await _currentUserService.GetCurrentUserAsync();
        if (me == null) return Unauthorized();

        var isSuper  = string.Equals(me.Role, "superadmin", StringComparison.OrdinalIgnoreCase);
        var schoolId = isSuper ? (Guid?)null : me.SchoolId;

        var result = await _emailJobService.RetryJobFailedAsync(jobId, schoolId, me.Id);
        return Json(result);
    }

    /// <summary>
    /// Reintenta un único ítem de la cola.
    /// jobId en query-string para verificación de ownership multi-tenant.
    /// </summary>
    [HttpPost]
    [Route("RetryItem/{queueItemId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryItem(Guid queueItemId, [FromQuery] Guid jobId)
    {
        if (jobId == Guid.Empty)
            return BadRequest(new { success = false, message = "jobId es requerido." });

        var me = await _currentUserService.GetCurrentUserAsync();
        if (me == null) return Unauthorized();

        var isSuper  = string.Equals(me.Role, "superadmin", StringComparison.OrdinalIgnoreCase);
        var schoolId = isSuper ? (Guid?)null : me.SchoolId;

        var result = await _emailJobService.RetryItemAsync(queueItemId, jobId, schoolId, me.Id);
        return Json(result);
    }
}
