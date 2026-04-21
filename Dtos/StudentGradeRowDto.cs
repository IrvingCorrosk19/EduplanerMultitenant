namespace SchoolManager.Dtos
{
    public class StudentGradeRowDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public IDictionary<Guid, decimal?> ScoresByActivity { get; set; } = new Dictionary<Guid, decimal?>();
    }
}
