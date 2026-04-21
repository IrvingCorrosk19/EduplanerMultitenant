using SchoolManager.Dtos;
using SchoolManager.Models;

namespace SchoolManager.ViewModels;

public class TeacherAssignmentViewModel
{
    // 📌 Asignaciones existentes para mostrar en tabla
    public List<TeacherAssignment> Assignments { get; set; } = new();

    // 📌 Datos del formulario (selección actual)
    public Guid? SelectedTeacherId { get; set; }
    public Guid? SelectedSubjectId { get; set; }
    public Guid? SelectedGroupId { get; set; }
    public Guid? SelectedGradeLevelId { get; set; }     // ✅ NUEVO
    public Guid? SelectedAreaId { get; set; }
    public Guid? SelectedSpecialtyId { get; set; }

    // 📌 Opciones para los selects
    public List<User> Teachers { get; set; } = new();
    public List<Subject> Subjects { get; set; } = new();
    public List<Group> Groups { get; set; } = new();
    public List<GradeLevel> GradeLevels { get; set; } = new();     // ✅ NUEVO
    public List<Area> Areas { get; set; } = new();
    public List<Specialty> Specialties { get; set; } = new();

    public List<TeacherAssignmentDisplayDto> TeachersWithAssignments { get; set; } = new();

}
