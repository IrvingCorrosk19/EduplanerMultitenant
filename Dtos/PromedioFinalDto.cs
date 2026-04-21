using System.Collections.Generic;

namespace SchoolManager.Dtos
{
    public class PromedioFinalDto
    {
        public string StudentId { get; set; }
        public string StudentFullName { get; set; }
        public string DocumentId { get; set; }
        public string Trimester { get; set; }
        public decimal? PromedioTareas { get; set; }
        public decimal? PromedioParciales { get; set; }
        public decimal? PromedioExamenes { get; set; }
        public decimal? NotaFinal { get; set; }
        public string Estado { get; set; }
    }
} 