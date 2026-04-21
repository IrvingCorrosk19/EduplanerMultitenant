namespace SchoolManager.Services.Interfaces;

/// <summary>
/// Contrato de almacenamiento de archivos (Application Layer).
/// Permite reemplazar por Azure Blob / S3 sin modificar lógica de negocio.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Guarda la foto del usuario y devuelve la URL/path relativo para persistir en BD.
    /// </summary>
    /// <param name="file">Archivo de imagen (JPEG/PNG, máx 2MB).</param>
    /// <param name="userId">Id del usuario (para nombres únicos).</param>
    /// <returns>URL https en Cloudinary (las fotos de usuario no se guardan en disco al subir).</returns>
    Task<string> SaveUserPhotoAsync(IFormFile file, Guid userId);

    /// <summary>
    /// Elimina el archivo asociado a la URL/path (liberar espacio).</summary>
    Task DeleteUserPhotoAsync(string? photoUrl);

    /// <summary>
    /// Obtiene los bytes de la foto (para PDF/embed sin depender de URL relativa).</summary>
    Task<byte[]?> GetUserPhotoBytesAsync(string? photoUrl);
}
