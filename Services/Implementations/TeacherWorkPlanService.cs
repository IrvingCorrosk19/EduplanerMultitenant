using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations;

public class TeacherWorkPlanService : ITeacherWorkPlanService
{
    private readonly SchoolDbContext _context;
    private readonly IAcademicYearService _academicYearService;

    public TeacherWorkPlanService(SchoolDbContext context, IAcademicYearService academicYearService)
    {
        _context = context;
        _academicYearService = academicYearService;
    }

    public async Task<TeacherWorkPlanDto> CreateAsync(Guid teacherId, CreateTeacherWorkPlanDto dto, Guid? schoolId)
    {
        await ValidateAssignmentAsync(teacherId, dto.SubjectId, dto.GradeLevelId, dto.GroupId);
        var duplicate = await _context.TeacherWorkPlans.AnyAsync(p =>
            p.TeacherId == teacherId &&
            p.AcademicYearId == dto.AcademicYearId &&
            p.Trimester == dto.Trimester &&
            p.SubjectId == dto.SubjectId &&
            p.GroupId == dto.GroupId);
        if (duplicate)
            throw new InvalidOperationException("Ya existe un plan para el mismo trimestre, materia y grupo.");

        var plan = new TeacherWorkPlan
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            SubjectId = dto.SubjectId,
            GradeLevelId = dto.GradeLevelId,
            GroupId = dto.GroupId,
            AcademicYearId = dto.AcademicYearId,
            Trimester = dto.Trimester,
            Objectives = dto.Objectives,
            Status = dto.Status ?? "Draft",
            SchoolId = schoolId,
            CreatedAt = DateTime.UtcNow
        };
        _context.TeacherWorkPlans.Add(plan);

