using System;

namespace SchoolManager.Dtos
{
    public class CounselorGroupDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public Guid UserId { get; set; }
        public Guid? GradeId { get; set; }
        public Guid? GroupId { get; set; }
        public string? GradeName { get; set; }
        public string? GroupName { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
