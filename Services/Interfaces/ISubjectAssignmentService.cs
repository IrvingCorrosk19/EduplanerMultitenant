using SchoolManager.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoolManager.Services.Interfaces
{
    public interface ISubjectAssignmentService
    {
        Task<List<(Guid GradeLevelId, Guid GroupId)>> GetDistinctGradeGroupCombinationsAsync();
        Task<IEnumerable<SubjectAssignment>> GetAllSubjectAssignments();

        Task<IEnumerable<Area>> GetBySpecialtyIdAsync(Guid specialtyId);
        Task<List<Subject>> GetSubjectsBySpecialtyAndAreaAsync(Guid specialtyId, Guid areaId);

        Task<IEnumerable<GradeLevel>> GetGradeLevelsBySubjectIdAsync(Guid subjectId,Guid specialtyId, Guid areaId);

        Task<IEnumerable<Group>> GetGroupsByGradeLevelAsync(Guid subjectId, Guid specialtyId, Guid areaId, Guid gradeLevelId);

        Task<IEnumerable<SubjectAssignment>> GetByGroupAndGradeAsync(Guid groupId, Guid gradeLevelId);

    }
}
