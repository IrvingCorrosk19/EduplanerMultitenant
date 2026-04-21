namespace SchoolManager.Dtos;

public class StudentCardRenderDto
{
    public Guid StudentId { get; set; }
    public string FullName { get; set; } = "";
    public string? DocumentId { get; set; }
    public string Grade { get; set; } = "";
    public string Group { get; set; } = "";
    public string Shift { get; set; } = "";
    public string CardNumber { get; set; } = "";
    public string QrToken { get; set; } = "";

    /// <summary>URL absoluta para segundo QR (emergencia / datos personales). Null = no dibujar.</summary>
    public string? EmergencyInfoPageUrl { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Allergies { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyRelationship { get; set; }
    public string? PolicyNumber { get; set; }
    public string? AcademicYear { get; set; }

    // School context
    public string SchoolName { get; set; } = "";
    public string? SchoolPhone { get; set; }
    public string? IdCardPolicy { get; set; }

    // Pre-loaded image bytes (null = not available / not shown)
    public byte[]? LogoBytes { get; set; }
    public byte[]? SecondaryLogoBytes { get; set; }
    public byte[]? PhotoBytes { get; set; }
    public byte[]? WatermarkBytes { get; set; }
}
