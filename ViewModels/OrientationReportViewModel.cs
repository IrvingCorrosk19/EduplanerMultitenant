using SchoolManager.Dtos;
using SchoolManager.Models;

namespace SchoolManager.ViewModels
{
    public class OrientationReportViewModel
    {
        public List<OrientationReport> Reports { get; set; } = new List<OrientationReport>();
        public List<Student> Students { get; set; } = new List<Student>();
        public IEnumerable<GroupDto> Groups { get; set; } = new List<GroupDto>();
        public IEnumerable<GradeLevel> GradeLevels { get; set; } = new List<GradeLevel>();
        public IEnumerable<Subject> Subjects { get; set; } = new List<Subject>();
        public Guid? SelectedStudentId { get; set; }
        public Guid? SelectedGroupId { get; set; }
        public Guid? SelectedGradeLevelId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
