using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SchoolManager.Services.Implementations
{
public class GradeLevelService : IGradeLevelService
{
    private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GradeLevelService(SchoolDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
            _currentUserService = currentUserService;
    }
    public async Task<GradeLevel?> GetByNameAsync(string name)
    {
        return await _context.GradeLevels
            .FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower());
    }
    public async Task<GradeLevel> GetOrCreateAsync(string name)
    {
        name = name.Trim().ToUpper();
        var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
        var grade = await _context.GradeLevels.FirstOrDefaultAsync(g => g.Name.ToUpper() == name && g.SchoolId == schoolId);
        if (grade == null)
        {
            grade = new GradeLevel
            {
                Id = Guid.NewGuid(),
                Name = name
            };
            
            // Configurar campos de auditoría y SchoolId
            await AuditHelper.SetAuditFieldsForCreateAsync(grade, _currentUserService);
            await AuditHelper.SetSchoolIdAsync(grade, _currentUserService);
            
            _context.GradeLevels.Add(grade);
            await _context.SaveChangesAsync();
        }
        return grade;
    }

    public async Task<IEnumerable<GradeLevel>> GetAllAsync()
    {
        var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
        if (schoolId == null) return Enumerable.Empty<GradeLevel>();
        return await _context.GradeLevels
            .Where(g => g.SchoolId == schoolId)
            .ToListAsync();
    }

    public async Task<GradeLevel?> GetByIdAsync(Guid id)
    {
        var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
        var grade = await _context.GradeLevels.FindAsync(id);
        if (grade == null || grade.SchoolId != schoolId) return null;
        return grade;
    }

    public async Task<GradeLevel> CreateAsync(GradeLevel gradeLevel)
    {
        try
        {
            gradeLevel.Id = Guid.NewGuid();
            
            // Configurar campos de auditoría y SchoolId
            await AuditHelper.SetAuditFieldsForCreateAsync(gradeLevel, _currentUserService);
            await AuditHelper.SetSchoolIdAsync(gradeLevel, _currentUserService);
            
            _context.GradeLevels.Add(gradeLevel);
            await _context.SaveChangesAsync();
            return gradeLevel;
        }
        catch (Exception ex)
        {
            throw new Exception("Error al crear el grado académico", ex);
        }
    }

    public async Task<GradeLevel> UpdateAsync(GradeLevel gradeLevel)
    {
        try
        {
            // Configurar campos de auditoría para actualización
            await AuditHelper.SetAuditFieldsForUpdateAsync(gradeLevel, _currentUserService);
            
            _context.GradeLevels.Update(gradeLevel);
            await _context.SaveChangesAsync();
            return gradeLevel;
        }
        catch (Exception ex)
        {
            throw new Exception("Error al actualizar el grado académico", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
        var entity = await _context.GradeLevels.FindAsync(id);
        if (entity == null || entity.SchoolId != schoolId) return false;

        bool enUso = await _context.SubjectAssignments.AnyAsync(sa => sa.GradeLevelId == id);
        if (enUso)
            throw new InvalidOperationException("No se puede borrar el grado porque está siendo utilizado en el catálogo de materias. Elimina o reasigna esas asignaciones primero.");
        try
        {
            _context.GradeLevels.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception("Error al eliminar el grado académico", ex);
        }
    }
    }
}
