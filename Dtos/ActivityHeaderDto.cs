namespace SchoolManager.Dtos
{
    public class ActivityHeaderDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // 'tarea', 'parcial', 'examen'
        public DateTime? Date { get; set; }
        public DateTime? DueDate { get; set; }
        public bool HasPdf { get; set; }
        public string? PdfUrl { get; set; }
    }
}
