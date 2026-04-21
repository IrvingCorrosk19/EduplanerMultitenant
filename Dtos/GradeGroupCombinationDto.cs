using System;

namespace SchoolManager.Dtos
{
    public class GradeGroupCombinationDto
    {
        public Guid GradeId { get; set; }
        public string GradeName { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string GroupGrade { get; set; } = string.Empty;
        public int StudentCount { get; set; }

        /// <summary>Jornadas distintas (Mañana, Tarde, …) según <c>student_assignments.shift_id</c> de los estudiantes activos.</summary>
        public string ShiftNamesSummary { get; set; } = string.Empty;

        public string DisplayText =>
            string.IsNullOrWhiteSpace(ShiftNamesSummary)
                ? $"{GradeName} - {GroupName} ({StudentCount} estudiantes)"
                : $"{GradeName} - {GroupName} · {ShiftNamesSummary} ({StudentCount} estudiantes)";
    }
}
