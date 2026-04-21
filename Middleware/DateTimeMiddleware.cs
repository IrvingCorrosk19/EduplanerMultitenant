using System.Text.Json;
using System.Text.Json.Serialization;

namespace SchoolManager.Middleware
{
    /// <summary>
    /// Middleware global para manejar conversiones de DateTime en JSON
    /// </summary>
    public class DateTimeMiddleware
    {
        private readonly RequestDelegate _next;

        public DateTimeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Configurar JSON para manejar DateTime correctamente
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new DateTimeJsonConverter() }
            };

            // Agregar las opciones al contexto para que estén disponibles en los controladores
            context.Items["JsonOptions"] = options;

            await _next(context);
        }
    }

    /// <summary>
    /// Convertidor JSON personalizado para DateTime
    /// </summary>
    public class DateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var dateString = reader.GetString();
                if (DateTime.TryParse(dateString, out DateTime dateTime))
                {
                    // Especificar el Kind como Local antes de convertir a UTC
                    var localDateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                    return localDateTime.ToUniversalTime();
                }
            }
            
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Escribir como UTC ISO string
            writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
    }

    /// <summary>
    /// Convertidor JSON personalizado para DateTime nullable
    /// </summary>
    public class NullableDateTimeJsonConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String)
            {
                var dateString = reader.GetString();
                if (DateTime.TryParse(dateString, out DateTime dateTime))
                {
                    // Especificar el Kind como Local antes de convertir a UTC
                    var localDateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                    return localDateTime.ToUniversalTime();
                }
            }
            
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                // Escribir como UTC ISO string
                writer.WriteStringValue(value.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
} 