using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public class EmailJob
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? SchoolId { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = EmailJobStatus.Accepted;
    public int TotalItems { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int RejectedCount { get; set; }
    public string? SummaryJson { get; set; }

    public virtual ICollection<EmailQueue> QueueItems { get; set; } = new List<EmailQueue>();
}
