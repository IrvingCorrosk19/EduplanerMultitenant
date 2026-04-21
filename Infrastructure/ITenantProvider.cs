namespace SchoolManager.Infrastructure;

public interface ITenantProvider
{
    Guid? SchoolId { get; }
}
