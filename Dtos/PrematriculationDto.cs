namespace SchoolManager.Dtos;

public class PrematriculationDto
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public string StudentDocumentId { get; set; } = "";
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public Guid? GradeId { get; set; }
    public string? GradeName { get; set; }
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public Guid PrematriculationPeriodId { get; set; }
    public string Status { get; set; } = ""; // Pendiente, Prematriculado, Pagado, Matriculado, Rechazado
    public int? FailedSubjectsCount { get; set; }
    public bool? AcademicConditionValid { get; set; }
    public string? RejectionReason { get; set; }
    public string? PrematriculationCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime? MatriculationDate { get; set; }
    public List<PaymentDto>? Payments { get; set; }
}

