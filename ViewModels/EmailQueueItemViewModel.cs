using System;

namespace SchoolManager.ViewModels;

public sealed class EmailQueueItemViewModel
{
    public Guid    Id                { get; init; }
    public string  Email             { get; init; } = string.Empty;
    public string  Status            { get; init; } = string.Empty;
    public int     Attempts          { get; init; }
    public int     MaxAttempts       { get; init; }
    public string? ErrorCode         { get; init; }
    public string? ProviderMessageId { get; init; }
    public DateTime  CreatedAt       { get; init; }
    public DateTime? ProcessedAt     { get; init; }
    public DateTime? NextAttemptAt   { get; init; }

    /// <summary>
    /// Indica si el ítem puede ser reintentado manualmente por un administrador.
    /// Solo estados Failed y DeadLetter son elegibles.
    /// </summary>
    public bool CanRetry => Status is "Failed" or "DeadLetter";

    public string StatusBadgeCss => Status switch
    {
        "Pending"     => "badge bg-info",
        "Processing"  => "badge bg-warning text-dark",
        "Sent"        => "badge bg-success",
        "Failed"      => "badge bg-danger",
        "DeadLetter"  => "badge bg-dark",
        _             => "badge bg-secondary"
    };
}
