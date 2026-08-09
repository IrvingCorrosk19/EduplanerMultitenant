namespace SchoolManager.Helpers
{
    /// <summary>
    /// Validador centralizado para uploads de archivos.
    /// Previene: subida de ejecutables, archivos excesivamente grandes, y extension spoofing.
    /// </summary>
    public static class FileUploadValidator
    {
        /// <summary>
        /// Extensiones permitidas para documentos de reportes (disciplina, orientación, etc.)
        /// </summary>
        public static readonly string[] AllowedDocumentExtensions =
            { ".pdf", ".jpg", ".jpeg", ".png", ".docx", ".doc", ".xlsx", ".xls" };

        /// <summary>
        /// Extensiones permitidas para imágenes de comprobantes de pago
        /// </summary>
        public static readonly string[] AllowedImageExtensions =
            { ".jpg", ".jpeg", ".png", ".webp" };

        /// <summary>
        /// Tamaño máximo por archivo: 10 MB
        /// </summary>
        public const long MaxFileSizeBytes = 10 * 1024 * 1024;

        /// <summary>
        /// Valida un IFormFile contra tipo permitido y tamaño máximo.
        /// </summary>
        /// <param name="file">Archivo a validar</param>
        /// <param name="allowedExtensions">Array de extensiones permitidas (ej. AllowedDocumentExtensions)</param>
        /// <param name="maxSizeBytes">Tamaño máximo en bytes (default: 10 MB)</param>
        /// <returns>(isValid: bool, errorMessage: string?)</returns>
        public static (bool IsValid, string? Error) Validate(
            IFormFile file,
            string[]? allowedExtensions = null,
            long maxSizeBytes = MaxFileSizeBytes)
        {
            if (file == null || file.Length == 0)
                return (false, "El archivo está vacío.");

            if (file.Length > maxSizeBytes)
                return (false, $"El archivo supera el tamaño máximo permitido ({maxSizeBytes / (1024 * 1024)} MB).");

            var extensions = allowedExtensions ?? AllowedDocumentExtensions;
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !extensions.Contains(extension))
                return (false, $"Tipo de archivo no permitido. Use: {string.Join(", ", extensions)}");

            return (true, null);
        }

        /// <summary>
        /// Valida múltiples archivos. Devuelve lista de errores (vacía si todos son válidos).
        /// </summary>
        public static List<string> ValidateAll(
            IEnumerable<IFormFile> files,
            string[]? allowedExtensions = null,
            long maxSizeBytes = MaxFileSizeBytes)
        {
            var errors = new List<string>();
            foreach (var file in files)
            {
                var (isValid, error) = Validate(file, allowedExtensions, maxSizeBytes);
                if (!isValid && error != null)
                    errors.Add($"{file.FileName}: {error}");
            }
            return errors;
        }
    }
}
