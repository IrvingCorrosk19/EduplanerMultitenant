using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManager.Models;
using SchoolManager.Repositories.Interfaces;

namespace SchoolManager.Repositories.Implementations;

public class EmailQueueRepository : IEmailQueueRepository
{
    // Constantes de backoff para reintentos
    private static readonly TimeSpan[] BackoffDelays =
    [
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(30)
    ];

    private readonly SchoolDbContext _db;
    private readonly ILogger<EmailQueueRepository> _logger;

    public EmailQueueRepository(SchoolDbContext db, ILogger<EmailQueueRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Operaciones básicas ───────────────────────────────────────────────────

    public void AddRange(List<EmailQueue> items)
    {
        if (items == null || items.Count == 0) return;
        _db.EmailQueues.AddRange(items);
    }

    public void Update(EmailQueue item) => _db.EmailQueues.Update(item);

    public void UpdateJob(EmailJob job) => _db.EmailJobs.Update(job);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);

    // ── Legacy (sin lease) ────────────────────────────────────────────────────

    public async Task<List<EmailQueue>> GetPendingBatchAsync(int batchSize)
    {
        return await _db.EmailQueues
            .Where(e => e.Status == EmailQueueStatus.Pending)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .Include(e => e.User)
            .ToListAsync();
    }

    // ── Claim con lease ───────────────────────────────────────────────────────

    public async Task<List<EmailQueue>> ClaimBatchAsync(
        int batchSize,
        string workerId,
        TimeSpan leaseDuration,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var leaseUntil = now + leaseDuration;

        // Selecciona ítems Pending cuyo NextAttemptAt ya pasó (o es null)
        var candidates = await _db.EmailQueues
            .Where(e => e.Status == EmailQueueStatus.Pending
                     && (e.NextAttemptAt == null || e.NextAttemptAt <= now))
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        if (candidates.Count == 0) return candidates;

        foreach (var item in candidates)
        {
            item.Status     = EmailQueueStatus.Processing;
            item.LockedAt   = now;
            item.LockedUntil = leaseUntil;
            item.LockedBy   = workerId;
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogDebug(
            "ClaimBatch WorkerId={WorkerId} Claimed={Count} LeaseUntil={LeaseUntil}",
            workerId, candidates.Count, leaseUntil);

        return candidates;
    }

    // ── Recovery de leases expiradas ──────────────────────────────────────────

    public async Task<int> RecoverExpiredLeasesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Ítems bloqueados cuyo lease ya expiró
        var expired = await _db.EmailQueues
            .Where(e => e.Status == EmailQueueStatus.Processing
                     && e.LockedUntil != null
                     && e.LockedUntil < now)
            .ToListAsync(ct);

        if (expired.Count == 0) return 0;

        foreach (var item in expired)
        {
            var workerThatFailed = item.LockedBy ?? "unknown";

            if (item.Attempts < item.MaxAttempts)
            {
                // Volver a Pending con backoff
                var backoff = GetBackoffDelay(item.Attempts);
                item.Status        = EmailQueueStatus.Pending;
                item.LockedAt      = null;
                item.LockedUntil   = null;
                item.LockedBy      = null;
                item.NextAttemptAt = now + backoff;

                _logger.LogWarning(
                    "LeaseExpired QueueItemId={Id} Worker={Worker} Attempts={Attempts} NextAttempt={Next} " +
                    "Action=retry_scheduled",
                    item.Id, workerThatFailed, item.Attempts, item.NextAttemptAt);
            }
            else
            {
                // Agotados los intentos → DeadLetter
                item.Status      = EmailQueueStatus.DeadLetter;
                item.ProcessedAt = now;
                item.LockedAt    = null;
                item.LockedUntil = null;
                item.LockedBy    = null;
                item.ErrorCode   ??= "LEASE_EXPIRED_MAX_ATTEMPTS";

                _logger.LogError(
                    "LeaseExpired QueueItemId={Id} Worker={Worker} Attempts={Attempts} " +
                    "Action=dead_lettered",
                    item.Id, workerThatFailed, item.Attempts);
            }
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "RecoverExpiredLeases recovered={Count} at={Now}",
            expired.Count, now);

        return expired.Count;
    }

    // ── Reintento administrativo ──────────────────────────────────────────────

    public async Task<EmailQueue?> GetQueueItemAsync(Guid queueItemId, CancellationToken ct = default) =>
        await _db.EmailQueues.FindAsync([queueItemId], ct);

    // ── Consulta de Job ───────────────────────────────────────────────────────

    public async Task<EmailJob?> GetJobAsync(Guid jobId, CancellationToken ct = default)
    {
        return await _db.EmailJobs.FindAsync([jobId], ct);
    }

    // ── Reconteo de agregados de un job (actualización desde la cola) ─────────

    public async Task RefreshJobCountersAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await _db.EmailJobs.FindAsync([jobId], ct);
        if (job == null) return;

        var items = await _db.EmailQueues
            .Where(e => e.JobId == jobId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total    = g.Count(),
                Sent     = g.Count(e => e.Status == EmailQueueStatus.Sent),
                Failed   = g.Count(e => e.Status == EmailQueueStatus.Failed
                                     || e.Status == EmailQueueStatus.DeadLetter),
                Pending  = g.Count(e => e.Status == EmailQueueStatus.Pending
                                     || e.Status == EmailQueueStatus.Processing)
            })
            .FirstOrDefaultAsync(ct);

        if (items == null) return;

        var now = DateTime.UtcNow;

        job.SentCount   = items.Sent;
        job.FailedCount = items.Failed;

        // Determinar estado del job
        if (items.Pending == 0)
        {
            job.CompletedAt = job.CompletedAt ?? now;
            job.Status = items.Failed > 0
                ? EmailJobStatus.CompletedWithErrors
                : EmailJobStatus.Completed;
        }
        else if (items.Sent > 0 || items.Failed > 0)
        {
            job.Status    = EmailJobStatus.Processing;
            job.StartedAt ??= now;
        }

        await _db.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TimeSpan GetBackoffDelay(int attempts)
    {
        // Backoff exponencial con pequeño jitter
        var idx = Math.Clamp(attempts, 0, BackoffDelays.Length - 1);
        var baseDelay = BackoffDelays[idx];
        // jitter hasta ±20% del delay base
        var jitterSeconds = (int)(baseDelay.TotalSeconds * 0.2 * (Random.Shared.NextDouble() * 2 - 1));
        return baseDelay + TimeSpan.FromSeconds(jitterSeconds);
    }
}
