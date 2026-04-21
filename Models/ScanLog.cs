using System;

namespace SchoolManager.Models;

public class ScanLog
{
    public Guid Id { get; set; }
    public Guid? StudentId { get; set; }
    public string ScanType { get; set; } = null!;
    public string Result { get; set; } = null!;
    public Guid ScannedBy { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

    public User? Student { get; set; }
}
