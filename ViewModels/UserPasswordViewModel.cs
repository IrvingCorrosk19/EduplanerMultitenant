using System;

namespace SchoolManager.ViewModels;

/// <summary>Fila de usuario en gestión de contraseñas (listado + envío masivo).</summary>
public class UserPasswordViewModel
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Grade { get; set; } = "-";
    public string Group { get; set; } = "-";
    public string Status { get; set; } = string.Empty;
    public string? PasswordEmailStatus { get; set; }
    public DateTime? PasswordEmailSentAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}
