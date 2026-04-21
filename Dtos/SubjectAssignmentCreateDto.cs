using System;

namespace SchoolManager.Dtos
{
    public class SubjectAssignmentCreateDto
    {
        public Guid SpecialtyId { get; set; }
        public Guid AreaId { get; set; }
        public Guid SubjectId { get; set; }
        public Guid GradeLevelId { get; set; }
        public Guid GroupId { get; set; }
    }
} 