using System.Text.Json.Serialization;

namespace SchoolManager.Dtos;

public class ScanRequestDto
{
    public string Token { get; set; } = null!;
    public Guid ScannedBy { get; set; }
    public string ScanType { get; set; } = "entry";

    /// <summary>
    /// Rol del usuario autenticado que realiza el escaneo.
    /// CRÍTICO: Poblado exclusivamente por el controller desde el JWT/cookie (ClaimTypes.Role).
    /// Nunca se deserializa desde el cuerpo del request para prevenir escalación de privilegios
    /// (un atacante no puede falsificar su propio rol enviando un GUID de inspector en ScannedBy).
    /// </summary>
    [JsonIgnore]
    public string? AuthenticatedRole { get; set; }
}
