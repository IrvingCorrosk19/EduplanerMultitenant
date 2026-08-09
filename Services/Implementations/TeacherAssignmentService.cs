using SchoolManager.Models;
using Microsoft.EntityFrameworkCore;
using SchoolManager.ViewModels;


public class TeacherAssignmentService : ITeacherAssignmentService
{
    private readonly SchoolDbContext _context;

    public TeacherAssignmentService(SchoolDbContext context)
    {
        _context = context;
    }

    private async Task<Guid> ResolveSchoolIdFromSubjectAssignmentAsync(Guid subjectAssignmentId)
    {
        var sa = await _context.SubjectAssignments.AsNoTracking()
            .Where(x => x.Id == subjectAssignmentId)
            .Select(x => new { x.SchoolId, x.GroupId })
            .FirstOrDefaultAsync();
        if (sa == null)
            throw new InvalidOperationException("Asignación de materia no encontrada.");

        if (sa.SchoolId.HasValue)
            return sa.SchoolId.Value;

        var gSchool = await _context.Groups.AsNoTracking()
            .Where(g => g.Id == sa.GroupId)
            .Select(g => g.SchoolId)
            .FirstOrDefaultAsync();
        if (!gSchool.HasValue)
            throw new InvalidOperationException("No se pudo determinar la escuela para la asignación de docente.");

        return gSchool.Value;
    }

    public async Task<TeacherAssignment?> GetExistingAssignmentAsync(
        Guid currentTeacherId,
        Guid specialtyId,
        Guid areaId,
        Guid subjectId,
        Guid gradeLevelId,
        Guid groupId)
    {
        return await _context.TeacherAssignments
            .Include(ta => ta.SubjectAssignment)
                .ThenInclude(sa => sa.Subject)
            .Include(ta => ta.SubjectAssignment)
                .ThenInclude(sa => sa.Group)
            .Include(ta => ta.SubjectAssignment)
                .ThenInclude(sa => sa.GradeLevel)
            .Include(ta => ta.SubjectAssignment)
                .ThenInclude(sa => sa.Specialty)
            .Include(ta => ta.SubjectAssignment)
                .ThenInclude(sa => sa.Area)
            .Include(ta => ta.Teacher)
            .Where(ta => 
                ta.TeacherId != currentTeacherId && // Diferente profesor
                ta.SubjectAssignment.SpecialtyId == specialtyId &&
                ta.SubjectAssignment.AreaId == areaId &&
                ta.SubjectAssignment.SubjectId == subjectId &&
                ta.SubjectAssignment.GradeLevelId == gradeLevelId &&
                ta.SubjectAssignment.GroupId == groupId)
            .FirstOrDefaultAsync();
    }

    // Elimina todas las asignaciones existentes de un profesor
    public async Task DeleteAllAssignmentsByTeacherIdAsync(Guid teacherId)
    {
        var assignmentIds = await _context.TeacherAssignments
            .Where(ta => ta.TeacherId == teacherId)
            .Select(ta => ta.Id)
            .ToListAsync();

        if (assignmentIds.Count == 0)
            return;

        var scheduleEntries = await _context.ScheduleEntries
            .Where(se => assignmentIds.Contains(se.TeacherAssignmentId))
            .ToListAsync();
        if (scheduleEntries.Count > 0)
            _context.ScheduleEntries.RemoveRange(scheduleEntries);

        var assignments = _context.TeacherAssignments.Where(ta => ta.TeacherId == teacherId);
        _context.TeacherAssignments.RemoveRange(assignments);
        await _context.SaveChangesAsync();
    }

    // Agrega una nueva asignación al profesor dado el SubjectAssignmentId
    public async Task AddAssignmentAsync(Guid teacherId, Guid subjectAssignmentId)
    {
        var schoolId = await ResolveSchoolIdFromSubjectAssignmentAsync(subjectAssignmentId);
        var newAssignment = new TeacherAssignment
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            SubjectAssignmentId = subjectAssignmentId,
            SchoolId = schoolId,
            CreatedAt = DateTime.UtcNow
        };

