using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using SchoolManager.Interfaces;

namespace SchoolManager.Services
{
    /// Almacena archivos en wwwroot/uploads; útil en desarrollo
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _basePath;

        public LocalFileStorage(IWebHostEnvironment env)
        {
            _basePath = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(_basePath);
        }

        public async Task<string> SaveAsync(string path, Stream content)
        {
            var fullPath = Path.Combine(_basePath, path.Replace("/", Path.DirectorySeparatorChar.ToString()));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            await using var fileStream = File.Create(fullPath);
            await content.CopyToAsync(fileStream);

            // Devuelve la URL relativa para usarla en <a href="">
            return $"/uploads/{path}";
        }
    }
}
