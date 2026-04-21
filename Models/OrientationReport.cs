using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class OrientationReport
{
    public Guid Id { get; set; }

    public Guid? SchoolId { get; set; }

    public Guid? StudentId { get; set; }

    public Guid? TeacherId { get; set; }

    public DateTime Date { get; set; }

    public string? ReportType { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public Guid? SubjectId { get; set; }

    public Guid? GroupId { get; set; }

    public Guid? GradeLevelId { get; set; }

    public string? Category { get; set; }

    public string? Documents { get; set; }

    public virtual GradeLevel? GradeLevel { get; set; }

    public virtual Group? Group { get; set; }

    public virtual School? School { get; set; }

    public virtual User? Student { get; set; }

    public virtual Subject? Subject { get; set; }

    public virtual User? Teacher { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual User? UpdatedByUser { get; set; }
}
