namespace SchoolManager.Models;

public class StaffQrToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Aislamiento MultiTenant (denormalizado desde User.SchoolId).</summary>
    public Guid SchoolId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime? ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual School? School { get; set; }
}
