namespace SchoolManager.Dtos
{
    public class TeacherAssignmentDisplayDto
    {
        public Guid TeacherId { get; set; }
        public string FullName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string DocumentId { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }

        public List<string> Subjects { get; set; } = new();
        public List<string> Groups { get; set; } = new();

        public List<string> AssignmentDetails { get; set; } = new(); // Ej: "Materia: Matemáticas, Grupo: 8-A"


        public List<SubjectGroupSummary> SubjectGroupDetails { get; set; } = new();


    }


}
