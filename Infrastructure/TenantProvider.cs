using System.Security.Claims;

namespace SchoolManager.Infrastructure;

public class TenantProvider : ITenantProvider
{
    public Guid? SchoolId { get; }

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        var claim = httpContextAccessor.HttpContext?.User?.FindFirst("school_id");
        if (claim != null && Guid.TryParse(claim.Value, out var id) && id != Guid.Empty)
            SchoolId = id;
    }
}
