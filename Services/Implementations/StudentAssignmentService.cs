using Microsoft.EntityFrameworkCore;

using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolManager.Services.Implementations
{
    public class StudentAssignmentService : IStudentAssignmentService
    {
        private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAcademicYearService _academicYearService;
        private readonly ILogger<StudentAssignmentService> _logger;

        public StudentAssignmentService(SchoolDbContext context, ICurrentUserService currentUserService, IAcademicYearService academicYearService, ILogger<StudentAssignmentService> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _academicYearService = academicYearService;
            _logger = logger;
        }
        private async Task<Guid> ResolveSchoolIdForStudentAssignmentAsync(StudentAssignment assignment)
        {
            var fromStudent = await _context.Users.AsNoTracking()
                .Where(u => u.Id == assignment.StudentId)
                .Select(u => u.SchoolId)
                .FirstOrDefaultAsync();
            if (fromStudent.HasValue)
                return fromStudent.Value;

            var fromGroup = await _context.Groups.AsNoTracking()
                .Where(g => g.Id == assignment.GroupId)
                .Select(g => g.SchoolId)
                .FirstOrDefaultAsync();
            if (fromGroup.HasValue)
                return fromGroup.Value;

            throw new InvalidOperationException("No se pudo determinar la escuela (SchoolId) para la asignación de estudiante.");
        }

        public async Task InsertAsync(StudentAssignment assignment)
        {
            if (assignment == null)
                throw new ArgumentNullException(nameof(assignment), "La asignación no puede ser null.");

            try
            {
                _logger.LogInformation("[StudentAssignmentService] Iniciando inserción para StudentId: {StudentId}, GradeId: {GradeId}, GroupId: {GroupId}",
                    assignment.StudentId, assignment.GradeId, assignment.GroupId);

                if (assignment.SchoolId == Guid.Empty)
                    assignment.SchoolId = await ResolveSchoolIdForStudentAssignmentAsync(assignment);

                // Asegurar que CreatedAt esté establecido si no lo está
                if (!assignment.CreatedAt.HasValue)
                {
                    assignment.CreatedAt = DateTime.UtcNow;
                    _logger.LogDebug("[StudentAssignmentService] CreatedAt establecido: {CreatedAt}", assignment.CreatedAt);
                }

                // MEJORADO: Asignar año académico si no está asignado
                if (!assignment.AcademicYearId.HasValue)
                {
                    var student = await _context.Users.Where(u => u.Id == assignment.StudentId).FirstOrDefaultAsync();
                    if (student?.SchoolId.HasValue == true)
                    {
                        var activeAcademicYear = await _academicYearService.GetActiveAcademicYearAsync(student.SchoolId.Value);
                        assignment.AcademicYearId = activeAcademicYear?.Id;
                        _logger.LogDebug("[StudentAssignmentService] AcademicYearId asignado: {AcademicYearId}", assignment.AcademicYearId);
                    }
                }

                // Asegurar que IsActive esté en true si no está establecido
                if (!assignment.IsActive && !assignment.EndDate.HasValue)
                {
                    assignment.IsActive = true;
                }

                _context.StudentAssignments.Add(assignment);
                _logger.LogDebug("[StudentAssignmentService] Entidad agregada al contexto");

                await _context.SaveChangesAsync();
                _logger.LogInformation("[StudentAssignmentService] InsertAsync completado exitosamente para StudentId: {StudentId}", assignment.StudentId);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "[StudentAssignmentService] DbUpdateException al insertar asignación para StudentId: {StudentId}", assignment.StudentId);
                // Excepción típica de clave foránea, clave primaria duplicada, etc.
                throw new InvalidOperationException($"Error al guardar la asignación en la base de datos. Verifica claves foráneas y datos duplicados. Detalles: {dbEx.Message}", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StudentAssignmentService] Error general al insertar asignación para StudentId: {StudentId}", assignment.StudentId);
                // Otro tipo de excepción general
                throw new Exception($"Ocurrió un error inesperado al insertar la asignación. Detalles: {ex.Message}", ex);
            }
        }


        public async Task<bool> ExistsAsync(Guid studentId, Guid gradeId, Guid groupId)
        {
            if (studentId == Guid.Empty || gradeId == Guid.Empty || groupId == Guid.Empty)
                return false;

            // Verificar solo asignaciones activas
            return await _context.StudentAssignments.AnyAsync(sa =>
                sa.StudentId == studentId &&
                sa.GradeId == gradeId &&
                sa.GroupId == groupId &&
                sa.IsActive);
        }


        public async Task<List<StudentAssignment>> GetAssignmentsByStudentIdAsync(Guid studentId, bool activeOnly = true)
        {
            var query = _context.StudentAssignments
                .Where(sa => sa.StudentId == studentId);
            
            // Por defecto, solo obtener asignaciones activas (para uso normal)
            // Si activeOnly = false, obtener todas incluyendo historial
            if (activeOnly)
            {
                query = query.Where(sa => sa.IsActive);
            }
            
            return await query
                .Select(sa => new StudentAssignment
                {
                    Id = sa.Id,
                    StudentId = sa.StudentId,
                    GradeId = sa.GradeId,
                    GroupId = sa.GroupId,
                    ShiftId = sa.ShiftId,
                    IsActive = sa.IsActive,
                    EndDate = sa.EndDate,
                    CreatedAt = sa.CreatedAt,
                    SchoolId = sa.SchoolId
                })
                .OrderByDescending(sa => sa.CreatedAt) // Más recientes primero
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Dictionary<Guid, List<StudentAssignment>>> GetActiveAssignmentsForCurrentSchoolAsync()
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser?.SchoolId == null)
                return new Dictionary<Guid, List<StudentAssignment>>();

            var schoolId = currentUser.SchoolId.Value;

            // JOIN por escuela: el planificador usa ix_users_school_id_lower_role + ix_student_assignments_active_student_created_at
            // (evita WHERE student_id IN (~1800 valores) que encarece parseo y plan).
            var rows = await (
                from sa in _context.StudentAssignments.AsNoTracking()
                join u in _context.Users.AsNoTracking() on sa.StudentId equals u.Id
                where sa.IsActive
                    && u.SchoolId == schoolId
                    && (u.Role.ToLower() == "student" || u.Role.ToLower() == "estudiante")
                orderby sa.StudentId, sa.CreatedAt descending
                select new StudentAssignment
                {
                    Id = sa.Id,
                    StudentId = sa.StudentId,
                    GradeId = sa.GradeId,
                    GroupId = sa.GroupId,
                    ShiftId = sa.ShiftId,
                    IsActive = sa.IsActive,
                    EndDate = sa.EndDate,
                    CreatedAt = sa.CreatedAt,
                    SchoolId = sa.SchoolId
                }).ToListAsync();

            return rows
                .GroupBy(sa => sa.StudentId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public async Task AssignAsync(Guid studentId, List<(Guid SubjectId, Guid GradeId, Guid GroupId)> assignments)
        {
            try
            {
                _logger.LogInformation("[StudentAssignmentService] Iniciando AssignAsync para StudentId: {StudentId}", studentId);

                // MEJORADO: Inactivar solo asignaciones activas para preservar historial
                var existing = await _context.StudentAssignments
                    .Where(a => a.StudentId == studentId && a.IsActive)
                    .ToListAsync();

                _logger.LogDebug("[StudentAssignmentService] Encontradas {Count} asignaciones activas existentes para StudentId: {StudentId}", existing.Count, studentId);

                // Inactivar asignaciones existentes en lugar de eliminarlas
                foreach (var assignment in existing)
                {
                    assignment.IsActive = false;
                    assignment.EndDate = DateTime.UtcNow;
                }
                
                _context.StudentAssignments.UpdateRange(existing);

                var student = await _context.Users.Where(u => u.Id == studentId).FirstOrDefaultAsync();

                Guid schoolIdForRow;
                if (student?.SchoolId != null)
                    schoolIdForRow = student.SchoolId.Value;
                else if (assignments.Count > 0)
                {
                    var gid = assignments[0].GroupId;
                    var gSchool = await _context.Groups.AsNoTracking().Where(g => g.Id == gid).Select(g => g.SchoolId).FirstOrDefaultAsync();
                    if (!gSchool.HasValue)
                        throw new InvalidOperationException("No se pudo determinar la escuela para las asignaciones.");
                    schoolIdForRow = gSchool.Value;
                }
                else
                    throw new InvalidOperationException("No hay asignaciones para resolver la escuela.");

                var activeAcademicYear = await _academicYearService.GetActiveAcademicYearAsync(schoolIdForRow);

                foreach (var item in assignments)
                {
                    _logger.LogDebug("[StudentAssignmentService] Agregando asignación: GradeId={GradeId}, GroupId={GroupId}", item.GradeId, item.GroupId);

                    _context.StudentAssignments.Add(new StudentAssignment
                    {
                        Id = Guid.NewGuid(),
                        StudentId = studentId,
                        GradeId = item.GradeId,
                        GroupId = item.GroupId,
                        IsActive = true, // Nueva asignación activa
                        AcademicYearId = activeAcademicYear?.Id, // Asignar año académico si existe
                        CreatedAt = DateTime.UtcNow,
                        SchoolId = schoolIdForRow
                    });
                }

                _logger.LogDebug("[StudentAssignmentService] Guardando cambios...");
                await _context.SaveChangesAsync();
                _logger.LogInformation("[StudentAssignmentService] AssignAsync completado exitosamente para StudentId: {StudentId}", studentId);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "[StudentAssignmentService] DbUpdateException en AssignAsync para StudentId: {StudentId}", studentId);
                throw new InvalidOperationException($"Error al asignar estudiantes. Detalles: {dbEx.Message}", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StudentAssignmentService] Error general en AssignAsync para StudentId: {StudentId}", studentId);
                throw new Exception($"Error inesperado al asignar estudiantes. Detalles: {ex.Message}", ex);
            }
        }

        public async Task RemoveAssignmentsAsync(Guid studentId)
        {
            // MEJORADO: Inactivar en lugar de eliminar para preservar historial
            var activeAssignments = await _context.StudentAssignments
                .Where(a => a.StudentId == studentId && a.IsActive)
                .ToListAsync();

            if (activeAssignments.Any())
            {
                foreach (var assignment in activeAssignments)
                {
                    assignment.IsActive = false;
                    assignment.EndDate = DateTime.UtcNow;
                }

                _context.StudentAssignments.UpdateRange(activeAssignments);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Elimina permanentemente las asignaciones (usar solo cuando sea necesario limpiar datos)
        /// </summary>
        [Obsolete("Usar RemoveAssignmentsAsync que preserva historial. Este método elimina datos permanentemente.")]
        public async Task DeleteAssignmentsPermanentlyAsync(Guid studentId)
        {
            var assignments = await _context.StudentAssignments
                .Where(a => a.StudentId == studentId)
                .ToListAsync();

            _context.StudentAssignments.RemoveRange(assignments);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AssignStudentAsync(Guid studentId, Guid subjectId, Guid gradeId, Guid groupId)
        {
            try
            {
                _logger.LogInformation("[StudentAssignmentService] AssignStudentAsync - StudentId: {StudentId}, GradeId: {GradeId}, GroupId: {GroupId}",
                    studentId, gradeId, groupId);

                // Verificar solo asignaciones activas
                bool exists = await _context.StudentAssignments.AnyAsync(sa =>
                    sa.StudentId == studentId &&
                    sa.GradeId == gradeId &&
                    sa.GroupId == groupId &&
                    sa.IsActive
                );

                if (exists)
                {
                    _logger.LogDebug("[StudentAssignmentService] La asignación ya existe para StudentId: {StudentId}", studentId);
                    return false;
                }

                // MEJORADO: Obtener año académico activo para la nueva asignación
                var student = await _context.Users.Where(u => u.Id == studentId).FirstOrDefaultAsync();
                var schoolId = student?.SchoolId;
                var activeAcademicYear = schoolId.HasValue 
                    ? await _academicYearService.GetActiveAcademicYearAsync(schoolId.Value)
                    : null;

                var schoolIdForRow = student?.SchoolId
                    ?? await _context.Groups.AsNoTracking().Where(g => g.Id == groupId).Select(g => g.SchoolId).FirstOrDefaultAsync()
                    ?? throw new InvalidOperationException("No se pudo determinar la escuela para la asignación.");

                var assignment = new StudentAssignment
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    GradeId = gradeId,
                    GroupId = groupId,
                    IsActive = true,
                    AcademicYearId = activeAcademicYear?.Id, // Asignar año académico si existe
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schoolIdForRow
                };

                _logger.LogDebug("[StudentAssignmentService] Nueva asignación creada para StudentId: {StudentId}", studentId);

                _context.StudentAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[StudentAssignmentService] Asignación guardada exitosamente para StudentId: {StudentId}", studentId);
                return true;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "[StudentAssignmentService] DbUpdateException en AssignStudentAsync para StudentId: {StudentId}", studentId);
                throw new InvalidOperationException($"Error al asignar estudiante. Detalles: {dbEx.Message}", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StudentAssignmentService] Error general en AssignStudentAsync para StudentId: {StudentId}", studentId);
                throw new Exception($"Error inesperado al asignar estudiante. Detalles: {ex.Message}", ex);
            }
        }

        public async Task BulkAssignFromFileAsync(List<(string StudentEmail, string SubjectCode, string GradeName, string GroupName)> rows)
        {
            if (rows == null || rows.Count == 0)
                return;

            var emails = rows.Select(r => r.StudentEmail).Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var subjectCodes = rows.Select(r => r.SubjectCode).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var gradeNames = rows.Select(r => r.GradeName).Where(g => !string.IsNullOrWhiteSpace(g)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var groupNames = rows.Select(r => r.GroupName).Where(g => !string.IsNullOrWhiteSpace(g)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var students = await _context.Users
                .Where(u => u.Email != null && emails.Contains(u.Email))
                .ToListAsync();
            var studentsByEmail = students
                .Where(u => u.Email != null)
                .GroupBy(u => u.Email!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var subjects = await _context.Subjects
                .Where(s => s.Code != null && subjectCodes.Contains(s.Code))
                .ToListAsync();
            var subjectsByCode = subjects
                .Where(s => s.Code != null)
                .GroupBy(s => s.Code!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var grades = await _context.GradeLevels
                .Where(g => gradeNames.Contains(g.Name))
                .ToListAsync();
            var gradesByName = grades
                .GroupBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var groups = await _context.Groups
                .Where(g => groupNames.Contains(g.Name) && g.Grade != null && gradeNames.Contains(g.Grade))
                .ToListAsync();
            var groupsByKey = groups
                .GroupBy(g => (Grade: g.Grade ?? "", Name: g.Name), new GradeGroupKeyComparer())
                .ToDictionary(g => g.Key, g => g.First());

            var studentIds = students.Select(s => s.Id).ToList();
            var existingActive = studentIds.Count == 0
                ? new List<StudentAssignment>()
                : await _context.StudentAssignments
                    .Where(sa => studentIds.Contains(sa.StudentId) && sa.IsActive)
                    .ToListAsync();
            var existingKeys = new HashSet<(Guid StudentId, Guid GradeId, Guid GroupId)>(
                existingActive.Select(sa => (sa.StudentId, sa.GradeId, sa.GroupId)));

            var academicYearCache = new Dictionary<Guid, Guid?>();

            foreach (var row in rows)
            {
                if (!studentsByEmail.TryGetValue(row.StudentEmail ?? "", out var student)
                    || !subjectsByCode.TryGetValue(row.SubjectCode ?? "", out _)
                    || !gradesByName.TryGetValue(row.GradeName ?? "", out var grade)
                    || !groupsByKey.TryGetValue((row.GradeName ?? "", row.GroupName ?? ""), out var group))
                {
                    continue;
                }

                var key = (student.Id, grade.Id, group.Id);
                if (existingKeys.Contains(key))
                    continue;

                Guid? academicYearId = null;
                if (student.SchoolId.HasValue)
                {
                    if (!academicYearCache.TryGetValue(student.SchoolId.Value, out academicYearId))
                    {
                        var activeAcademicYear = await _academicYearService.GetActiveAcademicYearAsync(student.SchoolId.Value);
                        academicYearId = activeAcademicYear?.Id;
                        academicYearCache[student.SchoolId.Value] = academicYearId;
                    }
                }

                var schoolIdRow = student.SchoolId
                    ?? group.SchoolId
                    ?? throw new InvalidOperationException("Falta SchoolId en estudiante o grupo.");

                _context.StudentAssignments.Add(new StudentAssignment
                {
                    Id = Guid.NewGuid(),
                    StudentId = student.Id,
                    GradeId = grade.Id,
                    GroupId = group.Id,
                    IsActive = true,
                    AcademicYearId = academicYearId,
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schoolIdRow
                });
                existingKeys.Add(key);
            }

            await _context.SaveChangesAsync();
        }

        private sealed class GradeGroupKeyComparer : IEqualityComparer<(string Grade, string Name)>
        {
            public bool Equals((string Grade, string Name) x, (string Grade, string Name) y) =>
                string.Equals(x.Grade, y.Grade, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((string Grade, string Name) obj) =>
                HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Grade ?? ""),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name ?? ""));
        }

    }
}
