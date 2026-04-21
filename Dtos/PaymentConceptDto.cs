namespace SchoolManager.Dtos;

public class PaymentConceptDto
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string? Periodicity { get; set; } // Unico, Mensual, Trimestral, Anual
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public string? UpdatedByName { get; set; }
}

