namespace SchoolManager.Dtos;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid PrematriculationId { get; set; }
    public Guid? RegisteredBy { get; set; }
    public string? RegisteredByName { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public string PaymentStatus { get; set; } = ""; // Pendiente, Confirmado
    public string? PaymentMethod { get; set; } // Tarjeta, Transferencia, Depósito, Yappy, Efectivo
    public string? ReceiptImage { get; set; } // URL o path de la imagen del comprobante
    public Guid? PaymentConceptId { get; set; }
    public string? PaymentConceptName { get; set; }
    public Guid? StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

