namespace SchoolManager.Dtos
{
    public class DisciplineReportCreateDto
    {
        public string StudentId { get; set; }
        public string TeacherId { get; set; }
        public string Date { get; set; }
        public string Hora { get; set; }
        public string ReportType { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string? Documents { get; set; }
        public string SubjectId { get; set; }
        public string GroupId { get; set; }
        public string GradeLevelId { get; set; }
    }
} 