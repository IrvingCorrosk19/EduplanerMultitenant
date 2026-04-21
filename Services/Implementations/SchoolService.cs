using SchoolManager.Models;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Services.Interfaces;

public class SchoolService : ISchoolService
{
    private readonly SchoolDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAcademicYearService _academicYearService;

    public SchoolService(SchoolDbContext context, ICurrentUserService currentUserService, IAcademicYearService academicYearService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _academicYearService = academicYearService;
    }

    public async Task<List<School>> GetAllAsync() =>
        await _context.Schools.ToListAsync();

    public async Task<School?> GetByIdAsync(Guid id) =>
        await _context.Schools.FirstOrDefaultAsync(s => s.Id == id);

    public async Task CreateAsync(School school)
    {
        _context.Schools.Add(school);
        await _context.SaveChangesAsync();

        try
        {
            await _academicYearService.EnsureDefaultAcademicYearForSchoolAsync(school.Id);
        }
        catch
        {
            // No fallar la creación de escuela si la tabla academic_years no existe o falla
        }
        try
        {
            await SchoolManager.Scripts.EnsureDefaultTimeSlots.EnsureForSchoolAsync(_context, school.Id);
        }
        catch
        {
            // No fallar la creación de escuela si la tabla time_slots no existe o falla
        }
    }

    public async Task UpdateAsync(School school)
    {
        _context.Schools.Update(school);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var school = await _context.Schools.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
        if (school != null)
        {
            school.IsActive = false;
            _context.Schools.Update(school);
            await _context.SaveChangesAsync();
        }
    }
}
