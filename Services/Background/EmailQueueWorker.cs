using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolManager.Models;
using SchoolManager.Repositories.Interfaces;
using SchoolManager.Services.Implementations;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Background;

/// <summary>
/// Worker de envío masivo de correos.
/// Usa claim/lease para procesar de forma segura incluso con reinicios del proceso.
/// Ejecuta recovery automático de leases expiradas en cada ciclo.
/// </summary>
public class EmailQueueWorker : BackgroundService
{
    // ── Configuración ─────────────────────────────────────────────────────────
    private const int  BatchSize        = 50;
    private const int  IntervalSeconds  = 10;
    private static readonly TimeSpan LeaseDuration         = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MinDelayBetweenSends  = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MaxDelayBetweenSends  = TimeSpan.FromMilliseconds(300);

    // Backoff exponencial por intento (índice = Attempts tras el fallo)
    private static readonly TimeSpan[] RetryBackoff =
    [
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(30)
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailQueueWorker> _logger;

    // Identificador único de esta instancia del worker (útil para multi-instancia)
    private readonly string _workerId = $"worker-{Environment.MachineName}-{Guid.NewGuid().ToString("N")[..8]}";

    public EmailQueueWorker(IServiceScopeFactory scopeFactory, ILogger<EmailQueueWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailQueueWorker started WorkerId={WorkerId}", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailQueueWorker ciclo error WorkerId={WorkerId}", _workerId);
            }

