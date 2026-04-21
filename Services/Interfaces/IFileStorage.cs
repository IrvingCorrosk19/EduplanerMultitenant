using System.IO;
using System.Threading.Tasks;

namespace SchoolManager.Interfaces
{
    /// <summary>
    /// Abstracción para guardar archivos (local, S3, Azure Blob, etc.)
    /// </summary>
    public interface IFileStorage
    {
        Task<string> SaveAsync(string path, Stream content);
    }
}
