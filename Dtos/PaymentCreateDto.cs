namespace SchoolManager.Dtos;

public class PaymentCreateDto
{
    public Guid? PrematriculationId { get; set; } // Opcional si es pago independiente
    public Guid? PaymentConceptId { get; set; } // Concepto de pago
    public Guid? StudentId { get; set; } // Estudiante (puede ser independiente de prematrícula)
    public decimal Amount { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public string? PaymentMethod { get; set; } // Tarjeta, Transferencia, Depósito, Yappy, Efectivo
    public string? ReceiptImage { get; set; } // URL o path de la imagen del comprobante
    public string? Notes { get; set; }
    public DateTime PaymentDate { get; set; }
}

