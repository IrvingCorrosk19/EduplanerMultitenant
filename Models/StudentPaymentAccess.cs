using System;

namespace SchoolManager.Models;

/// <summary>
/// Estado de pago de carnet y acceso a plataforma por estudiante (módulo Club de Padres).
/// Un registro por (StudentId, SchoolId). No modifica users ni student_id_cards.
/// </summary>
public class StudentPaymentAccess
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public Guid SchoolId { get; set; }

    /// <summary>Pendiente | Pagado | Impreso | Entregado</summary>
    public string CarnetStatus { get; set; } = "Pendiente";

    /// <summary>Pendiente | Activo</summary>
    public string PlatformAccessStatus { get; set; } = "Pendiente";

    public DateTime? CarnetStatusUpdatedAt { get; set; }

    public DateTime? PlatformStatusUpdatedAt { get; set; }

    public Guid? CarnetUpdatedByUserId { get; set; }

    public Guid? PlatformUpdatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User Student { get; set; } = null!;

    public virtual School School { get; set; } = null!;

    public virtual User? CarnetUpdatedByUser { get; set; }

    public virtual User? PlatformUpdatedByUser { get; set; }
}
