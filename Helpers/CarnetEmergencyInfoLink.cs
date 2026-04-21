using SchoolManager.Services.Security;

namespace SchoolManager.Helpers;

/// <summary>
/// Enlaces firmados para el QR de emergencia del carnet (URL https legible por cualquier teléfono).
/// </summary>
public static class CarnetEmergencyInfoLink
{
    public const string InnerTokenPrefix = "EMS";

    /// <summary>URL absoluta a la página pública de datos de emergencia, o null si no hay base URL.</summary>
    public static string? BuildPublicUrl(string? siteBaseUrl, Guid studentId, IQrSignatureService signatureService)
    {
        if (string.IsNullOrWhiteSpace(siteBaseUrl))
            return null;
        var inner = $"{InnerTokenPrefix}{studentId:N}";
        var signed = signatureService.GenerateSignedToken(inner);
        var q = Uri.EscapeDataString(signed);
        return $"{siteBaseUrl.TrimEnd('/')}/StudentIdCard/public/emergency-info?t={q}";
    }

    public static bool TryResolveStudentId(string? signedQueryToken, IQrSignatureService signatureService, out Guid studentId)
    {
        studentId = default;
        if (string.IsNullOrWhiteSpace(signedQueryToken))
            return false;
        if (!signatureService.ValidateSignedToken(signedQueryToken))
            return false;
        var inner = signatureService.ExtractTokenFromSigned(signedQueryToken);
        if (inner == null || !inner.StartsWith(InnerTokenPrefix, StringComparison.Ordinal))
            return false;
        var hex = inner[InnerTokenPrefix.Length..];
        return Guid.TryParseExact(hex, "N", out studentId);
    }
}
