using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Services.Interfaces;
using SchoolManager.Models;

namespace SchoolManager.Services
{
    public class TrimesterService : ITrimesterService
    {
        private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public TrimesterService(SchoolDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<TrimesterDto>> GetAllAsync()
        {
            // Obtener la escuela del usuario logueado para filtrar
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            var now = DateTime.UtcNow;
            var trimestres = await _context.Trimesters
                .Where(t => t.SchoolId == currentUserSchool.Id)  // ← Filtrar por escuela
                .ToListAsync();
            
            bool cambios = false;
            foreach (var t in trimestres)
            {
                if (t.IsActive && t.EndDate < now)
                {
                    t.IsActive = false;
                    t.UpdatedAt = now;
                    cambios = true;
                }
            }
            if (cambios)
                await _context.SaveChangesAsync();
            return trimestres
                .OrderBy(t => t.StartDate.Year)
                .ThenBy(t => t.Order)
                .Select(t => new TrimesterDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    IsActive = t.IsActive,
                    Order = t.Order,
                    Description = t.Description
                })
                .ToList();
        }

        public async Task<TrimesterValidationDto> ValidarTrimestresAsync(List<TrimesterDto> trimestres)
        {
            var validacion = new TrimesterValidationDto { IsValid = true };

            if (trimestres == null || !trimestres.Any())
            {
                validacion.IsValid = false;
                validacion.Errors.Add("No se proporcionaron datos de trimestres.");
                return validacion;
            }

            // Validar que todos los trimestres tengan datos básicos
            foreach (var trimestre in trimestres)
            {
                if (string.IsNullOrWhiteSpace(trimestre.Name))
                {
                    validacion.Errors.Add($"El trimestre {trimestre.Order} no tiene nombre.");
                }

                if (trimestre.StartDate >= trimestre.EndDate)
                {
                    validacion.Errors.Add($"El trimestre {trimestre.Name} tiene fecha de inicio posterior o igual a la fecha de fin.");
                }

                if (trimestre.Order <= 0)
                {
                    validacion.Errors.Add($"El trimestre {trimestre.Name} tiene un orden inválido.");
                }
            }

            // Validar que no haya nombres duplicados
            var nombresDuplicados = trimestres.GroupBy(t => t.Name).Where(g => g.Count() > 1);
            foreach (var grupo in nombresDuplicados)
            {
                validacion.Errors.Add($"Hay múltiples trimestres con el nombre '{grupo.Key}'.");
            }

            // Validar que no haya órdenes duplicados
            var ordenesDuplicados = trimestres.GroupBy(t => t.Order).Where(g => g.Count() > 1);
            foreach (var grupo in ordenesDuplicados)
            {
                validacion.Errors.Add($"Hay múltiples trimestres con el orden {grupo.Key}.");
            }

            // Validar que los trimestres sean consecutivos
            var trimestresOrdenados = trimestres.OrderBy(t => t.Order).ToList();
            for (int i = 0; i < trimestresOrdenados.Count - 1; i++)
            {
                var actual = trimestresOrdenados[i];
                var siguiente = trimestresOrdenados[i + 1];

                // Verificar que no haya solapamiento
                if (actual.EndDate >= siguiente.StartDate)
                {
                    validacion.Errors.Add($"El trimestre {actual.Name} se solapa con el trimestre {siguiente.Name}.");
                }

                // Verificar que haya al menos un día entre trimestres
                var diasEntre = (siguiente.StartDate - actual.EndDate).Days;
                if (diasEntre < 1)
                {
                    validacion.Errors.Add($"Debe haber al menos un día entre el trimestre {actual.Name} y el trimestre {siguiente.Name}.");
                }

                // Verificar que el orden sea consecutivo
                if (siguiente.Order != actual.Order + 1)
                {
                    validacion.Errors.Add($"El orden de los trimestres debe ser consecutivo. Se esperaba {actual.Order + 1} pero se encontró {siguiente.Order}.");
                }
            }

            // Validar que todos los trimestres estén en el mismo año escolar
            var aniosInicio = trimestres.Select(t => t.StartDate.Year).Distinct().ToList();
            var aniosFin = trimestres.Select(t => t.EndDate.Year).Distinct().ToList();
            
            if (aniosInicio.Count > 2 || aniosFin.Count > 2)
            {
                validacion.Errors.Add("Los trimestres deben estar dentro de un máximo de dos años consecutivos (año escolar).");
            }

            // Advertencias
            if (trimestres.Count < 2)
            {
                validacion.Warnings.Add("Se recomienda configurar al menos 2 trimestres para un año escolar completo.");
            }

            if (trimestres.Count > 4)
            {
                validacion.Warnings.Add("Se recomienda no más de 4 trimestres por año escolar.");
            }

            // Verificar que al menos un trimestre esté activo
            if (!trimestres.Any(t => t.IsActive))
            {
                validacion.Warnings.Add("Se recomienda tener al menos un trimestre activo.");
            }

            validacion.IsValid = !validacion.Errors.Any();
            return validacion;
        }

