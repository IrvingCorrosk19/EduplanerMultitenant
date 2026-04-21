using SchoolManager.Models;

namespace SchoolManager.ViewModels
{
    public class TeacherActivityViewModel
    {
        public Guid UserId { get; set; } = Guid.Empty;

        public List<Group> Groups { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<GradeLevel> GradeLevels { get; set; } = new(); // NUEVO

        // Campos del formulario
        public string? ActivityName { get; set; }
        public string? ActivityType { get; set; }
        public IFormFile? PdfFile { get; set; }

        // (Opcional) Campos para seleccionar a qué grupo, materia o grado pertenece la actividad
        public Guid? SelectedGroupId { get; set; }
        public Guid? SelectedSubjectId { get; set; }
        public Guid? SelectedGradeLevelId { get; set; }
    }
}
