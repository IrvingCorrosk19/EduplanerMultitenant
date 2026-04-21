using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Interfaces;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using SchoolManager.Services.Implementations;

namespace SchoolManager.Services
{
    public class StudentActivityScoreService : IStudentActivityScoreService
    {
        private readonly SchoolDbContext _context;
        private readonly ITrimesterService _trimesterService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAcademicYearService _academicYearService;
        private readonly IDocumentStorageService _documentStorage;

        public StudentActivityScoreService(
            SchoolDbContext context,
            ITrimesterService trimesterService,
            ICurrentUserService currentUserService,
            IAcademicYearService academicYearService,
            IDocumentStorageService documentStorage)
        {
            _context = context;
            _trimesterService = trimesterService;
            _currentUserService = currentUserService;
            _academicYearService = academicYearService;
            _documentStorage = documentStorage;
        }

        /* ------------ 1. Guardar / actualizar notas ------------ */
        public async Task SaveAsync(IEnumerable<StudentActivityScoreCreateDto> scores)
        {
            foreach (var dto in scores)
            {
                // Validar trimestre activo
                await _trimesterService.ValidateTrimesterActiveAsync(dto.Trimester);

                var entity = await _context.StudentActivityScores
                    .FirstOrDefaultAsync(s => s.StudentId == dto.StudentId &&
                                              s.ActivityId == dto.ActivityId);

                if (entity is null)
                {
                    // MEJORADO: Obtener año académico activo para la nueva nota
                    var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
                    var activeAcademicYear = currentUserSchool != null
                        ? await _academicYearService.GetActiveAcademicYearAsync(currentUserSchool.Id)
                        : null;

                    var newScore = new StudentActivityScore
                    {
                        Id = Guid.NewGuid(),
                        StudentId = dto.StudentId,
                        ActivityId = dto.ActivityId,
                        Score = dto.Score,
                        AcademicYearId = activeAcademicYear?.Id // Asignar año académico si existe
                    };
                    
                    // Configurar campos de auditoría y SchoolId
                    await AuditHelper.SetAuditFieldsForCreateAsync(newScore, _currentUserService);
                    await AuditHelper.SetSchoolIdAsync(newScore, _currentUserService);
                    
                    _context.StudentActivityScores.Add(newScore);
                }
                else
                {
                    entity.Score = dto.Score;
                    // Configurar campos de auditoría para actualización
                    await AuditHelper.SetAuditFieldsForUpdateAsync(entity, _currentUserService);
                }
            }
            await _context.SaveChangesAsync();
        }

        /* ------------ 2. Libro de calificaciones pivotado ------------ */
        public async Task<GradeBookDto> GetGradeBookAsync(Guid teacherId, Guid groupId, string trimesterCode, Guid subjectId, Guid gradeLevelId)
        {
            if (subjectId == Guid.Empty || gradeLevelId == Guid.Empty)
                return new GradeBookDto { Activities = new List<ActivityHeaderDto>(), Rows = new List<StudentGradeRowDto>() };

            var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
            if (currentUserId == null || teacherId != currentUserId.Value)
                return new GradeBookDto { Activities = new List<ActivityHeaderDto>(), Rows = new List<StudentGradeRowDto>() };

            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (schoolId == null)
                return new GradeBookDto { Activities = new List<ActivityHeaderDto>(), Rows = new List<StudentGradeRowDto>() };

            /* 2.1 Cabeceras: actividades del docente en ese grupo, trimestre, materia y grado */
            var headers = await _context.Activities
                .Where(a => a.TeacherId == teacherId &&
                            a.GroupId == groupId &&
                            a.Trimester == trimesterCode &&
                            a.SubjectId == subjectId &&
                            a.GradeLevelId == gradeLevelId &&
                            (a.SchoolId == null || a.SchoolId == schoolId))
                .OrderBy(a => a.CreatedAt)
                .Select(a => new ActivityHeaderDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Type = a.Type,
                    Date = a.CreatedAt,
                    DueDate = a.DueDate,
                    HasPdf = a.PdfUrl != null,
                    PdfUrl = a.PdfUrl
                })
                .ToListAsync();

