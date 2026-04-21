using System;
using System.Collections.Generic;

namespace SchoolManager.Dtos;

/// <summary>
/// Resultado estructurado de IEmailQueueService.EnqueueUsersAsync.
/// Permite que el controller devuelva una respuesta honesta al frontend.
/// </summary>
public sealed class EnqueueResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    /// <summary>Guid del EmailJob creado para este lote. Null si no se creó ninguno.</summary>
    public Guid? JobId { get; init; }

    /// <summary>CorrelationId propagado a logs y respuesta.</summary>
    public Guid CorrelationId { get; init; }

    public int TotalRequested { get; init; }
    public int AcceptedCount { get; init; }
    public int RejectedCount { get; init; }

    /// <summary>Razones de rechazo o advertencias no críticas.</summary>
    public List<string> Warnings { get; init; } = new();

    // ── Constructores de conveniencia ─────────────────────────────────────────

    public static EnqueueResult NoConfig(Guid correlationId, int requested) => new()
    {
        Success = false,
        Message = "No hay configuración de correo activa. Contacte al administrador.",
        CorrelationId = correlationId,
        TotalRequested = requested,
        Warnings = ["Sin configuración API activa (CONFIG_NOT_FOUND)"]
    };

    public static EnqueueResult Unauthorized(Guid correlationId, int requested) => new()
    {
        Success = false,
        Message = "No está autorizado para enviar correos masivos.",
        CorrelationId = correlationId,
        TotalRequested = requested
    };

    public static EnqueueResult NoneEligible(Guid correlationId, int requested, int rejected, List<string> warnings) => new()
    {
        Success = false,
        Message = "Ningún destinatario elegible encontrado. Revise que los usuarios tengan email válido y pertenezcan a su escuela.",
        CorrelationId = correlationId,
        TotalRequested = requested,
        AcceptedCount = 0,
        RejectedCount = rejected,
        Warnings = warnings
    };

    public static EnqueueResult Accepted(Guid jobId, Guid correlationId, int requested, int accepted, int rejected, List<string> warnings) => new()
    {
        Success = true,
        Message = accepted == requested
            ? $"{accepted} correo(s) encolados y en proceso."
            : $"{accepted} correo(s) encolados. {rejected} usuario(s) omitidos.",
        JobId = jobId,
        CorrelationId = correlationId,
        TotalRequested = requested,
        AcceptedCount = accepted,
        RejectedCount = rejected,
        Warnings = warnings
    };
}
