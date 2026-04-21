using System;

namespace SchoolManager.Services.Implementations
{
    /// <summary>
    /// Servicio global para manejar operaciones de DateTime
    /// </summary>
    public static class GlobalDateTimeService
    {
        /// <summary>
        /// Obtiene la fecha y hora actual en UTC
        /// </summary>
        public static DateTime GetCurrentUtcDateTime()
        {
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Convierte cualquier DateTime a UTC para PostgreSQL
        /// </summary>
        public static DateTime ToPostgreSqlDateTime(this DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime;
            
            if (dateTime.Kind == DateTimeKind.Local)
                return dateTime.ToUniversalTime();
            
            // Si Kind es Unspecified, asumir que es hora local y convertir a UTC
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime();
        }

        /// <summary>
        /// Convierte un DateTime nullable a UTC para PostgreSQL
        /// </summary>
        public static DateTime? ToPostgreSqlDateTime(this DateTime? dateTime)
        {
            if (!dateTime.HasValue)
                return null;
            
            return dateTime.Value.ToPostgreSqlDateTime();
        }

        /// <summary>
        /// Parsea una cadena de fecha a UTC
        /// </summary>
        public static DateTime ParseToUtcDateTime(string dateString)
        {
            if (DateTime.TryParse(dateString, out DateTime parsedDate))
            {
                return parsedDate.ToPostgreSqlDateTime();
            }
            
            throw new ArgumentException($"Cadena de fecha inválida: {dateString}");
        }

        /// <summary>
        /// Parsea una cadena de fecha a UTC nullable
        /// </summary>
        public static DateTime? ParseToUtcDateTimeNullable(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;
            
            return ParseToUtcDateTime(dateString);
        }

        /// <summary>
        /// Convierte una fecha a string ISO UTC
        /// </summary>
        public static string ToUtcIsoString(this DateTime dateTime)
        {
            return dateTime.ToPostgreSqlDateTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        /// <summary>
        /// Convierte una fecha nullable a string ISO UTC
        /// </summary>
        public static string? ToUtcIsoString(this DateTime? dateTime)
        {
            if (!dateTime.HasValue)
                return null;
            
            return dateTime.Value.ToUtcIsoString();
        }

        /// <summary>
        /// Valida si un DateTime es UTC
        /// </summary>
        public static bool IsUtc(this DateTime dateTime)
        {
            return dateTime.Kind == DateTimeKind.Utc;
        }

        /// <summary>
        /// Valida si un DateTime nullable es UTC
        /// </summary>
        public static bool IsUtc(this DateTime? dateTime)
        {
            return dateTime.HasValue && dateTime.Value.IsUtc();
        }
    }
} 