using SchoolManager.Dtos;

namespace SchoolManager.Services.Interfaces;

/// <summary>
/// Servicio de Dirección Académica: solo rol Director, solo planes de su escuela (SchoolId).
/// No modifica contenido del plan; solo aprobar/rechazar y comentar.
/// </summary>
public interface IDirectorWorkPlanService
{
    Task<DirectorWorkPlanDashboardDto> GetDashboardAsync(WorkPlanFiltersDto filters);
    Task<DirectorWorkPlanDetailDto?> GetPlanByIdAsync(Guid planId, Guid schoolId);
    Task ApproveAsync(Guid planId, Guid directorUserId, string? comment);
    Task RejectAsync(Guid planId, Guid directorUserId, string comment);
    Task<byte[]> ExportPlanPdfAsync(Guid planId, Guid schoolId);
    Task<byte[]> ExportConsolidatedPdfAsync(WorkPlanFiltersDto filters, Guid schoolId);
    Task<DirectorFilterOptionsDto> GetFilterOptionsAsync(Guid schoolId);
}
