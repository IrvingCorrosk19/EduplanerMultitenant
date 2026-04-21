using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class Trimester
{
    public Guid Id { get; set; }

    public Guid? SchoolId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int Order { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? AcademicYearId { get; set; }

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

    public virtual School? School { get; set; }

    public virtual AcademicYear? AcademicYear { get; set; }
}
