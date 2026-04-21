namespace SchoolManager.Services.Interfaces;

public interface IStudentIdCardHtmlCaptureService
{
    Task<byte[]> GenerateFromUrl(string url);

    /// <summary>
    /// Igual que varias veces <see cref="GenerateFromUrl"/>: por URL captura HTML; si falla (tras reintento), PDF nativo para ese estudiante solo — como GET ui/print.
    /// Un solo Chromium para las capturas HTML.
    /// </summary>
    Task<IReadOnlyList<byte[]>> GenerateBulkFromUrls(IReadOnlyList<string> urls);
}
