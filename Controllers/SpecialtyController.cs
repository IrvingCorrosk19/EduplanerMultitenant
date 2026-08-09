using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

[Authorize(Roles = "Director,Inspector,secretaria,admin,superadmin")]
public class SpecialtyController : Controller
{
    private readonly ISpecialtyService _specialtyService;

    public SpecialtyController(ISpecialtyService specialtyService)
    {
        _specialtyService = specialtyService;
    }

    public async Task<IActionResult> Index()
    {
        var specialties = await _specialtyService.GetAllAsync();
        return View(specialties);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] Specialty specialty)
    {
        if (string.IsNullOrWhiteSpace(specialty.Name))
        {
            return Json(new { success = false, message = "El nombre de la especialidad es obligatorio." });
        }

        try
        {
            var newSpecialty = await _specialtyService.CreateAsync(specialty);
            return Json(new { 
                success = true, 
                id = newSpecialty.Id, 
                name = newSpecialty.Name,
                description = newSpecialty.Description,
                message = "Especialidad creada exitosamente."
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error al crear la especialidad. Intente nuevamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromBody] Specialty specialty)
    {
        if (string.IsNullOrWhiteSpace(specialty.Name))
        {
            return Json(new { success = false, message = "El nombre de la especialidad es obligatorio." });
        }

        try
        {
            var updatedSpecialty = await _specialtyService.UpdateAsync(specialty);
            return Json(new { 
                success = true, 
                id = updatedSpecialty.Id, 
                name = updatedSpecialty.Name,
                description = updatedSpecialty.Description,
                message = "Especialidad actualizada exitosamente."
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error al actualizar la especialidad. Intente nuevamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromBody] DeleteSpecialtyRequest request)
    {
        if (request.Id == Guid.Empty)
        {
            return Json(new { success = false, message = "ID de especialidad inválido." });
        }

        try
        {
            await _specialtyService.DeleteAsync(request.Id);
            return Json(new { success = true, message = "Especialidad eliminada exitosamente." });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = "Error interno. Intente nuevamente." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error al eliminar la especialidad. Intente nuevamente." });
        }
    }
}

public class DeleteSpecialtyRequest
{
    public Guid Id { get; set; }
} 