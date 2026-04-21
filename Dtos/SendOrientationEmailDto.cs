using System;

namespace SchoolManager.Dtos
{
    public class SendOrientationEmailDto
    {
        public Guid StudentId { get; set; }
        public Guid OrientationReportId { get; set; }
        public string? CustomMessage { get; set; }
    }
}
