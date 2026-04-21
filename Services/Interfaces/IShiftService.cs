using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces
{
    public interface IShiftService
    {
        Task<List<Shift>> GetAllAsync();
        Task<List<Shift>> GetAllIncludingInactiveAsync();
        Task<Shift?> GetByIdAsync(Guid id);
        Task<Shift?> GetByNameAsync(string name);
        Task<Shift> CreateAsync(Shift shift);
        Task UpdateAsync(Shift shift);
        Task DeleteAsync(Guid id);
        Task<Shift> GetOrCreateAsync(string name);

        /// <summary>Obtiene o crea una jornada por escuela y nombre (ej. Mañana, Tarde).</summary>
        Task<Shift> GetOrCreateBySchoolAndNameAsync(Guid schoolId, string name);
    }
}

