using System;

namespace SchoolManager.Models;

public class StudentQrToken
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string Token { get; set; } = null!;
    public DateTime? ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;

    public User Student { get; set; } = null!;
}
