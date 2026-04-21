using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManager.Constants;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations;

public static class PasswordEmailStatusValues
{
    public const string Pending = "Pending";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
}

public class BulkPasswordEmailService : IBulkPasswordEmailService
{
    private const string Subject = "Acceso a la plataforma";
    private readonly SchoolDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IEmailApiConfigurationService _emailApiConfigurationService;
    private readonly ILogger<BulkPasswordEmailService> _logger;

    public BulkPasswordEmailService(
        SchoolDbContext db,
        IEmailService emailService,
        IEmailApiConfigurationService emailApiConfigurationService,
        ILogger<BulkPasswordEmailService> logger)
    {
        _db = db;
        _emailService = emailService;
        _emailApiConfigurationService = emailApiConfigurationService;
        _logger = logger;
    }

    public async Task<BulkPasswordEmailResult> SendPasswordsAsync(
        IReadOnlyList<Guid> userIds,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken = default)
    {
        var list = new List<BulkPasswordEmailItemResult>();
        if (userIds == null || userIds.Count == 0)
            return new BulkPasswordEmailResult { Items = list };

        var callerIdClaim = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(callerIdClaim, out var callerUserId))
        {
            _logger.LogWarning("SendPasswordsAsync: sin NameIdentifier en ClaimsPrincipal.");
            return new BulkPasswordEmailResult { Items = list };
        }

        var caller = await _db.Users.IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == callerUserId, cancellationToken);

        if (caller == null)
            return new BulkPasswordEmailResult { Items = list };

        var role = (caller.Role ?? string.Empty).ToLowerInvariant();
        // Director u otros: el controlador ya restringe; defensa en profundidad
        if (role != "superadmin" && role != "admin")
        {
            _logger.LogWarning("SendPasswordsAsync: rol no autorizado {Role}", caller.Role);
            foreach (var id in userIds.Distinct())
                list.Add(new BulkPasswordEmailItemResult { UserId = id, Success = false, Message = "No autorizado." });
            return new BulkPasswordEmailResult { Items = list };
        }

        var isSuperAdmin = role == "superadmin";
        var callerSchoolId = caller.SchoolId;

        var apiCfg = await _emailApiConfigurationService.GetActiveAsync(cancellationToken);
        if (apiCfg == null || string.IsNullOrWhiteSpace(apiCfg.ApiKey))
        {
            foreach (var id in userIds.Distinct())
                list.Add(new BulkPasswordEmailItemResult
                {
                    UserId = id,
                    Success = false,
                    Message = "Configure EmailApiConfiguration activa con API key."
                });
            return new BulkPasswordEmailResult { Items = list };
        }

        var fromName = string.IsNullOrWhiteSpace(apiCfg.FromName) ? "SchoolManager" : apiCfg.FromName.Trim();
        var distinctIds = userIds.Distinct().ToList();

        foreach (var userId in distinctIds)
        {
            var item = new BulkPasswordEmailItemResult { UserId = userId };
            list.Add(item);

            var user = await _db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                item.Success = false;
                item.Message = "Usuario no encontrado.";
                continue;
            }

            if (!isSuperAdmin)
            {
                if (!callerSchoolId.HasValue || user.SchoolId != callerSchoolId.Value)
                {
                    item.Success = false;
                    item.Message = "No autorizado (otra escuela).";
                    continue;
                }
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                user.PasswordEmailStatus = PasswordEmailStatusValues.Failed;
                user.PasswordEmailSentAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                item.Success = false;
                item.Message = "Sin correo electrónico.";
                continue;
            }

            var oldHash = user.PasswordHash;
            var plainPassword = DefaultTemporaryPassword.Value;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
            user.PasswordEmailStatus = PasswordEmailStatusValues.Pending;
            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando nuevo hash usuario {UserId}", userId);
                item.Success = false;
                item.Message = "Error al guardar contraseña.";
                continue;
            }

            var html = BuildEmailHtml(user.Email, plainPassword, fromName);
            var (ok, err) = await _emailService.SendEmailAsync(user.Email, Subject, html, cancellationToken);
            var now = DateTime.UtcNow;

            if (ok)
            {
                user.PasswordEmailStatus = PasswordEmailStatusValues.Sent;
                user.PasswordEmailSentAt = now;
                user.UpdatedAt = now;
                item.Success = true;
                _logger.LogInformation("Contraseña temporal enviada por correo UserId={UserId}", userId);
            }
            else
            {
                // Revertir hash: el usuario no recibió la nueva contraseña
                user.PasswordHash = oldHash;
                user.PasswordEmailStatus = PasswordEmailStatusValues.Failed;
                user.PasswordEmailSentAt = now;
                user.UpdatedAt = now;
                item.Success = false;
                item.Message = err;
                _logger.LogWarning("Fallo envío correo UserId={UserId}: {Err}", userId, err);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        return new BulkPasswordEmailResult { Items = list };
    }

    private static string BuildEmailHtml(string email, string tempPassword, string fromName)
    {
        var e = System.Net.WebUtility.HtmlEncode(email);
        var p = System.Net.WebUtility.HtmlEncode(tempPassword);
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
