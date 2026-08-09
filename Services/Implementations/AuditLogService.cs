using SchoolManager.Models;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Services.Interfaces;

public class AuditLogService : IAuditLogService
{
    private readonly SchoolDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AuditLogService(SchoolDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Devuelve logs paginados del tenant actual.
    /// Si el usuario es superadmin, devuelve todos los logs del sistema.
    /// Paginado para evitar timeout con tablas grandes (>50k registros).
    /// </summary>
    public async Task<(List<AuditLog> Items, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 100)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 500);

        var role = await _currentUserService.GetCurrentUserRoleAsync();
        IQueryable<AuditLog> query;

        if (string.Equals(role, "superadmin", StringComparison.OrdinalIgnoreCase))
        {
            query = _context.AuditLogs.OrderByDescending(l => l.Timestamp);
        }
        else
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            query = _context.AuditLogs
                .Where(l => l.SchoolId == schoolId)
                .OrderByDescending(l => l.Timestamp);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <summary>
    /// Devuelve un log por ID, verificando que pertenezca al tenant actual.
    /// Los superadmins pueden acceder a cualquier log.
    /// </summary>
    public async Task<AuditLog?> GetByIdAsync(Guid id)
    {
        var role = await _currentUserService.GetCurrentUserRoleAsync();
        if (string.Equals(role, "superadmin", StringComparison.OrdinalIgnoreCase))
        {
            return await _context.AuditLogs
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
        return await _context.AuditLogs
            .FirstOrDefaultAsync(l => l.Id == id && l.SchoolId == schoolId);
    }

    public async Task LogActionAsync(AuditLog log)
    {
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Devuelve los logs de un usuario específico, filtrados por tenant.
    /// Los superadmins pueden ver logs de cualquier usuario del sistema.
    /// </summary>
    public async Task<List<AuditLog>> GetByUserAsync(Guid userId)
    {
        var role = await _currentUserService.GetCurrentUserRoleAsync();
        if (string.Equals(role, "superadmin", StringComparison.OrdinalIgnoreCase))
        {
            return await _context.AuditLogs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
        return await _context.AuditLogs
            .Where(l => l.UserId == userId && l.SchoolId == schoolId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();
    }
}
