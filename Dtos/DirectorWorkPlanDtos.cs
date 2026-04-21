namespace SchoolManager.Dtos;

public class WorkPlanFiltersDto
{
    public Guid? SchoolId { get; set; }
    public Guid? AcademicYearId { get; set; }
    public int? Trimester { get; set; }
    public Guid? TeacherId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? GradeLevelId { get; set; }
    public Guid? GroupId { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; }
}

public class DirectorWorkPlanListItemDto
{
    public Guid Id { get; set; }
    public string TeacherName { get; set; } = "";
    public Guid TeacherId { get; set; }
    public string SubjectName { get; set; } = "";
    public string GradeLevelName { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string AcademicYearName { get; set; } = "";
    public int Trimester { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
}

public class DirectorWorkPlanDetailDto
{
    public Guid Id { get; set; }
    public string TeacherName { get; set; } = "";
    public Guid TeacherId { get; set; }
    public string SubjectName { get; set; } = "";
    public string GradeLevelName { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string AcademicYearName { get; set; } = "";
    public int Trimester { get; set; }
    public string? Objectives { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedByName { get; set; }
    public string? ReviewComment { get; set; }
    public List<TeacherWorkPlanDetailDto> Details { get; set; } = new();
    public List<ReviewLogEntryDto> ReviewLog { get; set; } = new();
}

public class ReviewLogEntryDto
{
    public string Action { get; set; } = "";
    public string PerformedByName { get; set; } = "";
    public DateTime PerformedAt { get; set; }
    public string? Comment { get; set; }
    public string? Summary { get; set; }
}

public class ApproveRejectRequestDto
{
    public string? Comment { get; set; }
}

public class DirectorFilterOptionsDto
{
    public List<FilterOptionDto> Teachers { get; set; } = new();
    public List<FilterOptionDto> Subjects { get; set; } = new();
    public List<FilterOptionDto> GradeLevels { get; set; } = new();
    public List<FilterOptionDto> Groups { get; set; } = new();
}

public class FilterOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class DirectorWorkPlanDashboardDto
{
    public int TotalPlans { get; set; }
    public int SubmittedCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int DraftCount { get; set; }
    public List<DirectorWorkPlanListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
