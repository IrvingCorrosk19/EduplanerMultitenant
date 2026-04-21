using System.Security.Claims;

namespace SchoolManager.Services.Interfaces;

public class BulkPasswordEmailItemResult
{
    public Guid UserId { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class BulkPasswordEmailResult
{
    public IReadOnlyList<BulkPasswordEmailItemResult> Items { get; init; } = Array.Empty<BulkPasswordEmailItemResult>();
}

public interface IBulkPasswordEmailService
{
    /// <summary>
    /// Contraseña temporal (hash), envío por Resend vía <see cref="IEmailService.SendEmailAsync"/>, estado en usuario.
    /// Si el correo falla, revierte el hash para no dejar al usuario bloqueado.
    /// </summary>
    Task<BulkPasswordEmailResult> SendPasswordsAsync(
        IReadOnlyList<Guid> userIds,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken = default);
}
