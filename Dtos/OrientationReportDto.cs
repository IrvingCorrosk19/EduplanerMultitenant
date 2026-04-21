namespace SchoolManager.Dtos
{
    public class OrientationReportDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public string Teacher { get; set; }
        public string? Documents { get; set; }
        public Guid? SubjectId { get; set; } // ✅ Agregado
        public string? SubjectName { get; set; } // ✅ Agregado
        public string? GroupName { get; set; } // ✅ Agregado
        public string? GradeName { get; set; } // ✅ Agregado
        public string? StudentName { get; set; } // ✅ Agregado para consejeros
        public Guid? StudentId { get; set; } // ✅ Agregado para consejeros
    }
}
