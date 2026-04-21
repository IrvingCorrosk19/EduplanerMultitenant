namespace SchoolManager.Dtos;

public class PaymentConceptCreateDto
{
    public Guid SchoolId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string? Periodicity { get; set; } // Unico, Mensual, Trimestral, Anual
    public bool IsActive { get; set; } = true;
}