            // Ajustar el tipo de fecha y valor por defecto después de traer los datos a memoria
            foreach (var h in headers)
            {
                h.Date = h.Date.HasValue
                    ? h.Date.Value.ToUniversalTime()
                    : DateTime.UtcNow;
                h.PdfUrl = _documentStorage.ToPublicDownloadUrl(h.PdfUrl);
            }

            var activityIds = headers.Select(h => h.Id).ToList();

            /* 2.2 Estudiantes asignados a ese grupo (StudentAssignments), restringidos a la institución */
            var studentIds = await _context.StudentAssignments
                .Where(sa => sa.GroupId == groupId)
                .Join(_context.Users.Where(u => u.SchoolId == schoolId),
                    sa => sa.StudentId,
                    u => u.Id,
                    (sa, u) => u.Id)
                .Distinct()
                .ToListAsync();

            var students = await _context.Students
                .Where(s => studentIds.Contains(s.Id) && (s.SchoolId == null || s.SchoolId == schoolId))
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();

            /* 2.3 Notas existentes */
            var scores = await _context.StudentActivityScores
                .Where(s => activityIds.Contains(s.ActivityId) && (s.SchoolId == null || s.SchoolId == schoolId))
                .ToListAsync();

            /* 2.4 Pivotar alumnos × actividades */
            var rows = students.Select(stu =>
            {
                var dict = new Dictionary<Guid, decimal?>();
                foreach (var hdr in headers)
                {
                    var score = scores.FirstOrDefault(x =>
                        x.StudentId == stu.Id && x.ActivityId == hdr.Id);
                    dict[hdr.Id] = score?.Score;
                }

                return new StudentGradeRowDto
                {
                    StudentId = stu.Id,
                    StudentName = stu.Name,
                    ScoresByActivity = dict
                };
            });

            return new GradeBookDto { Activities = headers, Rows = rows };
        }

