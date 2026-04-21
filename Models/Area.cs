using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class Area
{
    public Guid Id { get; set; }

    public Guid? SchoolId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Code { get; set; }

    public bool IsGlobal { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual School? School { get; set; }

    public virtual ICollection<SubjectAssignment> SubjectAssignments { get; set; } = new List<SubjectAssignment>();

    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
