using System;

namespace SchoolManager.Models;

public partial class Payment
{
    public Guid Id { get; set; }
    
    public Guid SchoolId { get; set; }
    
    public Guid PrematriculationId { get; set; }
    
    public Guid? RegisteredBy { get; set; }
    
    public decimal Amount { get; set; }
    
    public DateTime PaymentDate { get; set; }
    
    public string ReceiptNumber { get; set; } = null!;
    
    public string PaymentStatus { get; set; } = null!; // Pendiente, Confirmado
    
    public string? PaymentMethod { get; set; } // Tarjeta, Transferencia, Depósito, Yappy, Efectivo
    
    public string? ReceiptImage { get; set; } // URL o path de la imagen del comprobante
    
    public Guid? PaymentConceptId { get; set; } // Concepto de pago
    
    public Guid? StudentId { get; set; } // Estudiante (puede ser independiente de prematrícula)
    
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public DateTime? ConfirmedAt { get; set; }
    
    public virtual School School { get; set; } = null!;
    
    public virtual Prematriculation Prematriculation { get; set; } = null!;
    
    public virtual PaymentConcept? PaymentConcept { get; set; }
    
    public virtual User? RegisteredByUser { get; set; }
    
    public virtual User? Student { get; set; }
}

