using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SchoolManager.Dtos;
using SchoolManager.Interfaces;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using SchoolManager.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace SchoolManager.Controllers
{
    [Authorize(Roles = "teacher")]
    public class TeacherGradebookController : Controller
    {
        private readonly ITrimesterService _trimesterSvc;
        private readonly ITeacherGroupService _groupSvc;
        private readonly IActivityTypeService _typeSvc;
        private readonly IActivityService _activitySvc;
        private readonly IStudentActivityScoreService _scoreSvc;
        private readonly IUserService _userService;
        private readonly IStudentService _studentService;
        private readonly IAttendanceService _attendanceService;
        private readonly ICounselorAssignmentService _counselorAssignmentService;
        private readonly ISubjectAssignmentService _subjectAssignmentService;
        private readonly IDocumentStorageService _documentStorage;
        private readonly ITeacherGradebookPdfService _gradebookPdfService;
        private readonly ILogger<TeacherGradebookController> _logger;


        public TeacherGradebookController(
            ITrimesterService trimesterSvc,
            ITeacherGroupService groupSvc,
            IActivityTypeService typeSvc,
            IActivityService activitySvc,
            IStudentActivityScoreService scoreSvc,
            IUserService userService,
            IStudentService studentService,
            IAttendanceService attendanceService,
            ICounselorAssignmentService counselorAssignmentService,
            ISubjectAssignmentService subjectAssignmentService,
            IDocumentStorageService documentStorage,
            ITeacherGradebookPdfService gradebookPdfService,
            ILogger<TeacherGradebookController> logger)

        {
            _documentStorage = documentStorage;
            _gradebookPdfService = gradebookPdfService;
            _studentService = studentService;
            _trimesterSvc = trimesterSvc;
            _groupSvc = groupSvc;
            _typeSvc = typeSvc;
            _activitySvc = activitySvc;
            _scoreSvc = scoreSvc;
            _userService = userService;
            _attendanceService = attendanceService;
            _counselorAssignmentService = counselorAssignmentService;
            _subjectAssignmentService = subjectAssignmentService;
            _logger = logger;
        }


        [HttpPost]
        public async Task<IActionResult> GuardarNotasTemp([FromBody] List<StudentNotaDto> data)  
        {
            try
            {
                if (data == null || !data.Any())
                    return BadRequest("No se recibió información de notas.");

                var authenticatedTeacherId = GetTeacherId();
                var registros = new List<StudentActivityScoreCreateDto>();

                foreach (var alumno in data)
                {
                    if (!Guid.TryParse(alumno.StudentId, out var studentId) ||
                        !Guid.TryParse(alumno.SubjectId, out var subjectId) ||
                        !Guid.TryParse(alumno.GradeLevelId, out var gradeLevelId) ||
                        !Guid.TryParse(alumno.GroupId, out var groupId) ||
                        !Guid.TryParse(alumno.TeacherId, out var teacherId))
                    {
                        return BadRequest("Uno o más IDs tienen un formato inválido.");
                    }

                    if (teacherId != authenticatedTeacherId)
                        return Forbid();

                    if (subjectId == Guid.Empty || gradeLevelId == Guid.Empty)
                        return BadRequest("La materia y el grado son obligatorios para guardar notas.");

                    foreach (var nota in alumno.Notas)
                    {
                        decimal? score = null;
                        if (!string.IsNullOrWhiteSpace(nota.Nota))
                        {
                            if (decimal.TryParse(nota.Nota,
                                    System.Globalization.NumberStyles.Number,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out var parsedScore))
                            {
                                score = parsedScore;
                            }
                            else
                            {
                                return BadRequest($"La nota '{nota.Nota}' tiene un formato inválido.");
                            }
                        }
                        registros.Add(new StudentActivityScoreCreateDto
                        {
                            StudentId = studentId,
                            ActivityName = nota.Actividad,
                            Type = nota.Tipo,
                            Score = score, // Puede ser null
                            SubjectId = subjectId,
                            GradeLevelId = gradeLevelId,
                            GroupId = groupId,
                            TeacherId = teacherId,
                            Trimester = alumno.Trimester
                        });
                    }
                }

                if (registros.Count == 0)
                {
                    return BadRequest(
                        "No hay celdas de notas para guardar. Debe existir al menos una actividad en la tabla y filas con celdas editables.");
                }

                await _scoreSvc.SaveBulkFromNotasAsync(registros);

                return Ok(new { message = "Notas procesadas y guardadas correctamente." });
            }
            catch (InvalidOperationException ex)
            {
                // Business-rule violations (inactive trimester, wrong school, etc.)
                _logger.LogWarning(ex, "Validación de negocio en GuardarNotasTemp");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en GuardarNotasTemp");
                return StatusCode(500, new { message = "Error interno al guardar las notas. Intente nuevamente." });
            }
        }



        [HttpPost]
        public async Task<IActionResult> GetNotasCargadas([FromBody] GetNotesDto notes)
        {
            if (notes == null)
            {
                return BadRequest("Los datos están incompletos.");
            }

            if (notes.SubjectId == Guid.Empty || notes.GradeLevelId == Guid.Empty)
            {
                return BadRequest("Debe indicar la materia y el grado para cargar las notas.");
            }

            if (notes.GroupId == Guid.Empty || notes.TeacherId == Guid.Empty)
            {
                return BadRequest("Debe indicar el grupo y el docente.");
            }

            if (notes.TeacherId != GetTeacherId())
                return Forbid();

            // Actividades solo de la misma materia y el mismo grado que el combo seleccionado
            var activities = await _activitySvc.GetByTeacherGroupTrimesterAsync(
                notes.TeacherId, notes.GroupId, notes.Trimester, notes.SubjectId, notes.GradeLevelId);
            var actividadesPorTipo = activities.GroupBy(a => a.Type.ToLower()).ToDictionary(g => g.Key, g => g.ToList());

            // Obtener las notas existentes (como antes)
            var notas = await _scoreSvc.GetNotasPorFiltroAsync(notes);
            var estudiantes = notas.Select(n => n.StudentId).Distinct().ToList();

            // Si no hay estudiantes con notas, obtener la lista de estudiantes del grupo
            if (!estudiantes.Any())
            {
                // Usar el servicio para obtener los estudiantes del grupo
                var students = await _studentService.GetByGroupAndGradeAsync(notes.GroupId, notes.GradeLevelId);
                estudiantes = students.Select(s => s.StudentId.ToString()).ToList();
            }

            // Obtener información de todos los estudiantes
            var studentsInfo = await _studentService.GetByGroupAndGradeAsync(notes.GroupId, notes.GradeLevelId);
            var studentDict = studentsInfo.ToDictionary(s => s.StudentId.ToString(), s => s.FullName);

            // Construir la respuesta para cada estudiante
            var data = estudiantes.Select(studentId => {
                var alumno = notas.FirstOrDefault(n => n.StudentId == studentId);
                var notasAlumno = alumno?.Notas ?? new List<NotaDetalleDto>();
                var notasPorActividad = new List<object>();

                foreach (var tipo in actividadesPorTipo.Keys)
                {
                    foreach (var act in actividadesPorTipo[tipo])
                    {
                        var nota = notasAlumno.FirstOrDefault(n => n.Tipo.ToLower() == tipo && n.Actividad == act.Name);
                        notasPorActividad.Add(new {
                            tipo = tipo,
                            actividad = act.Name,
                            nota = nota != null ? nota.Nota : null,
                            pdfUrl = act.PdfUrl,
                            id = act.Id,
                            dueDate = act.DueDate
                        });
                    }
                }

                return new {
                    studentId = studentId,
                    fullName = studentDict.GetValueOrDefault(studentId, ""),
                    notas = notasPorActividad
                };
            }).ToList();

            return Json(data);
        }



        [HttpGet]
        public async Task<JsonResult> StudentsByGroupAndGrade(Guid groupId, Guid gradeId, Guid? subjectId = null)
        {
            IEnumerable<StudentBasicDto> students;
            if (subjectId.HasValue && subjectId.Value != Guid.Empty)
            {
                // Filtrar por materia, grupo y grado
                students = await _studentService.GetBySubjectGroupAndGradeAsync(subjectId.Value, groupId, gradeId);
            }
            else
            {
                // Filtrar solo por grupo y grado (comportamiento anterior)
                students = await _studentService.GetByGroupAndGradeAsync(groupId, gradeId);
            }
            return Json(students);
        }

        [HttpGet]
        public async Task<JsonResult> GetCounselorGroups()
        {
            try
            {
                var teacherId = GetTeacherId();
                var groups = await _counselorAssignmentService.GetCounselorGroupsAsync(teacherId);
                return Json(groups);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error interno. Intente nuevamente." });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCounselorGroupStudents(Guid groupId, Guid gradeId, string trimester)
        {
            try
            {
                var teacherId = GetTeacherId();
                
                // Obtener estudiantes del grupo
                var students = await _studentService.GetByGroupAndGradeAsync(groupId, gradeId);
                
                // Por ahora, solo devolver información básica de los estudiantes
                // TODO: Implementar lógica de calificaciones cuando esté disponible
                var result = students.Select(student => {
                    return new {
                        studentId = student.StudentId,
                        fullName = student.FullName,
                        trimester1 = 0.0,
                        trimester2 = 0.0,
                        trimester3 = 0.0,
                        finalAverage = 0.0,
                        status = "Sin calificar"
                    };
                }).ToList();
                
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error interno. Intente nuevamente." });
            }
        }
        private Guid GetTeacherId()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado.");
            }

            if (!Guid.TryParse(userId, out var teacherId))
            {
                throw new UnauthorizedAccessException("ID de usuario inválido.");
            }

            return teacherId;
        }






        public async Task<IActionResult> Index()
        {
            var teacherId = GetTeacherId();

            // 🔹 Obtener todos los docentes con asignaciones y filtrar solo al actual
            var teachers = await _userService.GetAllWithAssignmentsByRoleAsync("teacher");
            var teacher = teachers.FirstOrDefault(t => t.Id == teacherId);

            if (teacher == null)
                return NotFound("No se encontró el docente actual.");

            // 🔹 Preparar DTO visual con materias y grupos
            var assignments = teacher.TeacherAssignments ?? new List<TeacherAssignment>();

            var subjectGroupDetails = assignments.Any()
               ? assignments
                   .GroupBy(a => new
                   {
                       SubjectId = a.SubjectAssignment.SubjectId,
                       SubjectName = a.SubjectAssignment.Subject?.Name ?? "(Sin materia)"
                   })
                   .Select(g => new SubjectGroupSummary
                   {
                       SubjectId = g.Key.SubjectId,
                       SubjectName = g.Key.SubjectName,
                       GroupGradePairs = g.Select(x => new GroupGradeItem
                       {
                           GroupId = x.SubjectAssignment.GroupId,
                           GradeLevelId = x.SubjectAssignment.GradeLevelId,
                           GroupName = x.SubjectAssignment.Group?.Name ?? "(Grupo)",
                           GradeLevelName = x.SubjectAssignment.GradeLevel?.Name ?? "(Grado)"
                       })
                       .Distinct()
                       .ToList()
                   }).ToList()
               : new List<SubjectGroupSummary>();


            var teacherInfo = new TeacherAssignmentDisplayDto
            {
                TeacherId = teacher.Id,
                FullName = $"{teacher.Name} {teacher.LastName}",  // Nombre + Apellido concatenados
                Email = teacher.Email,
                Role = teacher.Role,
                IsActive = string.Equals(teacher.Status, "active", StringComparison.OrdinalIgnoreCase),
                SubjectGroupDetails = subjectGroupDetails
            };

            // 🔹 Obtener catálogo para filtros
            var trimesters = (await _trimesterSvc.GetAllAsync()).ToList();
            var firstTrim = trimesters.FirstOrDefault()?.Name ?? "";
            var groups = await _groupSvc.GetByTeacherAsync(teacherId, firstTrim);
            var types = await _typeSvc.GetAllAsync();

            // 🔹 ViewModel que combinamos con datos del docente y catálogos
            var viewModel = new TeacherGradebookViewModel
            {
                Teacher = teacherInfo,
                Trimesters = trimesters,
                Groups = groups,
                Types = types,
                TeacherId = teacherId
            };

            return View(viewModel);
        }





        // GET: /TeacherGradebook/GradeBookJson?groupId=...&trimester=...&subjectId=...&gradeLevelId=...
        [HttpGet]
        public async Task<JsonResult> GradeBookJson(Guid groupId, string trimester, Guid subjectId, Guid gradeLevelId)
        {
            var teacherId = GetTeacherId();
            var book = await _scoreSvc.GetGradeBookAsync(teacherId, groupId, trimester, subjectId, gradeLevelId);
            return Json(book);
        }

        // GET: /TeacherGradebook/ExportRegistroPdf?groupId=...&trimester=...&subjectId=...&gradeLevelId=...
        [HttpGet]
        public async Task<IActionResult> ExportRegistroPdf(Guid groupId, string trimester, Guid subjectId, Guid gradeLevelId)
        {
            try
            {
                var teacherId = GetTeacherId();
                var pdfBytes = await _gradebookPdfService.GenerateRegistroPdfAsync(
                    teacherId, groupId, trimester, subjectId, gradeLevelId);

                var safeTrim = string.IsNullOrWhiteSpace(trimester)
                    ? "Trimestre"
                    : new string(trimester.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());

                return File(pdfBytes, "application/pdf", $"Registro_Calificaciones_{safeTrim}.pdf");
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando PDF registro calificaciones. GroupId={GroupId}, SubjectId={SubjectId}, GradeLevelId={GradeLevelId}, Trimester={Trimester}",
                    groupId, subjectId, gradeLevelId, trimester);
                return StatusCode(500, "Error al generar el PDF del registro de calificaciones.");
            }
        }






        // POST: /TeacherGradebook/CreateActivity
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(52428800)]
        public async Task<JsonResult> CreateActivity([FromForm] ActivityCreateDto dto)
        {
            try
            {
                // Convertir la fecha de string a DateTime si viene del formulario
                if (dto.DueDate == default(DateTime))
                {
                    var dueDateStr = Request.Form["DueDate"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(dueDateStr) && DateTime.TryParse(dueDateStr, out DateTime dueDate))
                    {
                        dto.DueDate = dueDate;
                    }
                    else
                    {
                        return Json(new { success = false, error = "La fecha de entrega es obligatoria y debe ser válida" });
                    }
                }

                if (dto.SubjectId == Guid.Empty || dto.GradeLevelId == Guid.Empty)
                    return Json(new { success = false, error = "La materia y el grado son obligatorios." });

                if (dto.Pdf is { Length: > 0 })
                {
                    dto.PersistedTeacherGradebookFileName = await _documentStorage.SaveTeacherGradebookFileAsync(dto.Pdf).ConfigureAwait(false);
                    dto.Pdf = null;
                }

                var result = await _activitySvc.CreateAsync(dto);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Error interno. Intente nuevamente." });
            }
        }

        // POST: /TeacherGradebook/UpdateActivity
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(52428800)]
        public async Task<JsonResult> UpdateActivity([FromForm] ActivityUpdateDto dto)
        {
            try
            {
                // Convertir la fecha de string a DateTime si viene del formulario
                if (dto.DueDate == default(DateTime))
                {
                    var dueDateStr = Request.Form["DueDate"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(dueDateStr) && DateTime.TryParse(dueDateStr, out DateTime dueDate))
                    {
                        dto.DueDate = dueDate;
                    }
                    else
                    {
                        return Json(new { success = false, error = "La fecha de entrega es obligatoria y debe ser válida" });
                    }
                }

                if (dto.SubjectId == Guid.Empty || dto.GradeLevelId == Guid.Empty)
                    return Json(new { success = false, error = "La materia y el grado son obligatorios." });

                if (dto.Pdf is { Length: > 0 })
                {
                    dto.PersistedTeacherGradebookFileName = await _documentStorage.SaveTeacherGradebookFileAsync(dto.Pdf).ConfigureAwait(false);
                    dto.Pdf = null;
                }

                var result = await _activitySvc.UpdateAsync(dto);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Error interno. Intente nuevamente." });
            }
        }






        // POST: /TeacherGradebook/SaveScores
        [HttpPost]
        public async Task<IActionResult> SaveScores([FromBody] StudentActivityScoreCreateDto[] scores)
        {
            try
            {
                await _scoreSvc.SaveAsync(scores);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = "Error interno. Intente nuevamente." });
            }
        }




        // DELETE: /TeacherGradebook/DeleteActivity/{id}
        [HttpDelete]
        public async Task<IActionResult> DeleteActivity(Guid id)
        {
            try
            {
                await _activitySvc.DeleteAsync(id);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = "Error interno. Intente nuevamente." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPromediosFinales([FromBody] GetNotesDto notes)
        {
            if (notes == null)
            {
                return BadRequest("Los datos están incompletos.");
            }

            if (notes.SubjectId == Guid.Empty || notes.GradeLevelId == Guid.Empty)
            {
                return BadRequest("Debe indicar la materia y el grado para los promedios.");
            }

            try
            {
                // Si Trimester es null, no lo uses en el filtro (para promedios finales de todos los trimestres)
                var promedios = await _scoreSvc.GetPromediosFinalesAsync(notes);
                return Json(new { 
                    success = true, 
                    data = promedios 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    error = "Error interno. Intente nuevamente." 
                });
            }
        }

        [HttpPost]
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

        [HttpGet]
        public async Task<IActionResult> GetAttendancesByDate(Guid groupId, Guid gradeId, DateOnly date)
        {
            try
            {
                var attendances = await _attendanceService.GetAttendancesByDateAsync(groupId, gradeId, date);
                return Ok(attendances);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error interno. Intente nuevamente." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetCounselorGroupAverages([FromBody] GetNotesDto request)
        {
            try
            {
                _logger.LogInformation("=== INICIANDO GetCounselorGroupAverages: GroupId={GroupId}, Trimester={Trimester} ===", request?.GroupId, request?.Trimester);

                if (request == null)
                {
                    _logger.LogWarning("GetCounselorGroupAverages: Request es null");
                    return BadRequest(new { success = false, error = "Request es null" });
                }

                if (request.GroupId == Guid.Empty || string.IsNullOrEmpty(request.Trimester))
                {
                    _logger.LogWarning("GetCounselorGroupAverages: Datos incompletos para la consulta");
                    return BadRequest(new { success = false, error = "Datos incompletos para la consulta" });
                }

                var teacherId = GetTeacherId();

                // Obtener grupos de consejería del docente
                var counselorGroups = await _counselorAssignmentService.GetCounselorGroupsAsync(teacherId);
                _logger.LogDebug("GetCounselorGroupAverages: Grupos de consejería encontrados: {Count}", counselorGroups?.Count() ?? 0);

                // Si GradeLevelId está vacío, intentar obtenerlo del grupo de consejería
                if (request.GradeLevelId == Guid.Empty)
                {
                    var matchingGroup = counselorGroups.FirstOrDefault(g => g.GroupId == request.GroupId);

                    if (matchingGroup != null && matchingGroup.GradeId.HasValue && matchingGroup.GradeId.Value != Guid.Empty)
                    {
                        request.GradeLevelId = matchingGroup.GradeId.Value;
                        _logger.LogDebug("GetCounselorGroupAverages: GradeLevelId obtenido del grupo de consejería: {GradeId}", request.GradeLevelId);
                    }
                    else
                    {
                        _logger.LogWarning("GetCounselorGroupAverages: No se pudo obtener GradeLevelId del grupo de consejería");
                    }
                }

                var isCounselor = counselorGroups.Any(g => g.GroupId == request.GroupId &&
                    (request.GradeLevelId == Guid.Empty || g.GradeId == request.GradeLevelId));

                if (!isCounselor)
                {
                    _logger.LogWarning("GetCounselorGroupAverages: TeacherId={TeacherId} no tiene permisos de consejero para GroupId={GroupId}", teacherId, request.GroupId);
                    return BadRequest(new { success = false, error = "No tienes permisos para acceder a este grupo como consejero" });
                }

                // Obtener estudiantes del grupo
                IEnumerable<StudentBasicDto> students;
                if (request.GradeLevelId != Guid.Empty)
                {
                    students = await _studentService.GetByGroupAndGradeAsync(request.GroupId, request.GradeLevelId);
                }
                else
                {
                    students = await _studentService.GetByGroupAndGradeAsync(request.GroupId, Guid.Empty);
                }
                _logger.LogDebug("GetCounselorGroupAverages: Estudiantes encontrados: {Count}", students?.Count() ?? 0);

                if (!students.Any())
                {
                    return Ok(new {
                        success = true,
                        data = new {
                            students = new List<object>(),
                            subjects = new List<object>(),
                            generalAverage = 0.0,
                            approvalPercentage = 0.0
                        }
                    });
                }

                // Obtener todas las materias asignadas al grupo
                IEnumerable<object> subjectAssignments;
                if (request.GradeLevelId != Guid.Empty)
                {
                    subjectAssignments = await _subjectAssignmentService.GetByGroupAndGradeAsync(request.GroupId, request.GradeLevelId);
                }
                else
                {
                    subjectAssignments = await _subjectAssignmentService.GetByGroupAndGradeAsync(request.GroupId, Guid.Empty);
                }

                var subjects = subjectAssignments.Select(sa => {
                    // Usar reflexión para acceder a las propiedades dinámicamente
                    var subjectId = sa.GetType().GetProperty("SubjectId")?.GetValue(sa);
                    var subject = sa.GetType().GetProperty("Subject")?.GetValue(sa);
                    var subjectName = subject?.GetType().GetProperty("Name")?.GetValue(subject);

                    return new {
                        id = subjectId,
                        name = subjectName ?? "Sin nombre"
                    };
                }).ToList();

                var result = new List<object>();
                var allAverages = new List<double>();
                var approvedCount = 0;

                foreach (var student in students)
                {
                    var studentAverages = new Dictionary<Guid, double>();
                    var studentTotalScore = 0.0;
                    var studentValidScores = 0;

                    foreach (var subject in subjects)
                    {
                        // Obtener promedio del estudiante en esta materia para el trimestre
                        var notesRequest = new GetNotesDto
                        {
                            SubjectId = (Guid)subject.id,
                            GroupId = request.GroupId,
                            GradeLevelId = request.GradeLevelId,
                            TeacherId = teacherId,
                            Trimester = request.Trimester
                        };

                        var notas = await _scoreSvc.GetNotasPorFiltroAsync(notesRequest);
                        var studentNotes = notas.FirstOrDefault(n => n.StudentId == student.StudentId.ToString());

                        double average = 0.0;
                        if (studentNotes?.Notas != null && studentNotes.Notas.Any())
                        {
                            var validNotes = studentNotes.Notas
                                .Where(n => !string.IsNullOrEmpty(n.Nota) && decimal.TryParse(n.Nota,
                                    System.Globalization.NumberStyles.Number,
                                    System.Globalization.CultureInfo.InvariantCulture, out _))
                                .Select(n => (double)decimal.Parse(n.Nota,
                                    System.Globalization.CultureInfo.InvariantCulture))
                                .ToList();

                            if (validNotes.Any())
                            {
                                average = validNotes.Average();
                                studentTotalScore += average;
                                studentValidScores++;
                            }
                        }

                        studentAverages[(Guid)subject.id] = average;
                    }

                    var studentGeneralAverage = studentValidScores > 0 ? studentTotalScore / studentValidScores : 0.0;

                    if (studentGeneralAverage > 0)
                    {
                        allAverages.Add(studentGeneralAverage);
                        if (studentGeneralAverage >= 3.0)
                        {
                            approvedCount++;
                        }
                    }

                    result.Add(new
                    {
                        studentId = student.StudentId,
                        fullName = student.FullName,
                        documentId = student.DocumentId ?? "",
                        averages = studentAverages
                    });
                }

                var generalAverage = allAverages.Any() ? allAverages.Average() : 0.0;
                var approvalPercentage = allAverages.Any() ? (approvedCount * 100.0 / allAverages.Count) : 0.0;

                _logger.LogInformation("=== FINALIZANDO GetCounselorGroupAverages: GeneralAverage={GeneralAverage:F2}, ApprovalPct={ApprovalPct:F2} ===", generalAverage, approvalPercentage);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        students = result,
                        subjects = subjects,
                        generalAverage = generalAverage,
                        approvalPercentage = approvalPercentage
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR en GetCounselorGroupAverages: GroupId={GroupId}", request?.GroupId);
                return StatusCode(500, new { success = false, error = "Error interno. Intente nuevamente." });
            }
        }
    }
}
