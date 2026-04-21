namespace SchoolManager.Dtos
{
    public class DisciplineReportDto
    {
        public Guid? Id { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public string Teacher { get; set; }
        public string? Documents { get; set; }
        public Guid? SubjectId { get; set; } // ✅ Agregado para consistencia
        public string? SubjectName { get; set; } // ✅ Agregado para consistencia
        public string? StudentName { get; set; } // ✅ Agregado para consejeros
        public Guid? StudentId { get; set; } // ✅ Agregado para consejeros

        /// <summary>Usuario (docente) que registró el reporte.</summary>
        public Guid? TeacherId { get; set; }

        /// <summary>JSON array string of disciplinary actions, same format as stored in DB.</summary>
        public string? DisciplineActionsJson { get; set; }
    }
} 