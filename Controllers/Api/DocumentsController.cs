using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Controllers.Api;

[ApiController]
[Route("api/documents")]
[Authorize]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentStorageService _documentStorage;

    public DocumentsController(IDocumentStorageService documentStorage)
    {
        _documentStorage = documentStorage;
    }

    /// <summary>Descarga un documento guardado por TeacherGradebook (volumen persistente).</summary>
    [HttpGet("download/{fileName}")]
    public IActionResult Download(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest();

        var decoded = Uri.UnescapeDataString(fileName);
        var path = _documentStorage.TryGetExistingTeacherGradebookPath(decoded);
        if (path == null)
            return NotFound();

        var contentType = GetContentType(path);
        return PhysicalFile(path, contentType, fileDownloadName: Path.GetFileName(path));
    }

    private static string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }
}
