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

    public async Task<List<AuditLog>> GetAllAsync() =>
        await _context.AuditLogs.ToListAsync();

    public async Task<AuditLog?> GetByIdAsync(Guid id) =>
        await _context.AuditLogs.FindAsync(id);

    public async Task LogActionAsync(AuditLog log)
    {
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetByUserAsync(Guid userId)
    {
        return await _context.AuditLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();
    }
}
