using System;

namespace SchoolManager.Dtos
{
    public class AttendanceResponseDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
    }
}
