using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SchoolManager.Dtos;
using SchoolManager.Services.Interfaces;
using SchoolManager.Interfaces;

namespace SchoolManager.Controllers;

[Authorize(Roles = "admin,superadmin")]
public class PaymentConceptController : Controller
{
    private readonly IPaymentConceptService _paymentConceptService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PaymentConceptController> _logger;

    public PaymentConceptController(
        IPaymentConceptService paymentConceptService,
        ICurrentUserService currentUserService,
        ILogger<PaymentConceptController> logger)
    {
        _paymentConceptService = paymentConceptService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        if (currentUser?.SchoolId == null)
            return Unauthorized();

        var concepts = await _paymentConceptService.GetAllAsync(currentUser.SchoolId.Value);
        return View(concepts);
    }

    public IActionResult Create()
    {
        return View(new PaymentConceptCreateDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentConceptCreateDto dto)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        if (currentUser?.SchoolId == null)
            return Unauthorized();

        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            dto.SchoolId = currentUser.SchoolId.Value;
            await _paymentConceptService.CreateAsync(dto, currentUser.Id);
            
            TempData["SuccessMessage"] = "Concepto de pago creado exitosamente";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear concepto de pago");
            ModelState.AddModelError("", "Error al crear el concepto de pago: " + ex.Message);
            return View(dto);
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var concept = await _paymentConceptService.GetByIdAsync(id);
        if (concept == null)
            return NotFound();

        var currentUser = await _currentUserService.GetCurrentUserAsync();
        if (currentUser?.SchoolId == null || concept.SchoolId != currentUser.SchoolId.Value)
            return Unauthorized();

        var dto = new PaymentConceptCreateDto
        {
            Name = concept.Name,
            Description = concept.Description,
            Amount = concept.Amount,
            Periodicity = concept.Periodicity,
            IsActive = concept.IsActive
        };

        ViewBag.ConceptId = id;
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PaymentConceptCreateDto dto)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();

        if (!ModelState.IsValid)
        {
            ViewBag.ConceptId = id;
            return View(dto);
        }

        try
        {
            await _paymentConceptService.UpdateAsync(id, dto, currentUser.Id);
            
            TempData["SuccessMessage"] = "Concepto de pago actualizado exitosamente";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar concepto de pago");
            ModelState.AddModelError("", "Error al actualizar el concepto de pago: " + ex.Message);
            ViewBag.ConceptId = id;
            return View(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var concept = await _paymentConceptService.GetByIdAsync(id);
        if (concept == null)
            return NotFound();

        var currentUser = await _currentUserService.GetCurrentUserAsync();
        if (currentUser?.SchoolId == null || concept.SchoolId != currentUser.SchoolId.Value)
            return Unauthorized();

        try
        {
            await _paymentConceptService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Concepto de pago eliminado exitosamente";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar concepto de pago");
            TempData["ErrorMessage"] = "Error al eliminar el concepto de pago: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}

