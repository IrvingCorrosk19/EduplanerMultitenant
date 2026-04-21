using System;

namespace SchoolManager.Dtos
{
    public class EmailConfigurationDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public bool SmtpUseSsl { get; set; }
        public bool SmtpUseTls { get; set; }
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class EmailConfigurationCreateDto
    {
        public Guid SchoolId { get; set; }
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public bool SmtpUseSsl { get; set; } = true;
        public bool SmtpUseTls { get; set; } = true;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class EmailConfigurationUpdateDto
    {
        public Guid Id { get; set; }
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public bool SmtpUseSsl { get; set; }
        public bool SmtpUseTls { get; set; }
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
