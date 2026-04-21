namespace SchoolManager.Services.Interfaces;

/// <summary>
/// Servicio corporativo para conversión de fechas UTC a zona de presentación.
/// Todas las fechas con hora se almacenan en UTC; en UI se muestran en la zona configurada (ej. America/Panama).
/// </summary>
public interface ITimeZoneService
{
    /// <summary>
    /// Convierte un DateTime UTC a la zona de presentación del sistema.
    /// Si el valor es null, devuelve null. Si Kind no es UTC, se asume UTC.
    /// </summary>
    DateTime? ToLocal(DateTime? utc);

    /// <summary>
    /// Convierte un DateTime UTC a la zona de presentación del sistema.
    /// Si Kind no es UTC, se asume UTC.
    /// </summary>
    DateTime ToLocal(DateTime utc);

    /// <summary>
    /// Formatea un DateTime UTC en la zona de presentación con el formato indicado.
    /// Devuelve string vacío o "N/A" si el valor es null según el formato de negocio.
    /// </summary>
    /// <param name="utc">Fecha en UTC (nullable).</param>
    /// <param name="format">Formato .NET (ej. "dd/MM/yyyy HH:mm").</param>
    /// <param name="nullDisplay">Texto cuando utc es null (ej. "N/A", "Nunca").</param>
    string ToLocalDisplayString(DateTime? utc, string format, string? nullDisplay = "N/A");

    /// <summary>
    /// Obtiene "ahora" en la zona de presentación, formateado para el atributo value de input datetime-local (yyyy-MM-ddTHH:mm).
    /// No usar UTC para valores por defecto en datetime-local.
    /// </summary>
    string GetNowForDisplayInput();

    /// <summary>
    /// Obtiene la fecha/hora actual en la zona de presentación (para "Hoy es", "Generado el", etc.).
    /// Formato largo con día de la semana y mes en cultura del sistema.
    /// </summary>
    string GetTodayLongString();

    /// <summary>
    /// Obtiene la fecha actual en zona de presentación en formato yyyy-MM-dd (para value de input type="date").
    /// </summary>
    string GetTodayForDateInput();
}