        public async Task SaveBulkFromNotasAsync(List<StudentActivityScoreCreateDto> registros)
        {
            if (registros == null || registros.Count == 0)
                return;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
                if (currentUserSchool == null)
                {
                    throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
                }

                var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
                if (currentUserId == null)
                    throw new InvalidOperationException("Usuario no autenticado.");

                foreach (var dto in registros)
                {
                    if (dto.TeacherId != currentUserId.Value)
                        throw new InvalidOperationException("No puede registrar calificaciones en nombre de otro docente.");
                }

                var studentIdsForValidation = registros.Select(r => r.StudentId).Distinct().ToList();
                var outsiderStudents = await _context.Users.CountAsync(u =>
                    studentIdsForValidation.Contains(u.Id) &&
                    u.SchoolId != currentUserSchool.Id);
                if (outsiderStudents > 0)
                    throw new InvalidOperationException("Hay estudiantes que no pertenecen a su institución.");

                static (Guid TeacherId, Guid SubjectId, Guid GroupId, Guid GradeLevelId, string Trimester, string Name, string Type) ActivityKey(
                    StudentActivityScoreCreateDto dto) =>
                    (dto.TeacherId, dto.SubjectId, dto.GroupId, dto.GradeLevelId, dto.Trimester ?? "",
                        dto.ActivityName ?? "", dto.Type ?? "");

                static (Guid TeacherId, Guid SubjectId, Guid GroupId, Guid GradeLevelId, string Trimester, string Name, string Type) ActivityKeyEntity(Activity a) =>
                    (a.TeacherId ?? Guid.Empty, a.SubjectId ?? Guid.Empty, a.GroupId ?? Guid.Empty, a.GradeLevelId ?? Guid.Empty,
                        a.Trimester ?? "", a.Name, a.Type);

                foreach (var trimCode in registros.Select(r => r.Trimester).Distinct())
                {
                    await _trimesterService.ValidateTrimesterActiveAsync(trimCode);
                }

                var trimesterIdByCode = new Dictionary<string, Guid>(StringComparer.Ordinal);
                foreach (var trimCode in registros.Select(r => r.Trimester).Distinct())
                {
                    var trimesterRow = await _context.Trimesters
                        .FirstOrDefaultAsync(t =>
                            t.Name == trimCode && t.SchoolId == currentUserSchool.Id);
                    if (trimesterRow == null)
                    {
                        throw new InvalidOperationException(
                            $"No se encontró el trimestre '{trimCode}' para la escuela actual.");
                    }

                    trimesterIdByCode[trimCode] = trimesterRow.Id;
                }

                var scopes = registros
                    .Select(r => (r.TeacherId, r.SubjectId, r.GroupId, r.GradeLevelId, r.Trimester))
                    .Distinct()
                    .ToList();

                var allActivities = new List<Activity>();
                foreach (var scope in scopes)
                {
                    var batch = await _context.Activities
                        .Where(a =>
                            a.TeacherId == scope.TeacherId &&
                            a.SubjectId == scope.SubjectId &&
                            a.GroupId == scope.GroupId &&
                            a.GradeLevelId == scope.GradeLevelId &&
                            a.Trimester == scope.Trimester &&
                            (a.SchoolId == null || a.SchoolId == currentUserSchool.Id))
                        .ToListAsync();
                    allActivities.AddRange(batch);
                }

                var activityByKey = allActivities
                    .GroupBy(ActivityKeyEntity)
                    .ToDictionary(g => g.Key, g => g.First());

                var activeAcademicYear = await _academicYearService.GetActiveAcademicYearAsync(currentUserSchool.Id);

                foreach (var dto in registros)
                {
                    var key = ActivityKey(dto);
                    if (!activityByKey.TryGetValue(key, out var activity))
                    {
                        var trimesterId = trimesterIdByCode[dto.Trimester];
                        activity = new Activity
                        {
                            Id = Guid.NewGuid(),
                            Name = dto.ActivityName,
                            Type = dto.Type,
                            TeacherId = dto.TeacherId,
                            SubjectId = dto.SubjectId,
                            GroupId = dto.GroupId,
                            GradeLevelId = dto.GradeLevelId,
                            Trimester = dto.Trimester,
                            TrimesterId = trimesterId,
                            SchoolId = currentUserSchool.Id,
                            CreatedAt = DateTime.UtcNow
                        };

                        await AuditHelper.SetAuditFieldsForCreateAsync(activity, _currentUserService);
                        _context.Activities.Add(activity);
                        activityByKey[key] = activity;
                    }
                    else if (activity.SchoolId == null || activity.TrimesterId == null)
                    {
                        if (activity.SchoolId == null)
                            activity.SchoolId = currentUserSchool.Id;
                        if (activity.TrimesterId == null)
                            activity.TrimesterId = trimesterIdByCode[dto.Trimester];
                        await AuditHelper.SetAuditFieldsForUpdateAsync(activity, _currentUserService);
                    }
                    else if (activity.SchoolId.HasValue && activity.SchoolId.Value != currentUserSchool.Id)
                    {
                        throw new InvalidOperationException("La actividad indicada pertenece a otra institución.");
                    }
                }

                var activityIds = activityByKey.Values.Select(a => a.Id).Distinct().ToList();
                var studentIds = registros.Select(r => r.StudentId).Distinct().ToList();

                var existingScores = await _context.StudentActivityScores
                    .Where(s => activityIds.Contains(s.ActivityId) && studentIds.Contains(s.StudentId))
                    .ToListAsync();

                var scoreByStudentActivity = existingScores.ToDictionary(s => (s.StudentId, s.ActivityId));

                foreach (var dto in registros)
                {
                    var activity = activityByKey[ActivityKey(dto)];
                    var pair = (dto.StudentId, activity.Id);

                    if (!scoreByStudentActivity.TryGetValue(pair, out var row))
                    {
                        row = new StudentActivityScore
                        {
                            Id = Guid.NewGuid(),
                            StudentId = dto.StudentId,
                            ActivityId = activity.Id,
                            Score = dto.Score,
                            AcademicYearId = activeAcademicYear?.Id,
                            CreatedAt = DateTime.UtcNow,
                            SchoolId = currentUserSchool.Id
                        };
                        await AuditHelper.SetAuditFieldsForCreateAsync(row, _currentUserService);
                        _context.StudentActivityScores.Add(row);
                        scoreByStudentActivity[pair] = row;
                    }
                    else
                    {
                        row.Score = dto.Score;
                        if (row.SchoolId == null)
                            row.SchoolId = currentUserSchool.Id;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Manejamos el error
                Console.WriteLine("❌ Error guardando notas en bloque:");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw new Exception($"Error al guardar las notas: {ex.Message}", ex);
            }
        }

        public async Task<List<StudentNotaDto>> GetNotasPorFiltroAsync(GetNotesDto notes)
        {
            if (notes.SubjectId == Guid.Empty || notes.GradeLevelId == Guid.Empty)
                return new List<StudentNotaDto>();

            var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
            if (currentUserId == null || notes.TeacherId != currentUserId.Value)
                return new List<StudentNotaDto>();

            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (schoolId == null)
                return new List<StudentNotaDto>();

            // Obtener las notas existentes con información del estudiante
            var notas = await _context.StudentActivityScores
                .Include(sa => sa.Activity)
                .Include(sa => sa.Student)
                .Where(sa =>
                    sa.Activity.TeacherId == notes.TeacherId &&
                    sa.Activity.SubjectId == notes.SubjectId &&
                    sa.Activity.GroupId == notes.GroupId &&
                    sa.Activity.GradeLevelId == notes.GradeLevelId &&
                    sa.Activity.Trimester == notes.Trimester &&
                    (sa.Activity.SchoolId == null || sa.Activity.SchoolId == schoolId) &&
                    (sa.SchoolId == null || sa.SchoolId == schoolId))
                .ToListAsync();

            // Obtener información de los estudiantes
            var studentIds = notas.Select(n => n.StudentId).Distinct().ToList();
            var estudiantes = await _context.Users
                .Where(u => studentIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name, u.LastName, u.DocumentId })
                .ToListAsync();

            // Agrupar las notas por estudiante
            var resultado = notas
                .GroupBy(n => n.StudentId)
                .Select(g => {
                    var estudiante = estudiantes.FirstOrDefault(e => e.Id == g.Key);
                    var nombre = estudiante != null ? 
                        $"{(estudiante.Name ?? "").Trim()} {(estudiante.LastName ?? "").Trim()}".Trim() : 
                        "(Sin nombre)";
                    if (string.IsNullOrWhiteSpace(nombre)) nombre = "(Sin nombre)";
                    
                    return new StudentNotaDto
                    {
                        StudentId = g.Key.ToString(),
                        StudentFullName = nombre,
                        DocumentId = estudiante?.DocumentId ?? "",
                        TeacherId = notes.TeacherId.ToString(),
                        SubjectId = notes.SubjectId.ToString(),
                        GroupId = notes.GroupId.ToString(),
                        GradeLevelId = notes.GradeLevelId.ToString(),
                        Trimester = notes.Trimester,
                        Notas = g.Select(n => new NotaDetalleDto
                        {
                            Tipo = n.Activity.Type,
                            Actividad = n.Activity.Name,
                            Nota = n.Score.HasValue ? n.Score.Value.ToString("0.00") : "",
                            DueDate = n.Activity.DueDate
                        }).ToList()
                    };
                })
                .ToList();

            return resultado;
        }

