using System;
using System.Collections.Generic;

namespace SchoolManager.ViewModels
{
    public class TeacherAssignmentRequest
    {
        public Guid UserId { get; set; }

        public Guid SubjectId { get; set; }

        public Guid GradeId { get; set; }

        public Guid SpecialtyId { get; set; }   // 👈 NUEVO

        public Guid AreaId { get; set; }        // 👈 NUEVO

        public List<Guid> GroupIds { get; set; } = new();
    }
}
