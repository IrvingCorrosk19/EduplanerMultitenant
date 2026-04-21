using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace SchoolManager.Services.Security;

/// <summary>Configuración de firma para el QR del carnet (appsettings: QrSecurity:SecretKey).</summary>
public class QrSecurityOptions
{
    public const string SectionName = "QrSecurity";
    public string SecretKey { get; set; } = "CHANGE_THIS_TO_RANDOM_SECRET";
}

public interface IQrSignatureService
{
    /// <summary>Genera token firmado: token|timestamp|signature (HMAC-SHA256).</summary>
    string GenerateSignedToken(string token);
    /// <summary>Valida la firma del token en formato token|timestamp|signature.</summary>
    bool ValidateSignedToken(string signedToken);
    /// <summary>Extrae el token interno (primera parte) cuando el formato es token|timestamp|signature.</summary>
    string? ExtractTokenFromSigned(string signedToken);
}

public class QrSignatureService : IQrSignatureService
{
    private readonly byte[] _secretKeyBytes;
    private const char Separator = '|';

    /// <summary>
    /// Antigüedad máxima permitida de un token firmado (días).
    /// Los tokens del carnet PDF duran 6 meses; 400 días da margen a carnets anuales.
    /// Defensa en profundidad: si la clave se compromete, los tokens muy viejos dejan de funcionar.
    /// </summary>
    private const int MaxSignedTokenAgeDays = 400;

    public QrSignatureService(IOptions<QrSecurityOptions> options)
    {
        var key = options?.Value?.SecretKey;

        // CRÍTICO-4: Rechazar clave vacía, por defecto o demasiado corta
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "QrSecurity:SecretKey no está configurado. Agrega un secreto seguro en appsettings.json.");

        if (key == "CHANGE_THIS_TO_RANDOM_SECRET")
            throw new InvalidOperationException(
                "QrSecurity:SecretKey está usando el valor por defecto. Configura un secreto único y seguro en appsettings.json (mínimo 20 caracteres).");

        if (key.Length < 20)
            throw new InvalidOperationException(
                $"QrSecurity:SecretKey es demasiado corta ({key.Length} chars). Usa al menos 20 caracteres aleatorios.");

        _secretKeyBytes = Encoding.UTF8.GetBytes(key);
    }

    public string GenerateSignedToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return token;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var payload = token + timestamp;
        var signature = ComputeHmacSha256(payload);
        return $"{token}{Separator}{timestamp}{Separator}{signature}";
    }

    public bool ValidateSignedToken(string signedToken)
    {
        if (string.IsNullOrEmpty(signedToken) || !signedToken.Contains(Separator))
            return false;

        var parts = signedToken.Split(Separator, 3, StringSplitOptions.None);
        if (parts.Length != 3)
            return false;

        var token = parts[0];
        var timestamp = parts[1];
        var receivedSignature = parts[2];

        // CRÍTICO-3: Validar que el timestamp sea un Unix timestamp válido
        if (!long.TryParse(timestamp, out var unixTs))
            return false;

        // CRÍTICO-3: Rechazar tokens firmados hace más de MaxSignedTokenAgeDays días
        // Previene replay attack con tokens muy antiguos filtrados de PDFs expirados
        var tokenAge = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(unixTs);
        if (tokenAge < TimeSpan.Zero || tokenAge.TotalDays > MaxSignedTokenAgeDays)
            return false;

        var payload = token + timestamp;
        var expectedSignature = ComputeHmacSha256(payload);

        try
        {
            var receivedBytes = Convert.FromHexString(receivedSignature);
            var expectedBytes = Convert.FromHexString(expectedSignature);
            return receivedBytes.Length == expectedBytes.Length
                   && CryptographicOperations.FixedTimeEquals(receivedBytes, expectedBytes);
        }
        catch
        {
            return false;
        }
    }

    public string? ExtractTokenFromSigned(string signedToken)
    {
        if (string.IsNullOrEmpty(signedToken) || !signedToken.Contains(Separator))
            return null;
        var parts = signedToken.Split(Separator, 3, StringSplitOptions.None);
        return parts.Length == 3 ? parts[0] : null;
    }

    private string ComputeHmacSha256(string payload)
    {
        using var hmac = new HMACSHA256(_secretKeyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
