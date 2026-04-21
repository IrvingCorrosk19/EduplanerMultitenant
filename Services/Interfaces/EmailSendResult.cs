using System.Net;

namespace SchoolManager.Services.Interfaces;

/// <summary>Códigos de error estables para clasificar fallos de envío.</summary>
public static class EmailSendErrorCode
{
    public const string ResendUnauthorized  = "RESEND_UNAUTHORIZED";
    public const string ResendRateLimit     = "RESEND_RATE_LIMIT";
    public const string ResendBadRequest    = "RESEND_BAD_REQUEST";
    public const string ResendServerError   = "RESEND_SERVER_ERROR";
    public const string NetworkError        = "NETWORK_ERROR";
    public const string InvalidDestination  = "INVALID_DESTINATION";
    public const string InvalidFrom         = "INVALID_FROM";
    public const string ConfigNotFound      = "CONFIG_NOT_FOUND";
    public const string ProviderNotSupported = "PROVIDER_NOT_SUPPORTED";
    public const string ProviderUnknown     = "PROVIDER_UNKNOWN";
}

/// <summary>Resultado rico devuelto por IEmailSender.SendAsync.</summary>
public sealed class EmailSendResult
{
    public bool Success { get; init; }

    /// <summary>Código estable de error, null si Success=true.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Mensaje humano del error.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>ID de mensaje devuelto por el proveedor (ej. Resend).</summary>
    public string? ProviderMessageId { get; init; }

    /// <summary>True si el error es transitorio y el ítem debe reintentarse.</summary>
    public bool IsRetryable { get; init; }

    /// <summary>HTTP status code de la respuesta del proveedor, si aplica.</summary>
    public int? HttpStatusCode { get; init; }

    public static EmailSendResult Ok(string? providerMessageId = null) =>
        new() { Success = true, ProviderMessageId = providerMessageId };

    public static EmailSendResult Fail(
        string errorCode,
        string? errorMessage,
        bool isRetryable,
        int? httpStatusCode = null) =>
        new()
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            IsRetryable = isRetryable,
            HttpStatusCode = httpStatusCode
        };
}
