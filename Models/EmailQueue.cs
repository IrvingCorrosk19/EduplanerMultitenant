using System;

namespace SchoolManager.Models;

public static class EmailQueueStatus
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
    public const string DeadLetter = "DeadLetter";
}

public class EmailQueue
{
    public Guid Id { get; set; }
    public Guid? JobId { get; set; }
    /// <summary>Nullable para filas creadas antes de la Fase 1.</summary>
    public Guid? CorrelationId { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string Status { get; set; } = EmailQueueStatus.Pending;
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public string? LastError { get; set; }
    public string? ErrorCode { get; set; }
    public string? ProviderMessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string? LockedBy { get; set; }
    public DateTime? NextAttemptAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual EmailJob? Job { get; set; }
}
