using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class AcademicYear
{
    public Guid Id { get; set; }

    public Guid SchoolId { get; set; }

    public string Name { get; set; } = null!; // Ej: "2024-2025"

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual School School { get; set; } = null!;

    public virtual ICollection<Trimester> Trimesters { get; set; } = new List<Trimester>();

    public virtual ICollection<StudentAssignment> StudentAssignments { get; set; } = new List<StudentAssignment>();

    public virtual ICollection<StudentActivityScore> StudentActivityScores { get; set; } = new List<StudentActivityScore>();

    public virtual User? CreatedByUser { get; set; }

    public virtual User? UpdatedByUser { get; set; }
}

