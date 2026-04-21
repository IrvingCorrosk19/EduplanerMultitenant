using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations;

/// <summary>
/// Implementación del estándar corporativo de presentación de fechas.
/// Lee la zona horaria de configuración (default America/Panama); convierte UTC → local para UI.
/// </summary>
public class TimeZoneService : ITimeZoneService
{
    private readonly TimeZoneInfo _displayTimeZone;
    private readonly ILogger<TimeZoneService> _logger;

    public TimeZoneService(IConfiguration configuration, ILogger<TimeZoneService> logger)
    {
        _logger = logger;
        var timeZoneId = configuration["DateTime:DisplayTimeZoneId"] ?? "America/Panama";
        try
        {
            _displayTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogWarning("Zona horaria {TimeZoneId} no encontrada; usando UTC.", timeZoneId);
            _displayTimeZone = TimeZoneInfo.Utc;
        }
    }

    /// <inheritdoc />
    public DateTime? ToLocal(DateTime? utc)
    {
        if (!utc.HasValue) return null;
        return ToLocal(utc.Value);
    }

    /// <inheritdoc />
    public DateTime ToLocal(DateTime utc)
    {
        var normalized = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(normalized, _displayTimeZone);
    }

    /// <inheritdoc />
    public string ToLocalDisplayString(DateTime? utc, string format, string? nullDisplay = "N/A")
    {
        if (!utc.HasValue) return nullDisplay ?? "N/A";
        var local = ToLocal(utc.Value);
        return local.ToString(format, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc />
    public string GetNowForDisplayInput()
    {
        var nowUtc = DateTime.UtcNow;
        var local = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, _displayTimeZone);
        return local.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public string GetTodayLongString()
    {
        var nowUtc = DateTime.UtcNow;
        var local = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, _displayTimeZone);
        return local.ToString("dddd, dd 'de' MMMM 'de' yyyy", CultureInfo.CurrentCulture);
    }

    /// <inheritdoc />
    public string GetTodayForDateInput()
    {
        var nowUtc = DateTime.UtcNow;
        var local = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, _displayTimeZone);
        return local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
