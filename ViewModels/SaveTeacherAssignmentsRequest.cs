namespace SchoolManager.ViewModels
{
    public class SaveTeacherAssignmentsRequest
    {
        public Guid TeacherId { get; set; }
        public List<AssignmentDto> Assignments { get; set; }
    }

    public class AssignmentDto
    {
        public Guid SpecialtyId { get; set; }
        public Guid AreaId { get; set; }
        public Guid SubjectId { get; set; }
        public Guid GradeLevelId { get; set; }
        public Guid GroupId { get; set; }
    }
}
