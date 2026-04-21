using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Filters;

/// <summary>
/// Filtro global que bloquea el acceso al portal académico cuando el estudiante tiene
/// PlatformAccessStatus = Pendiente. Redirige a /Student/AccessPending y registra log de auditoría.
/// No modifica IPlatformAccessGuardService; solo lo utiliza.
/// </summary>
public class PlatformAccessGuardFilter : IAsyncActionFilter
{
    private readonly IPlatformAccessGuardService _platformAccessGuard;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PlatformAccessGuardFilter> _logger;

    private static readonly string[] ExcludedPathPrefixes = new[]
    {
        "/Auth",
        "/Account",
        "/Student/AccessPending",
        "/api/auth"
    };

    public PlatformAccessGuardFilter(
        IPlatformAccessGuardService platformAccessGuard,
        ICurrentUserService currentUserService,
        ILogger<PlatformAccessGuardFilter> logger)
    {
        _platformAccessGuard = platformAccessGuard;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;

        if (IsExcludedPath(path))
        {
            await next();
            return;
        }

        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            await next();
            return;
        }

        var role = (await _currentUserService.GetCurrentUserRoleAsync())?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(role) || (role != "student" && role != "estudiante"))
        {
            await next();
            return;
        }

        var studentId = await _currentUserService.GetCurrentUserIdAsync();
        if (!studentId.HasValue)
        {
            await next();
            return;
        }

        var hasAccess = await _platformAccessGuard.ValidatePlatformAccessAsync(studentId);
        if (hasAccess)
        {
            await next();
            return;
        }

        _logger.LogWarning(
            "Acceso a plataforma bloqueado. StudentId={StudentId}, Ruta={Path}, Fecha={Fecha:O}, Motivo=PlatformAccessPending",
            studentId.Value,
            path,
            DateTime.UtcNow);

        context.Result = new RedirectResult("/Student/AccessPending");
    }

    private static bool IsExcludedPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        var normalized = path.Replace('\\', '/').Trim();
        if (normalized.Length > 0 && !normalized.StartsWith('/'))
            normalized = "/" + normalized;

        foreach (var prefix in ExcludedPathPrefixes)
        {
            var p = prefix.Trim();
            if (p.Length > 0 && !p.StartsWith('/'))
                p = "/" + p;
            if (normalized.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
