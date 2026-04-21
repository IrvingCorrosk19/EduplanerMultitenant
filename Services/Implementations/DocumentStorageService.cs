using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SchoolManager.Options;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations;

public sealed class DocumentStorageService : IDocumentStorageService
{
    private const string TeacherGradebookFolder = "teacher-gradebook-files";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx"
    };

    private readonly string _teacherGradebookDirectory;

    public DocumentStorageService(IOptions<StorageOptions> options, IWebHostEnvironment env)
    {
        var basePath = options.Value?.BasePath?.Trim();
        if (string.IsNullOrEmpty(basePath))
            basePath = Path.Combine(env.ContentRootPath, "App_Data", "teacher-gradebook-persistent");
        else if (!Path.IsPathFullyQualified(basePath))
            basePath = Path.GetFullPath(Path.Combine(env.ContentRootPath, basePath));

        _teacherGradebookDirectory = Path.Combine(basePath, TeacherGradebookFolder);
        Directory.CreateDirectory(_teacherGradebookDirectory);
    }

    /// <inheritdoc />
    public async Task<string> SaveTeacherGradebookFileAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("Debe seleccionar un archivo no vacío.");

        var originalName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(originalName))
            throw new InvalidOperationException("El nombre del archivo no es válido.");

        var ext = Path.GetExtension(originalName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            throw new InvalidOperationException(
                "Tipo de archivo no permitido. Use PDF, Word o Excel (.pdf, .doc, .docx, .xls, .xlsx).");

        var safeBase = SanitizeFileName(Path.GetFileNameWithoutExtension(originalName));
        if (string.IsNullOrEmpty(safeBase))
            safeBase = "documento";

        var storedName = $"{Guid.NewGuid():N}_{safeBase}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(_teacherGradebookDirectory, storedName);

        await using (var stream = File.Create(fullPath))
        {
            await file.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        return storedName;
    }

    /// <inheritdoc />
    public bool IsPersistedTeacherGradebookFileName(string? pdfUrl)
    {
        if (string.IsNullOrWhiteSpace(pdfUrl))
            return false;
        if (pdfUrl.StartsWith("/", StringComparison.Ordinal) || pdfUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return false;
        if (pdfUrl.Contains("..", StringComparison.Ordinal)
            || pdfUrl.Contains('\\', StringComparison.Ordinal)
            || pdfUrl.Contains('/', StringComparison.Ordinal))
            return false;
        var name = Path.GetFileName(pdfUrl);
        return string.Equals(name, pdfUrl, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public string? ToPublicDownloadUrl(string? storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName) || !IsPersistedTeacherGradebookFileName(storedFileName))
            return storedFileName;
        return "/api/documents/download/" + Uri.EscapeDataString(storedFileName);
    }

    /// <inheritdoc />
    public Task TryDeleteTeacherGradebookFileAsync(string? storedFileName, CancellationToken cancellationToken = default)
    {
        if (!IsPersistedTeacherGradebookFileName(storedFileName))
            return Task.CompletedTask;

        var path = Path.Combine(_teacherGradebookDirectory, storedFileName!);
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // No bloquear borrado de actividad por fallo de limpieza de disco
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public string? TryGetExistingTeacherGradebookPath(string fileName)
    {
        try
        {
            var safe = Path.GetFileName(fileName);
            if (string.IsNullOrEmpty(safe) || !string.Equals(safe, fileName, StringComparison.Ordinal))
                return null;
            if (!IsPersistedTeacherGradebookFileName(safe))
                return null;
            var full = Path.Combine(_teacherGradebookDirectory, safe);
            return File.Exists(full) ? full : null;
        }
        catch
        {
            return null;
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name.Trim())
        {
            if (Array.IndexOf(invalid, c) >= 0 || c is '/' or '\\')
                sb.Append('_');
            else
                sb.Append(c);
        }
        var s = sb.ToString().Trim();
        return s.Length > 120 ? s[..120] : s;
    }
}
