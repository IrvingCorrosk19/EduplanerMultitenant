using System.ComponentModel.DataAnnotations;

namespace SchoolManager.ViewModels;

public class EmailApiSettingsViewModel
{
    /// <summary>Dejar vacío para mantener la API key actual.</summary>
    [Display(Name = "Nueva API key (Resend)")]
    public string? NewApiKey { get; set; }

    [Required(ErrorMessage = "Indique el correo remitente")]
    [EmailAddress]
    [Display(Name = "Correo remitente (From)")]
    public string FromEmail { get; set; } = string.Empty;

    [Display(Name = "Nombre remitente")]
    public string FromName { get; set; } = "SchoolManager";

    [Display(Name = "Configuración activa")]
    public bool IsActive { get; set; } = true;

    /// <summary>Solo informativo en la vista.</summary>
    public bool HasStoredApiKey { get; set; }

    public string Provider { get; set; } = "Resend";
}
