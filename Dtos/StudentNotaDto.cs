namespace SchoolManager.Dtos
{
    // Dtos/StudentNotaDto.cs
    public class StudentNotaDto
    {
        public string StudentId { get; set; }
        public string StudentFullName { get; set; }
        public string DocumentId { get; set; }

        public string GroupId { get; set; }  
        public string GradeLevelId { get; set; } 

        public string SubjectId { get; set; }

        public string TeacherId { get; set; }       
        public List<NotaDetalleDto> Notas { get; set; } = new();
        public Dictionary<string, decimal> Promedios { get; set; } = new(); // tarea, parcial, examen
        public decimal NotaFinal { get; set; }

        public string Trimester { get; set; }

        
    }

    // Dtos/NotaDetalleDto.cs
    public class NotaDetalleDto
    {
        public string Tipo { get; set; }           // tarea, parcial, examen
        public string Actividad { get; set; }      // nombre de la actividad
        public string Nota { get; set; }           // nota en string con 2 decimales
        public DateTime? DueDate { get; set; }     // fecha de entrega
    }

}
