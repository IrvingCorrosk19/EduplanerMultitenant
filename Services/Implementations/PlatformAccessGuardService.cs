using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations;

/// <summary>Valida si un usuario (estudiante) tiene acceso a plataforma (PlatformAccessStatus = Activo). Para bloqueo de rutas académicas.</summary>
public class PlatformAccessGuardService : IPlatformAccessGuardService
{
    private readonly SchoolDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    private const string PlatformActivo = "Activo";

    public PlatformAccessGuardService(SchoolDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <inheritdoc />
    public async Task<bool> ValidatePlatformAccessAsync(Guid? userId = null)
    {
        var id = userId ?? await _currentUserService.GetCurrentUserIdAsync();
        if (!id.HasValue)
            return false;

        var user = await _context.Users.FindAsync(id.Value);
        if (user == null)
            return false;

        var role = user.Role?.ToLowerInvariant() ?? "";
        if (role != "student" && role != "estudiante")
            return true; // No es estudiante: no aplicar bloqueo (acceso permitido)

        if (!user.SchoolId.HasValue)
            return false;

        var access = await _context.StudentPaymentAccesses
            .FirstOrDefaultAsync(a => a.StudentId == user.Id && a.SchoolId == user.SchoolId.Value);
        if (access == null)
            return false; // Sin registro = Pendiente

        return access.PlatformAccessStatus == PlatformActivo;
    }
}
