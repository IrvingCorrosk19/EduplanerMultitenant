using SchoolManager.Dtos;

namespace SchoolManager.Services.Interfaces;

/// <summary>Servicio QL Services: carnets pagados pendientes de impresión, marcar Impreso y Entregado.</summary>
public interface IQlServicesCarnetService
{
    /// <summary>Lista registros con carnet_status = Pagado (pendientes de impresión) de la escuela del usuario.</summary>
    Task<IReadOnlyList<PendingPrintItemDto>> GetPendingPrintAsync();

    /// <summary>Transición Pagado → Impreso.</summary>
    Task MarkCarnetAsPrintedAsync(Guid studentId);

    /// <summary>Transición Impreso → Entregado.</summary>
    Task MarkCarnetAsDeliveredAsync(Guid studentId);
}
