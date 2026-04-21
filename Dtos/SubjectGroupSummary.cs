public class SubjectGroupSummary
{
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public List<GroupGradeItem> GroupGradePairs { get; set; } = new();
}
