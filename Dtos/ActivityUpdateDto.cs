using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SchoolManager.Dtos
{
    public class ActivityUpdateDto
    {
        public Guid ActivityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;   // 'tarea' | 'parcial' | 'examen'
        public string TrimesterCode { get; set; } = string.Empty;   // '1T', '2T', '3T'
        public DateTime Date { get; set; }
        public DateTime DueDate { get; set; }
        public IFormFile? Pdf { get; set; }

        [BindNever]
        public string? PersistedTeacherGradebookFileName { get; set; }

        public Guid TeacherId { get; set; }
        public Guid SubjectId { get; set; }
        public Guid GroupId { get; set; }
        public Guid GradeLevelId { get; set; }
    }
}
