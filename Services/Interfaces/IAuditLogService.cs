using SchoolManager.Models;

public interface IAuditLogService
{
    /// <summary>Devuelve logs paginados del tenant actual (o todos si superadmin).</summary>
    Task<(List<AuditLog> Items, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 100);

    Task<AuditLog?> GetByIdAsync(Guid id);
    Task LogActionAsync(AuditLog log);

    /// <summary>Devuelve logs de un usuario, filtrados por tenant.</summary>
    Task<List<AuditLog>> GetByUserAsync(Guid userId);
}
