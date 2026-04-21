using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolManager.Models
{
    public class EmailConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SchoolId { get; set; }

        [Required]
        [StringLength(255)]
        public string SmtpServer { get; set; } = string.Empty;

        [Required]
        public int SmtpPort { get; set; } = 587;

        [Required]
        [StringLength(255)]
        public string SmtpUsername { get; set; } = string.Empty;

        [Required]
        public string SmtpPassword { get; set; } = string.Empty;

        [Required]
        public bool SmtpUseSsl { get; set; } = true;

        [Required]
        public bool SmtpUseTls { get; set; } = true;

        [Required]
        [StringLength(255)]
        public string FromEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string FromName { get; set; } = string.Empty;

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual School? School { get; set; }
    }
}
