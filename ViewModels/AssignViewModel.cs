using SchoolManager.Models;

namespace SchoolManager.ViewModels
{
    public class AssignViewModel
    {
        public User Teacher { get; set; }
        public List<Subject> Subjects { get; set; }
        public List<GradeLevel> Grades { get; set; }
        public List<Group> Groups { get; set; }
    }
}
