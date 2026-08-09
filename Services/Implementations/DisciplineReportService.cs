using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services
{
    public class DisciplineReportService : IDisciplineReportService
    {
        private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<DisciplineReportService> _logger;
        private readonly IAuditLogService _auditLog;

        public DisciplineReportService(
            SchoolDbContext context,
            ICurrentUserService currentUserService,
            ILogger<DisciplineReportService> logger,
            IAuditLogService auditLog)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
            _auditLog = auditLog;
        }

        private async Task WriteAuditAsync(string action, string resource, string details)
        {
            try
            {
                var user = await _currentUserService.GetCurrentUserAsync();
                var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
                await _auditLog.LogActionAsync(new SchoolManager.Models.AuditLog
                {
                    Id = Guid.NewGuid(),
                    SchoolId = schoolId,
                    UserId = user?.Id,
                    UserName = user != null ? $"{user.Name} {user.LastName}" : null,
                    UserRole = user?.Role,
                    Action = action,
                    Resource = resource,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo escribir audit log: {Action} {Resource}", action, resource);
            }
        }

        public async Task<List<DisciplineReport>> GetAllAsync()
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (schoolId == null) return new List<DisciplineReport>();
            var result = await _context.DisciplineReports
                .Where(r => r.SchoolId == schoolId)
                .ToListAsync();
            await WriteAuditAsync("LIST", "DisciplineReport", $"schoolId={schoolId} count={result.Count}");
            return result;
        }

        public async Task<DisciplineReport?> GetByIdAsync(Guid? id)
        {
            if (!id.HasValue) return null;
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (!schoolId.HasValue) return null;
            var report = await _context.DisciplineReports
                .Where(r => r.Id == id.Value && r.SchoolId == schoolId.Value)
                .FirstOrDefaultAsync();
            if (report != null)
                await WriteAuditAsync("READ", "DisciplineReport", $"id={id.Value} schoolId={schoolId}");
            return report;
        }

        public async Task CreateAsync(DisciplineReport report)
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (!schoolId.HasValue)
                throw new InvalidOperationException("No se puede crear un reporte de disciplina sin escuela (SchoolId) en el contexto del usuario.");
            report.SchoolId = schoolId.Value;
            report.CreatedAt = DateTime.UtcNow;
            _context.DisciplineReports.Add(report);
            await _context.SaveChangesAsync();
            await WriteAuditAsync("CREATE", "DisciplineReport", $"id={report.Id} studentId={report.StudentId} schoolId={schoolId}");
        }

        public async Task UpdateAsync(DisciplineReport report)
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (!schoolId.HasValue) return;

            var existing = await _context.DisciplineReports
                .Where(r => r.Id == report.Id && r.SchoolId == schoolId.Value)
                .FirstOrDefaultAsync();
            if (existing == null)
                return;

            existing.Date = report.Date;
            existing.ReportType = report.ReportType;
            existing.Description = report.Description;
            existing.Status = report.Status;
            existing.Category = report.Category;
            existing.Documents = report.Documents;
            existing.DisciplineActionsJson = report.DisciplineActionsJson;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = report.UpdatedBy ?? existing.UpdatedBy;

            await _context.SaveChangesAsync();
            await WriteAuditAsync("UPDATE", "DisciplineReport", $"id={report.Id} schoolId={schoolId}");
        }

        public async Task DeleteAsync(Guid id)
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (!schoolId.HasValue)
                return;
            var report = await _context.DisciplineReports
                .Where(r => r.Id == id && r.SchoolId == schoolId.Value)
                .FirstOrDefaultAsync();
            if (report == null)
                return;
            await WriteAuditAsync("DELETE", "DisciplineReport", $"id={id} schoolId={schoolId}");
            _context.DisciplineReports.Remove(report);
            await _context.SaveChangesAsync();
        }

        public async Task<List<DisciplineReport>> GetByStudentAsync(Guid studentId)
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (!schoolId.HasValue)
                return new List<DisciplineReport>();
            return await _context.DisciplineReports
                .Where(r => r.StudentId == studentId && r.SchoolId == schoolId.Value)
                .ToListAsync();
        }

        public async Task<List<DisciplineReport>> GetFilteredAsync(DateTime? fechaInicio, DateTime? fechaFin, Guid? gradoId, Guid? groupId = null, Guid? studentId = null)
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (!schoolId.HasValue)
                return new List<DisciplineReport>();

            var query = _context.DisciplineReports
                .Include(r => r.Student)
                .Include(r => r.Teacher)
                .Include(r => r.Group)
                .Include(r => r.GradeLevel)
                .Where(r => r.SchoolId == schoolId.Value)
                .AsQueryable();

            // Filtros obligatorios
            if (gradoId.HasValue)
            {
                query = query.Where(r => r.GradeLevelId == gradoId);
            }

            // Filtros opcionales
            if (groupId.HasValue)
            {
                query = query.Where(r => r.GroupId == groupId);
            }

            if (studentId.HasValue)
            {
                query = query.Where(r => r.StudentId == studentId);
            }

            if (fechaInicio.HasValue)
            {
                var startDate = fechaInicio.Value.Date.ToUniversalTime();
                query = query.Where(r => r.Date >= startDate);
            }

            if (fechaFin.HasValue)
            {
                var endDate = fechaFin.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                query = query.Where(r => r.Date <= endDate);
            }

            return await query
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<DisciplineReportDto>> GetByStudentDtoAsync(Guid studentId, string trimester = null)
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (!schoolId.HasValue)
                return new List<DisciplineReportDto>();

            var query = _context.DisciplineReports
                .Include(r => r.Teacher)
                .Include(r => r.Subject) // ✅ Incluir Subject
                .Where(r => r.StudentId == studentId && r.SchoolId == schoolId.Value);

            if (!string.IsNullOrEmpty(trimester))
            {
                var trimesterInfo = await _context.Trimesters
                    .FirstOrDefaultAsync(t => t.Name == trimester && t.SchoolId == schoolId);

                if (trimesterInfo != null)
                {
                    var startDate = trimesterInfo.StartDate.ToUniversalTime();
                    var endDate = trimesterInfo.EndDate.ToUniversalTime();
                    query = query.Where(r => r.Date >= startDate && r.Date <= endDate);
                }
            }

            var reports = await query
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reports.Select(r => new DisciplineReportDto
            {
                Id = r.Id,
                Type = r.ReportType,
                Category = r.Category,
                Status = r.Status,
                Description = r.Description,
                Date = r.Date,
                Documents = r.Documents,
                DisciplineActionsJson = r.DisciplineActionsJson,
                TeacherId = r.TeacherId,
                Teacher = r.Teacher != null ? $"{r.Teacher.Name} {r.Teacher.LastName}" : null,
                SubjectId = r.SubjectId, // ✅ SubjectId (puede ser NULL)
                SubjectName = r.Subject?.Name // ✅ SubjectName (puede ser NULL)
            }).ToList();
        }

        public async Task<List<DisciplineReportDto>> GetByCounselorAsync(Guid counselorId, string trimester = null)
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (!schoolId.HasValue)
                return new List<DisciplineReportDto>();

            // Obtener los grupos donde el usuario es consejero
            var counselorGroups = await _context.CounselorAssignments
                .Where(ca => ca.UserId == counselorId && ca.SchoolId == schoolId && ca.IsActive)
                .Select(ca => new { ca.GroupId, ca.GradeId })
                .ToListAsync();

            if (!counselorGroups.Any())
            {
                return new List<DisciplineReportDto>();
            }

            // Obtener estudiantes de los grupos donde es consejero
            var groupIds = counselorGroups.Where(cg => cg.GroupId.HasValue).Select(cg => cg.GroupId.Value).ToList();
            var gradeIds = counselorGroups.Where(cg => cg.GradeId.HasValue).Select(cg => cg.GradeId.Value).ToList();

            var studentIds = await _context.StudentAssignments
                .Where(sa => groupIds.Contains(sa.GroupId) || gradeIds.Contains(sa.GradeId))
                .Join(_context.Users, sa => sa.StudentId, u => u.Id, (sa, u) => u)
                .Where(u => u.SchoolId == schoolId.Value)
                .Select(u => u.Id)
                .Distinct()
                .ToListAsync();

            if (!studentIds.Any())
            {
                return new List<DisciplineReportDto>();
            }

            var query = _context.DisciplineReports
                .Include(r => r.Teacher)
                .Include(r => r.Subject)
                .Include(r => r.Student)
                .Where(r => r.StudentId.HasValue && studentIds.Contains(r.StudentId.Value) && r.SchoolId == schoolId.Value);

            if (!string.IsNullOrEmpty(trimester))
            {
                var trimesterInfo = await _context.Trimesters
                    .FirstOrDefaultAsync(t => t.Name == trimester && t.SchoolId == schoolId);

                if (trimesterInfo != null)
                {
                    var startDate = trimesterInfo.StartDate.ToUniversalTime();
                    var endDate = trimesterInfo.EndDate.ToUniversalTime();
                    query = query.Where(r => r.Date >= startDate && r.Date <= endDate);
                }
            }

            var reports = await query
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reports.Select(r => new DisciplineReportDto
            {
                Id = r.Id,
                Type = r.ReportType,
                Category = r.Category,
                Status = r.Status,
                Description = r.Description,
                Date = r.Date,
                Documents = r.Documents,
                DisciplineActionsJson = r.DisciplineActionsJson,
                TeacherId = r.TeacherId,
                Teacher = r.Teacher != null ? $"{r.Teacher.Name} {r.Teacher.LastName}" : null,
                SubjectId = r.SubjectId,
                SubjectName = r.Subject?.Name,
                StudentName = r.Student != null ? $"{r.Student.Name} {r.Student.LastName}" : null,
                StudentId = r.StudentId
            }).ToList();
        }

        public async Task<bool> UpdateStatusAsync(Guid reportId, string status, string? comments = null)
        {
            try
            {
                var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
                if (!schoolId.HasValue)
                    return false;
                var report = await _context.DisciplineReports
                    .Where(r => r.Id == reportId && r.SchoolId == schoolId.Value)
                    .FirstOrDefaultAsync();
                if (report == null)
                    return false;

                var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
                
                report.Status = status;
                report.UpdatedAt = DateTime.UtcNow;
                report.UpdatedBy = currentUserId;

                // Si hay comentarios, agregarlos a la descripción
                if (!string.IsNullOrEmpty(comments))
                {
                    report.Description += $"\n\n[Actualización {DateTime.UtcNow:dd/MM/yyyy HH:mm}]: {comments}";
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar estado del reporte de disciplina {ReportId}", reportId);
                return false;
            }
        }
    }
}
