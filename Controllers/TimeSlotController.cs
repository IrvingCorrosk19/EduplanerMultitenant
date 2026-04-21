using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Controllers;

[Authorize(Roles = "Admin,Director,admin,director")]
public class TimeSlotController : Controller
{
    private readonly SchoolDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public TimeSlotController(SchoolDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Gestión de bloques horarios (Admin/Director). URL: /TimeSlot/Manage
    /// </summary>
    [HttpGet]
    [Route("TimeSlot/Manage")]
    public async Task<IActionResult> Manage()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user?.SchoolId == null)
            return RedirectToAction("Index", "Home");
        var list = await _context.TimeSlots
            .Where(t => t.SchoolId == user.SchoolId.Value)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.StartTime)
            .ToListAsync();
        return View("Index", list);
    }

    public async Task<IActionResult> Index()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user?.SchoolId == null)
            return RedirectToAction("Index", "Home");

        var list = await _context.TimeSlots
            .Where(t => t.SchoolId == user.SchoolId.Value)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.StartTime)
            .ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new TimeSlot
        {
            StartTime = new TimeOnly(7, 0),
            EndTime = new TimeOnly(7, 45), // 45 min por bloque (jornada estándar)
            DisplayOrder = 0
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TimeSlot model)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user?.SchoolId == null)
            return RedirectToAction("Index", "Home");

        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError("Name", "El nombre del bloque es requerido.");
        if (model.EndTime <= model.StartTime)
            ModelState.AddModelError("EndTime", "La hora de fin debe ser posterior a la de inicio.");

        if (ModelState.IsValid)
        {
            model.Id = Guid.NewGuid();
            model.SchoolId = user.SchoolId.Value;
            model.IsActive = true;
            model.CreatedAt = DateTime.UtcNow;
            if (model.DisplayOrder < 0) model.DisplayOrder = 0;
            _context.TimeSlots.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Bloque horario creado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user?.SchoolId == null)
            return RedirectToAction("Index", "Home");

        var slot = await _context.TimeSlots.FirstOrDefaultAsync(t => t.Id == id && t.SchoolId == user.SchoolId.Value);
        if (slot == null)
            return NotFound();
        return View(slot);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TimeSlot model)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user?.SchoolId == null)
            return RedirectToAction("Index", "Home");

        var slot = await _context.TimeSlots.FirstOrDefaultAsync(t => t.Id == id && t.SchoolId == user.SchoolId.Value);
        if (slot == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError("Name", "El nombre del bloque es requerido.");
        if (model.EndTime <= model.StartTime)
            ModelState.AddModelError("EndTime", "La hora de fin debe ser posterior a la de inicio.");

        if (ModelState.IsValid)
        {
            slot.Name = model.Name.Trim();
            slot.StartTime = model.StartTime;
            slot.EndTime = model.EndTime;
            slot.DisplayOrder = model.DisplayOrder < 0 ? 0 : model.DisplayOrder;
            slot.IsActive = model.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Bloque horario actualizado.";
            return RedirectToAction(nameof(Index));
        }
        return View(slot);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user?.SchoolId == null)
            return RedirectToAction("Index", "Home");

        var slot = await _context.TimeSlots.FirstOrDefaultAsync(t => t.Id == id && t.SchoolId == user.SchoolId.Value);
        if (slot == null)
            return NotFound();

        var hasEntries = await _context.ScheduleEntries.AnyAsync(e => e.TimeSlotId == id);
        if (hasEntries)
        {
            slot.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Bloque desactivado (tiene horarios asignados).";
        }
        else
        {
            _context.TimeSlots.Remove(slot);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Bloque horario eliminado.";
        }
        return RedirectToAction(nameof(Index));
    }
}
