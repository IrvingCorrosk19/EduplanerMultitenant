namespace SchoolManager.Dtos;

public class TeacherWorkPlanListDto
{
    public Guid Id { get; set; }
    public string TeacherName { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string GradeLevelName { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string AcademicYearName { get; set; } = "";
    public int Trimester { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class TeacherWorkPlanDetailDto
{
    public Guid Id { get; set; }
    public string WeeksRange { get; set; } = "";
    public string? Topic { get; set; }
    public string? ConceptualContent { get; set; }
    public string? ProceduralContent { get; set; }
    public string? AttitudinalContent { get; set; }
    public string? BasicCompetencies { get; set; }
    public string? AchievementIndicators { get; set; }
    public int DisplayOrder { get; set; }
}

public class TeacherWorkPlanDto
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = "";
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public Guid GradeLevelId { get; set; }
    public string GradeLevelName { get; set; } = "";
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = "";
    public Guid AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = "";
    public int Trimester { get; set; }
    public string? Objectives { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<TeacherWorkPlanDetailDto> Details { get; set; } = new();
}

public class CreateTeacherWorkPlanDto
{
    public Guid SubjectId { get; set; }
    public Guid GradeLevelId { get; set; }
    public Guid GroupId { get; set; }
    public Guid AcademicYearId { get; set; }
    public int Trimester { get; set; }
    public string? Objectives { get; set; }
    public string Status { get; set; } = "Draft";
    public List<CreateTeacherWorkPlanDetailDto> Details { get; set; } = new();
}

public class CreateTeacherWorkPlanDetailDto
{
    public string WeeksRange { get; set; } = "";
    public string? Topic { get; set; }
    public string? ConceptualContent { get; set; }
    public string? ProceduralContent { get; set; }
    public string? AttitudinalContent { get; set; }
    public string? BasicCompetencies { get; set; }
    public string? AchievementIndicators { get; set; }
    public int DisplayOrder { get; set; }
}

public class TeacherWorkPlanFormOptionsDto
{
    public List<AcademicYearOptionDto> AcademicYears { get; set; } = new();
    /// <summary>Combinaciones válidas (materia + grado + grupo) asignadas al docente.</summary>
    public List<TeacherAssignmentOptionDto> AssignmentOptions { get; set; } = new();
}

public class TeacherAssignmentOptionDto
{
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public Guid GradeLevelId { get; set; }
    public string GradeLevelName { get; set; } = "";
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = "";
    /// <summary>Etiqueta para dropdown: "Materia - Grado - Grupo".</summary>
    public string Label => $"{SubjectName} - {GradeLevelName} - {GroupName}";
}

public class AcademicYearOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class SubjectOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class GradeLevelOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class GroupOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}
