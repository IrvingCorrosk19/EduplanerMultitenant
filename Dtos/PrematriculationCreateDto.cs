namespace SchoolManager.Dtos;

public class PrematriculationCreateDto
{
    public Guid StudentId { get; set; }
    public Guid? GradeId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid PrematriculationPeriodId { get; set; }
}

