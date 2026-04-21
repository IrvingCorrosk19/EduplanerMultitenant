using SchoolManager.Models;

public interface ISchoolService
{
    Task<List<School>> GetAllAsync();
    Task<School?> GetByIdAsync(Guid id);
    Task CreateAsync(School school);
    Task UpdateAsync(School school);
    Task DeleteAsync(Guid id);
}
