namespace SchoolManager.Models;

/// <summary>
/// Bloque de contenido del plan (semanas, tema, conceptual, procedimental, actitudinal, competencias, indicadores).
/// </summary>
public class TeacherWorkPlanDetail
{
    public Guid Id { get; set; }
    public Guid TeacherWorkPlanId { get; set; }
    /// <summary>Rango de semanas, ej: "1-4", "5-8".</summary>
    public string WeeksRange { get; set; } = "";
    public string? Topic { get; set; }
    public string? ConceptualContent { get; set; }
    public string? ProceduralContent { get; set; }
    public string? AttitudinalContent { get; set; }
    public string? BasicCompetencies { get; set; }
    public string? AchievementIndicators { get; set; }
    public int DisplayOrder { get; set; }

    public virtual TeacherWorkPlan TeacherWorkPlan { get; set; } = null!;
}
