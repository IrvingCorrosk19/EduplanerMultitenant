namespace SchoolManager.ViewModels
{
    public class AssignmentInputModel
    {
        // Campos del profesor
        public string EmailDocente { get; set; } // Email del profesor
        public string Nombre { get; set; } // Nombre del profesor
        public string Apellido { get; set; } // Apellido del profesor
        public string DocumentoId { get; set; } // Documento de identidad
        public string FechaNacimiento { get; set; } // Fecha de nacimiento
        
        // Campos de asignación académica
        public string Especialidad { get; set; }
        public string Area { get; set; }
        public string Materia { get; set; }
        public string Grado { get; set; }
        public string Grupo { get; set; }
        
        // Mantener compatibilidad con código existente
        public string Docente { get; set; } // Correo del profesor (deprecated)
    }
}
