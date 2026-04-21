using System;

namespace SchoolManager.Models;

public partial class PrematriculationHistory
{
    public Guid Id { get; set; }
    
    public Guid PrematriculationId { get; set; }
    
    public string PreviousStatus { get; set; } = null!;
    
    public string NewStatus { get; set; } = null!;
    
    public Guid? ChangedBy { get; set; }
    
    public string? Reason { get; set; }
    
    public DateTime ChangedAt { get; set; }
    
    public string? AdditionalInfo { get; set; } // JSON o texto adicional
    
    public virtual Prematriculation Prematriculation { get; set; } = null!;
    
    public virtual User? ChangedByUser { get; set; }
}

