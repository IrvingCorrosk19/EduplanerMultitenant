using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations;

public class ResendEmailSender : IEmailSender
{
    private const string ResendEmailsUrl = "https://api.resend.com/emails";

    private readonly IEmailApiConfigurationService _emailApiConfig;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        IEmailApiConfigurationService emailApiConfig,
        IHttpClientFactory httpClientFactory,
        ILogger<ResendEmailSender> logger)
    {
        _emailApiConfig = emailApiConfig;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        // ── Validaciones previas (no reintentables) ───────────────────────────
        if (string.IsNullOrWhiteSpace(to))
            return EmailSendResult.Fail(
                EmailSendErrorCode.InvalidDestination,
                "Correo destino vacío.",
                isRetryable: false);

        var cfg = await _emailApiConfig.GetActiveAsync(cancellationToken);
        if (cfg == null)
            return EmailSendResult.Fail(
                EmailSendErrorCode.ConfigNotFound,
                "No hay configuración API de correo activa.",
                isRetryable: false);

        if (string.IsNullOrWhiteSpace(cfg.ApiKey))
            return EmailSendResult.Fail(
                EmailSendErrorCode.ConfigNotFound,
                "API key no configurada.",
                isRetryable: false);

        if (string.IsNullOrWhiteSpace(cfg.FromEmail))
            return EmailSendResult.Fail(
                EmailSendErrorCode.InvalidFrom,
                "FromEmail no configurado.",
                isRetryable: false);

        var provider = (cfg.Provider ?? "").Trim();
        if (!provider.Equals("Resend", StringComparison.OrdinalIgnoreCase))
            return EmailSendResult.Fail(
                EmailSendErrorCode.ProviderNotSupported,
                $"Proveedor no soportado: {provider}.",
                isRetryable: false);

        // ── Llamada HTTP a Resend ─────────────────────────────────────────────
        var fromDisplay = $"{cfg.FromName} <{cfg.FromEmail.Trim()}>";
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(45);

        using var request = new HttpRequestMessage(HttpMethod.Post, ResendEmailsUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ApiKey.Trim());

        var payload = new { from = fromDisplay, to = new[] { to.Trim() }, subject, html = body };
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            var statusCode = (int)response.StatusCode;
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var providerMsgId = ExtractResendId(responseBody);
                _logger.LogDebug(
                    "Resend OK To={To} ProviderMsgId={ProviderMsgId}",
                    to, providerMsgId);
                return EmailSendResult.Ok(providerMsgId);
            }

            // ── Mapear error HTTP a código estable ────────────────────────────
            var (errorCode, isRetryable) = MapHttpError(statusCode);
            var truncatedBody = responseBody.Length > 500
                ? responseBody[..500]
                : responseBody;

            _logger.LogWarning(
                "Resend error To={To} HTTP={StatusCode} Code={ErrorCode} Retryable={Retryable} Body={Body}",
                to, statusCode, errorCode, isRetryable, truncatedBody);

            return EmailSendResult.Fail(errorCode, truncatedBody, isRetryable, statusCode);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout del HttpClient (no del CancellationToken externo)
            _logger.LogError(ex, "Resend timeout Email={Email}", to);
            return EmailSendResult.Fail(
                EmailSendErrorCode.NetworkError,
                "Timeout al conectar con Resend.",
                isRetryable: true);
        }
        catch (OperationCanceledException)
        {
            throw; // propagar para que el worker la maneje
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Resend red/timeout Email={Email}", to);
            return EmailSendResult.Fail(
                EmailSendErrorCode.NetworkError,
                ex.Message,
                isRetryable: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend SendAsync error inesperado Email={Email}", to);
            return EmailSendResult.Fail(
                EmailSendErrorCode.ProviderUnknown,
                ex.Message,
                isRetryable: false);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (string errorCode, bool isRetryable) MapHttpError(int statusCode) =>
        statusCode switch
        {
            401 or 403 => (EmailSendErrorCode.ResendUnauthorized, false),
            400        => (EmailSendErrorCode.ResendBadRequest,   false),
            422        => (EmailSendErrorCode.ResendBadRequest,   false), // from inválido, destinatario inválido
            429        => (EmailSendErrorCode.ResendRateLimit,    true),
            >= 500     => (EmailSendErrorCode.ResendServerError,  true),
            >= 400     => (EmailSendErrorCode.ProviderUnknown,    false),
            _          => (EmailSendErrorCode.ProviderUnknown,    false)
        };

    private static string? ExtractResendId(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return null;
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("id", out var idEl))
                return idEl.GetString();
        }
        catch { /* JSON malformado — no crítico */ }
        return null;
    }
}
