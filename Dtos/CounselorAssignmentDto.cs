namespace SchoolManager.Dtos
{
    public class CounselorAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserLastName { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public Guid? GradeId { get; set; }
        public string? GradeName { get; set; }
        public Guid? GroupId { get; set; }
        public string? GroupName { get; set; }
        public bool IsCounselor { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string AssignmentType { get; set; } = string.Empty; // "General", "Grado", "Grupo"
    }

    public class CounselorAssignmentCreateDto
    {
        public Guid SchoolId { get; set; }
        public Guid UserId { get; set; }
        public Guid? GradeId { get; set; }
        public Guid? GroupId { get; set; }
        public bool IsCounselor { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }

    public class CounselorAssignmentUpdateDto
    {
        public Guid Id { get; set; }
        public Guid? GradeId { get; set; }
        public Guid? GroupId { get; set; }
        public bool IsCounselor { get; set; }
        public bool IsActive { get; set; }
    }

    public class CounselorAssignmentStatsDto
    {
        public int TotalAssignments { get; set; }
        public int ActiveAssignments { get; set; }
        public int InactiveAssignments { get; set; }
        public int GeneralCounselors { get; set; }
        public int GradeCounselors { get; set; }
        public int GroupCounselors { get; set; }
        public List<CounselorAssignmentByTypeDto> AssignmentsByType { get; set; } = new();
    }

    public class CounselorAssignmentByTypeDto
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<CounselorAssignmentDto> Assignments { get; set; } = new();
    }
}
