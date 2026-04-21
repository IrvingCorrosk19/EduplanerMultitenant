namespace SchoolManager.Models;

/// <summary>
/// Auditoría de decisiones sobre planes de trabajo (Submitted, Approved, Rejected, Edited, EditedAfterApproval).
/// </summary>
public class TeacherWorkPlanReviewLog
{
    public Guid Id { get; set; }
    public Guid TeacherWorkPlanId { get; set; }
    /// <summary>Submitted, Approved, Rejected, Edited, EditedAfterApproval</summary>
    public string Action { get; set; } = "";
    public Guid PerformedByUserId { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? Comment { get; set; }
    public string? Summary { get; set; }

    public virtual TeacherWorkPlan TeacherWorkPlan { get; set; } = null!;
    public virtual User PerformedByUser { get; set; } = null!;
}
