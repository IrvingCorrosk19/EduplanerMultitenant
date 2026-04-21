namespace SchoolManager.Dtos
{
    public class ActivityDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string TrimesterCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string GroupDisplayName { get; set; } = string.Empty;
        public string? PdfUrl { get; set; }
    }
}
