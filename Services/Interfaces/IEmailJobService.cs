using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SchoolManager.Dtos;
using SchoolManager.ViewModels;

namespace SchoolManager.Services.Interfaces;

public interface IEmailJobService
{
    /// <summary>Lista paginada de jobs. Si schoolId es null, devuelve todos (superadmin).</summary>
    Task<IReadOnlyList<EmailJobListItemViewModel>> GetJobsAsync(
        Guid? schoolId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Detalle completo de un job con sus ítems de cola.</summary>
    Task<EmailJobDetailsViewModel?> GetJobDetailsAsync(
        Guid jobId,
        Guid? schoolId,
        CancellationToken ct = default);

    /// <summary>Métricas de resumen del día.</summary>
    Task<EmailJobSummaryViewModel> GetSummaryAsync(
        Guid? schoolId,
        CancellationToken ct = default);

    // ── Reintentos administrativos ────────────────────────────────────────────

    /// <summary>
    /// Reintenta todos los ítems Failed/DeadLetter de un job.
    /// Política de reset: Attempts=0 (obligatorio para DeadLetter, evita re-dead-letter inmediato),
    /// NextAttemptAt=null (procesamiento inmediato sin backoff), todos los campos de lock limpiados.
    /// </summary>
    Task<RetryResultDto> RetryJobFailedAsync(
        Guid jobId,
        Guid? schoolId,
        Guid actorUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Reintenta un único ítem de la cola.
    /// Valida que el ítem pertenezca al jobId indicado (protección de ownership multi-tenant).
    /// Misma política de reset que RetryJobFailedAsync.
    /// </summary>
    Task<RetryResultDto> RetryItemAsync(
        Guid queueItemId,
        Guid jobId,
        Guid? schoolId,
        Guid actorUserId,
        CancellationToken ct = default);
}
