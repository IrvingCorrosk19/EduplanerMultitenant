using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Constants;
using SchoolManager.Dtos;
using SchoolManager.Models;
using SchoolManager.Repositories.Interfaces;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations;

public class EmailQueueService : IEmailQueueService
{
    private const string Subject = "Acceso a la plataforma";

    private readonly SchoolDbContext _db;
    private readonly IEmailQueueRepository _repository;
    private readonly IEmailApiConfigurationService _emailApiConfig;
    private readonly ILogger<EmailQueueService> _logger;

    public EmailQueueService(
        SchoolDbContext db,
        IEmailQueueRepository repository,
        IEmailApiConfigurationService emailApiConfig,
        ILogger<EmailQueueService> logger)
    {
        _db = db;
        _repository = repository;
        _emailApiConfig = emailApiConfig;
        _logger = logger;
    }

    public async Task<EnqueueResult> EnqueueUsersAsync(List<Guid> userIds, ClaimsPrincipal currentUser)
    {
        var correlationId = Guid.NewGuid();
        var requested = userIds?.Count ?? 0;

        _logger.LogInformation(
            "EnqueueUsers start CorrelationId={CorrelationId} Requested={Requested}",
            correlationId, requested);

        if (userIds == null || userIds.Count == 0)
            return EnqueueResult.NoneEligible(correlationId, 0, 0, ["Lista de usuarios vacía."]);

        // ── Validar caller ────────────────────────────────────────────────────
        var callerIdClaim = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(callerIdClaim, out var callerUserId))
        {
            _logger.LogWarning(
                "EnqueueUsers CorrelationId={CorrelationId} sin NameIdentifier en ClaimsPrincipal",
                correlationId);
            return EnqueueResult.Unauthorized(correlationId, requested);
        }

