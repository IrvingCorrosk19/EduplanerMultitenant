using System;

namespace SchoolManager.Models;

public class SchoolIdCardSetting
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    /// <summary>
    /// Clave de plantilla persistida por compatibilidad. El PDF no ramifica por este valor:
    /// el layout es "CarnetQR" (frente+reverso) si no hay filas activas en IdCardTemplateField;
    /// si las hay, se usa el layout posicional de una sola cara.
    /// </summary>
    public string TemplateKey { get; set; } = "default_v1";
    public int PageWidthMm { get; set; } = 55;
    public int PageHeightMm { get; set; } = 85;
    public int BleedMm { get; set; } = 0;
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string PrimaryColor { get; set; } = "#0D6EFD";
    public string TextColor { get; set; } = "#111111";
    public bool ShowQr { get; set; } = true;
    public bool ShowPhoto { get; set; } = true;
    /// <summary>Mostrar teléfono del colegio en el reverso del carnet.</summary>
    public bool ShowSchoolPhone { get; set; } = true;
    /// <summary>Mostrar contacto de emergencia en el reverso del carnet.</summary>
    public bool ShowEmergencyContact { get; set; } = false;
    /// <summary>Mostrar alergias en el reverso del carnet.</summary>
    public bool ShowAllergies { get; set; } = false;
    /// <summary>Orientación del carnet: Vertical (55×85 mm) u Horizontal (85×55 mm), tipo CR.</summary>
    public string Orientation { get; set; } = "Vertical";
    /// <summary>Mostrar logo del colegio como marca de agua en el frente y reverso del carnet.</summary>
    public bool ShowWatermark { get; set; } = true;

    // ── Campos del diseño moderno ──────────────────────────────────────────────
    /// <summary>Activar el nuevo layout moderno (foto circular, bloques de datos, logos lado a lado).</summary>
    public bool UseModernLayout { get; set; } = false;
    /// <summary>Mostrar cédula/documento del estudiante en el carnet.</summary>
    public bool ShowDocumentId { get; set; } = false;
    /// <summary>Mostrar número de póliza en el carnet (temporal: calculado desde studentId hasta que exista campo en BD).</summary>
    public bool ShowPolicyNumber { get; set; } = false;
    /// <summary>Mostrar año lectivo activo del estudiante en el carnet.</summary>
    public bool ShowAcademicYear { get; set; } = false;
    /// <summary>Mostrar insignia/logo secundario en el carnet (esquina opuesta al logo principal).</summary>
    public bool ShowSecondaryLogo { get; set; } = false;
    /// <summary>URL o path de la insignia secundaria (escudo, sello, etc.). Diferente al LogoUrl principal de la escuela.</summary>
    public string? SecondaryLogoUrl { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual School School { get; set; } = null!;
}
