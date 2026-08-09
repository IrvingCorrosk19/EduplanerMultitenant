using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Models;
using SchoolManager.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SchoolManager.Services.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using SchoolManager.Helpers;

[Authorize]
public class DisciplineReportController : Controller
{
    private readonly IDisciplineReportService _disciplineReportService;
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly ILogger<DisciplineReportController> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly SchoolDbContext _context;

    public DisciplineReportController(
        IDisciplineReportService disciplineReportService, 
        IUserService userService,
        IEmailService emailService,
        ILogger<DisciplineReportController> logger,
        ICurrentUserService currentUserService,
        SchoolDbContext context)
    {
        _disciplineReportService = disciplineReportService;
        _userService = userService;
        _emailService = emailService;
        _logger = logger;
        _currentUserService = currentUserService;
        _context = context;
    }

    [Authorize(Roles = "Director,Inspector,Docente,Teacher")]
    public async Task<IActionResult> Index()
    {
        var reports = await _disciplineReportService.GetAllAsync();
        return View(reports);
    }

    [Authorize(Roles = "Director,Inspector,Docente,Teacher")]
    public async Task<IActionResult> Details(Guid id)
    {
        var report = await _disciplineReportService.GetByIdAsync(id);
        if (report == null) return NotFound();
        return View(report);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Director,Inspector,Docente,Teacher")]
    public async Task<IActionResult> CreateWithFiles()
    {
        try
        {
            // Obtener datos del formulario
            var studentId = Request.Form["StudentId"].ToString();
            var teacherId = Request.Form["TeacherId"].ToString();
            var subjectId = Request.Form["SubjectId"].ToString();
            var groupId = Request.Form["GroupId"].ToString();
            var gradeLevelId = Request.Form["GradeLevelId"].ToString();
            var date = Request.Form["Date"].ToString();
            var hora = Request.Form["Hora"].ToString();
            var reportType = Request.Form["ReportType"].ToString();
            var status = Request.Form["Status"].ToString();
            var description = Request.Form["Description"].ToString();
            var category = Request.Form["Category"].ToString();

            // Validar datos requeridos
            if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(teacherId) || string.IsNullOrEmpty(date) || string.IsNullOrEmpty(hora))
            {
                return Json(new { success = false, error = "Datos requeridos faltantes" });
            }

            // Procesar archivos si existen
            var documentsJson = "";
            var files = Request.Form.Files.Where(f => f.Name == "Documents").ToList();
            if (files.Any())
            {
                var documentList = new List<object>();
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "discipline");
                if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

                foreach (var file in files)
                {
                    // Validar tipo y tamaño antes de guardar
                    var (isValid, validationError) = FileUploadValidator.Validate(file, FileUploadValidator.AllowedDocumentExtensions);
                    if (!isValid)
                    {
                        return Json(new { success = false, message = $"Archivo rechazado: {validationError}" });
                    }

                    // Generar nombre único (Path.GetFileName evita path traversal)
                    var safeExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var fileName = $"{Guid.NewGuid()}{safeExtension}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    documentList.Add(new
                    {
                        fileName = Path.GetFileName(file.FileName), // nombre original (sin rutas)
                        savedName = fileName,
                        size = file.Length,
                        uploadDate = DateTime.UtcNow
                    });
                }
                documentsJson = JsonSerializer.Serialize(documentList);
            }

            var disciplineActionList = ParseDisciplineActionList(Request.Form);
            if (disciplineActionList.Count == 0)
            {
                return Json(new { success = false, error = "Debe seleccionar al menos una acción observada" });
            }

            var disciplineActionsJson = JsonSerializer.Serialize(disciplineActionList);

            // Obtener usuario autenticado y su school_id
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
            var schoolScopeId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (!schoolScopeId.HasValue)
            {
                return Json(new { success = false, error = "Su cuenta no tiene escuela asignada; no puede crear reportes de disciplina." });
            }

            if (!Guid.TryParse(studentId, out var studentGuid) || !Guid.TryParse(teacherId, out var teacherGuid))
            {
                return Json(new { success = false, error = "Identificadores de estudiante o docente inválidos." });
            }

