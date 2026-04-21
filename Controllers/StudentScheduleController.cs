using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Controllers;

/// <summary>
/// Módulo READ-ONLY para que el estudiante autenticado visualice su horario.
/// No recibe studentId por parámetro; no modifica ScheduleEntry, StudentAssignment ni ninguna entidad.
/// </summary>
[Authorize(Roles = "Student,student,Estudiante,estudiante")]
[AutoValidateAntiforgeryToken]
public class StudentScheduleController : Controller
{
    private readonly IScheduleService _scheduleService;
    private readonly ICurrentUserService _currentUserService;
    private readonly SchoolDbContext _context;
    private readonly IAcademicYearService _academicYearService;
    private readonly ILogger<StudentScheduleController> _logger;

    public StudentScheduleController(
        IScheduleService scheduleService,
        ICurrentUserService currentUserService,
        SchoolDbContext context,
        IAcademicYearService academicYearService,
        ILogger<StudentScheduleController> logger)
    {
        _scheduleService = scheduleService;
        _currentUserService = currentUserService;
        _context = context;
        _academicYearService = academicYearService;
        _logger = logger;
    }

    /// <summary>
    /// GET /StudentSchedule/MySchedule. Vista de horario del estudiante autenticado. Solo lectura.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> MySchedule(Guid? academicYearId)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null || user.SchoolId == null)
            return RedirectToAction("Index", "Home");

        var effectiveAcademicYearId = academicYearId ?? Guid.Empty;
        if (effectiveAcademicYearId == Guid.Empty)
        {
            var currentYear = await _academicYearService.GetActiveAcademicYearAsync(user.SchoolId).ConfigureAwait(false);
            if (currentYear != null)
                effectiveAcademicYearId = currentYear.Id;
        }

        var timeSlots = await _context.TimeSlots
            .AsNoTracking()
            .Where(t => t.SchoolId == user.SchoolId && t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.StartTime)
            .Select(t => new { t.Id, t.Name, StartTime = t.StartTime.ToString("HH:mm"), EndTime = t.EndTime.ToString("HH:mm"), t.DisplayOrder, ShiftName = t.Shift != null ? t.Shift.Name : (string?)null })
            .ToListAsync();

        var academicYears = await _academicYearService.GetAllBySchoolAsync(user.SchoolId.Value).ConfigureAwait(false);

        var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
        ViewBag.TimeSlotsJson = System.Text.Json.JsonSerializer.Serialize(timeSlots, jsonOptions);
        ViewBag.AcademicYearsJson = System.Text.Json.JsonSerializer.Serialize(academicYears.Select(a => new { a.Id, a.Name }), jsonOptions);
        ViewBag.AcademicYearId = effectiveAcademicYearId;
        ViewBag.HasNoAcademicYears = academicYears.Count == 0;

        return View();
    }

    /// <summary>
    /// GET /StudentSchedule/ListJsonMySchedule. Devuelve las entradas de horario del estudiante autenticado. Solo lectura.
    /// No recibe studentId; usa únicamente CurrentUser.Id.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListJsonMySchedule(Guid? academicYearId)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null || user.SchoolId == null)
            return Json(new { success = false, message = "Usuario no autenticado o sin escuela.", data = (object?)null });

        var effectiveAcademicYearId = academicYearId ?? Guid.Empty;
        if (effectiveAcademicYearId == Guid.Empty)
        {
            var currentYear = await _academicYearService.GetActiveAcademicYearAsync(user.SchoolId).ConfigureAwait(false);
            if (currentYear == null)
                return Json(new { success = true, message = "", data = Array.Empty<object>() });
            effectiveAcademicYearId = currentYear.Id;
        }

        var entries = await _scheduleService.GetByStudentUserAsync(user.Id, effectiveAcademicYearId).ConfigureAwait(false);
        var data = entries.Select(MapEntryToJson).ToList();
        return Json(new { success = true, message = "", data });
    }

    private static object MapEntryToJson(ScheduleEntry e)
    {
        var ta = e.TeacherAssignment;
        var sa = ta?.SubjectAssignment;
        var subjectName = sa?.Subject?.Name;
        var groupName = sa?.Group?.Name;
        var teacherName = ta?.Teacher != null ? $"{ta.Teacher.Name} {ta.Teacher.LastName}".Trim() : null;
        var timeSlot = e.TimeSlot;
        return new
        {
            id = e.Id,
            teacherAssignmentId = e.TeacherAssignmentId,
            timeSlotId = e.TimeSlotId,
            dayOfWeek = e.DayOfWeek,
            academicYearId = e.AcademicYearId,
            subjectName,
            groupName,
            teacherName,
            timeSlotName = timeSlot?.Name,
            startTime = timeSlot != null ? timeSlot.StartTime.ToString("HH\\:mm") : null,
            endTime = timeSlot != null ? timeSlot.EndTime.ToString("HH\\:mm") : null,
            createdAt = e.CreatedAt
        };
    }
}
