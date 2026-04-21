namespace SchoolManager.Dtos
{
    public class UpdateDisciplineStatusDto
    {
        public Guid ReportId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Comments { get; set; }
    }
}
