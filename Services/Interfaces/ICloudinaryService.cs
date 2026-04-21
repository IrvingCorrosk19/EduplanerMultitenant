using Microsoft.AspNetCore.Http;

namespace SchoolManager.Services.Interfaces
{
    /// <summary>
    /// Servicio para gestionar archivos en Cloudinary
    /// </summary>
    public interface ICloudinaryService
    {
        /// <summary>True si hay cliente Cloudinary con credenciales reales (no placeholders).</summary>
        bool IsConfigured { get; }

        /// <summary>
        /// Sube una imagen a Cloudinary
        /// </summary>
        /// <param name="file">Archivo a subir</param>
        /// <param name="folder">Carpeta en Cloudinary (ej: "schools/logos")</param>
        /// <returns>URL pública de la imagen</returns>
        Task<string?> UploadImageAsync(IFormFile file, string folder);

        /// <summary>
        /// Elimina una imagen de Cloudinary
        /// </summary>
        /// <param name="publicId">ID público de la imagen en Cloudinary</param>
        /// <returns>True si se eliminó correctamente</returns>
        Task<bool> DeleteImageAsync(string publicId);

        /// <summary>
        /// Obtiene la URL de una imagen con transformaciones
        /// </summary>
        /// <param name="publicId">ID público de la imagen</param>
        /// <param name="width">Ancho deseado</param>
        /// <param name="height">Alto deseado</param>
        /// <returns>URL transformada</returns>
        string GetImageUrl(string publicId, int? width = null, int? height = null);
    }
}

