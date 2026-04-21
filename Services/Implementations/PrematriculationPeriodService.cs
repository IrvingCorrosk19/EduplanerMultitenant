using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations;

public class PrematriculationPeriodService : IPrematriculationPeriodService
{
    private readonly SchoolDbContext _context;
    private readonly ILogger<PrematriculationPeriodService> _logger;

    public PrematriculationPeriodService(
        SchoolDbContext context,
        ILogger<PrematriculationPeriodService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PrematriculationPeriod?> GetActivePeriodAsync(Guid schoolId)
    {
        var now = DateTime.UtcNow;
        return await _context.PrematriculationPeriods
            .Where(p => p.SchoolId == schoolId 
                && p.IsActive 
                && p.StartDate <= now 
                && p.EndDate >= now)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<PrematriculationPeriod?> GetByIdAsync(Guid id)
    {
        return await _context.PrematriculationPeriods
            .Include(p => p.School)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<PrematriculationPeriodDto>> GetAllAsync(Guid schoolId)
    {
        var now = DateTime.UtcNow;
        return await _context.PrematriculationPeriods
            .Where(p => p.SchoolId == schoolId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PrematriculationPeriodDto
            {
                Id = p.Id,
                SchoolId = p.SchoolId,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsActive = p.IsActive,
                MaxCapacityPerGroup = p.MaxCapacityPerGroup,
                AutoAssignByShift = p.AutoAssignByShift,
                CreatedAt = p.CreatedAt,
                IsPeriodActive = p.StartDate <= now && p.EndDate >= now
            })
            .ToListAsync();
    }

    public async Task<PrematriculationPeriod> CreateAsync(PrematriculationPeriod period, Guid createdBy)
    {
        period.CreatedBy = createdBy;
        period.CreatedAt = DateTime.UtcNow;
        
        // Si este es activo, desactivar los demás períodos de la escuela
        if (period.IsActive)
        {
            var existingActive = await _context.PrematriculationPeriods
                .Where(p => p.SchoolId == period.SchoolId && p.IsActive)
                .ToListAsync();
            
            foreach (var existing in existingActive)
            {
                existing.IsActive = false;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = createdBy;
            }
        }

        _context.PrematriculationPeriods.Add(period);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Período de prematrícula creado: {PeriodId} para escuela {SchoolId}", period.Id, period.SchoolId);
        
        return period;
    }

    public async Task<PrematriculationPeriod> UpdateAsync(PrematriculationPeriod period, Guid updatedBy)
    {
        period.UpdatedAt = DateTime.UtcNow;
        period.UpdatedBy = updatedBy;
        
        // Si este es activo, desactivar los demás períodos de la escuela
        if (period.IsActive)
        {
            var existingActive = await _context.PrematriculationPeriods
                .Where(p => p.SchoolId == period.SchoolId && p.IsActive && p.Id != period.Id)
                .ToListAsync();
            
            foreach (var existing in existingActive)
            {
                existing.IsActive = false;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = updatedBy;
            }
        }

        _context.PrematriculationPeriods.Update(period);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Período de prematrícula actualizado: {PeriodId}", period.Id);
        
        return period;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var period = await _context.PrematriculationPeriods.FindAsync(id);
        if (period == null)
            return false;

        // Verificar que no tenga prematrículas asociadas
        var hasPrematriculations = await _context.Prematriculations
            .AnyAsync(p => p.PrematriculationPeriodId == id);
        
        if (hasPrematriculations)
        {
            _logger.LogWarning("No se puede eliminar el período {PeriodId} porque tiene prematrículas asociadas", id);
            return false;
        }

        _context.PrematriculationPeriods.Remove(period);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Período de prematrícula eliminado: {PeriodId}", id);
        
        return true;
    }

    public async Task<bool> IsPeriodActiveAsync(Guid periodId)
    {
        var now = DateTime.UtcNow;
        return await _context.PrematriculationPeriods
            .AnyAsync(p => p.Id == periodId 
                && p.IsActive 
                && p.StartDate <= now 
                && p.EndDate >= now);
    }
}

