namespace SchoolManager.Dtos;

/// <summary>Resultado de una operación de reintento de correos fallidos.</summary>
public sealed class RetryResultDto
{
    public bool   Success      { get; init; }
    public string Message      { get; init; } = string.Empty;
    /// <summary>Número de ítems que pasaron a Pending para reintento.</summary>
    public int    RetriedCount { get; init; }
    /// <summary>Ítems omitidos porque no estaban en estado reintentable.</summary>
    public int    SkippedCount { get; init; }
}
