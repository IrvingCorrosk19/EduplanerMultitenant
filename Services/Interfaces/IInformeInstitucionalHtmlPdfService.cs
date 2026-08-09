namespace SchoolManager.Services.Interfaces;

/// <summary>Captura HTML de informes institucionales (#printArea) a PDF mediante Puppeteer (sin reemplazar QuestPDF del resto del sistema).</summary>
public interface IInformeInstitucionalHtmlPdfService
{
    Task<byte[]> GenerarPdfDesdeUrlAsync(string urlAbsoluta, bool landscape = true);
    Task<byte[]> GenerarPdfDesdeHtmlAsync(string html, bool landscape = true);
}
