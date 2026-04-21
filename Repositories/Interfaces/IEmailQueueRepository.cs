using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SchoolManager.Models;

namespace SchoolManager.Repositories.Interfaces;

public interface IEmailQueueRepository
{
    // ── Operaciones básicas ───────────────────────────────────────────────────
    void AddRange(List<EmailQueue> items);
    void Update(EmailQueue item);
    Task SaveChangesAsync(CancellationToken ct = default);

    // ── Claim con lease (para worker con lock recuperable) ────────────────────
    /// <summary>
    /// Reclama atómicamente hasta <paramref name="batchSize"/> ítems listos para procesar.
    /// Solo toma ítems Pending con NextAttemptAt nulo o pasado.
    /// Establece LockedAt, LockedUntil y LockedBy.
    /// </summary>
    Task<List<EmailQueue>> ClaimBatchAsync(
        int batchSize,
        string workerId,
        TimeSpan leaseDuration,
        CancellationToken ct = default);

    // ── Recovery de leases expiradas ──────────────────────────────────────────
    /// <summary>
    /// Recupera ítems Processing/Locked cuyo LockedUntil ya expiró.
    /// Si Attempts &lt; MaxAttempts → vuelve a Pending con NextAttemptAt (backoff).
    /// Si Attempts >= MaxAttempts → marca DeadLetter.
    /// Devuelve número de ítems recuperados.
    /// </summary>
    Task<int> RecoverExpiredLeasesAsync(CancellationToken ct = default);

    // ── Consultas de job ──────────────────────────────────────────────────────
    Task<EmailJob?> GetJobAsync(Guid jobId, CancellationToken ct = default);
    void UpdateJob(EmailJob job);

    // ── Reconteo de agregados de un job ──────────────────────────────────────
    Task RefreshJobCountersAsync(Guid jobId, CancellationToken ct = default);

    // ── Reintento administrativo ──────────────────────────────────────────────

    /// <summary>Carga un ítem de cola por id (para validación antes de reintentar).</summary>
    Task<EmailQueue?> GetQueueItemAsync(Guid queueItemId, CancellationToken ct = default);

    // ── Legacy (getpending sin lease) — mantenida para backward compat ────────
    Task<List<EmailQueue>> GetPendingBatchAsync(int batchSize);
}
