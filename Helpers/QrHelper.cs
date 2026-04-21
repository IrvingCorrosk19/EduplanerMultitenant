using QRCoder;
using SchoolManager.Services.Security;

namespace SchoolManager.Helpers;

public static class QrHelper
{
    /// <summary>Genera PNG del QR con el contenido dado. Si se pasa <paramref name="signatureService"/>, el contenido se firma (formato token|timestamp|signature) antes de codificar.</summary>
    public static byte[] GenerateQrPng(string content, IQrSignatureService? signatureService = null)
    {
        var toEncode = signatureService != null ? signatureService.GenerateSignedToken(content) : content;
        return GenerateQrPngInternal(toEncode);
    }

    private static byte[] GenerateQrPngInternal(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        return qr.GetGraphic(10);
    }
}