            var studentInSchool = await _context.Users.AsNoTracking()
                .AnyAsync(u => u.Id == studentGuid && u.SchoolId == schoolScopeId);
            var teacherInSchool = await _context.Users.AsNoTracking()
                .AnyAsync(u => u.Id == teacherGuid && u.SchoolId == schoolScopeId);
            if (!studentInSchool || !teacherInSchool)
            {
                return Json(new { success = false, error = "El estudiante o el docente no pertenecen a su escuela." });
            }

            if (!string.IsNullOrEmpty(subjectId) && Guid.TryParse(subjectId, out var subjectGuid))
            {
                var subjectOk = await _context.Subjects.AsNoTracking()
                    .AnyAsync(s => s.Id == subjectGuid && s.SchoolId == schoolScopeId);
                if (!subjectOk)
                    return Json(new { success = false, error = "La materia no es válida para su escuela." });
            }

            if (!string.IsNullOrEmpty(groupId) && Guid.TryParse(groupId, out var groupGuid))
            {
                var groupOk = await _context.Groups.AsNoTracking()
                    .AnyAsync(g => g.Id == groupGuid && g.SchoolId == schoolScopeId);
                if (!groupOk)
                    return Json(new { success = false, error = "El grupo no es válido para su escuela." });
            }

            if (!string.IsNullOrEmpty(gradeLevelId) && Guid.TryParse(gradeLevelId, out var gradeGuid))
            {
                var gradeOk = await _context.GradeLevels.AsNoTracking()
                    .AnyAsync(gl => gl.Id == gradeGuid && gl.SchoolId == schoolScopeId);
                if (!gradeOk)
                    return Json(new { success = false, error = "El grado no es válido para su escuela." });
            }

