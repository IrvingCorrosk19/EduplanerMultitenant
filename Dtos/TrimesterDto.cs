namespace SchoolManager.Dtos
{
    public class TrimesterDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int Order { get; set; }
        public string? Description { get; set; }
    }

    public class TrimestersConfigDto
    {
        public int AnioEscolar { get; set; }
        public List<TrimesterDto> Trimestres { get; set; }
    }

    public class TrimesterValidationDto
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    public class TrimesterActivationDto
    {
        public Guid Id { get; set; }
    }
}