        _context.TeacherAssignments.Add(newAssignment);
        await _context.SaveChangesAsync();
    }


    public async Task<(bool Success, List<Guid>? SubjectAssignmentIds, AssignmentDto? FailedAssignment)> GetSubjectAssignmentIdsAsync(SaveTeacherAssignmentsRequest request)
    {
        var subjectAssignmentIds = new List<Guid>();
        if (request.Assignments == null || request.Assignments.Count == 0)
            return (true, subjectAssignmentIds, null);

        // Una sola query en lugar de N+1 por asignación del modal
        var specialtyIds = request.Assignments.Select(a => a.SpecialtyId).Distinct().ToList();
        var areaIds = request.Assignments.Select(a => a.AreaId).Distinct().ToList();
        var subjectIds = request.Assignments.Select(a => a.SubjectId).Distinct().ToList();
        var gradeIds = request.Assignments.Select(a => a.GradeLevelId).Distinct().ToList();
        var groupIds = request.Assignments.Select(a => a.GroupId).Distinct().ToList();

        var candidates = await _context.SubjectAssignments
            .AsNoTracking()
            .Where(sa =>
                specialtyIds.Contains(sa.SpecialtyId) &&
                areaIds.Contains(sa.AreaId) &&
                subjectIds.Contains(sa.SubjectId) &&
                gradeIds.Contains(sa.GradeLevelId) &&
                groupIds.Contains(sa.GroupId))
            .Select(sa => new
            {
                sa.Id,
                sa.SpecialtyId,
                sa.AreaId,
                sa.SubjectId,
                sa.GradeLevelId,
                sa.GroupId
            })
            .ToListAsync();

        var lookup = candidates.ToDictionary(
            c => (c.SpecialtyId, c.AreaId, c.SubjectId, c.GradeLevelId, c.GroupId),
            c => c.Id);

        foreach (var assignment in request.Assignments)
        {
            var key = (assignment.SpecialtyId, assignment.AreaId, assignment.SubjectId, assignment.GradeLevelId, assignment.GroupId);
            if (lookup.TryGetValue(key, out var id))
            {
                subjectAssignmentIds.Add(id);
            }
            else
            {
                return (false, null, assignment);
            }
        }

        return (true, subjectAssignmentIds, null);
    }

    public async Task<List<TeacherAssignment>> GetAllWithIncludesAsync()
{
    return await _context.TeacherAssignments
        .AsNoTracking()
        .AsSplitQuery()
        .Include(ta => ta.Teacher)
        .Include(ta => ta.SubjectAssignment)
            .ThenInclude(sa => sa.Subject)
        .Include(ta => ta.SubjectAssignment.Group)
        .Include(ta => ta.SubjectAssignment.GradeLevel)
        .Include(ta => ta.SubjectAssignment.Area)
        .Include(ta => ta.SubjectAssignment.Specialty)
        .ToListAsync();
}
    public async Task<List<TeacherAssignment>> GetAssignmentsForModalByTeacherIdAsync(Guid teacherId)
    {
        return await _context.TeacherAssignments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.SubjectAssignment)
                .ThenInclude(sa => sa.Subject)
            .Include(t => t.SubjectAssignment.Area)
            .Include(t => t.SubjectAssignment.Specialty)
            .Include(t => t.SubjectAssignment.GradeLevel)
            .Include(t => t.SubjectAssignment.Group)
            .Where(t => t.TeacherId == teacherId)
            .ToListAsync();
    }

    public async Task<List<TeacherAssignment>> GetByTeacherIdAsync(Guid teacherId)
    {
        var assignments = await _context.TeacherAssignments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(ta => ta.SubjectAssignment)
                .ThenInclude(sa => sa.Subject)
            .Include(ta => ta.SubjectAssignment)
                .ThenInclude(sa => sa.Group)
            .Include(ta => ta.SubjectAssignment)
                .ThenInclude(sa => sa.Area)
            .Include(ta => ta.SubjectAssignment)
                .ThenInclude(sa => sa.Specialty)
            .Include(ta => ta.SubjectAssignment)
                .ThenInclude(sa => sa.GradeLevel)
            .Where(ta => ta.TeacherId == teacherId)
            .ToListAsync();

        // Log para debugging
        System.Diagnostics.Debug.WriteLine($"Found {assignments.Count} assignments for teacher {teacherId}");
        foreach (var assignment in assignments)
        {
            System.Diagnostics.Debug.WriteLine($"Assignment: {assignment.Id}");
            System.Diagnostics.Debug.WriteLine($"- SubjectAssignment: {assignment.SubjectAssignment?.Id}");
            System.Diagnostics.Debug.WriteLine($"- Subject: {assignment.SubjectAssignment?.Subject?.Name}");
            System.Diagnostics.Debug.WriteLine($"- Group: {assignment.SubjectAssignment?.Group?.Name}");
            System.Diagnostics.Debug.WriteLine($"- Grade: {assignment.SubjectAssignment?.GradeLevel?.Name}");
            System.Diagnostics.Debug.WriteLine($"- Area: {assignment.SubjectAssignment?.Area?.Name}");
            System.Diagnostics.Debug.WriteLine($"- Specialty: {assignment.SubjectAssignment?.Specialty?.Name}");
        }

        return assignments;
    }

    public async Task CreateAsync(Guid teacherId, Guid subjectId, Guid groupId, Guid gradeLevelId, Guid areaId, Guid specialtyId)
    {
        var subjectAssignment = await GetOrCreateSubjectAssignment(subjectId, groupId, gradeLevelId, areaId, specialtyId);

        var exists = await _context.TeacherAssignments.AnyAsync(ta =>
            ta.TeacherId == teacherId &&
            ta.SubjectAssignmentId == subjectAssignment.Id);

        if (!exists)
        {
            var schoolResolved = await ResolveSchoolIdFromSubjectAssignmentAsync(subjectAssignment.Id);

            var newAssignment = new TeacherAssignment
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                SubjectAssignmentId = subjectAssignment.Id,
                SchoolId = schoolResolved,
                CreatedAt = DateTime.UtcNow
            };

            _context.TeacherAssignments.Add(newAssignment);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateAsync(Guid assignmentId, Guid subjectId, Guid groupId, Guid gradeLevelId, Guid areaId, Guid specialtyId)
    {
        var assignment = await _context.TeacherAssignments.Where(x => x.Id == assignmentId).FirstOrDefaultAsync();
        if (assignment == null)
            throw new InvalidOperationException("Asignación no encontrada.");

        var subjectAssignment = await GetOrCreateSubjectAssignment(subjectId, groupId, gradeLevelId, areaId, specialtyId);

        assignment.SubjectAssignmentId = subjectAssignment.Id;
        assignment.SchoolId = await ResolveSchoolIdFromSubjectAssignmentAsync(subjectAssignment.Id);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid assignmentId)
    {
        var scheduleEntries = await _context.ScheduleEntries
            .Where(se => se.TeacherAssignmentId == assignmentId)
            .ToListAsync();
        if (scheduleEntries.Count > 0)
            _context.ScheduleEntries.RemoveRange(scheduleEntries);

        var assignment = await _context.TeacherAssignments.Where(x => x.Id == assignmentId).FirstOrDefaultAsync();
        if (assignment != null)
        {
            _context.TeacherAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<TeacherAssignment?> GetByIdAsync(Guid id)
    {
        return await _context.TeacherAssignments
            .Include(ta => ta.SubjectAssignment)
            .FirstOrDefaultAsync(ta => ta.Id == id);
    }

    private async Task<SubjectAssignment> GetOrCreateSubjectAssignment(Guid subjectId, Guid groupId, Guid gradeLevelId, Guid areaId, Guid specialtyId)
    {
        var existing = await _context.SubjectAssignments.FirstOrDefaultAsync(sa =>
            sa.SubjectId == subjectId &&
            sa.GroupId == groupId &&
            sa.GradeLevelId == gradeLevelId &&
            sa.AreaId == areaId &&
            sa.SpecialtyId == specialtyId
        );

        if (existing != null)
            return existing;

        var groupEntity = await _context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);

        var newAssignment = new SubjectAssignment
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            GroupId = groupId,
            GradeLevelId = gradeLevelId,
            AreaId = areaId,
            SpecialtyId = specialtyId,
            CreatedAt = DateTime.UtcNow,
            SchoolId = groupEntity?.SchoolId
        };

        _context.SubjectAssignments.Add(newAssignment);
        await _context.SaveChangesAsync();

        return newAssignment;
    }
}
