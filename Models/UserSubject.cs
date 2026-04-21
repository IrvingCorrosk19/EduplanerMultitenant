using System;

namespace SchoolManager.Models
{
    public class UserSubject
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;
    }
}
