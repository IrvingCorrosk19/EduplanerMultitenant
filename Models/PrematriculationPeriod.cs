using System;

namespace SchoolManager.Models;

public partial class PrematriculationPeriod
{
    public Guid Id { get; set; }
    
    public Guid SchoolId { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public bool IsActive { get; set; }
    
    public int MaxCapacityPerGroup { get; set; }
    
    public bool AutoAssignByShift { get; set; }
    
    public decimal RequiredAmount { get; set; } // Monto total requerido para completar el pago
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public Guid? CreatedBy { get; set; }
    
    public Guid? UpdatedBy { get; set; }
    
    public virtual School School { get; set; } = null!;
    
    public virtual User? CreatedByUser { get; set; }
    
    public virtual User? UpdatedByUser { get; set; }
    
    public virtual ICollection<Prematriculation> Prematriculations { get; set; } = new List<Prematriculation>();
}

