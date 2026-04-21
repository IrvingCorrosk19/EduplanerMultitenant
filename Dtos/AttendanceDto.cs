namespace SchoolManager.Dtos
{
    public class AttendanceDto
    {
        public string Month { get; set; } = string.Empty;
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
        public string Trimester { get; set; } = string.Empty;
    }

}
