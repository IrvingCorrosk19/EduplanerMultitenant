using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SchoolManager.Services.Implementations
{
    public class SpecialtyService : ISpecialtyService
    {
        private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public SpecialtyService(SchoolDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Specialty> GetOrCreateAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("El nombre de la especialidad no puede estar vacío.", nameof(name));

                name = name.Trim().ToUpper();
                var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
                var specialty = await _context.Specialties
                    .FirstOrDefaultAsync(e => e.Name.ToUpper() == name && e.SchoolId == schoolId);

                if (specialty == null)
                {
                    specialty = new Specialty
                    {
                        Id = Guid.NewGuid(),
                        Name = name
                    };
                    
                    // Configurar campos de auditoría y SchoolId
                    await AuditHelper.SetAuditFieldsForCreateAsync(specialty, _currentUserService);
                    await AuditHelper.SetSchoolIdAsync(specialty, _currentUserService);
                    
                    _context.Specialties.Add(specialty);
                    await _context.SaveChangesAsync();
                }

                return specialty;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception($"No se pudo guardar la especialidad: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al procesar la especialidad: {ex.Message}", ex);
            }
        }

        public async Task<List<Specialty>> GetAllAsync()
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            if (schoolId == null) return new List<Specialty>();
            return await _context.Specialties
                .Where(s => s.SchoolId == schoolId)
                .ToListAsync();
        }

        public async Task<Specialty?> GetByIdAsync(Guid id)
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            var specialty = await _context.Specialties.FindAsync(id);
            if (specialty == null || specialty.SchoolId != schoolId) return null;
            return specialty;
        }

        public async Task<Specialty> CreateAsync(Specialty specialty)
        {
            if (specialty == null || string.IsNullOrWhiteSpace(specialty.Name))
                throw new ArgumentException("La especialidad no es válida.");

            specialty.Id = Guid.NewGuid();
            specialty.Name = specialty.Name.Trim();
            specialty.Description = specialty.Description?.Trim();

            // Configurar campos de auditoría y SchoolId
            await AuditHelper.SetAuditFieldsForCreateAsync(specialty, _currentUserService);
            await AuditHelper.SetSchoolIdAsync(specialty, _currentUserService);

            _context.Specialties.Add(specialty);
            await _context.SaveChangesAsync();

            return specialty;
        }

        public async Task<Specialty> UpdateAsync(Specialty specialty)
        {
            if (specialty == null || string.IsNullOrWhiteSpace(specialty.Name))
                throw new ArgumentException("La especialidad no es válida.");

            var existing = await _context.Specialties.FindAsync(specialty.Id);
            if (existing == null)
                throw new InvalidOperationException("Especialidad no encontrada.");

            existing.Name = specialty.Name.Trim();
            existing.Description = specialty.Description?.Trim();

            // Configurar campos de auditoría para actualización
            await AuditHelper.SetAuditFieldsForUpdateAsync(existing, _currentUserService);

            await _context.SaveChangesAsync();

            return existing;
        }

        public async Task DeleteAsync(Guid id)
        {
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            var specialty = await _context.Specialties.FindAsync(id);
            if (specialty == null || specialty.SchoolId != schoolId) return;

            bool enUso = await _context.SubjectAssignments.AnyAsync(sa => sa.SpecialtyId == id);
            if (enUso)
                throw new InvalidOperationException("No se puede borrar la especialidad porque está siendo utilizada en el catálogo de materias. Elimina o reasigna esas asignaciones primero.");
            _context.Specialties.Remove(specialty);
            await _context.SaveChangesAsync();
        }
    }
}
