using SchoolManager.Models;

public interface IAuditLogService
{
    Task<List<AuditLog>> GetAllAsync();
    Task<AuditLog?> GetByIdAsync(Guid id);
    Task LogActionAsync(AuditLog log);
    Task<List<AuditLog>> GetByUserAsync(Guid userId);
}
