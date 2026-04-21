using System;

namespace SchoolManager.Models;

public class IdCardTemplateField
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public string FieldKey { get; set; } = null!; // FullName, DocumentId, Grade, Group, Shift, CardNumber, SchoolName, SchoolLogo, Photo, Qr
    public bool IsEnabled { get; set; } = true;
    public decimal XMm { get; set; } = 0;
    public decimal YMm { get; set; } = 0;
    public decimal WMm { get; set; } = 0;
    public decimal HMm { get; set; } = 0;
    public decimal FontSize { get; set; } = 10;
    public string FontWeight { get; set; } = "Normal";
    public DateTime? CreatedAt { get; set; }

    public virtual School School { get; set; } = null!;
}
