using SchoolManager.Dtos;
using SchoolManager.Models;

namespace SchoolManager.ViewModels
{
    public class TeacherGradebookViewModel
    {
        public TeacherAssignmentDisplayDto Teacher { get; set; } = null!;
        public List<Student> Students { get; set; } = new List<Student>();  // Asegúrate de que no sea null
        public IEnumerable<TrimesterDto> Trimesters { get; set; } = new List<TrimesterDto>();
        public IEnumerable<GroupDto> Groups { get; set; } = new List<GroupDto>();
        public IEnumerable<ActivityTypeDto> Types { get; set; } = new List<ActivityTypeDto>();
        public Guid TeacherId { get; set; }
        // NUEVA propiedad para promedios por estudiante
        public List<StudentAverageDto> StudentAverages { get; set; } = new List<StudentAverageDto>();

    }
    
}

