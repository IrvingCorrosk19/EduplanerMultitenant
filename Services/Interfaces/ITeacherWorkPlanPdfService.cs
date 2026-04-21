namespace SchoolManager.Services.Interfaces;

/// <summary>
/// Generación de PDF formal del plan de trabajo trimestral (estilo Ministerio/institución).
/// </summary>
public interface ITeacherWorkPlanPdfService
{
    Task<byte[]> GeneratePdfAsync(Guid planId, Guid? requestedByUserId, bool isAdmin);
}
