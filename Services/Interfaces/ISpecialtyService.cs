using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces
{
    public interface ISpecialtyService
    {
        // Obtener o crear una especialidad por nombre
        Task<Specialty> GetOrCreateAsync(string name);

        // Obtener todas las especialidades
        Task<List<Specialty>> GetAllAsync();

        // Obtener una especialidad por ID
        Task<Specialty?> GetByIdAsync(Guid id);

        // Crear una nueva especialidad
        Task<Specialty> CreateAsync(Specialty specialty);

        // Actualizar una especialidad existente
        Task<Specialty> UpdateAsync(Specialty specialty);

        // Eliminar una especialidad por ID
        Task DeleteAsync(Guid id);
    }
}
