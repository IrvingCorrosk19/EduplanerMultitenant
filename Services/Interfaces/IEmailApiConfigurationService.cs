using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces;

/// <summary>Lee la fila activa de <see cref="EmailApiConfiguration"/> para envío vía API (Resend).</summary>
public interface IEmailApiConfigurationService
{
    Task<EmailApiConfiguration?> GetActiveAsync(CancellationToken cancellationToken = default);
}
