using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SchoolManager.Middleware;

/// <summary>
/// Autentica requests de la app móvil con el token HMAC-SHA256 devuelto por POST /api/auth/login.
/// Formato del token: base64(userId:schoolId:timestamp:hmac_sha256(userId:schoolId:timestamp, secret)).
/// El token tiene vigencia de 24 horas y se valida criptográficamente; no requiere consulta a BD.
/// </summary>
public class ApiBearerTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _secretKey;
    private static readonly TimeSpan TokenMaxAge = TimeSpan.FromHours(24);

    public ApiBearerTokenMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _secretKey = configuration["ApiToken:SecretKey"] ?? "EduPlaner-ApiToken-2024-HmacSecretKey-Min32Chars!!";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated != true &&
            context.Request.Headers.Authorization.FirstOrDefault() is string auth &&
            auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = auth["Bearer ".Length..].Trim();
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                    // Format: userId:schoolId:role:timestamp:hmac
                    var parts = decoded.Split(':', 5, StringSplitOptions.None);
                    if (parts.Length == 5 &&
                        Guid.TryParse(parts[0], out var userId) &&
                        DateTime.TryParseExact(parts[3], "yyyyMMddHHmmss", null,
                            System.Globalization.DateTimeStyles.None, out var tokenTime))
                    {
                        var payload = $"{parts[0]}:{parts[1]}:{parts[2]}:{parts[3]}";
                        var expectedSig = ComputeHmac(payload);

                        var age = DateTime.UtcNow - tokenTime.ToUniversalTime();
                        if (age >= TimeSpan.Zero && age <= TokenMaxAge &&
                            CryptographicOperations.FixedTimeEquals(
                                Encoding.UTF8.GetBytes(parts[4]),
                                Encoding.UTF8.GetBytes(expectedSig)))
                        {
                            Guid.TryParse(parts[1], out var schoolId);
                            var role = parts[2];

                            var claims = new List<Claim>
                            {
                                new(ClaimTypes.NameIdentifier, userId.ToString()),
                                new(ClaimTypes.Role, role),
                                new("school_id", schoolId == Guid.Empty ? "" : schoolId.ToString())
                            };
                            var identity = new ClaimsIdentity(claims, "ApiBearer");
                            context.User = new ClaimsPrincipal(identity);
                        }
                    }
                }
                catch
                {
                    // Token malformado o inválido: seguir sin autenticar
                }
            }
        }

        await _next(context);
    }

    private string ComputeHmac(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }
}
