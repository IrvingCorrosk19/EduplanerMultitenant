using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SchoolManager.Models;
using SchoolManager.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[Authorize(Roles = "Director,Inspector,Teacher,Docente,secretaria,admin")]
public class AttendanceController : Controller
{
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(IAttendanceService attendanceService, ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var attendances = await _attendanceService.GetAllAsync();
        return View(attendances);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var attendance = await _attendanceService.GetByIdAsync(id);
        if (attendance == null) return NotFound();
        return View(attendance);
    }

    [Authorize(Roles = "Director,Inspector,secretaria,admin")]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Director,Inspector,secretaria,admin")]
    public async Task<IActionResult> Create(Attendance attendance)
    {
        if (ModelState.IsValid)
        {
            await _attendanceService.CreateAsync(attendance);
            return RedirectToAction(nameof(Index));
        }
        return View(attendance);
    }

    [Authorize(Roles = "Director,Inspector,secretaria,admin")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var attendance = await _attendanceService.GetByIdAsync(id);
        if (attendance == null) return NotFound();
        return View(attendance);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Director,Inspector,secretaria,admin")]
    public async Task<IActionResult> Edit(Attendance attendance)
    {
        if (ModelState.IsValid)
        {
            await _attendanceService.UpdateAsync(attendance);
            return RedirectToAction(nameof(Index));
        }
        return View(attendance);
    }

    [Authorize(Roles = "Director,Inspector")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var attendance = await _attendanceService.GetByIdAsync(id);
        if (attendance == null) return NotFound();
        return View(attendance);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Director,Inspector")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _attendanceService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAttendances([FromBody] List<AttendanceSaveDto> attendances)
    {
        try
        {
            await _attendanceService.SaveAttendancesAsync(attendances);
            return Ok(new { success = true, message = "Asistencias guardadas correctamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Error interno. Intente nuevamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Historial([FromBody] HistorialAsistenciaFiltroDto filtro)
    {
        try
        {
            var resultado = await _attendanceService.GetHistorialAsistenciaAsync(filtro);
            return Json(resultado);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Error interno. Intente nuevamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Estadisticas([FromBody] EstadisticasFiltroDto filtro)
    {
        _logger.LogDebug("Estadísticas solicitadas: Trimestre={Trimestre}, GroupId={GroupId}, GradeId={GradeId}", filtro.Trimestre, filtro.GroupId, filtro.GradeId);
        if (filtro == null || filtro.GroupId == Guid.Empty || filtro.GradeId == Guid.Empty || string.IsNullOrEmpty(filtro.Trimestre))
            return BadRequest("Faltan datos para la consulta.");

        var estadisticas = await _attendanceService.GetEstadisticasAsync(
            filtro.GroupId, filtro.GradeId, filtro.Trimestre, filtro.FechaInicio, filtro.FechaFin
        );
        return Json(estadisticas);
    }
}
