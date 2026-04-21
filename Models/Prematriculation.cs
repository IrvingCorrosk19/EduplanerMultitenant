using System;

namespace SchoolManager.Models;

public partial class Prematriculation
{
    public Guid Id { get; set; }
    
    public Guid SchoolId { get; set; }
    
    public Guid StudentId { get; set; }
    
    public Guid? ParentId { get; set; }
    
    public Guid? GradeId { get; set; }
    
    public Guid? GroupId { get; set; }
    
    public Guid PrematriculationPeriodId { get; set; }
    
    public string Status { get; set; } = null!; // Pendiente, Prematriculado, Pagado, Matriculado, Rechazado
    
    public int? FailedSubjectsCount { get; set; }
    
    public bool? AcademicConditionValid { get; set; }
    
    public string? RejectionReason { get; set; }
    
    public string? PrematriculationCode { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public DateTime? PaymentDate { get; set; }
    
    public DateTime? MatriculationDate { get; set; }
    
    public Guid? ConfirmedBy { get; set; }
    
    public Guid? RejectedBy { get; set; }
    
    public Guid? CancelledBy { get; set; }
    
    public virtual School School { get; set; } = null!;
    
    public virtual User Student { get; set; } = null!;
    
    public virtual User? Parent { get; set; }
    
    public virtual GradeLevel? Grade { get; set; }
    
    public virtual Group? Group { get; set; }
    
    public virtual PrematriculationPeriod PrematriculationPeriod { get; set; } = null!;
    
    public virtual User? ConfirmedByUser { get; set; }
    
    public virtual User? RejectedByUser { get; set; }
    
    public virtual User? CancelledByUser { get; set; }
    
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

