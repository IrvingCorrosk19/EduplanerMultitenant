using System;
using System.Collections.Generic;

namespace SchoolManager.ViewModels;

public sealed class EmailJobDetailsViewModel
{
    public Guid   JobId         { get; init; }
    public Guid   CorrelationId { get; init; }
    public string Status        { get; init; } = string.Empty;
    public int    TotalItems    { get; init; }
    public int    SentCount     { get; init; }
    public int    FailedCount   { get; init; }
    public int    RejectedCount { get; init; }
    public DateTime  RequestedAt { get; init; }
    public DateTime? StartedAt   { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string CreatedByName  { get; init; } = string.Empty;
    public string? SchoolName    { get; init; }

    public IReadOnlyList<EmailQueueItemViewModel> Items { get; init; } = Array.Empty<EmailQueueItemViewModel>();

    public int ProgressPct => TotalItems == 0
        ? -1
        : (int)Math.Round((SentCount + FailedCount) * 100.0 / TotalItems);

    public bool IsActive => Status is "Accepted" or "Processing";

    public string StatusBadgeCss => Status switch
    {
        "Accepted"            => "badge bg-info",
        "Processing"          => "badge bg-warning text-dark",
        "Completed"           => "badge bg-success",
        "CompletedWithErrors" => "badge bg-warning text-dark",
        "Failed"              => "badge bg-danger",
        "Cancelled"           => "badge bg-secondary",
        _                     => "badge bg-secondary"
    };
}
