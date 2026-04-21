using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations;

public class DirectorWorkPlanService : IDirectorWorkPlanService
{
    private readonly SchoolDbContext _context;

    public DirectorWorkPlanService(SchoolDbContext context)
    {
        _context = context;
    }

    public async Task<DirectorWorkPlanDashboardDto> GetDashboardAsync(WorkPlanFiltersDto filters)
    {
        if (!filters.SchoolId.HasValue)
            return new DirectorWorkPlanDashboardDto();

        var baseQuery = _context.TeacherWorkPlans
            .AsNoTracking()
            .Where(p => p.SchoolId == filters.SchoolId.Value);

        if (filters.AcademicYearId.HasValue)
            baseQuery = baseQuery.Where(p => p.AcademicYearId == filters.AcademicYearId.Value);
        if (filters.Trimester.HasValue)
            baseQuery = baseQuery.Where(p => p.Trimester == filters.Trimester.Value);
        if (filters.TeacherId.HasValue)
            baseQuery = baseQuery.Where(p => p.TeacherId == filters.TeacherId.Value);
        if (filters.SubjectId.HasValue)
            baseQuery = baseQuery.Where(p => p.SubjectId == filters.SubjectId.Value);
        if (filters.GradeLevelId.HasValue)
            baseQuery = baseQuery.Where(p => p.GradeLevelId == filters.GradeLevelId.Value);
        if (filters.GroupId.HasValue)
            baseQuery = baseQuery.Where(p => p.GroupId == filters.GroupId.Value);
        if (!string.IsNullOrEmpty(filters.Status))
            baseQuery = baseQuery.Where(p => p.Status == filters.Status);

        var dashboard = new DirectorWorkPlanDashboardDto
        {
            TotalPlans = await baseQuery.CountAsync(),
            SubmittedCount = await baseQuery.CountAsync(p => p.Status == "Submitted"),
            ApprovedCount = await baseQuery.CountAsync(p => p.Status == "Approved"),
            RejectedCount = await baseQuery.CountAsync(p => p.Status == "Rejected"),
            DraftCount = await baseQuery.CountAsync(p => p.Status == "Draft")
        };

        var query = baseQuery
            .Include(p => p.Teacher)
            .Include(p => p.Subject)
            .Include(p => p.GradeLevel)
            .Include(p => p.Group)
            .Include(p => p.AcademicYear)
            .AsNoTracking();

        query = (filters.SortBy?.ToLowerInvariant()) switch
        {
            "teacher" => filters.SortDesc ? query.OrderByDescending(p => p.Teacher != null ? p.Teacher.Name : "") : query.OrderBy(p => p.Teacher != null ? p.Teacher.Name : ""),
            "subject" => filters.SortDesc ? query.OrderByDescending(p => p.Subject != null ? p.Subject.Name : "") : query.OrderBy(p => p.Subject != null ? p.Subject.Name : ""),
            "status" => filters.SortDesc ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
            "updated" => filters.SortDesc ? query.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt) : query.OrderBy(p => p.UpdatedAt ?? p.CreatedAt),
            _ => query.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
        };