        public async Task GuardarTrimestresAsync(List<TrimesterDto> trimestres)
        {
            // Obtener la escuela del usuario logueado
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            // Validar antes de guardar
            var validacion = await ValidarTrimestresAsync(trimestres);
            if (!validacion.IsValid)
            {
                throw new InvalidOperationException($"Error de validación: {string.Join("; ", validacion.Errors)}");
            }

            foreach (var dto in trimestres)
            {
                var trimestre = new Trimester
                {
                    Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
                    Name = dto.Name,
                    StartDate = dto.StartDate.ToUniversalTime(),
                    EndDate = dto.EndDate.ToUniversalTime(),
                    Order = dto.Order,
                    IsActive = dto.IsActive,
                    Description = dto.Description,
                    SchoolId = currentUserSchool.Id,  // ← Agregar SchoolId del usuario logueado
                    CreatedAt = DateTime.UtcNow
                };
                _context.Trimesters.Add(trimestre);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<bool> EditarFechasTrimestreAsync(TrimesterDto dto)
        {
            // Obtener la escuela del usuario logueado para validación
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            var trimestre = await _context.Trimesters.FindAsync(dto.Id);
            if (trimestre == null)
                return false;

            // Verificar que el trimestre pertenece a la misma escuela
            if (trimestre.SchoolId != currentUserSchool.Id)
            {
                throw new UnauthorizedAccessException("No tiene permisos para modificar trimestres de otra escuela.");
            }

            // Validar que las nuevas fechas no causen conflictos
            var trimestresRelacionados = await _context.Trimesters
                .Where(t => t.Id != dto.Id && 
                           t.SchoolId == currentUserSchool.Id &&  // ← Solo trimestres de la misma escuela
                           ((t.StartDate.Year == dto.StartDate.Year || t.EndDate.Year == dto.StartDate.Year) ||
                            (t.StartDate.Year == dto.EndDate.Year || t.EndDate.Year == dto.EndDate.Year)))
                .OrderBy(t => t.Order)
                .ToListAsync();

            // Verificar solapamiento con otros trimestres
            foreach (var relacionado in trimestresRelacionados)
            {
                if ((dto.StartDate <= relacionado.EndDate && dto.EndDate >= relacionado.StartDate))
                {
                    throw new InvalidOperationException($"Las nuevas fechas se solapan con el trimestre {relacionado.Name}.");
                }
            }

            trimestre.StartDate = dto.StartDate.ToUniversalTime();
            trimestre.EndDate = dto.EndDate.ToUniversalTime();
            trimestre.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task EliminarTodosLosTrimestresAsync()
        {
            // Obtener la escuela del usuario logueado
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            var trimestres = await _context.Trimesters
                .Where(t => t.SchoolId == currentUserSchool.Id)
                .ToListAsync();

            if (trimestres.Count == 0)
                return;

            var trimesterIds = trimestres.Select(t => t.Id).ToList();

            // Desvincular actividades docentes: FK activities.trimester_id → trimester.id impide DELETE sin esto
            var activitiesConTrimestre = await _context.Activities
                .Where(a => a.TrimesterId != null && trimesterIds.Contains(a.TrimesterId.Value))
                .ToListAsync();

            foreach (var a in activitiesConTrimestre)
                a.TrimesterId = null;

            _context.Trimesters.RemoveRange(trimestres);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TrimesterDto>> GetTrimestresPorAnioEscolarAsync(int anioEscolar)
        {
            // Obtener la escuela del usuario logueado para filtrar
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            var trimestres = await _context.Trimesters
                .Where(t => t.SchoolId == currentUserSchool.Id &&  // ← Filtrar por escuela
                           (t.StartDate.Year == anioEscolar || t.EndDate.Year == anioEscolar))
                .OrderBy(t => t.Order)
                .Select(t => new TrimesterDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    IsActive = t.IsActive,
                    Order = t.Order,
                    Description = t.Description
                })
                .ToListAsync();
            return trimestres;
        }

        public async Task<bool> IsTrimesterActiveAsync(string trimesterName)
        {
            // Obtener la escuela del usuario logueado para filtrar
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            var trimestre = await _context.Trimesters
                .FirstOrDefaultAsync(t => t.Name == trimesterName && t.SchoolId == currentUserSchool.Id);
            return trimestre?.IsActive ?? false;
        }

        public async Task ValidateTrimesterActiveAsync(string trimesterName)
        {
            // Obtener la escuela del usuario logueado para filtrar
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            var trimestre = await _context.Trimesters
                .FirstOrDefaultAsync(t => t.Name == trimesterName && t.SchoolId == currentUserSchool.Id);
            if (trimestre == null)
                throw new InvalidOperationException($"No existe el trimestre '{trimesterName}' en su escuela.");
            if (!trimestre.IsActive)
                throw new InvalidOperationException($"El trimestre '{trimesterName}' está inactivo. No se pueden realizar operaciones en un trimestre inactivo.");
        }

        public async Task<bool> ActivarTrimestreAsync(Guid id)
        {
            // Obtener la escuela del usuario logueado para validación
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            var trimestre = await _context.Trimesters.FindAsync(id);
            if (trimestre == null)
                return false;

            // Verificar que el trimestre pertenece a la misma escuela
            if (trimestre.SchoolId != currentUserSchool.Id)
            {
                throw new UnauthorizedAccessException("No tiene permisos para activar trimestres de otra escuela.");
            }

            trimestre.IsActive = true;
            trimestre.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DesactivarTrimestreAsync(Guid id)
        {
            // Obtener la escuela del usuario logueado para validación
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            var trimestre = await _context.Trimesters.FindAsync(id);
            if (trimestre == null)
                return false;

            // Verificar que el trimestre pertenece a la misma escuela
            if (trimestre.SchoolId != currentUserSchool.Id)
            {
                throw new UnauthorizedAccessException("No tiene permisos para desactivar trimestres de otra escuela.");
            }

            trimestre.IsActive = false;
            trimestre.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
