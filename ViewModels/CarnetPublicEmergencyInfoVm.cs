namespace SchoolManager.ViewModels;

/// <summary>Vista pública (solo lectura) tras escanear el QR de emergencia del carnet.</summary>
public class CarnetPublicEmergencyInfoVm
{
    public string FullName { get; set; } = "";
    public string? DocumentId { get; set; }
    public string? Email { get; set; }
    public string? DateOfBirthDisplay { get; set; }
    public string? CellphonePrimary { get; set; }
    public string? CellphoneSecondary { get; set; }
    public string? BloodType { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyRelationship { get; set; }
    public string? Allergies { get; set; }
    public string? SchoolName { get; set; }
    public string? Grade { get; set; }
    public string? Group { get; set; }
    public string? Shift { get; set; }
    public string? UserShift { get; set; }
    public string? PhotoUrl { get; set; }

    /// <summary>True si <c>users.status</c> es <c>active</c> (insensible a mayúsculas).</summary>
    public bool IsUserAccountActive { get; set; }

    /// <summary>Valor crudo de <c>users.status</c> para matizar inactivo (ej. retirado, inactive).</summary>
    public string? UserAccountStatusRaw { get; set; }
}
