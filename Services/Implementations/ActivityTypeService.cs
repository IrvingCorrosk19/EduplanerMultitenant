// Services/ActivityTypeService.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Enums;
using SchoolManager.Interfaces;
using SchoolManager.Models;   // SchoolDbContext, ActivityTypes
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations
{
    /// <summary>
    /// Devuelve la lista de tipos de actividad (tarea, parcial, examen…).
    /// </summary>
    public class ActivityTypeService : IActivityTypeService
    {
        private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ActivityTypeService(SchoolDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IEnumerable<ActivityTypeDto>> GetAllAsync()
        {
            var types = await _context.ActivityTypes
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.Name)
                .Select(t => new ActivityTypeDto
                {
                    Id = t.Id,
                    Name = t.Name
                })
                .ToListAsync();

            // Si no hay tipos en la base de datos, devolver los tipos por defecto del enum
            if (!types.Any())
            {
                return GetDefaultActivityTypes();
            }

            return types;
        }

        /// <summary>
        /// Obtiene los tipos de actividad por defecto desde el enum
        /// </summary>
        private static IEnumerable<ActivityTypeDto> GetDefaultActivityTypes()
        {
            return Enum.GetValues(typeof(ActivityTypeEnum))
                .Cast<ActivityTypeEnum>()
                .Select(enumValue => new ActivityTypeDto
                {
                    Id = Guid.Empty,
                    Name = enumValue.GetDisplayName()
                })
                .ToList();
        }
    }
}
