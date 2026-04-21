using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolManager.Services
{
    public class SubjectAssignmentService : ISubjectAssignmentService
    {
        private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public SubjectAssignmentService(SchoolDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IEnumerable<GradeLevel>> GetGradeLevelsBySubjectIdAsync(Guid subjectId, Guid specialtyId, Guid areaId)
        {
            var gradeLevels = await _context.SubjectAssignments
                .Where(sa => sa.SubjectId == subjectId && sa.SpecialtyId == specialtyId && sa.AreaId == areaId)
                .Select(sa => sa.GradeLevel)
                .Distinct()
                .ToListAsync();

            return gradeLevels;
        }
        public async Task<IEnumerable<Group>> GetGroupsByGradeLevelAsync(Guid subjectId, Guid specialtyId, Guid areaId, Guid gradeLevelId)
        {
            var groups = await _context.SubjectAssignments
                .Where(sa =>
                    sa.SubjectId == subjectId &&
                    sa.SpecialtyId == specialtyId &&
                    sa.AreaId == areaId &&
                    sa.GradeLevelId == gradeLevelId)
                .Select(sa => sa.Group)
                .Distinct()
                .ToListAsync();

            return groups;
        }

        public async Task<List<Subject>> GetSubjectsBySpecialtyAndAreaAsync(Guid specialtyId, Guid areaId)
        {
            return await _context.SubjectAssignments
                .Where(sa => sa.SpecialtyId == specialtyId && sa.AreaId == areaId)
                .Select(sa => sa.Subject)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<Subject>> GetSubjectsByAreaIdAsync(Guid areaId)
        {
            return await _context.SubjectAssignments
                .Where(sa => sa.AreaId == areaId)
                .Select(sa => sa.Subject)
                .Distinct()
                .ToListAsync();
        }
        public async Task<IEnumerable<Area>> GetBySpecialtyIdAsync(Guid specialtyId)
        {
            var areas = await _context.SubjectAssignments
                .Where(sa => sa.SpecialtyId == specialtyId)
                .Select(sa => sa.Area)
                .Distinct()
                .ToListAsync();

            return areas;
        }
        public async Task<List<(Guid GradeLevelId, Guid GroupId)>> GetDistinctGradeGroupCombinationsAsync()
        {
            var result = await _context.SubjectAssignments
                .Select(x => new { x.GradeLevelId, x.GroupId }) // EF-friendly
                .Distinct()
                .ToListAsync(); // Aquí sí usamos ToListAsync porque aún estamos en EF

            return result
                .Select(x => (x.GradeLevelId, x.GroupId)) // en memoria: convertimos a tuplas
                .ToList(); // ✅ ToList normal porque ya es IEnumerable
        }

        public async Task<IEnumerable<SubjectAssignment>> GetAllSubjectAssignments()
        {

             var usercurrent =await _currentUserService.GetCurrentUserAsync();


            return await _context.SubjectAssignments.Where(x=>x.SchoolId == usercurrent.SchoolId)
                .Include(sa => sa.Specialty)
                .Include(sa => sa.Area)
                .Include(sa => sa.Subject)
                .Include(sa => sa.GradeLevel)
                .Include(sa => sa.Group)
                .ToListAsync();
        }

        public async Task<IEnumerable<SubjectAssignment>> GetByGroupAndGradeAsync(Guid groupId, Guid gradeLevelId)
        {
            var usercurrent = await _currentUserService.GetCurrentUserAsync();

            return await _context.SubjectAssignments
                .Where(sa => sa.GroupId == groupId && 
                           sa.GradeLevelId == gradeLevelId && 
                           sa.SchoolId == usercurrent.SchoolId)
                .Include(sa => sa.Subject)
                .Include(sa => sa.GradeLevel)
                .Include(sa => sa.Group)
                .Include(sa => sa.Area)
                .Include(sa => sa.Specialty)
                .ToListAsync();
        }

    }
}
