namespace SchoolManager.Dtos;

public class AvailableGroupsDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = "";
    public string? GradeName { get; set; }
    public string? Shift { get; set; } // Jornada del grupo
    public int CurrentStudents { get; set; }
    public int? MaxCapacity { get; set; }
    public int AvailableSpots { get; set; }
    public bool IsAvailable { get; set; }
}

