using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class SubjectAssignment
{
    public Guid Id { get; set; }

    public Guid SpecialtyId { get; set; }

    public Guid AreaId { get; set; }

    public Guid SubjectId { get; set; }

    public Guid GradeLevelId { get; set; }

    public Guid GroupId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Status { get; set; }

    public Guid? SchoolId { get; set; }

    public virtual Area Area { get; set; } = null!;

    public virtual GradeLevel GradeLevel { get; set; } = null!;

    public virtual Group Group { get; set; } = null!;

    public virtual School? School { get; set; }

    public virtual Specialty Specialty { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;

    public virtual ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();
}