        var order = 0;
        foreach (var item in dto.Details.OrderBy(x => x.DisplayOrder))
        {
            _context.TeacherWorkPlanDetails.Add(new TeacherWorkPlanDetail
            {
                Id = Guid.NewGuid(),
                TeacherWorkPlanId = plan.Id,
                WeeksRange = item.WeeksRange ?? "",
                Topic = item.Topic,
                ConceptualContent = item.ConceptualContent,
                ProceduralContent = item.ProceduralContent,
                AttitudinalContent = item.AttitudinalContent,
                BasicCompetencies = item.BasicCompetencies,
                AchievementIndicators = item.AchievementIndicators,
                DisplayOrder = order++
            });
        }
        await _context.SaveChangesAsync();
        return await GetByIdAsync(plan.Id, teacherId, schoolId, false) ?? throw new InvalidOperationException("Plan creado pero no se pudo recuperar.");
    }

    public async Task<TeacherWorkPlanDto> UpdateAsync(Guid planId, Guid teacherId, CreateTeacherWorkPlanDto dto)
    {
        var plan = await _context.TeacherWorkPlans
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null) throw new InvalidOperationException("Plan no encontrado.");
        if (plan.TeacherId != teacherId) throw new UnauthorizedAccessException("No puede editar el plan de otro docente.");

        await ValidateAssignmentAsync(teacherId, dto.SubjectId, dto.GradeLevelId, dto.GroupId);
        var duplicate = await _context.TeacherWorkPlans.AnyAsync(p =>
            p.Id != planId &&
            p.TeacherId == teacherId &&
            p.AcademicYearId == dto.AcademicYearId &&
            p.Trimester == dto.Trimester &&
            p.SubjectId == dto.SubjectId &&
            p.GroupId == dto.GroupId);
        if (duplicate)
            throw new InvalidOperationException("Ya existe otro plan para el mismo trimestre, materia y grupo.");

        plan.SubjectId = dto.SubjectId;
        plan.GradeLevelId = dto.GradeLevelId;
        plan.GroupId = dto.GroupId;
        plan.AcademicYearId = dto.AcademicYearId;
        plan.Trimester = dto.Trimester;
        plan.Objectives = dto.Objectives;
        plan.Status = dto.Status ?? plan.Status;
        plan.UpdatedAt = DateTime.UtcNow;

        _context.TeacherWorkPlanDetails.RemoveRange(plan.Details);
        var order = 0;
        foreach (var item in dto.Details.OrderBy(x => x.DisplayOrder))
        {
            _context.TeacherWorkPlanDetails.Add(new TeacherWorkPlanDetail
            {
                Id = Guid.NewGuid(),
                TeacherWorkPlanId = plan.Id,
                WeeksRange = item.WeeksRange ?? "",
                Topic = item.Topic,
                ConceptualContent = item.ConceptualContent,
                ProceduralContent = item.ProceduralContent,
                AttitudinalContent = item.AttitudinalContent,
                BasicCompetencies = item.BasicCompetencies,
                AchievementIndicators = item.AchievementIndicators,
                DisplayOrder = order++
            });
        }
        await _context.SaveChangesAsync();
        return await GetByIdAsync(plan.Id, teacherId, plan.SchoolId, false) ?? throw new InvalidOperationException("Plan actualizado pero no se pudo recuperar.");
    }

    public async Task<List<TeacherWorkPlanListDto>> GetByTeacherAsync(Guid? teacherId, Guid? schoolId, bool adminSeesAll)
    {
        var query = _context.TeacherWorkPlans
            .AsNoTracking()
            .Include(p => p.Teacher)
            .Include(p => p.Subject)
            .Include(p => p.GradeLevel)
            .Include(p => p.Group)
            .Include(p => p.AcademicYear)
            .AsQueryable();

        if (adminSeesAll && schoolId.HasValue)
            query = query.Where(p => p.SchoolId == schoolId.Value);
        else if (teacherId.HasValue)
            query = query.Where(p => p.TeacherId == teacherId.Value);

        var list = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new TeacherWorkPlanListDto
            {
                Id = p.Id,
                TeacherName = p.Teacher != null ? (p.Teacher.Name + " " + (p.Teacher.LastName ?? "")).Trim() : "",
                SubjectName = p.Subject != null ? p.Subject.Name : "",
                GradeLevelName = p.GradeLevel != null ? p.GradeLevel.Name : "",
                GroupName = p.Group != null ? p.Group.Name : "",
                AcademicYearName = p.AcademicYear != null ? p.AcademicYear.Name : "",
                Trimester = p.Trimester,
                Status = p.Status,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
        return list;
    }

    public async Task<TeacherWorkPlanDto?> GetByIdAsync(Guid planId, Guid? teacherId, Guid? schoolId, bool isAdmin)
    {
        var plan = await _context.TeacherWorkPlans
            .AsNoTracking()
            .Include(p => p.Teacher)
            .Include(p => p.Subject)
            .Include(p => p.GradeLevel)
            .Include(p => p.Group)
            .Include(p => p.AcademicYear)
            .Include(p => p.Details.OrderBy(d => d.DisplayOrder))
            .FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null) return null;
        if (!isAdmin && plan.TeacherId != teacherId) return null;
        if (isAdmin && schoolId.HasValue && plan.SchoolId != schoolId) return null;

        return new TeacherWorkPlanDto
        {
            Id = plan.Id,
            TeacherId = plan.TeacherId,
            TeacherName = plan.Teacher != null ? (plan.Teacher.Name + " " + (plan.Teacher.LastName ?? "")).Trim() : "",
            SubjectId = plan.SubjectId,
            SubjectName = plan.Subject?.Name ?? "",
            GradeLevelId = plan.GradeLevelId,
            GradeLevelName = plan.GradeLevel?.Name ?? "",
            GroupId = plan.GroupId,
            GroupName = plan.Group?.Name ?? "",
            AcademicYearId = plan.AcademicYearId,
            AcademicYearName = plan.AcademicYear?.Name ?? "",
            Trimester = plan.Trimester,
            Objectives = plan.Objectives,
            Status = plan.Status,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt,
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
            }).ToList()
        };
    }

    public async Task<TeacherWorkPlanFormOptionsDto> GetFormOptionsAsync(Guid teacherId, Guid? schoolId)
    {
        var options = new TeacherWorkPlanFormOptionsDto();
        if (!schoolId.HasValue) return options;

        var academicYears = await _academicYearService.GetAllBySchoolAsync(schoolId.Value);
        options.AcademicYears = academicYears.Select(ay => new AcademicYearOptionDto { Id = ay.Id, Name = ay.Name }).ToList();

        var assignments = await _context.TeacherAssignments
            .AsNoTracking()
            .Include(ta => ta.SubjectAssignment).ThenInclude(sa => sa.Subject)
            .Include(ta => ta.SubjectAssignment).ThenInclude(sa => sa.GradeLevel)
            .Include(ta => ta.SubjectAssignment).ThenInclude(sa => sa.Group)
            .Where(ta => ta.TeacherId == teacherId)
            .ToListAsync();

        var seen = new HashSet<(Guid, Guid, Guid)>();
        foreach (var ta in assignments)
        {
            var sa = ta.SubjectAssignment;
            if (sa == null) continue;
            var key = (sa.SubjectId, sa.GradeLevelId, sa.GroupId);
            if (seen.Contains(key)) continue;
            seen.Add(key);
            options.AssignmentOptions.Add(new TeacherAssignmentOptionDto
            {
                SubjectId = sa.SubjectId,
                SubjectName = sa.Subject?.Name ?? "",
                GradeLevelId = sa.GradeLevelId,
                GradeLevelName = sa.GradeLevel?.Name ?? "",
                GroupId = sa.GroupId,
                GroupName = sa.Group?.Name ?? ""
            });
        }
        return options;
    }

    public async Task DeleteAsync(Guid planId, Guid teacherId)
    {
        var plan = await _context.TeacherWorkPlans.FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null) return;
        if (plan.TeacherId != teacherId) throw new UnauthorizedAccessException("No puede eliminar el plan de otro docente.");
        _context.TeacherWorkPlans.Remove(plan);
        await _context.SaveChangesAsync();
    }

    public async Task SubmitAsync(Guid planId, Guid teacherId)
    {
        var plan = await _context.TeacherWorkPlans.FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null) throw new InvalidOperationException("Plan no encontrado.");
        if (plan.TeacherId != teacherId) throw new UnauthorizedAccessException("No puede enviar el plan de otro docente.");
        plan.Status = "Submitted";
        plan.SubmittedAt = DateTime.UtcNow;
        plan.UpdatedAt = DateTime.UtcNow;
        _context.TeacherWorkPlanReviewLogs.Add(new TeacherWorkPlanReviewLog
        {
            Id = Guid.NewGuid(),
            TeacherWorkPlanId = plan.Id,
            Action = "Submitted",
            PerformedByUserId = teacherId,
            PerformedAt = DateTime.UtcNow,
            Summary = "Enviado a revisión por el docente"
        });
        await _context.SaveChangesAsync();
    }

    private async Task ValidateAssignmentAsync(Guid teacherId, Guid subjectId, Guid gradeLevelId, Guid groupId)
    {
        var hasAssignment = await _context.TeacherAssignments
            .Include(ta => ta.SubjectAssignment)
            .AnyAsync(ta =>
                ta.TeacherId == teacherId &&
                ta.SubjectAssignment.SubjectId == subjectId &&
                ta.SubjectAssignment.GradeLevelId == gradeLevelId &&
                ta.SubjectAssignment.GroupId == groupId);
        if (!hasAssignment)
            throw new InvalidOperationException("La combinación de materia, grado y grupo no está asignada a usted.");
    }
}
