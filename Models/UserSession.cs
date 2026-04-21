using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManager.Models
{
    public class UserSession
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string SessionToken { get; set; }

        [Required]
        public string IpAddress { get; set; }

        [Required]
        public string UserAgent { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        public DateTime? LastActivityAt { get; set; }

        public bool IsActive { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
} 