        public async Task<List<PromedioFinalDto>> GetPromediosFinalesAsync(GetNotesDto notes)
        {
            if (notes.SubjectId == Guid.Empty || notes.GradeLevelId == Guid.Empty)
                return new List<PromedioFinalDto>();

            var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
            if (currentUserId == null || notes.TeacherId != currentUserId.Value)
                return new List<PromedioFinalDto>();

            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (schoolId == null)
                return new List<PromedioFinalDto>();

            // 1. Obtener todos los estudiantes del grupo y grado usando solo User y StudentAssignment
            // Ordenar alfabéticamente por apellido primero, luego por nombre
            var students = await _context.StudentAssignments
                .Where(sa => sa.GroupId == notes.GroupId && sa.GradeId == notes.GradeLevelId)
                .Join(_context.Users.Where(u => u.SchoolId == schoolId),
                    sa => sa.StudentId,
                    u => u.Id,
                    (sa, u) => new { u.Id, u.Name, u.LastName, u.DocumentId })
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.Name)
                .ToListAsync();

            // 2. Obtener todas las notas del grupo, materia, grado y docente
            var notasPorTrimestre = await _context.StudentActivityScores
                .Where(score => score.SchoolId == null || score.SchoolId == schoolId)
                .Join(_context.Activities.Where(a => a.SchoolId == null || a.SchoolId == schoolId),
                    score => score.ActivityId,
                    activity => activity.Id,
                    (score, activity) => new
                    {
                        StudentId = score.StudentId,
                        Score = score.Score,
                        Trimester = activity.Trimester,
                        ActivityType = activity.Type,
                        SubjectId = activity.SubjectId,
                        GroupId = activity.GroupId,
                        GradeLevelId = activity.GradeLevelId,
                        TeacherId = activity.TeacherId
                    })
                .Where(x => x.SubjectId == notes.SubjectId &&
                           x.GroupId == notes.GroupId &&
                           x.GradeLevelId == notes.GradeLevelId &&
                           x.TeacherId == notes.TeacherId
                           && (string.IsNullOrEmpty(notes.Trimester) || x.Trimester == notes.Trimester))
                .ToListAsync();

