using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Controllers;

[Authorize(Roles = "SuperAdmin,superadmin,admin,director")]
[Route("id-card/settings")]
public class IdCardSettingsController : Controller
{
    private readonly SchoolDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public IdCardSettingsController(SchoolDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] Guid? schoolId)
    {
        // No mostrar en esta página errores de otras secciones (ej. "Estudiante no encontrado" de Carnet)
        TempData.Remove("Error");

        var school = await _currentUserService.GetCurrentUserSchoolAsync();

        // SuperAdmin no tiene escuela: debe elegir una
        if (school == null)
        {
            var schoolList = await _context.Schools
                .OrderBy(s => s.Name)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();
            ViewBag.Schools = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(schoolList, "Id", "Name");
            ViewBag.NeedSchoolSelection = true;

            if (!schoolId.HasValue || schoolId == Guid.Empty)
                return View(new SchoolIdCardSetting { SchoolId = Guid.Empty, TemplateKey = "default_v1", PageWidthMm = 55, PageHeightMm = 85, BackgroundColor = "#FFFFFF", PrimaryColor = "#0D6EFD", TextColor = "#111111", ShowQr = true, ShowPhoto = true, ShowSchoolPhone = true, ShowWatermark = true, Orientation = "Vertical" });

            var selectedSchool = await _context.Schools.FindAsync(schoolId.Value);
            if (selectedSchool == null)
            {
                TempData["IdCardSettings.Error"] = "Escuela no encontrada o inactiva.";
                return RedirectToAction("Index");
            }
            school = selectedSchool;
            ViewBag.NeedSchoolSelection = false;
            ViewBag.SelectedSchoolId = schoolId.Value;
            ViewBag.SelectedSchoolName = selectedSchool.Name;
        }
        else
        {
            ViewBag.NeedSchoolSelection = false;
            ViewBag.SelectedSchoolId = school.Id;
            ViewBag.SelectedSchoolName = school.Name;
        }

        var settings = await _context.Set<SchoolIdCardSetting>()
            .FirstOrDefaultAsync(x => x.SchoolId == school.Id);

        settings ??= new SchoolIdCardSetting 
        { 
            SchoolId = school.Id,
            TemplateKey = "default_v1",
            PageWidthMm = 55,
            PageHeightMm = 85,
            BackgroundColor = "#FFFFFF",
            PrimaryColor = "#0D6EFD",
            TextColor = "#111111",
            ShowQr = true,
            ShowPhoto = true,
            ShowSchoolPhone = true,
            ShowEmergencyContact = false,
            ShowAllergies = false,
            Orientation = "Vertical",
            ShowWatermark = true
        };

        ViewBag.IdCardPolicy = school.IdCardPolicy ?? "";
        return View(settings);
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save(SchoolIdCardSetting model)
    {
        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        if (school == null)
        {
            // SuperAdmin: usar escuela enviada en el formulario
            if (model.SchoolId == Guid.Empty)
            {
                TempData["IdCardSettings.Error"] = "Seleccione una escuela.";
                return RedirectToAction("Index");
            }
            school = await _context.Schools.FindAsync(model.SchoolId);
            if (school == null)
            {
                TempData["IdCardSettings.Error"] = "Escuela no encontrada o inactiva.";
                return RedirectToAction("Index");
            }
        }
        else
            model.SchoolId = school.Id;

        // Guardar política del carnet en School (única por escuela)
        var idCardPolicy = Request.Form["IdCardPolicy"].ToString();
        var schoolEntity = await _context.Schools.FindAsync(school.Id);
        if (schoolEntity != null)
        {
            schoolEntity.IdCardPolicy = string.IsNullOrWhiteSpace(idCardPolicy) ? null : idCardPolicy.Trim();
        }

        var existing = await _context.Set<SchoolIdCardSetting>()
            .FirstOrDefaultAsync(x => x.SchoolId == school.Id);

        if (existing == null)
        {
            model.Id = Guid.NewGuid();
            model.Orientation = model.Orientation ?? "Vertical";
            var isHorizontal = string.Equals(model.Orientation, "Horizontal", StringComparison.OrdinalIgnoreCase);
            model.PageWidthMm = isHorizontal ? 85 : 55;
            model.PageHeightMm = isHorizontal ? 55 : 85;
            model.SecondaryLogoUrl = string.IsNullOrWhiteSpace(model.SecondaryLogoUrl) ? null : model.SecondaryLogoUrl.Trim();
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;
            _context.Add(model);
        }
        else
        {
            existing.TemplateKey = model.TemplateKey;
            existing.PageWidthMm = model.PageWidthMm;
            existing.PageHeightMm = model.PageHeightMm;
            existing.BleedMm = model.BleedMm;
            existing.BackgroundColor = model.BackgroundColor;
            existing.PrimaryColor = model.PrimaryColor;
            existing.TextColor = model.TextColor;
            existing.ShowQr = model.ShowQr;
            existing.ShowPhoto = model.ShowPhoto;
            existing.ShowSchoolPhone = model.ShowSchoolPhone;
            existing.ShowEmergencyContact = model.ShowEmergencyContact;
            existing.ShowAllergies = model.ShowAllergies;
            existing.Orientation = model.Orientation ?? "Vertical";
            existing.ShowWatermark = model.ShowWatermark;
            // Sincronizar dimensiones con orientación para que el PDF y la vista previa sean consistentes
            var isHorizontal = string.Equals(existing.Orientation, "Horizontal", StringComparison.OrdinalIgnoreCase);
            existing.PageWidthMm = isHorizontal ? 85 : 55;
            existing.PageHeightMm = isHorizontal ? 55 : 85;
            // Campos del diseño moderno
            existing.UseModernLayout = model.UseModernLayout;
            existing.ShowDocumentId = model.ShowDocumentId;
            existing.ShowPolicyNumber = model.ShowPolicyNumber;
            existing.ShowAcademicYear = model.ShowAcademicYear;
            existing.ShowSecondaryLogo = model.ShowSecondaryLogo;
            existing.SecondaryLogoUrl = string.IsNullOrWhiteSpace(model.SecondaryLogoUrl) ? null : model.SecondaryLogoUrl.Trim();
            existing.UpdatedAt = DateTime.UtcNow;
            _context.Update(existing);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Configuración guardada exitosamente.";

        var isSuperAdmin = await _currentUserService.GetCurrentUserSchoolAsync() == null;
        return isSuperAdmin
            ? RedirectToAction("Index", new { schoolId = model.SchoolId })
            : RedirectToAction("Index");
    }
}
