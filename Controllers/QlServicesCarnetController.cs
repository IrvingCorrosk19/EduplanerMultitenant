using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Dtos;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Controllers;

/// <summary>API QL Services: carnets pagados pendientes de impresión, marcar Impreso y Entregado.</summary>
[Authorize(Roles = "QlServices,Admin")]
[Route("QlServices")]
public class QlServicesCarnetController : Controller
{
    private readonly IQlServicesCarnetService _service;
    private readonly ILogger<QlServicesCarnetController> _logger;

    public QlServicesCarnetController(IQlServicesCarnetService service, ILogger<QlServicesCarnetController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>GET /QlServices/Api/Carnet/PendingPrint — Lista carnets con estado Pagado (pendientes de impresión).</summary>
    [HttpGet("Api/Carnet/PendingPrint")]
    public async Task<IActionResult> GetPendingPrint()
    {
        try
        {
            var list = await _service.GetPendingPrintAsync();
            return Ok(new { data = list });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[QlServices] GetPendingPrint error");
            return StatusCode(500, new { message = "Error al obtener la lista." });
        }
    }

    /// <summary>POST /QlServices/Carnet/MarkPrinted — Marcar carnet como Impreso (Pagado → Impreso).</summary>
    [HttpPost("Carnet/MarkPrinted")]
    public async Task<IActionResult> MarkPrinted([FromBody] StudentIdRequest request)
    {
        if (request == null || request.StudentId == Guid.Empty)
            return BadRequest(new { message = "StudentId es requerido." });

        try
        {
            await _service.MarkCarnetAsPrintedAsync(request.StudentId);
            return Ok(new { success = true, message = "Carnet marcado como impreso." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QlServices] MarkPrinted StudentId={StudentId}", request.StudentId);
            return StatusCode(500, new { message = "Error al marcar como impreso." });
        }
    }

    /// <summary>POST /QlServices/Carnet/MarkDelivered — Marcar carnet como Entregado (Impreso → Entregado).</summary>
    [HttpPost("Carnet/MarkDelivered")]
    public async Task<IActionResult> MarkDelivered([FromBody] StudentIdRequest request)
    {
        if (request == null || request.StudentId == Guid.Empty)
            return BadRequest(new { message = "StudentId es requerido." });

        try
        {
            await _service.MarkCarnetAsDeliveredAsync(request.StudentId);
            return Ok(new { success = true, message = "Carnet marcado como entregado." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QlServices] MarkDelivered StudentId={StudentId}", request.StudentId);
            return StatusCode(500, new { message = "Error al marcar como entregado." });
        }
    }
}
