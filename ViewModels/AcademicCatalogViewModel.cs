using System.Collections.Generic;
using SchoolManager.Models;
using SchoolManager.Dtos;

namespace SchoolManager.ViewModels
{
    public class AcademicCatalogViewModel
    {
        public IEnumerable<GradeLevel> GradesLevel { get; set; } = new List<GradeLevel>();
        public IEnumerable<Group> Groups { get; set; } = new List<Group>();
        public IEnumerable<Subject> Subjects { get; set; } = new List<Subject>();
        public IEnumerable<Specialty> Specialties { get; set; } = new List<Specialty>();
        public IEnumerable<Area> Areas { get; set; } = new List<Area>();
        public List<TrimesterDto> Trimestres { get; set; } = new List<TrimesterDto>();
        public IEnumerable<Shift> Shifts { get; set; } = new List<Shift>();
    }
}
