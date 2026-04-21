namespace SchoolManager.Infrastructure;

public interface ITenantProvider
{
    Guid? SchoolId { get; }
    bool IsSuperAdmin { get; }
}
