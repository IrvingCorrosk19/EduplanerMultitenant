using SchoolManager.Dtos;

namespace SchoolManager.ViewModels
{
    public class TeacherAssignmentListViewModel
    {
        public List<TeacherAssignmentDisplayDto> TeachersWithAssignments { get; set; } = new();

    }
}