        dashboard.TotalCount = await query.CountAsync();
        var page = Math.Max(1, filters.Page);
        var size = Math.Clamp(filters.PageSize, 1, 100);
        var list = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => new DirectorWorkPlanListItemDto
            {
                Id = p.Id,
                TeacherId = p.TeacherId,
                TeacherName = p.Teacher != null ? (p.Teacher.Name + " " + (p.Teacher.LastName ?? "")).Trim() : "",
                SubjectName = p.Subject != null ? p.Subject.Name : "",
                GradeLevelName = p.GradeLevel != null ? p.GradeLevel.Name : "",
                GroupName = p.Group != null ? p.Group.Name : "",
                AcademicYearName = p.AcademicYear != null ? p.AcademicYear.Name : "",
                Trimester = p.Trimester,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                SubmittedAt = p.SubmittedAt,
                ApprovedAt = p.ApprovedAt,
                RejectedAt = p.RejectedAt
            })
            .ToListAsync();
        dashboard.Items = list;
        return dashboard;
    }

    public async Task<DirectorWorkPlanDetailDto?> GetPlanByIdAsync(Guid planId, Guid schoolId)
    {
        var plan = await _context.TeacherWorkPlans
            .AsNoTracking()
            .Include(p => p.Teacher)
            .Include(p => p.Subject)
            .Include(p => p.GradeLevel)
            .Include(p => p.Group)
            .Include(p => p.AcademicYear)
            .Include(p => p.ApprovedByUser)
            .Include(p => p.RejectedByUser)
            .Include(p => p.Details.OrderBy(d => d.DisplayOrder))
            .FirstOrDefaultAsync(p => p.Id == planId && p.SchoolId == schoolId);
        if (plan == null) return null;

        var logs = await _context.TeacherWorkPlanReviewLogs
            .AsNoTracking()
            .Include(l => l.PerformedByUser)
            .Where(l => l.TeacherWorkPlanId == planId)
            .OrderByDescending(l => l.PerformedAt)
            .Select(l => new ReviewLogEntryDto
            {
                Action = l.Action,
                PerformedByName = l.PerformedByUser != null ? (l.PerformedByUser.Name + " " + (l.PerformedByUser.LastName ?? "")).Trim() : "",
                PerformedAt = l.PerformedAt,
                Comment = l.Comment,
                Summary = l.Summary
            })
            .ToListAsync();

        return new DirectorWorkPlanDetailDto
        {
            Id = plan.Id,
            TeacherId = plan.TeacherId,
            TeacherName = plan.Teacher != null ? (plan.Teacher.Name + " " + (plan.Teacher.LastName ?? "")).Trim() : "",
            SubjectName = plan.Subject?.Name ?? "",
            GradeLevelName = plan.GradeLevel?.Name ?? "",
            GroupName = plan.Group?.Name ?? "",
            AcademicYearName = plan.AcademicYear?.Name ?? "",
            Trimester = plan.Trimester,
            Objectives = plan.Objectives,
            Status = plan.Status,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt,
            SubmittedAt = plan.SubmittedAt,
            ApprovedAt = plan.ApprovedAt,
            ApprovedByName = plan.ApprovedByUser != null ? (plan.ApprovedByUser.Name + " " + (plan.ApprovedByUser.LastName ?? "")).Trim() : null,
            RejectedAt = plan.RejectedAt,
            RejectedByName = plan.RejectedByUser != null ? (plan.RejectedByUser.Name + " " + (plan.RejectedByUser.LastName ?? "")).Trim() : null,
            ReviewComment = plan.ReviewComment,
            Details = plan.Details.Select(d => new TeacherWorkPlanDetailDto
            {
                Id = d.Id,
                WeeksRange = d.WeeksRange,
                Topic = d.Topic,
                ConceptualContent = d.ConceptualContent,
                ProceduralContent = d.ProceduralContent,
                AttitudinalContent = d.AttitudinalContent,
                BasicCompetencies = d.BasicCompetencies,
                AchievementIndicators = d.AchievementIndicators,
                DisplayOrder = d.DisplayOrder
            }).ToList(),
            ReviewLog = logs
        };
    }

    public async Task ApproveAsync(Guid planId, Guid directorUserId, string? comment)
    {
        var plan = await _context.TeacherWorkPlans.FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null) throw new InvalidOperationException("Plan no encontrado.");
        if (plan.SchoolId == null) throw new UnauthorizedAccessException("Plan sin institución asignada.");
        if (plan.Status != "Submitted")
            throw new InvalidOperationException("Solo se pueden aprobar planes en estado Enviado (Submitted).");

        plan.Status = "Approved";
        plan.ApprovedAt = DateTime.UtcNow;
        plan.ApprovedByUserId = directorUserId;
        plan.RejectedAt = null;
        plan.RejectedByUserId = null;
        plan.ReviewComment = comment;
        plan.UpdatedAt = DateTime.UtcNow;

        _context.TeacherWorkPlanReviewLogs.Add(new TeacherWorkPlanReviewLog
        {
            Id = Guid.NewGuid(),
            TeacherWorkPlanId = plan.Id,
            Action = "Approved",
            PerformedByUserId = directorUserId,
            PerformedAt = DateTime.UtcNow,
            Comment = comment,
            Summary = "Aprobado por Dirección Académica"
        });
        await _context.SaveChangesAsync();
    }

    public async Task RejectAsync(Guid planId, Guid directorUserId, string comment)
    {
        if (string.IsNullOrWhiteSpace(comment) || comment.Trim().Length < 10)
            throw new InvalidOperationException("El comentario de rechazo es obligatorio y debe tener al menos 10 caracteres.");

        var plan = await _context.TeacherWorkPlans.FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null) throw new InvalidOperationException("Plan no encontrado.");
        if (plan.SchoolId == null) throw new UnauthorizedAccessException("Plan sin institución asignada.");
        if (plan.Status != "Submitted")
            throw new InvalidOperationException("Solo se pueden rechazar planes en estado Enviado (Submitted).");

        plan.Status = "Rejected";
        plan.RejectedAt = DateTime.UtcNow;
        plan.RejectedByUserId = directorUserId;
        plan.ApprovedAt = null;
        plan.ApprovedByUserId = null;
        plan.ReviewComment = comment.Trim();
        plan.UpdatedAt = DateTime.UtcNow;

        _context.TeacherWorkPlanReviewLogs.Add(new TeacherWorkPlanReviewLog
        {
            Id = Guid.NewGuid(),
            TeacherWorkPlanId = plan.Id,
            Action = "Rejected",
            PerformedByUserId = directorUserId,
            PerformedAt = DateTime.UtcNow,
            Comment = comment.Trim(),
            Summary = "Rechazado por Dirección Académica"
        });
        await _context.SaveChangesAsync();
    }

    public async Task<byte[]> ExportPlanPdfAsync(Guid planId, Guid schoolId)
    {
        var plan = await _context.TeacherWorkPlans
            .AsNoTracking()
            .Include(p => p.Teacher)
            .Include(p => p.Subject)
            .Include(p => p.GradeLevel)
            .Include(p => p.Group)
            .Include(p => p.AcademicYear)
            .Include(p => p.Details.OrderBy(d => d.DisplayOrder))
            .Include(p => p.School)
            .FirstOrDefaultAsync(p => p.Id == planId && p.SchoolId == schoolId);
        if (plan == null) throw new InvalidOperationException("Plan no encontrado o no pertenece a su institución.");
        return await DirectorWorkPlanPdfService.GeneratePlanPdfAsync(plan);
    }

    public async Task<byte[]> ExportConsolidatedPdfAsync(WorkPlanFiltersDto filters, Guid schoolId)
    {
        filters.SchoolId = schoolId;
        filters.PageSize = 500;
        filters.Page = 1;
        var dashboard = await GetDashboardAsync(filters);
        var school = await _context.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == schoolId);
        return DirectorWorkPlanPdfService.GenerateConsolidatedPdfAsync(dashboard, school?.Name ?? "Institución");
    }

    public async Task<DirectorFilterOptionsDto> GetFilterOptionsAsync(Guid schoolId)
    {
        var plans = await _context.TeacherWorkPlans
            .AsNoTracking()
            .Include(p => p.Teacher)
            .Include(p => p.Subject)
            .Include(p => p.GradeLevel)
            .Include(p => p.Group)
            .Where(p => p.SchoolId == schoolId)
            .ToListAsync();

        var result = new DirectorFilterOptionsDto();
        foreach (var p in plans.Where(p => p.TeacherId != Guid.Empty).GroupBy(p => p.TeacherId))
        {
            var t = p.First().Teacher;
            result.Teachers.Add(new FilterOptionDto { Id = p.Key, Name = t != null ? (t.Name + " " + (t.LastName ?? "")).Trim() : "" });
        }
        foreach (var p in plans.Where(p => p.SubjectId != Guid.Empty).GroupBy(p => p.SubjectId))
            result.Subjects.Add(new FilterOptionDto { Id = p.Key, Name = p.First().Subject?.Name ?? "" });
        foreach (var p in plans.Where(p => p.GradeLevelId != Guid.Empty).GroupBy(p => p.GradeLevelId))
            result.GradeLevels.Add(new FilterOptionDto { Id = p.Key, Name = p.First().GradeLevel?.Name ?? "" });
        foreach (var p in plans.Where(p => p.GroupId != Guid.Empty).GroupBy(p => p.GroupId))
            result.Groups.Add(new FilterOptionDto { Id = p.Key, Name = p.First().Group?.Name ?? "" });

        result.Teachers = result.Teachers.Where(x => !string.IsNullOrWhiteSpace(x.Name)).DistinctBy(x => x.Id).OrderBy(x => x.Name).ToList();
        result.Subjects = result.Subjects.Where(x => !string.IsNullOrWhiteSpace(x.Name)).DistinctBy(x => x.Id).OrderBy(x => x.Name).ToList();
        result.GradeLevels = result.GradeLevels.Where(x => !string.IsNullOrWhiteSpace(x.Name)).DistinctBy(x => x.Id).OrderBy(x => x.Name).ToList();
        result.Groups = result.Groups.Where(x => !string.IsNullOrWhiteSpace(x.Name)).DistinctBy(x => x.Id).OrderBy(x => x.Name).ToList();
        return result;
    }
}
