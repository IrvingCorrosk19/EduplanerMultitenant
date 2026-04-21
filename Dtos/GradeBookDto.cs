namespace SchoolManager.Dtos
{
    public class GradeBookDto
    {
        public IEnumerable<ActivityHeaderDto> Activities { get; set; } = Enumerable.Empty<ActivityHeaderDto>();
        public IEnumerable<StudentGradeRowDto> Rows { get; set; } = Enumerable.Empty<StudentGradeRowDto>();
    }
}
