using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de Cloudinary para almacenamiento de imágenes
    /// </summary>
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary? _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
        {
            _logger = logger;

            var cloudName = configuration["Cloudinary:CloudName"]?.Trim();
            var apiKey = configuration["Cloudinary:ApiKey"]?.Trim();
            var apiSecret = configuration["Cloudinary:ApiSecret"]?.Trim();

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret)
                || IsPlaceholder(cloudName) || IsPlaceholder(apiKey) || IsPlaceholder(apiSecret))
            {
                _logger.LogWarning(
                    "Cloudinary sin credenciales válidas (placeholders o vacío). " +
                    "Todas las subidas de imágenes de producto van a Cloudinary en cualquier ambiente; " +
                    "defina CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY y CLOUDINARY_API_SECRET.");
                _cloudinary = null;
                return;
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true; // Usar HTTPS
        }

        private static bool IsPlaceholder(string value) =>
            value.StartsWith("TU_", StringComparison.OrdinalIgnoreCase)
            || value.Contains("AQUI", StringComparison.OrdinalIgnoreCase);

        public bool IsConfigured => _cloudinary != null;

        public async Task<string?> UploadImageAsync(IFormFile file, string folder)
        {
            if (_cloudinary == null)
                return null;

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Archivo vacío o nulo");
                return null;
            }

            try
            {
                using var stream = file.OpenReadStream();
                
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                };

                var uploadResult = await _cloudinary!.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    _logger.LogError("Error en Cloudinary: {Error}", uploadResult.Error.Message);
                    return null;
                }

                _logger.LogInformation("✅ Imagen subida a Cloudinary: {Url}", uploadResult.SecureUrl);
                return uploadResult.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subiendo imagen a Cloudinary");
                return null;
            }
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (_cloudinary == null || string.IsNullOrEmpty(publicId))
            {
                return false;
            }

            try
            {
                var deleteParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deleteParams);

                if (result.Error != null)
                {
                    _logger.LogError("Error eliminando imagen de Cloudinary: {Error}", result.Error.Message);
                    return false;
                }

                _logger.LogInformation("✅ Imagen eliminada de Cloudinary: {PublicId}", publicId);
                return result.Result == "ok";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando imagen de Cloudinary");
                return false;
            }
        }

        public string GetImageUrl(string publicId, int? width = null, int? height = null)
        {
            if (_cloudinary == null || string.IsNullOrEmpty(publicId))
            {
                return string.Empty;
            }

            try
            {
                var transformation = new Transformation();

                if (width.HasValue)
                {
                    transformation.Width(width.Value);
                }

                if (height.HasValue)
                {
                    transformation.Height(height.Value);
                }

                transformation.Crop("fill").Quality("auto").FetchFormat("auto");

                return _cloudinary!.Api.UrlImgUp.Transform(transformation).BuildUrl(publicId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando URL de Cloudinary");
                return string.Empty;
            }
        }
    }
}

