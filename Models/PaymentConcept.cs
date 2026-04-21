using System;

namespace SchoolManager.Models;

public partial class PaymentConcept
{
    public Guid Id { get; set; }
    
    public Guid SchoolId { get; set; }
    
    public string Name { get; set; } = null!; // Matrícula, Mensualidad, Materiales, etc.
    
    public string? Description { get; set; }
    
    public decimal Amount { get; set; }
    
    public string? Periodicity { get; set; } // Unico, Mensual, Trimestral, Anual
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public Guid? CreatedBy { get; set; }
    
    public Guid? UpdatedBy { get; set; }
    
    public virtual School School { get; set; } = null!;
    
    public virtual User? CreatedByUser { get; set; }
    
    public virtual User? UpdatedByUser { get; set; }
    
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

