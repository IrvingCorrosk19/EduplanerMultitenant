using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Dtos;
using SchoolManager.Interfaces;
using SchoolManager.Models;

[Authorize(Roles = "Director,Inspector,Teacher,Docente,secretaria,admin,superadmin")]
public class ActivityController : Controller
{
    private readonly IActivityService _activityService;

    public ActivityController(IActivityService activityService)
    {
        _activityService = activityService;
    }

    /* ---------- LISTAR Y DETALLES (ya funcionaban) ---------- */

    public async Task<IActionResult> Index()
    {
        var activities = await _activityService.GetAllAsync();
        return View(activities);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var activity = await _activityService.GetByIdAsync(id);
        if (activity == null) return NotFound();
        return View(activity);
    }

    /* ---------- CREAR ---------- */

    [Authorize(Roles = "Director,Inspector,secretaria,admin,superadmin")]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Director,Inspector,secretaria,admin,superadmin")]
    public async Task<IActionResult> Create(ActivityCreateDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        await _activityService.CreateAsync(dto);      // <-- ahora coincide con la interfaz
        return RedirectToAction(nameof(Index));
    }

    /* ---------- EDITAR (opcional, sigue usando entidad) ---------- */

    [Authorize(Roles = "Director,Inspector,secretaria,admin,superadmin")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var activity = await _activityService.GetByIdAsync(id);
        if (activity == null) return NotFound();
        return View(activity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Director,Inspector,secretaria,admin,superadmin")]
    public async Task<IActionResult> Edit(Activity activity)
    {
        if (!ModelState.IsValid) return View(activity);

        await _activityService.UpdateAsync(activity);
        return RedirectToAction(nameof(Index));
    }

    /* ---------- ELIMINAR ---------- */

    [Authorize(Roles = "Director,Inspector,secretaria,admin,superadmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var activity = await _activityService.GetByIdAsync(id);
        if (activity == null) return NotFound();
        return View(activity);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Director,Inspector,secretaria,admin,superadmin")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _activityService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
