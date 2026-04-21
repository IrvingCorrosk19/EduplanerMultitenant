namespace SchoolManager.Options;

/// <summary>
/// URL pública del sitio para QR del reverso (datos de emergencia). En Render use variable StudentIdCard__PublicBaseUrl.
/// Si está vacío, la vista /ui/generate usa el host de la petición; el PDF nativo solo incluye el segundo QR si aquí hay valor.
/// </summary>
public class StudentIdCardOptions
{
    public const string SectionName = "StudentIdCard";

    /// <summary>Ej. https://tu-app.onrender.com (sin barra final).</summary>
    public string? PublicBaseUrl { get; set; }
}
