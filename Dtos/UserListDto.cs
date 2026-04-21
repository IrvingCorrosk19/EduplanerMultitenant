using System;

namespace SchoolManager.Dtos
{
    public class UserListDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        /// <summary>Grado (estudiantes con asignación activa); "-" si no aplica.</summary>
        public string Grade { get; set; } = "-";
        /// <summary>Grupo (estudiantes con asignación activa); "-" si no aplica.</summary>
        public string Group { get; set; } = "-";
        public string Status { get; set; } = string.Empty;
        public string? PasswordEmailStatus { get; set; }
        public DateTime? PasswordEmailSentAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
