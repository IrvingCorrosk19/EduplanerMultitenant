using System;
using System.Collections.Generic;
using SchoolManager.Models;

namespace SchoolManager.ViewModels
{
    public class StudentAssignmentViewModel
    {
        public Guid StudentId { get; set; }

        public List<Guid> SelectedSubjects { get; set; } = new();
        public List<Guid> SelectedGrades { get; set; } = new();
        public List<Guid> SelectedGroups { get; set; } = new();

        public List<Subject> AllSubjects { get; set; } = new();
        public List<GradeLevel> AllGrades { get; set; } = new();
        public List<Group> AllGroups { get; set; } = new();
    }
}
