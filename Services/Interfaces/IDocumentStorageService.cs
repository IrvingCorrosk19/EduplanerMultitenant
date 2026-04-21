using Microsoft.AspNetCore.Http;

namespace SchoolManager.Services.Interfaces;

/// <summary>
/// Almacenamiento de documentos del libro de calificaciones (TeacherGradebook), fuera de wwwroot.
/// No interviene con imágenes ni Cloudinary.
/// </summary>
public interface IDocumentStorageService
{
    /// <summary>Guarda el archivo y devuelve solo el nombre almacenado (GUID_nombreOriginalSanitizado.ext).</summary>
    Task<string> SaveTeacherGradebookFileAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>True si el valor en BD corresponde a un archivo guardado por este servicio (nombre plano, sin rutas URL).</summary>
    bool IsPersistedTeacherGradebookFileName(string? pdfUrl);

    /// <summary>Ruta relativa de la app para el enlace de descarga (incluye codificación del nombre).</summary>
    string? ToPublicDownloadUrl(string? storedFileName);

    /// <summary>Elimina el archivo del disco si existe y el nombre es válido.</summary>
    Task TryDeleteTeacherGradebookFileAsync(string? storedFileName, CancellationToken cancellationToken = default);

    /// <summary>Ruta física absoluta si el nombre es seguro y el archivo existe; si no, null.</summary>
    string? TryGetExistingTeacherGradebookPath(string fileName);
}
