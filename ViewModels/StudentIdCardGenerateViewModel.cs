using SchoolManager.Dtos;
using SchoolManager.Models;

namespace SchoolManager.ViewModels;

/// <summary>
/// Vista de generación del carnet: datos del estudiante, escuela del alumno y configuración visual real (school_id_card_settings).
/// </summary>
public class StudentIdCardGenerateViewModel
{
    public Guid StudentId { get; set; }

    /// <summary>True si no existe usuario estudiante con este Id.</summary>
    public bool StudentNotFound { get; set; }

    public StudentIdCardDto? Card { get; set; }

    public bool HasActiveCard => Card != null;

    // ── Escuela del estudiante ──
    public string SchoolName { get; set; } = "";

    public string? SchoolLogoUrl { get; set; }

    public string? SchoolPhone { get; set; }

    public string? IdCardPolicy { get; set; }

    // ── Configuración del carnet (SchoolIdCardSetting o defaults) ──
    public string Orientation { get; set; } = "Vertical";

    public bool ShowQr { get; set; } = true;

    public bool ShowPhoto { get; set; } = true;

    public bool ShowSchoolPhone { get; set; } = true;

    public bool ShowEmergencyContact { get; set; }

    public bool ShowAllergies { get; set; }

    public bool ShowWatermark { get; set; } = true;

    public string PrimaryColor { get; set; } = "#0D6EFD";

    public string BackgroundColor { get; set; } = "#FFFFFF";

    public string TextColor { get; set; } = "#111111";

    /// <summary>Plantilla lógica (hoy no cambia layout en PDF salvo por IdCardTemplateField).</summary>
    public string TemplateKey { get; set; } = "default_v1";

    // ── Datos del reverso (estudiante) ──
    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public string? Allergies { get; set; }

    // ── Flags de visibilidad del frente (sincronizados con SchoolIdCardSetting) ──
    public bool ShowDocumentId { get; set; }

    public bool ShowAcademicYear { get; set; }

    public bool ShowPolicyNumber { get; set; }

    // ── Datos para los bloques condicionales del frente ──
    public string? DocumentId { get; set; }

    public string? AcademicYear { get; set; }

    public string? PolicyNumber { get; set; }

    /// <summary>Hay campos IdCardTemplateField activos: el PDF usa layout posicional de una cara.</summary>
    public bool UsesCustomPdfTemplate { get; set; }

    /// <summary>
    /// Construye el modelo a partir de la escuela del estudiante y opcionalmente fila de settings.
    /// </summary>
    public static StudentIdCardGenerateViewModel ForStudent(
        Guid studentId,
        School? school,
        SchoolIdCardSetting? settings)
    {
        var s = settings;
        return new StudentIdCardGenerateViewModel
        {
            StudentId = studentId,
            SchoolName = school?.Name ?? "Sin escuela asignada",
            SchoolLogoUrl = school?.LogoUrl,
            SchoolPhone = school != null && !string.IsNullOrWhiteSpace(school.Phone) ? school.Phone.Trim() : null,
            IdCardPolicy = school?.IdCardPolicy,
            Orientation = s?.Orientation ?? "Vertical",
            ShowQr = s?.ShowQr ?? true,
            ShowPhoto = s?.ShowPhoto ?? true,
            ShowSchoolPhone = s?.ShowSchoolPhone ?? true,
            ShowEmergencyContact = s?.ShowEmergencyContact ?? false,
            ShowAllergies = s?.ShowAllergies ?? false,
            ShowWatermark = s?.ShowWatermark ?? true,
            ShowDocumentId = s?.ShowDocumentId ?? false,
            ShowAcademicYear = s?.ShowAcademicYear ?? false,
            ShowPolicyNumber = s?.ShowPolicyNumber ?? false,
            PrimaryColor = string.IsNullOrWhiteSpace(s?.PrimaryColor) ? "#0D6EFD" : s!.PrimaryColor,
            BackgroundColor = string.IsNullOrWhiteSpace(s?.BackgroundColor) ? "#FFFFFF" : s!.BackgroundColor,
            TextColor = string.IsNullOrWhiteSpace(s?.TextColor) ? "#111111" : s!.TextColor,
            TemplateKey = string.IsNullOrWhiteSpace(s?.TemplateKey) ? "default_v1" : s!.TemplateKey
        };
    }
}