            await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("EmailQueueWorker stopped WorkerId={WorkerId}", _workerId);
    }

    // ── Ciclo principal ───────────────────────────────────────────────────────

    private async Task RunCycleAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo   = scope.ServiceProvider.GetRequiredService<IEmailQueueRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var db     = scope.ServiceProvider.GetRequiredService<SchoolDbContext>();

        // 1. Recovery de leases expiradas (siempre antes de reclamar nuevas)
        await RecoverAsync(repo, ct);

        // 2. Reclamar lote con lease
        var batch = await repo.ClaimBatchAsync(BatchSize, _workerId, LeaseDuration, ct);
        if (batch.Count == 0) return;

        _logger.LogInformation(
            "EmailQueueWorker batch claimed WorkerId={WorkerId} Count={Count}",
            _workerId, batch.Count);

        // Conjunto de jobs afectados en este ciclo (para actualizar agregados)
        var affectedJobIds = new HashSet<Guid>();

        // 3. Procesar ítem a ítem
        foreach (var item in batch)
        {
            if (ct.IsCancellationRequested) break;

            await ProcessItemAsync(item, sender, db, repo, affectedJobIds, ct);

            // Throttle entre envíos
            var delayMs = Random.Shared.Next(
                (int)MinDelayBetweenSends.TotalMilliseconds,
                (int)MaxDelayBetweenSends.TotalMilliseconds + 1);
            await Task.Delay(delayMs, ct);
        }

        // 4. Actualizar agregados de los jobs afectados
        foreach (var jobId in affectedJobIds)
        {
            try
            {
                await repo.RefreshJobCountersAsync(jobId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "EmailQueueWorker error al refrescar job JobId={JobId}",
                    jobId);
            }
        }
    }

    // ── Procesar un ítem individual ───────────────────────────────────────────

    private async Task ProcessItemAsync(
        EmailQueue item,
        IEmailSender sender,
        SchoolDbContext db,
        IEmailQueueRepository repo,
        HashSet<Guid> affectedJobIds,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        if (item.JobId.HasValue)
            affectedJobIds.Add(item.JobId.Value);

        _logger.LogDebug(
            "EmailQueueWorker sending QueueItemId={Id} CorrelationId={CorrelationId} Email={Email} " +
            "Attempt={Attempt} WorkerId={WorkerId}",
            item.Id, item.CorrelationId, item.Email, item.Attempts + 1, _workerId);

        EmailSendResult result;
        try
        {
            result = await sender.SendAsync(
                item.Email,
                item.Subject ?? "Acceso a la plataforma",
                item.Body ?? "",
                ct);
        }
        catch (OperationCanceledException)
        {
            // El worker está siendo detenido; limpiar lease para que otro ciclo lo recupere
            item.Status      = EmailQueueStatus.Pending;
            item.LockedAt    = null;
            item.LockedUntil = null;
            item.LockedBy    = null;
            repo.Update(item);
            await repo.SaveChangesAsync(ct);
            throw;
        }

        if (result.Success)
        {
            item.Status            = EmailQueueStatus.Sent;
            item.ProcessedAt       = now;
            item.LockedAt          = null;
            item.LockedUntil       = null;
            item.LockedBy          = null;
            item.LastError         = null;
            item.ErrorCode         = null;
            item.ProviderMessageId = result.ProviderMessageId;

            // Actualizar estado del usuario
            var user = await db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == item.UserId, ct);
            if (user != null)
            {
                user.PasswordEmailStatus = PasswordEmailStatusValues.Sent;
                user.PasswordEmailSentAt = now;
                user.UpdatedAt           = now;
            }

            _logger.LogInformation(
                "EmailQueueWorker sent QueueItemId={Id} CorrelationId={CorrelationId} " +
                "JobId={JobId} Email={Email} ProviderMsgId={ProviderMsgId} WorkerId={WorkerId}",
                item.Id, item.CorrelationId, item.JobId, item.Email,
                result.ProviderMessageId, _workerId);
        }
        else
        {
            item.Attempts++;
            item.ErrorCode = result.ErrorCode;
            item.LastError = (result.ErrorMessage?.Length > 2000)
                ? result.ErrorMessage[..2000]
                : result.ErrorMessage;
            item.LockedAt    = null;
            item.LockedUntil = null;
            item.LockedBy    = null;

            _logger.LogWarning(
                "EmailQueueWorker failed QueueItemId={Id} CorrelationId={CorrelationId} " +
                "JobId={JobId} Email={Email} Attempt={Attempt} ErrorCode={ErrorCode} " +
                "Retryable={Retryable} WorkerId={WorkerId}",
                item.Id, item.CorrelationId, item.JobId, item.Email,
                item.Attempts, result.ErrorCode, result.IsRetryable, _workerId);

            var canRetry = result.IsRetryable && item.Attempts < item.MaxAttempts;

            if (canRetry)
            {
                item.Status        = EmailQueueStatus.Pending;
                item.NextAttemptAt = now + GetBackoffDelay(item.Attempts);

                _logger.LogInformation(
                    "EmailQueueWorker retry_scheduled QueueItemId={Id} NextAttempt={Next}",
                    item.Id, item.NextAttemptAt);
            }
            else
            {
                // Error no reintentable o agotados los intentos
                var isFinal = item.Attempts >= item.MaxAttempts;
                item.Status      = isFinal ? EmailQueueStatus.DeadLetter : EmailQueueStatus.Failed;
                item.ProcessedAt = now;

                var user = await db.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == item.UserId, ct);
                if (user != null)
                {
                    user.PasswordEmailStatus = PasswordEmailStatusValues.Failed;
                    user.PasswordEmailSentAt = now;
                    user.UpdatedAt           = now;
                }

                _logger.LogError(
                    "EmailQueueWorker dead_lettered QueueItemId={Id} CorrelationId={CorrelationId} " +
                    "JobId={JobId} Email={Email} ErrorCode={ErrorCode} Attempts={Attempts} " +
                    "WorkerId={WorkerId}",
                    item.Id, item.CorrelationId, item.JobId, item.Email,
                    result.ErrorCode, item.Attempts, _workerId);
            }
        }

        repo.Update(item);
        await repo.SaveChangesAsync(ct);
    }

    // ── Recovery ──────────────────────────────────────────────────────────────

    private async Task RecoverAsync(IEmailQueueRepository repo, CancellationToken ct)
    {
        try
        {
            var recovered = await repo.RecoverExpiredLeasesAsync(ct);
            if (recovered > 0)
            {
                _logger.LogInformation(
                    "EmailQueueWorker worker_recovery Recovered={Count} WorkerId={WorkerId}",
                    recovered, _workerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "EmailQueueWorker error en recovery WorkerId={WorkerId}", _workerId);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TimeSpan GetBackoffDelay(int attempts)
    {
        var idx  = Math.Clamp(attempts - 1, 0, RetryBackoff.Length - 1);
        var base_ = RetryBackoff[idx];
        var jitterSeconds = (int)(base_.TotalSeconds * 0.2 * (Random.Shared.NextDouble() * 2 - 1));
        return base_ + TimeSpan.FromSeconds(jitterSeconds);
    }
}
