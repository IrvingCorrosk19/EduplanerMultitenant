using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Models;

[Authorize(Roles = "Director,superadmin")]
public class AuditLogController : Controller
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 100)
    {
        var (items, totalCount) = await _auditLogService.GetAllAsync(page, pageSize);

        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return View(items);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var log = await _auditLogService.GetByIdAsync(id);
        if (log == null) return NotFound();
        return View(log);
    }

    public async Task<IActionResult> LogsByUser(Guid userId)
    {
        var logs = await _auditLogService.GetByUserAsync(userId);
        ViewBag.CurrentPage = 1;
        ViewBag.TotalPages = 1;
        ViewBag.TotalCount = logs.Count;
        return View("Index", logs);
    }
}
