using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolManager.Models
{
    public class ErrorViewModel
    {
        [Required]
        public string RequestId { get; set; } = null!;

        public string? Message { get; set; }

        public string? Details { get; set; }

        public string? StackTrace { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? UserId { get; set; }

        public string? SchoolId { get; set; }  // Opcional para tracking

        public string? Path { get; set; }

        public int? StatusCode { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
