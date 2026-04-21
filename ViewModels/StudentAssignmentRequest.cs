using System;
using System.Collections.Generic;

namespace SchoolManager.ViewModels
{
    public class StudentAssignmentRequest
    {
        public Guid UserId { get; set; }
        public Guid SubjectId { get; set; }
        public Guid GradeId { get; set; }
        public List<Guid> GroupIds { get; set; }
    }
}
