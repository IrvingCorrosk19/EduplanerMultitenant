using System;

namespace SchoolManager.Dtos
{
    public class SendDisciplineEmailDto
    {
        public Guid StudentId { get; set; }
        public Guid DisciplineReportId { get; set; }
        public string? CustomMessage { get; set; }
    }
}
