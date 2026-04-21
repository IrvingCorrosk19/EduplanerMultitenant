using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class TeacherAssignment
{
    public Guid Id { get; set; }

    public Guid TeacherId { get; set; }

    public Guid SubjectAssignmentId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual SubjectAssignment SubjectAssignment { get; set; } = null!;

    public virtual User Teacher { get; set; } = null!;
}
