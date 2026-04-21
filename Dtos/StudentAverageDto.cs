namespace SchoolManager.Dtos
{
    public class StudentAverageDto
    {
        public string StudentId { get; set; }
        public string StudentFullName { get; set; }
        // Diccionario con claves como "1T", "2T", "3T"
        public Dictionary<string, decimal> Promedios { get; set; } = new();
    }
}