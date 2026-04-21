using System;

namespace SchoolManager.Models;

/// <summary>
/// Configuración de envío por API (p. ej. Resend). Solo debe haber un registro con IsActive = true.
/// </summary>
public class EmailApiConfiguration
{
    public Guid Id { get; set; }

    /// <summary>Proveedor: "Resend", etc.</summary>
    public string Provider { get; set; } = "Resend";

    public string ApiKey { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "SchoolManager";

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
