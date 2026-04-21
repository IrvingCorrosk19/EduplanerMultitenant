using System;

namespace SchoolManager.Dtos;

/// <summary>Respuesta del endpoint GET JobStatus/{jobId}.</summary>
public sealed class JobStatusDto
{
    public Guid   JobId         { get; init; }
    public Guid   CorrelationId { get; init; }
    public string Status        { get; init; } = string.Empty;
    public int    TotalItems    { get; init; }
    public int    SentCount     { get; init; }
    public int    FailedCount   { get; init; }
    public int    RejectedCount { get; init; }
    public DateTime  RequestedAt  { get; init; }
    public DateTime? StartedAt    { get; init; }
    public DateTime? CompletedAt  { get; init; }

    /// <summary>Porcentaje de progreso (0–100). -1 si no hay ítems.</summary>
    public int ProgressPct => TotalItems == 0
        ? -1
        : (int)Math.Round((SentCount + FailedCount) * 100.0 / TotalItems);
}