        var caller = await _db.Users.IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == callerUserId);

        if (caller == null)
        {
            _logger.LogWarning(
                "EnqueueUsers CorrelationId={CorrelationId} caller no encontrado UserId={UserId}",
                correlationId, callerUserId);
            return EnqueueResult.Unauthorized(correlationId, requested);
        }

        var role = (caller.Role ?? string.Empty).ToLowerInvariant();
        if (role != "superadmin" && role != "admin")
        {
            _logger.LogWarning(
                "EnqueueUsers CorrelationId={CorrelationId} rol no autorizado Role={Role}",
                correlationId, caller.Role);
            return EnqueueResult.Unauthorized(correlationId, requested);
        }

        // ── Verificar configuración activa de email ───────────────────────────
        var apiCfg = await _emailApiConfig.GetActiveAsync();
        if (apiCfg == null || string.IsNullOrWhiteSpace(apiCfg.ApiKey))
        {
            _logger.LogWarning(
                "EnqueueUsers CorrelationId={CorrelationId} sin EmailApiConfiguration activa",
                correlationId);
            return EnqueueResult.NoConfig(correlationId, requested);
        }

        var isSuperAdmin  = role == "superadmin";
        var callerSchoolId = caller.SchoolId;
        var fromName = string.IsNullOrWhiteSpace(apiCfg.FromName) ? "SchoolManager" : apiCfg.FromName.Trim();
        var distinctIds = userIds.Distinct().ToList();
        var now = DateTime.UtcNow;

        // ── Cargar usuarios ───────────────────────────────────────────────────
        var users = await _db.Users.IgnoreQueryFilters()
            .Where(u => distinctIds.Contains(u.Id))
            .ToListAsync();

        var queueItems = new List<EmailQueue>();
        var warnings   = new List<string>();
        var rejected   = 0;

        // ── Crear EmailJob para este lote ─────────────────────────────────────
        var job = new EmailJob
        {
            Id              = Guid.NewGuid(),
            CorrelationId   = correlationId,
            CreatedByUserId = callerUserId,
            SchoolId        = isSuperAdmin ? null : callerSchoolId,
            RequestedAt     = now,
            Status          = EmailJobStatus.Accepted,
            TotalItems      = 0, // se fija al final
            RejectedCount   = 0
        };
        _db.EmailJobs.Add(job);

        // ── Procesar cada usuario ─────────────────────────────────────────────
        foreach (var user in users)
        {
            // Scope: admin solo puede enviar a su propia escuela
            if (!isSuperAdmin && (callerSchoolId == null || user.SchoolId != callerSchoolId.Value))
            {
                rejected++;
                warnings.Add($"Usuario {user.Id} omitido: escuela no coincide.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                rejected++;
                warnings.Add($"Usuario {user.Id} omitido: sin email.");
                user.PasswordEmailStatus = PasswordEmailStatusValues.Failed;
                user.PasswordEmailSentAt = now;
                user.UpdatedAt = now;
                continue;
            }

            var plainPassword = DefaultTemporaryPassword.Value;
            user.PasswordHash        = BCrypt.Net.BCrypt.HashPassword(plainPassword);
            user.PasswordEmailStatus = PasswordEmailStatusValues.Pending;
            user.UpdatedAt           = now;

            var body = BuildEmailHtml(user.Email, plainPassword, fromName);
            queueItems.Add(new EmailQueue
            {
                Id            = Guid.NewGuid(),
                JobId         = job.Id,
                CorrelationId = correlationId,
                UserId        = user.Id,
                Email         = user.Email,
                Subject       = Subject,
                Body          = body,
                Status        = EmailQueueStatus.Pending,
                Attempts      = 0,
                MaxAttempts   = 3,
                CreatedAt     = now
            });
        }

        // ── Verificar que haya algo que encolar ───────────────────────────────
        if (queueItems.Count == 0)
        {
            // No persistir un job vacío
            _db.EmailJobs.Remove(job);
            await _db.SaveChangesAsync();

            _logger.LogWarning(
                "EnqueueUsers CorrelationId={CorrelationId} 0 ítems elegibles Rejected={Rejected}",
                correlationId, rejected);

            return EnqueueResult.NoneEligible(correlationId, requested, rejected, warnings);
        }

        // ── Fijar contadores y persistir ──────────────────────────────────────
        job.TotalItems    = queueItems.Count;
        job.RejectedCount = rejected;

        await _db.SaveChangesAsync(); // usuarios + job

        _repository.AddRange(queueItems);
        await _repository.SaveChangesAsync(); // email_queues

        _logger.LogInformation(
            "EnqueueUsers OK CorrelationId={CorrelationId} JobId={JobId} Accepted={Accepted} Rejected={Rejected}",
            correlationId, job.Id, queueItems.Count, rejected);

        return EnqueueResult.Accepted(
            job.Id,
            correlationId,
            requested,
            queueItems.Count,
            rejected,
            warnings);
    }

    private static string BuildEmailHtml(string email, string tempPassword, string fromName)
    {
        var e  = System.Net.WebUtility.HtmlEncode(email);
        var p  = System.Net.WebUtility.HtmlEncode(tempPassword);
        var fn = System.Net.WebUtility.HtmlEncode(fromName);
        const string platformUrl = "https://eduplaner.net/";
        return $@"
<div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; max-width: 520px; margin: 0 auto; color: #333; line-height: 1.5;"">
  <div style=""background: linear-gradient(135deg, #2563eb 0%, #1e40af 100%); color: #fff; padding: 24px 28px; border-radius: 12px 12px 0 0; text-align: center;"">
    <h1 style=""margin: 0; font-size: 22px; font-weight: 600;"">Acceso a la plataforma</h1>
    <p style=""margin: 8px 0 0; font-size: 14px; opacity: 0.95;"">Sistema de Gestión Educativa</p>
  </div>
  <div style=""padding: 28px; background: #fff; border: 1px solid #e2e8f0; border-top: none; border-radius: 0 0 12px 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.05);"">
    <p style=""margin: 0 0 20px; font-size: 16px;"">Hola,</p>
    <p style=""margin: 0 0 20px; font-size: 15px; color: #475569;"">Se ha actualizado tu acceso. Utiliza los siguientes datos para iniciar sesión:</p>
    <div style=""background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 20px; margin: 24px 0;"">
      <table style=""width: 100%; border-collapse: collapse;"">
        <tr><td style=""padding: 8px 0; font-size: 13px; color: #64748b;"">Usuario</td></tr>
        <tr><td style=""padding: 0 0 12px; font-size: 16px; font-weight: 600; color: #1e293b;"">{e}</td></tr>
        <tr><td style=""padding: 8px 0; font-size: 13px; color: #64748b;"">Contraseña temporal</td></tr>
        <tr><td style=""padding: 0; font-size: 16px; font-weight: 600; color: #1e293b; letter-spacing: 1px;"">{p}</td></tr>
      </table>
    </div>
    <p style=""margin: 0 0 20px; font-size: 15px; color: #475569;"">Accede a la plataforma en el siguiente enlace:</p>
    <p style=""margin: 0 0 24px;"">
      <a href=""{platformUrl}"" style=""display: inline-block; background: #2563eb; color: #fff !important; text-decoration: none; padding: 12px 24px; border-radius: 8px; font-weight: 600; font-size: 15px;"">Ir a Eduplaner</a>
    </p>
    <p style=""margin: 0; font-size: 14px; color: #64748b; border-top: 1px solid #e2e8f0; padding-top: 20px;"">Por seguridad, te recomendamos cambiar tu contraseña después del primer acceso.</p>
  </div>
  <p style=""margin: 20px 0 0; font-size: 13px; color: #94a3b8; text-align: center;"">Saludos,<br/><strong style=""color: #64748b;"">{fn}</strong></p>
</div>";
    }
}
