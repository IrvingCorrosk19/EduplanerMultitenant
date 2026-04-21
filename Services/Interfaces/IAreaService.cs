using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces
{
    public interface IAreaService
    {
        // Crear una nueva área o devolver una existente por nombre
        Task<Area> GetOrCreateAsync(string name);

        // Obtener todas las áreas
        Task<List<Area>> GetAllAsync();

        // Obtener un área por su ID
        Task<Area?> GetByIdAsync(Guid id);

        // Crear un área (si necesitas una creación directa separada de GetOrCreateAsync)
        Task<Area> CreateAsync(Area area);

        // Actualizar un área existente
        Task<Area> UpdateAsync(Area area);

        // Eliminar un área por ID
        Task DeleteAsync(Guid id);
    }
}
