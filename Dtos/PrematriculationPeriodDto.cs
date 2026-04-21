namespace SchoolManager.Dtos;

public class PrematriculationPeriodDto
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public int MaxCapacityPerGroup { get; set; }
    public bool AutoAssignByShift { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsPeriodActive { get; set; } // Calculado: si la fecha actual está entre StartDate y EndDate
}

