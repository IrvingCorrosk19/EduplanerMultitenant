using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Controllers.Api;

[ApiController]
[Route("api/documents")]
[Authorize]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentStorageService _documentStorage;
    private readonly ICurrentUserService _currentUserService;
    private readonly SchoolDbContext _context;

    public DocumentsController(
        IDocumentStorageService documentStorage,
        ICurrentUserService currentUserService,
        SchoolDbContext context)
    {
        _documentStorage = documentStorage;
        _currentUserService = currentUserService;
        _context = context;
    }

    /// <summary>Descarga un documento guardado por TeacherGradebook (volumen persistente).</summary>
    [HttpGet("download/{fileName}")]
    public async Task<IActionResult> Download(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest();

        var decoded = Uri.UnescapeDataString(fileName);

        // Validar ownership: el archivo debe pertenecer a una Activity del colegio del usuario.
        var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
        if (schoolId.HasValue)
        {
            var ownerExists = await _context.Activities
                .AnyAsync(a => a.PdfUrl == decoded && a.SchoolId == schoolId.Value);
            if (!ownerExists)
                return NotFound();
        }

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