            var disciplineReport = new DisciplineReport
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolScopeId.Value,
                StudentId = studentGuid,
                TeacherId = teacherGuid,
                SubjectId = !string.IsNullOrEmpty(subjectId) ? Guid.Parse(subjectId) : (Guid?)null,
                GroupId = !string.IsNullOrEmpty(groupId) ? Guid.Parse(groupId) : (Guid?)null,
                GradeLevelId = !string.IsNullOrEmpty(gradeLevelId) ? Guid.Parse(gradeLevelId) : (Guid?)null,
                Date = DateTime.SpecifyKind(DateTime.Parse($"{date} {hora}"), DateTimeKind.Local).ToUniversalTime(),
                ReportType = reportType,
                Status = status,
                Description = description,
                Category = category,
                Documents = documentsJson,
                DisciplineActionsJson = disciplineActionsJson,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId, // ✅ ID del usuario autenticado
                UpdatedBy = currentUserId  // ✅ ID del usuario autenticado
            };

            try
            {
                await _disciplineReportService.CreateAsync(disciplineReport);
                return Json(new { success = true, message = "Registro guardado correctamente", disciplineReportId = disciplineReport.Id });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al guardar en la base de datos");
                return Json(new { success = false, error = "Error al guardar en la base de datos", details = ex.InnerException?.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el reporte de disciplina");
            return Json(new { success = false, error = "Error al crear el reporte", details = "Error interno. Intente nuevamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Director,Inspector,Docente,Teacher")]
    public async Task<IActionResult> UpdateWithFilesForTeacher()
    {
        try
        {
            var reportIdStr = Request.Form["ReportId"].ToString();
            if (string.IsNullOrEmpty(reportIdStr) || !Guid.TryParse(reportIdStr, out var reportId))
                return Json(new { success = false, error = "ID de reporte inválido" });

            var report = await GetOwnedDisciplineReportForTeacherAsync(reportId);
            if (report == null)
                return Json(new { success = false, error = "No autorizado o registro no encontrado" });

            var date = Request.Form["Date"].ToString();
            var hora = Request.Form["Hora"].ToString();
            var reportType = Request.Form["ReportType"].ToString();
            var status = Request.Form["Status"].ToString();
            var description = Request.Form["Description"].ToString();
            var category = Request.Form["Category"].ToString();

            if (string.IsNullOrEmpty(date) || string.IsNullOrEmpty(hora))
                return Json(new { success = false, error = "Fecha y hora son requeridas" });

            var disciplineActionList = ParseDisciplineActionList(Request.Form);
            if (disciplineActionList.Count == 0)
                return Json(new { success = false, error = "Debe seleccionar al menos una acción observada" });

            var files = Request.Form.Files.Where(f => f.Name == "Documents").ToList();
            if (files.Any(f => f.Length > 0))
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "discipline");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                JsonArray arr;
                if (string.IsNullOrWhiteSpace(report.Documents))
                    arr = new JsonArray();
                else
                {
                    try
                    {
                        var node = JsonNode.Parse(report.Documents!);
                        arr = node as JsonArray ?? new JsonArray();
                    }
                    catch
                    {
                        arr = new JsonArray();
                    }
                }

                foreach (var file in files.Where(f => f.Length > 0))
                {
                    var (isValid, validationError) = FileUploadValidator.Validate(file, FileUploadValidator.AllowedDocumentExtensions);
                    if (!isValid)
                        return Json(new { success = false, message = $"Archivo rechazado: {validationError}" });

                    var safeExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var fileName = $"{Guid.NewGuid()}{safeExtension}";
                    var filePath = Path.Combine(uploadsPath, fileName);
                    await using (var stream = new FileStream(filePath, FileMode.Create))
                        await file.CopyToAsync(stream);

                    arr.Add(new JsonObject
                    {
                        ["fileName"] = Path.GetFileName(file.FileName),
                        ["savedName"] = fileName,
                        ["size"] = file.Length,
                        ["uploadDate"] = DateTime.UtcNow
                    });
                }

                report.Documents = arr.ToJsonString();
            }

            var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
            report.Date = DateTime.SpecifyKind(DateTime.Parse($"{date} {hora}"), DateTimeKind.Local).ToUniversalTime();
            report.ReportType = reportType;
            report.Status = status;
            report.Description = description;
            report.Category = category;
            report.DisciplineActionsJson = JsonSerializer.Serialize(disciplineActionList);
            report.UpdatedBy = currentUserId;

            await _disciplineReportService.UpdateAsync(report);
            return Json(new { success = true, message = "Registro actualizado correctamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar reporte de disciplina (profesor)");
            return Json(new { success = false, error = "Error al actualizar el registro", details = "Error interno. Intente nuevamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Director,Inspector,Docente,Teacher")]
    public async Task<IActionResult> TeacherDeleteReport([FromBody] TeacherDisciplineReportIdDto dto)
    {
        try
        {
            if (dto.ReportId == Guid.Empty)
                return Json(new { success = false, error = "ID inválido" });

            var report = await GetOwnedDisciplineReportForTeacherAsync(dto.ReportId);
            if (report == null)
                return Json(new { success = false, error = "No autorizado o registro no encontrado" });

            TryDeleteDisciplineUploadedFiles(report.Documents, _logger);
            await _disciplineReportService.DeleteAsync(dto.ReportId);
            return Json(new { success = true, message = "Registro eliminado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar reporte de disciplina (profesor)");
            return Json(new { success = false, error = "Error al eliminar el registro", details = "Error interno. Intente nuevamente." });
        }
    }

    private async Task<DisciplineReport?> GetOwnedDisciplineReportForTeacherAsync(Guid reportId)
    {
        var teacherUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!teacherUserId.HasValue)
            return null;

        var currentUser = await _currentUserService.GetCurrentUserAsync();
        if (currentUser == null)
            return null;

        var report = await _disciplineReportService.GetByIdAsync(reportId);
        if (report == null || report.TeacherId != teacherUserId.Value)
            return null;

        if (!currentUser.SchoolId.HasValue || report.SchoolId != currentUser.SchoolId.Value)
            return null;

        return report;
    }

    private static void TryDeleteDisciplineUploadedFiles(string? documentsJson, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(documentsJson))
            return;

        try
        {
            using var doc = JsonDocument.Parse(documentsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return;

            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "discipline");
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (!el.TryGetProperty("savedName", out var sn))
                    continue;
                var name = sn.GetString();
                if (string.IsNullOrEmpty(name))
                    continue;
                var safe = Path.GetFileName(name);
                var full = Path.Combine(basePath, safe);
                if (!full.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (System.IO.File.Exists(full))
                {
                    try
                    {
                        System.IO.File.Delete(full);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "No se pudo borrar archivo disciplina {Path}", full);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error al interpretar documentos para borrar");
        }
    }

    [Authorize(Roles = "Director,Inspector")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var report = await _disciplineReportService.GetByIdAsync(id);
        if (report == null) return NotFound();
        return View(report);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Director,Inspector")]
    public async Task<IActionResult> Edit(DisciplineReport report)
    {
        if (ModelState.IsValid)
        {
            await _disciplineReportService.UpdateAsync(report);
            return RedirectToAction(nameof(Index));
        }
        return View(report);
    }

    [Authorize(Roles = "Director,Inspector")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var report = await _disciplineReportService.GetByIdAsync(id);
        if (report == null) return NotFound();
        return View(report);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Director,Inspector")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _disciplineReportService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Director,Inspector,Docente,Teacher")]
    public async Task<IActionResult> GetForTeacherEdit(Guid id)
    {
        var report = await GetOwnedDisciplineReportForTeacherAsync(id);
        if (report == null)
            return Json(new { success = false, error = "No autorizado o registro no encontrado" });

        return Json(new
        {
            success = true,
            id = report.Id,
            studentId = report.StudentId,
            date = report.Date,
            reportType = report.ReportType,
            status = report.Status,
            description = report.Description,
            category = report.Category,
            disciplineActionsJson = report.DisciplineActionsJson
        });
    }

    [HttpGet]
    [Authorize(Roles = "Director,Inspector,Docente,Teacher")]
    public async Task<IActionResult> GetByStudent(Guid studentId)
    {
        if (!await CallerMayAccessStudentDisciplineDataAsync(studentId))
            return new JsonResult(new { error = "No autorizado para ver disciplina de este estudiante." }) { StatusCode = StatusCodes.Status403Forbidden };

        var reports = await _disciplineReportService.GetByStudentDtoAsync(studentId);
        return Json(reports.Select(r => new {
            id = r.Id,
            date = r.Date,
            time = r.Date.ToString("HH:mm"),
            type = r.Type,
            categoria = r.Category,
            status = r.Status,
            description = r.Description,
            documents = r.Documents,
            disciplineActionsJson = r.DisciplineActionsJson,
            reportTeacherId = r.TeacherId,
            teacher = r.Teacher,
            subjectId = r.SubjectId, // ✅ Agregado
            subjectName = r.SubjectName // ✅ Agregado
        }));
    }

    [HttpGet]
    [Authorize(Roles = "Director,Inspector,Docente,Teacher")]
    public async Task<IActionResult> GetFiltered(DateTime? fechaInicio, DateTime? fechaFin, Guid? gradoId, Guid? groupId = null, Guid? studentId = null)
    {
        if (!gradoId.HasValue)
        {
            return BadRequest(new { error = "El grado es obligatorio" });
        }

        try
        {
            var reports = await _disciplineReportService.GetFilteredAsync(fechaInicio, fechaFin, gradoId, groupId, studentId);
            
            var result = reports.Select(r => new {
                id = r.Id,
                studentId = r.StudentId,
                reportTeacherId = r.TeacherId,
                estudiante = r.Student != null ? $"{r.Student.Name} {r.Student.LastName}" : null,
                documentId = r.Student?.DocumentId,
                fecha = r.Date.ToString("dd/MM/yyyy"),
                hora = r.Date.ToString("HH:mm"),
                tipo = r.ReportType,
                categoria = r.Category,
                status = r.Status,
                description = r.Description,
                documents = r.Documents,
                disciplineActionsJson = r.DisciplineActionsJson,
                grupo = r.Group?.Name,
                grado = r.GradeLevel?.Name
            });

            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Error interno. Intente nuevamente." });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Director,Inspector")]
    public async Task<IActionResult> ExportToCsv(DateTime? fechaInicio, DateTime? fechaFin, Guid? gradoId)
    {
        var reports = await _disciplineReportService.GetFilteredAsync(fechaInicio, fechaFin, gradoId);
        var csv = "Estudiante,Fecha,Tipo,Estado,Descripción\n" +
            string.Join("\n", reports.Select(r => $"{(r.Student != null ? r.Student.Name : "")},{r.Date:yyyy-MM-dd},{r.ReportType},{r.Status},{r.Description}"));
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", "registros_disciplina.csv");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Director,Inspector,Docente,Teacher")]
    public async Task<IActionResult> SendEmailToStudent([FromBody] SendDisciplineEmailDto request)
    {
        try
        {
            if (request.StudentId == Guid.Empty || request.DisciplineReportId == Guid.Empty)
            {
                return Json(new { success = false, message = "ID de estudiante y reporte son requeridos" });
            }

            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser?.SchoolId == null)
            {
                return Json(new { success = false, message = "No se pudo determinar la escuela del usuario." });
            }

            var studentSchoolId = await _context.Users.AsNoTracking()
                .Where(u => u.Id == request.StudentId)
                .Select(u => u.SchoolId)
                .FirstOrDefaultAsync();
            if (studentSchoolId != currentUser.SchoolId)
            {
                return Json(new { success = false, message = "No autorizado." });
            }

            var reportScoped = await _disciplineReportService.GetByIdAsync(request.DisciplineReportId);
            if (reportScoped == null)
            {
                return Json(new { success = false, message = "Reporte no encontrado o no pertenece a su escuela." });
            }

            var success = await _emailService.SendDisciplineReportEmailAsync(
                request.StudentId, 
                request.DisciplineReportId, 
                request.CustomMessage ?? "");

            if (success)
            {
                return Json(new { success = true, message = "Correo enviado exitosamente al estudiante" });
            }
            else
            {
                return Json(new { success = false, message = "Error al enviar el correo. Verifique que el estudiante tenga email configurado y que la configuración SMTP esté activa." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo disciplinario");
            return Json(new { success = false, message = "Error interno del servidor al enviar el correo" });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Director,Inspector,Docente,Teacher")]
    public async Task<IActionResult> GetByCounselor(string trimester = null)
    {
        try
        {
            var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
            if (!currentUserId.HasValue)
            {
                return Unauthorized(new { error = "Usuario no autenticado" });
            }

            var reports = await _disciplineReportService.GetByCounselorAsync(currentUserId.Value, trimester);
            
            return Json(reports.Select(r => new {
                id = r.Id,
                studentName = r.StudentName,
                studentId = r.StudentId,
                date = r.Date.ToString("dd/MM/yyyy"),
                time = r.Date.ToString("HH:mm"),
                type = r.Type,
                category = r.Category,
                status = r.Status,
                description = r.Description,
                documents = r.Documents,
                disciplineActionsJson = r.DisciplineActionsJson,
                teacher = r.Teacher,
                subjectName = r.SubjectName
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener reportes de disciplina para consejero");
            return BadRequest(new { error = "Error al obtener los reportes de disciplina" });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Director,Inspector,Docente,Teacher,Parent,Acudiente")]
    public async Task<IActionResult> GetVisibleDisciplineInfo(Guid studentId, string trimester = null)
    {
        try
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized(new { error = "Usuario no autenticado" });
            }

            // Verificar permisos según el rol (app escáner: inspector/docente/teacher de la misma escuela que el estudiante)
            var role = (currentUser.Role ?? "").Trim().ToLowerInvariant();
            var canView = role switch
            {
                // Director debe estar en la misma escuela que el estudiante — no bypass global
                "director" => await CanSameSchoolStaffViewStudentDisciplineAsync(currentUser, studentId),
                "inspector" or "docente" => await CanSameSchoolStaffViewStudentDisciplineAsync(currentUser, studentId),
                "teacher" => await CanTeacherViewStudentDiscipline(currentUser.Id, studentId)
                    || await CanSameSchoolStaffViewStudentDisciplineAsync(currentUser, studentId),
                "parent" => await CanParentViewStudentDiscipline(currentUser.Id, studentId),
                _ => false
            };

            if (!canView)
            {
                return Forbid("No tienes permisos para ver la información de disciplina de este estudiante");
            }

            var reports = await _disciplineReportService.GetByStudentDtoAsync(studentId, trimester);
            
            return Json(reports.Select(r => new {
                id = r.Id,
                date = r.Date.ToString("dd/MM/yyyy"),
                time = r.Date.ToString("HH:mm"),
                type = r.Type,
                category = r.Category,
                status = r.Status,
                description = r.Description,
                documents = r.Documents,
                disciplineActionsJson = r.DisciplineActionsJson,
                reportTeacherId = r.TeacherId,
                teacher = r.Teacher,
                subjectName = r.SubjectName
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener información de disciplina visible");
            return BadRequest(new { error = "Error al obtener la información de disciplina" });
        }
    }

    private static List<string> ParseDisciplineActionList(IFormCollection form)
    {
        var list = new List<string>();
        var jsonRaw = form["DisciplineActionsJson"].ToString();
        if (!string.IsNullOrWhiteSpace(jsonRaw))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(jsonRaw);
                if (parsed != null)
                {
                    foreach (var x in parsed)
                    {
                        if (!string.IsNullOrWhiteSpace(x))
                            list.Add(x.Trim());
                    }
                }
            }
            catch
            {
                // ignorar JSON inválido; se intentará con campos repetidos
            }
        }

        if (list.Count == 0)
        {
            foreach (var v in form["DisciplineActions"])
            {
                if (!string.IsNullOrWhiteSpace(v))
                    list.Add(v.Trim());
            }
        }

        return list;
    }

    private async Task<bool> CallerMayAccessStudentDisciplineDataAsync(Guid studentId)
    {
        var current = await _currentUserService.GetCurrentUserAsync();
        if (current == null || !current.SchoolId.HasValue) return false;
        var stSchool = await _context.Users.AsNoTracking()
            .Where(u => u.Id == studentId)
            .Select(u => u.SchoolId)
            .FirstOrDefaultAsync();
        return stSchool == current.SchoolId;
    }

    private async Task<bool> CanSameSchoolStaffViewStudentDisciplineAsync(User staffUser, Guid studentId)
    {
        if (staffUser.SchoolId == null)
            return false;

        var studentSchoolId = await _context.Users.AsNoTracking()
            .Where(u => u.Id == studentId)
            .Select(u => u.SchoolId)
            .FirstOrDefaultAsync();

        return studentSchoolId.HasValue && studentSchoolId == staffUser.SchoolId;
    }

    private async Task<bool> CanTeacherViewStudentDiscipline(Guid teacherId, Guid studentId)
    {
        try
        {
            // Verificar si el profesor es consejero del estudiante
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser?.SchoolId == null) return false;

            var counselorGroups = await _context.CounselorAssignments
                .Where(ca => ca.UserId == teacherId && ca.SchoolId == currentUser.SchoolId && ca.IsActive)
                .Select(ca => new { ca.GroupId, ca.GradeId })
                .ToListAsync();

            if (!counselorGroups.Any()) return false;

            var groupIds = counselorGroups.Where(cg => cg.GroupId.HasValue).Select(cg => cg.GroupId.Value).ToList();
            var gradeIds = counselorGroups.Where(cg => cg.GradeId.HasValue).Select(cg => cg.GradeId.Value).ToList();

            var studentAssignment = await _context.StudentAssignments
                .Where(sa => sa.StudentId == studentId &&
                           (groupIds.Contains(sa.GroupId) || gradeIds.Contains(sa.GradeId)))
                .FirstOrDefaultAsync();

            if (studentAssignment == null) return false;

            return await _context.Users.AsNoTracking()
                .AnyAsync(u => u.Id == studentId && u.SchoolId == currentUser.SchoolId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar permisos de profesor para ver disciplina");
            return false;
        }
    }

    private async Task<bool> CanParentViewStudentDiscipline(Guid parentId, Guid studentId)
    {
        try
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null) return false;
            var role = (currentUser.Role ?? "").Trim().ToLowerInvariant();
            if (role != "parent" && role != "acudiente") return false;

            var studentSchoolId = await _context.Users.AsNoTracking()
                .Where(u => u.Id == studentId)
                .Select(u => u.SchoolId)
                .FirstOrDefaultAsync();
            if (currentUser.SchoolId.HasValue && studentSchoolId != currentUser.SchoolId)
                return false;

            var linkedViaStudentsTable = await _context.Students.AsNoTracking()
                .AnyAsync(s => s.Id == studentId && s.ParentId == parentId);

            if (linkedViaStudentsTable)
                return true;

            if (!currentUser.SchoolId.HasValue)
                return false;

            return await _context.Prematriculations.AsNoTracking()
                .AnyAsync(p => p.StudentId == studentId && p.ParentId == parentId && p.SchoolId == currentUser.SchoolId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar permisos de padre para ver disciplina");
            return false;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Director,Inspector,Teacher,Docente")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateDisciplineStatusDto request)
    {
        try
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized(new { error = "Usuario no autenticado" });
            }

            // Verificar permisos según el rol
            var canUpdate = currentUser.Role?.ToLower() switch
            {
                "director" => true,
                "inspector" => true,
                "teacher" => request.Status?.ToLower() == "escalado", // Los profesores solo pueden escalar
                _ => false
            };

            // Solo el director puede aplicar sanciones graves
            var severeSanctions = new[] { "suspension", "suspensión", "condicional", "expulsion", "expulsión" };
            var isSevereSanction = severeSanctions.Any(s => request.Status?.ToLower().Contains(s) == true);

            if (isSevereSanction && currentUser.Role?.ToLower() != "director")
            {
                return Forbid("Solo el director puede aplicar sanciones graves como suspensiones o clasificar estudiantes como condicionales");
            }

            if (!canUpdate)
            {
                return Forbid("No tienes permisos para realizar esta acción");
            }

            var success = await _disciplineReportService.UpdateStatusAsync(request.ReportId, request.Status, request.Comments);
            
            if (success)
            {
                // Si se escaló el caso, enviar mensaje al director
                if (request.Status?.ToLower() == "escalado")
                {
                    await SendEscalationMessageToDirector(request.ReportId, request.Comments);
                }

                return Json(new { success = true, message = "Estado actualizado correctamente" });
            }
            else
            {
                return Json(new { success = false, message = "Error al actualizar el estado" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar estado del reporte de disciplina");
            return BadRequest(new { error = "Error al actualizar el estado" });
        }
    }

    private async Task SendEscalationMessageToDirector(Guid reportId, string? comments)
    {
        try
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
            
            if (!currentUserId.HasValue || currentUser?.SchoolId == null)
            {
                return;
            }

            // Buscar al director de la escuela
            var director = await _userService.GetByRoleAndSchoolAsync("Director", currentUser.SchoolId.Value);
            if (director == null)
            {
                _logger.LogWarning("No se encontró director para la escuela {SchoolId}", currentUser.SchoolId);
                return;
            }

            // Obtener información del reporte
            var report = await _disciplineReportService.GetByIdAsync(reportId);
            if (report == null)
            {
                return;
            }

            var messageContent = $"Caso de disciplina escalado por {currentUser.Name} {currentUser.LastName}.\n\n" +
                               $"Estudiante: {report.Student?.Name} {report.Student?.LastName}\n" +
                               $"Fecha: {report.Date:dd/MM/yyyy HH:mm}\n" +
                               $"Tipo: {report.ReportType}\n" +
                               $"Descripción: {report.Description}\n\n" +
                               $"Comentarios adicionales: {comments ?? "Sin comentarios adicionales"}";

            var message = new Message
            {
                Id = Guid.NewGuid(),
                SchoolId = currentUser.SchoolId,
                SenderId = currentUserId.Value,
                RecipientId = director.Id,
                Subject = "Caso de Disciplina Escalado",
                Content = messageContent,
                MessageType = "DisciplineEscalation",
                IsRead = false,
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Mensaje de escalación enviado al director {DirectorId} para el reporte {ReportId}",
                director.Id, reportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar mensaje de escalación al director");
        }
    }
}
