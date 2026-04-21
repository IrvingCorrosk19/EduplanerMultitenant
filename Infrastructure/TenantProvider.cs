using System.Security.Claims;

namespace SchoolManager.Infrastructure;

public class TenantProvider : ITenantProvider
{
    public Guid? SchoolId { get; }
    public bool IsSuperAdmin { get; }

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var role = user?.FindFirst(ClaimTypes.Role)?.Value ?? "";
        IsSuperAdmin = role.Equals("superadmin", StringComparison.OrdinalIgnoreCase);

        var claim = user?.FindFirst("school_id");
        if (claim != null && Guid.TryParse(claim.Value, out var id) && id != Guid.Empty)
            SchoolId = id;
    }
}
