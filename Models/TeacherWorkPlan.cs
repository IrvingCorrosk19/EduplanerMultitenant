namespace SchoolManager.Models;

/// <summary>
/// Plan de trabajo trimestral del docente. Solo el docente dueño puede editarlo; Admin solo visualiza.
/// </summary>
public class TeacherWorkPlan
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid GradeLevelId { get; set; }
    public Guid GroupId { get; set; }
    public Guid AcademicYearId { get; set; }
    /// <summary>Número de trimestre: 1, 2 o 3.</summary>
    public int Trimester { get; set; }
    /// <summary>Objetivos de aprendizaje (texto largo).</summary>
    public string? Objectives { get; set; }
    /// <summary>Draft, Submitted, Approved, Rejected.</summary>
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? SchoolId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? RejectedAt { get; set; }
    public Guid? RejectedByUserId { get; set; }
    /// <summary>Comentario de revisión; obligatorio en rechazo.</summary>
    public string? ReviewComment { get; set; }

    public virtual User? ApprovedByUser { get; set; }
    public virtual User? RejectedByUser { get; set; }
    public virtual User Teacher { get; set; } = null!;
    public virtual Subject Subject { get; set; } = null!;
    public virtual GradeLevel GradeLevel { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;
    public virtual AcademicYear AcademicYear { get; set; } = null!;
    public virtual School? School { get; set; }
    public virtual ICollection<TeacherWorkPlanDetail> Details { get; set; } = new List<TeacherWorkPlanDetail>();
}
