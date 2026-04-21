using System;
using System.Collections.Generic;

namespace SchoolManager.Models;

public partial class AuditLog
{
    public Guid Id { get; set; }

    public Guid? SchoolId { get; set; }

    public Guid? UserId { get; set; }

    public string? UserName { get; set; }

    public string? UserRole { get; set; }

    public string? Action { get; set; }

    public string? Resource { get; set; }

    public string? Details { get; set; }

    public string? IpAddress { get; set; }

    public DateTime? Timestamp { get; set; }

    public virtual School? School { get; set; }

    public virtual User? User { get; set; }
}
