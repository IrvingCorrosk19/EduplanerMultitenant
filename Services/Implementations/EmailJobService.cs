using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManager.Dtos;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using SchoolManager.ViewModels;

namespace SchoolManager.Services.Implementations;

public sealed class EmailJobService : IEmailJobService
{
    private readonly SchoolDbContext              _db;
    private readonly ILogger<EmailJobService>     _logger;

    public EmailJobService(SchoolDbContext db, ILogger<EmailJobService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EmailJobListItemViewModel>> GetJobsAsync(
        Guid? schoolId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var q = _db.EmailJobs.AsNoTracking().AsQueryable();

        if (schoolId.HasValue)
            q = q.Where(j => j.SchoolId == schoolId.Value);

        var jobs = await q
            .OrderByDescending(j => j.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(_db.Users.AsNoTracking(),
                  j => j.CreatedByUserId,
                  u => u.Id,
                  (j, u) => new
                  {
                      j.Id,
                      j.CorrelationId,
                      j.Status,
                      j.TotalItems,
                      j.SentCount,
                      j.FailedCount,
                      j.RejectedCount,
                      j.RequestedAt,
                      j.CompletedAt,
                      j.SchoolId,
                      CreatedByName = (u.Name + " " + u.LastName).Trim()
                  })
            .ToListAsync(ct);

        var schoolIds = jobs
            .Where(j => j.SchoolId.HasValue)
            .Select(j => j.SchoolId!.Value)
            .Distinct()
            .ToList();

        var schoolNames = new Dictionary<Guid, string>();
        if (schoolIds.Count > 0)
        {
            schoolNames = await _db.Schools
                .AsNoTracking()
                .Where(s => schoolIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        }

        return jobs.Select(j => new EmailJobListItemViewModel
        {
            JobId         = j.Id,
            CorrelationId = j.CorrelationId,
            Status        = j.Status,
            TotalItems    = j.TotalItems,
            SentCount     = j.SentCount,
            FailedCount   = j.FailedCount,
            RejectedCount = j.RejectedCount,
            RequestedAt   = j.RequestedAt,
            CompletedAt   = j.CompletedAt,
            CreatedByName = j.CreatedByName,
            SchoolName    = j.SchoolId.HasValue && schoolNames.TryGetValue(j.SchoolId.Value, out string? sn)
                ? sn
                : null
        }).ToList();
    }

    public async Task<EmailJobDetailsViewModel?> GetJobDetailsAsync(
        Guid jobId,
        Guid? schoolId,
        CancellationToken ct = default)
    {
        var job = await _db.EmailJobs
            .AsNoTracking()
            .Include(j => j.QueueItems)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

        if (job == null) return null;

        if (schoolId.HasValue && job.SchoolId != schoolId.Value)
            return null;

        var creator = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == job.CreatedByUserId)
            .Select(u => new { u.Name, u.LastName })
            .FirstOrDefaultAsync(ct);

        string createdByName = creator != null
            ? $"{creator.Name} {creator.LastName}".Trim()
            : "—";

        string? schoolName = null;
        if (job.SchoolId.HasValue)
        {
            schoolName = await _db.Schools
                .AsNoTracking()
                .Where(s => s.Id == job.SchoolId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(ct);
        }

        var items = job.QueueItems
            .OrderBy(q => q.CreatedAt)
            .Select(q => new EmailQueueItemViewModel
            {
                Id                = q.Id,
                Email             = q.Email,
                Status            = q.Status,
                Attempts          = q.Attempts,
                MaxAttempts       = q.MaxAttempts,
                ErrorCode         = q.ErrorCode,
                ProviderMessageId = q.ProviderMessageId,
                CreatedAt         = q.CreatedAt,
                ProcessedAt       = q.ProcessedAt,
                NextAttemptAt     = q.NextAttemptAt
            })
            .ToList();

        return new EmailJobDetailsViewModel
        {
            JobId         = job.Id,
            CorrelationId = job.CorrelationId,
            Status        = job.Status,
            TotalItems    = job.TotalItems,
            SentCount     = job.SentCount,
            FailedCount   = job.FailedCount,
            RejectedCount = job.RejectedCount,
            RequestedAt   = job.RequestedAt,
            StartedAt     = job.StartedAt,
            CompletedAt   = job.CompletedAt,
            CreatedByName = createdByName,
            SchoolName    = schoolName,
            Items         = items
        };
    }

    public async Task<EmailJobSummaryViewModel> GetSummaryAsync(
        Guid? schoolId,
        CancellationToken ct = default)
    {
        var todayUtc = DateTime.UtcNow.Date;

        var jobsQuery = _db.EmailJobs.AsNoTracking().AsQueryable();
        if (schoolId.HasValue)
            jobsQuery = jobsQuery.Where(j => j.SchoolId == schoolId.Value);

        var jobsToday = await jobsQuery
            .CountAsync(j => j.RequestedAt >= todayUtc, ct);

        IQueryable<EmailQueue> queueQuery = _db.EmailQueues.AsNoTracking();

        if (schoolId.HasValue)
        {
            var schoolJobIds = await _db.EmailJobs
                .AsNoTracking()
                .Where(j => j.SchoolId == schoolId.Value)
                .Select(j => j.Id)
                .ToListAsync(ct);

            queueQuery = queueQuery.Where(q => q.JobId != null && schoolJobIds.Contains(q.JobId!.Value));
        }
        else
        {
            queueQuery = queueQuery.Where(q => q.JobId != null);
        }

        var sentToday = await queueQuery
            .CountAsync(q => q.Status == EmailQueueStatus.Sent && q.ProcessedAt >= todayUtc, ct);

        var failedToday = await queueQuery
            .CountAsync(q => q.Status == EmailQueueStatus.Failed && q.ProcessedAt >= todayUtc, ct);

        var pendingNow = await queueQuery
            .CountAsync(q => q.Status == EmailQueueStatus.Pending || q.Status == EmailQueueStatus.Processing, ct);

        var deadLetterNow = await queueQuery
            .CountAsync(q => q.Status == EmailQueueStatus.DeadLetter, ct);

        return new EmailJobSummaryViewModel
        {
            JobsToday     = jobsToday,
            SentToday     = sentToday,
            FailedToday   = failedToday,
            PendingNow    = pendingNow,
            DeadLetterNow = deadLetterNow
        };
    }

    // ── Reintentos administrativos ────────────────────────────────────────────

    public async Task<RetryResultDto> RetryJobFailedAsync(
        Guid jobId,
        Guid? schoolId,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        var job = await _db.EmailJobs.FindAsync([jobId], ct);
        if (job == null)
            return new RetryResultDto { Success = false, Message = "Job no encontrado." };

        // Validación de tenant
        if (schoolId.HasValue && job.SchoolId != schoolId.Value)
            return new RetryResultDto { Success = false, Message = "Acceso denegado al job indicado." };

        // Cargar ítems elegibles
        var retryableStatuses = new[] { EmailQueueStatus.Failed, EmailQueueStatus.DeadLetter };
        var items = await _db.EmailQueues
            .Where(q => q.JobId == jobId && retryableStatuses.Contains(q.Status))
            .ToListAsync(ct);

        if (items.Count == 0)
            return new RetryResultDto
            {
                Success      = true,
                Message      = "No hay ítems fallidos para reintentar.",
                RetriedCount = 0
            };

        var now = DateTime.UtcNow;

        foreach (var item in items)
            ResetItemForRetry(item, now);

        // Reactivar el job si estaba completado
        if (job.Status is EmailJobStatus.Completed or
                          EmailJobStatus.CompletedWithErrors or
                          EmailJobStatus.Failed)
        {
            job.Status      = EmailJobStatus.Processing;
            job.CompletedAt = null;
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "AdminRetryJob JobId={JobId} CorrelationId={CorrelationId} " +
            "RetriedCount={Count} ActorUserId={ActorUserId}",
            jobId, job.CorrelationId, items.Count, actorUserId);

        return new RetryResultDto
        {
            Success      = true,
            Message      = $"{items.Count} ítem(s) programados para reintento.",
            RetriedCount = items.Count
        };
    }

    public async Task<RetryResultDto> RetryItemAsync(
        Guid queueItemId,
        Guid jobId,
        Guid? schoolId,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        var item = await _db.EmailQueues.FindAsync([queueItemId], ct);
        if (item == null)
            return new RetryResultDto { Success = false, Message = "Ítem no encontrado." };

        // Validar ownership: el ítem debe pertenecer al job indicado
        if (item.JobId != jobId)
            return new RetryResultDto { Success = false, Message = "El ítem no pertenece al job indicado." };

        // Cargar job para validación de tenant y logging
        var job = await _db.EmailJobs.FindAsync([jobId], ct);
        if (job == null)
            return new RetryResultDto { Success = false, Message = "Job no encontrado." };

        if (schoolId.HasValue && job.SchoolId != schoolId.Value)
            return new RetryResultDto { Success = false, Message = "Acceso denegado al job indicado." };

        // Verificar que el ítem sea elegible
        if (item.Status is not (EmailQueueStatus.Failed or EmailQueueStatus.DeadLetter))
        {
            return new RetryResultDto
            {
                Success      = true,
                Message      = $"El ítem está en estado '{item.Status}' y no requiere reintento.",
                SkippedCount = 1
            };
        }

        var now = DateTime.UtcNow;
        ResetItemForRetry(item, now);

        // Reactivar el job si estaba completado
        if (job.Status is EmailJobStatus.Completed or
                          EmailJobStatus.CompletedWithErrors or
                          EmailJobStatus.Failed)
        {
            job.Status      = EmailJobStatus.Processing;
            job.CompletedAt = null;
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "AdminRetryItem QueueItemId={ItemId} JobId={JobId} CorrelationId={CorrelationId} " +
            "Email={Email} ActorUserId={ActorUserId}",
            queueItemId, jobId, job.CorrelationId, item.Email, actorUserId);

        return new RetryResultDto
        {
            Success      = true,
            Message      = "Ítem programado para reintento.",
            RetriedCount = 1
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Resetea los campos operativos de un ítem para reintento administrativo.
    /// Política documentada:
    /// - Attempts = 0: obligatorio. Si no se resetea, DeadLetter (Attempts >= MaxAttempts)
    ///   volvería a ser dead-letterado inmediatamente en el siguiente ciclo del worker.
    /// - MaxAttempts: se preserva (mantiene el límite configurado).
    /// - NextAttemptAt = null: procesado inmediatamente sin backoff adicional.
    /// - Status = Pending: entra en la cola normal del worker.
    /// - Todos los campos de lock/lease se limpian.
    /// - LastError / ErrorCode se limpian para indicar estado fresco.
    /// - ProcessedAt = null: aún no procesado.
    /// </summary>
    private static void ResetItemForRetry(EmailQueue item, DateTime now)
    {
        item.Status        = EmailQueueStatus.Pending;
        item.Attempts      = 0;
        item.LastError     = null;
        item.ErrorCode     = null;
        item.LockedAt      = null;
        item.LockedUntil   = null;
        item.LockedBy      = null;
        item.NextAttemptAt = null;
        item.ProcessedAt   = null;
    }
}