            // 3. Usar siempre los tres trimestres estándar
            var trimestres = new List<string> { "1T", "2T", "3T" };

            // 4. Construir la lista de promedios por estudiante y trimestre
            var promedios = new List<PromedioFinalDto>();
            foreach (var student in students)
            {
                foreach (var trimestre in trimestres)
                {
                    var notasEstudianteTrimestre = notasPorTrimestre
                        .Where(x => x.StudentId == student.Id && x.Trimester == trimestre)
                        .ToList();

                    var notasValidas = notasEstudianteTrimestre.Where(x => x.Score.HasValue).ToList();

                    // Siempre armar el nombre correctamente como "Apellido, Nombre"
                    var nombre = $"{(student.LastName ?? "").Trim()}, {(student.Name ?? "").Trim()}".Trim();
                    if (string.IsNullOrWhiteSpace(nombre) || nombre == ",") nombre = "(Sin nombre)";

                    // Calcular promedios por tipo de actividad con los nuevos nombres
                    var promedioNotasApreciacion = notasEstudianteTrimestre.Where(x => x.ActivityType.ToLower() == "notas de apreciación" && x.Score.HasValue)
                        .Any() ? notasEstudianteTrimestre.Where(x => x.ActivityType.ToLower() == "notas de apreciación" && x.Score.HasValue).Average(x => x.Score.Value) : (decimal?)null;
                    
                    var promedioEjerciciosDiarios = notasEstudianteTrimestre.Where(x => x.ActivityType.ToLower() == "ejercicios diarios" && x.Score.HasValue)
                        .Any() ? notasEstudianteTrimestre.Where(x => x.ActivityType.ToLower() == "ejercicios diarios" && x.Score.HasValue).Average(x => x.Score.Value) : (decimal?)null;
                    
                    var promedioExamenFinal = notasEstudianteTrimestre.Where(x => x.ActivityType.ToLower() == "examen final" && x.Score.HasValue)
                        .Any() ? notasEstudianteTrimestre.Where(x => x.ActivityType.ToLower() == "examen final" && x.Score.HasValue).Average(x => x.Score.Value) : (decimal?)null;

                    // Calcular nota final como el promedio de los 3 promedios (solo los que tienen valor)
                    var promediosConValor = new[] { promedioNotasApreciacion, promedioEjerciciosDiarios, promedioExamenFinal }
                        .Where(p => p.HasValue)
                        .Select(p => p.Value)
                        .ToList();
                    
                    var notaFinal = promediosConValor.Any() ? promediosConValor.Average() : (decimal?)null;

                    promedios.Add(new PromedioFinalDto
                    {
                        StudentId = student.Id.ToString(),
                        StudentFullName = nombre,
                        DocumentId = student.DocumentId ?? "",
                        Trimester = trimestre,
                        PromedioTareas = promedioNotasApreciacion,
                        PromedioParciales = promedioEjerciciosDiarios,
                        PromedioExamenes = promedioExamenFinal,
                        NotaFinal = notaFinal,
                        Estado = notaFinal.HasValue ? (notaFinal.Value >= 3.0m ? "Aprobado" : "Reprobado") : "Sin calificar"
                    });
                }
            }

            return promedios;
        }
    }
}

