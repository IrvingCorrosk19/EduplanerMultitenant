using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class Subject
{
    public Guid Id { get; set; }

    public Guid? SchoolId { get; set; }

    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public string? Description { get; set; }

    public bool? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public Guid? AreaId { get; set; }

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

    public virtual Area? Area { get; set; }

    public virtual ICollection<DisciplineReport> DisciplineReports { get; set; } = new List<DisciplineReport>();

    public virtual ICollection<OrientationReport> OrientationReports { get; set; } = new List<OrientationReport>();

    public virtual School? School { get; set; }

    public virtual ICollection<SubjectAssignment> SubjectAssignments { get; set; } = new List<SubjectAssignment>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual User? CreatedByUser { get; set; }

    public virtual User? UpdatedByUser { get; set; }
}
