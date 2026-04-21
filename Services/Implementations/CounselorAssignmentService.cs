using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManager.Models;
using SchoolManager.Dtos;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations
{
    public class CounselorAssignmentService : ICounselorAssignmentService
    {
        private readonly SchoolDbContext _context;
        private readonly ILogger<CounselorAssignmentService> _logger;
        private readonly ICurrentUserService _currentUserService;

        public CounselorAssignmentService(
            SchoolDbContext context,
            ILogger<CounselorAssignmentService> logger,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<List<CounselorAssignmentDto>> GetAllAsync()
        {
            try
            {
                var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
                _logger.LogInformation("Obteniendo asignaciones de consejeros SchoolId={SchoolId}", schoolId);

                var query = _context.CounselorAssignments
                    .Include(ca => ca.School)
                    .Include(ca => ca.User)
                    .Include(ca => ca.GradeLevel)
                    .Include(ca => ca.Group)
                    .AsQueryable();

                if (schoolId.HasValue)
                    query = query.Where(ca => ca.SchoolId == schoolId.Value);

                var assignments = await query
                    .Select(ca => new CounselorAssignmentDto
                    {
                        Id = ca.Id,
                        SchoolId = ca.SchoolId,
                        SchoolName = ca.School != null ? ca.School.Name : "N/A",
                        UserId = ca.UserId,
                        UserName = ca.User.Name,
                        UserLastName = ca.User.LastName,
                        UserFullName = $"{ca.User.Name} {ca.User.LastName}",
                        UserEmail = ca.User.Email,
                        GradeId = ca.GradeId,
                        GradeName = ca.GradeLevel != null ? ca.GradeLevel.Name : null,
                        GroupId = ca.GroupId,
                        GroupName = ca.Group != null ? ca.Group.Name : null,
                        IsCounselor = ca.IsCounselor,
                        IsActive = ca.IsActive,
                        CreatedAt = ca.CreatedAt,
                        UpdatedAt = ca.UpdatedAt,
                        AssignmentType = GetAssignmentType(ca.GradeId, ca.GroupId)
                    })
                    .ToListAsync();

                _logger.LogInformation("Se encontraron {Count} asignaciones de consejeros", assignments.Count);
                return assignments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las asignaciones de consejeros");
                throw;
            }
        }

        public async Task<CounselorAssignmentDto?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Obteniendo asignación de consejero con ID: {Id}", id);
                
                var assignment = await _context.CounselorAssignments
                    .Include(ca => ca.School)
                    .Include(ca => ca.User)
                    .Include(ca => ca.GradeLevel)
                    .Include(ca => ca.Group)
                    .Where(ca => ca.Id == id)
                    .Select(ca => new CounselorAssignmentDto
                    {
                        Id = ca.Id,
                        SchoolId = ca.SchoolId,
                        SchoolName = ca.School != null ? ca.School.Name : "N/A",
                        UserId = ca.UserId,
                        UserName = ca.User.Name,
                        UserLastName = ca.User.LastName,
                        UserFullName = $"{ca.User.Name} {ca.User.LastName}",
                        UserEmail = ca.User.Email,
                        GradeId = ca.GradeId,
                        GradeName = ca.GradeLevel != null ? ca.GradeLevel.Name : null,
                        GroupId = ca.GroupId,
                        GroupName = ca.Group != null ? ca.Group.Name : null,
                        IsCounselor = ca.IsCounselor,
                        IsActive = ca.IsActive,
                        CreatedAt = ca.CreatedAt,
                        UpdatedAt = ca.UpdatedAt,
                        AssignmentType = GetAssignmentType(ca.GradeId, ca.GroupId)
                    })
                    .FirstOrDefaultAsync();

                if (assignment != null)
                {
                    _logger.LogInformation("Asignación encontrada: {AssignmentType} para {UserFullName}", 
                        assignment.AssignmentType, assignment.UserFullName);
                }
                else
                {
                    _logger.LogWarning("No se encontró asignación con ID: {Id}", id);
                }

                return assignment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener asignación con ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<CounselorAssignmentDto>> GetBySchoolIdAsync(Guid schoolId)
        {
            try
            {
                _logger.LogInformation("Obteniendo asignaciones de consejeros para escuela: {SchoolId}", schoolId);
                
                // Lista completa de registros en counselor_assignments para la escuela (activas e inactivas).
                // No usar navegación School con Include: School tiene HasQueryFilter(IsActive) y en algunos
                // escenarios EF puede traducir el join de forma que excluya filas dependientes.
                var assignments = await _context.CounselorAssignments
                    .AsNoTracking()
                    .Where(ca => ca.SchoolId == schoolId)
                    .OrderByDescending(ca => ca.CreatedAt)
                    .Select(ca => new CounselorAssignmentDto
                    {
                        Id = ca.Id,
                        SchoolId = ca.SchoolId,
                        SchoolName = _context.Schools.IgnoreQueryFilters()
                            .Where(s => s.Id == ca.SchoolId)
                            .Select(s => s.Name)
                            .FirstOrDefault() ?? "N/A",
                        UserId = ca.UserId,
                        UserName = ca.User.Name,
                        UserLastName = ca.User.LastName,
                        UserFullName = ca.User.Name + " " + ca.User.LastName,
                        UserEmail = ca.User.Email,
                        GradeId = ca.GradeId,
                        GradeName = ca.GradeLevel != null ? ca.GradeLevel.Name : null,
                        GroupId = ca.GroupId,
                        GroupName = ca.Group != null ? ca.Group.Name : null,
                        IsCounselor = ca.IsCounselor,
                        IsActive = ca.IsActive,
                        CreatedAt = ca.CreatedAt,
                        UpdatedAt = ca.UpdatedAt,
                        AssignmentType = GetAssignmentType(ca.GradeId, ca.GroupId)
                    })
                    .ToListAsync();

                _logger.LogInformation("Se encontraron {Count} asignaciones para la escuela {SchoolId}", 
                    assignments.Count, schoolId);
                return assignments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener asignaciones para escuela: {SchoolId}", schoolId);
                throw;
            }
        }

        public async Task<List<CounselorAssignmentDto>> GetByUserIdAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Obteniendo asignaciones de consejero para usuario: {UserId}", userId);
                
                var assignments = await _context.CounselorAssignments
                    .Include(ca => ca.School)
                    .Include(ca => ca.User)
                    .Include(ca => ca.GradeLevel)
                    .Include(ca => ca.Group)
                    .Where(ca => ca.UserId == userId)
                    .Select(ca => new CounselorAssignmentDto
                    {
                        Id = ca.Id,
                        SchoolId = ca.SchoolId,
                        SchoolName = ca.School != null ? ca.School.Name : "N/A",
                        UserId = ca.UserId,
                        UserName = ca.User.Name,
                        UserLastName = ca.User.LastName,
                        UserFullName = $"{ca.User.Name} {ca.User.LastName}",
                        UserEmail = ca.User.Email,
                        GradeId = ca.GradeId,
                        GradeName = ca.GradeLevel != null ? ca.GradeLevel.Name : null,
                        GroupId = ca.GroupId,
                        GroupName = ca.Group != null ? ca.Group.Name : null,
                        IsCounselor = ca.IsCounselor,
                        IsActive = ca.IsActive,
                        CreatedAt = ca.CreatedAt,
                        UpdatedAt = ca.UpdatedAt,
                        AssignmentType = GetAssignmentType(ca.GradeId, ca.GroupId)
                    })
                    .ToListAsync();

                _logger.LogInformation("Se encontraron {Count} asignaciones para el usuario {UserId}", 
                    assignments.Count, userId);
                return assignments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener asignaciones para usuario: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<CounselorAssignmentDto>> GetByGradeIdAsync(Guid gradeId)
        {
            try
            {
                _logger.LogInformation("Obteniendo asignaciones de consejeros para grado: {GradeId}", gradeId);
                
                var assignments = await _context.CounselorAssignments
                    .Include(ca => ca.School)
                    .Include(ca => ca.User)
                    .Include(ca => ca.GradeLevel)
                    .Include(ca => ca.Group)
                    .Where(ca => ca.GradeId == gradeId)
                    .Select(ca => new CounselorAssignmentDto
                    {
                        Id = ca.Id,
                        SchoolId = ca.SchoolId,
                        SchoolName = ca.School != null ? ca.School.Name : "N/A",
                        UserId = ca.UserId,
                        UserName = ca.User.Name,
                        UserLastName = ca.User.LastName,
                        UserFullName = $"{ca.User.Name} {ca.User.LastName}",
                        UserEmail = ca.User.Email,
                        GradeId = ca.GradeId,
                        GradeName = ca.GradeLevel != null ? ca.GradeLevel.Name : null,
                        GroupId = ca.GroupId,
                        GroupName = ca.Group != null ? ca.Group.Name : null,
                        IsCounselor = ca.IsCounselor,
                        IsActive = ca.IsActive,
                        CreatedAt = ca.CreatedAt,
                        UpdatedAt = ca.UpdatedAt,
                        AssignmentType = GetAssignmentType(ca.GradeId, ca.GroupId)
                    })
                    .ToListAsync();

                _logger.LogInformation("Se encontraron {Count} asignaciones para el grado {GradeId}", 
                    assignments.Count, gradeId);
                return assignments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener asignaciones para grado: {GradeId}", gradeId);
                throw;
            }
        }

        public async Task<List<CounselorAssignmentDto>> GetByGroupIdAsync(Guid groupId)
        {
            try
            {
                _logger.LogInformation("Obteniendo asignaciones de consejeros para grupo: {GroupId}", groupId);
                
                var assignments = await _context.CounselorAssignments
                    .Include(ca => ca.School)
                    .Include(ca => ca.User)
                    .Include(ca => ca.GradeLevel)
                    .Include(ca => ca.Group)
                    .Where(ca => ca.GroupId == groupId)
                    .Select(ca => new CounselorAssignmentDto
                    {
                        Id = ca.Id,
                        SchoolId = ca.SchoolId,
                        SchoolName = ca.School != null ? ca.School.Name : "N/A",
                        UserId = ca.UserId,
                        UserName = ca.User.Name,
                        UserLastName = ca.User.LastName,
                        UserFullName = $"{ca.User.Name} {ca.User.LastName}",
                        UserEmail = ca.User.Email,
                        GradeId = ca.GradeId,
                        GradeName = ca.GradeLevel != null ? ca.GradeLevel.Name : null,
                        GroupId = ca.GroupId,
                        GroupName = ca.Group != null ? ca.Group.Name : null,
                        IsCounselor = ca.IsCounselor,
                        IsActive = ca.IsActive,
                        CreatedAt = ca.CreatedAt,
                        UpdatedAt = ca.UpdatedAt,
                        AssignmentType = GetAssignmentType(ca.GradeId, ca.GroupId)
                    })
                    .ToListAsync();

                _logger.LogInformation("Se encontraron {Count} asignaciones para el grupo {GroupId}", 
                    assignments.Count, groupId);
                return assignments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener asignaciones para grupo: {GroupId}", groupId);
                throw;
            }
        }

        public async Task<CounselorAssignmentDto?> GetCounselorByGradeAsync(Guid schoolId, Guid gradeId)
        {
            try
            {
                _logger.LogInformation("Obteniendo consejero para escuela {SchoolId} y grado {GradeId}", schoolId, gradeId);
                
                var assignment = await _context.CounselorAssignments
                    .Include(ca => ca.School)
                    .Include(ca => ca.User)
                    .Include(ca => ca.GradeLevel)
                    .Include(ca => ca.Group)
                    .Where(ca => ca.SchoolId == schoolId && ca.GradeId == gradeId && ca.IsActive)
                    .Select(ca => new CounselorAssignmentDto
                    {
                        Id = ca.Id,
                        SchoolId = ca.SchoolId,
                        SchoolName = ca.School != null ? ca.School.Name : "N/A",
                        UserId = ca.UserId,
                        UserName = ca.User.Name,
                        UserLastName = ca.User.LastName,
                        UserFullName = $"{ca.User.Name} {ca.User.LastName}",
                        UserEmail = ca.User.Email,
                        GradeId = ca.GradeId,
                        GradeName = ca.GradeLevel != null ? ca.GradeLevel.Name : null,
                        GroupId = ca.GroupId,
                        GroupName = ca.Group != null ? ca.Group.Name : null,
                        IsCounselor = ca.IsCounselor,
                        IsActive = ca.IsActive,
                        CreatedAt = ca.CreatedAt,
                        UpdatedAt = ca.UpdatedAt,
                        AssignmentType = GetAssignmentType(ca.GradeId, ca.GroupId)
                    })
                    .FirstOrDefaultAsync();

                if (assignment != null)
                {
                    _logger.LogInformation("Consejero encontrado: {UserFullName} para grado {GradeName}", 
                        assignment.UserFullName, assignment.GradeName);
                }
                else
                {
                    _logger.LogWarning("No se encontró consejero para escuela {SchoolId} y grado {GradeId}", 
                        schoolId, gradeId);
                }

                return assignment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener consejero para escuela {SchoolId} y grado {GradeId}", 
                    schoolId, gradeId);
                throw;
            }
        }

        public async Task<CounselorAssignmentDto?> GetCounselorByGroupAsync(Guid schoolId, Guid groupId)
        {
            try
            {
                _logger.LogInformation("Obteniendo consejero para escuela {SchoolId} y grupo {GroupId}", schoolId, groupId);
                
                var assignment = await _context.CounselorAssignments
                    .Include(ca => ca.School)
                    .Include(ca => ca.User)
                    .Include(ca => ca.GradeLevel)
                    .Include(ca => ca.Group)
                    .Where(ca => ca.SchoolId == schoolId && ca.GroupId == groupId && ca.IsActive)
                    .Select(ca => new CounselorAssignmentDto
                    {
                        Id = ca.Id,
                        SchoolId = ca.SchoolId,
                        SchoolName = ca.School != null ? ca.School.Name : "N/A",
                        UserId = ca.UserId,
                        UserName = ca.User.Name,
                        UserLastName = ca.User.LastName,
                        UserFullName = $"{ca.User.Name} {ca.User.LastName}",
                        UserEmail = ca.User.Email,
                        GradeId = ca.GradeId,
                        GradeName = ca.GradeLevel != null ? ca.GradeLevel.Name : null,
                        GroupId = ca.GroupId,
                        GroupName = ca.Group != null ? ca.Group.Name : null,
                        IsCounselor = ca.IsCounselor,
                        IsActive = ca.IsActive,
                        CreatedAt = ca.CreatedAt,
                        UpdatedAt = ca.UpdatedAt,
                        AssignmentType = GetAssignmentType(ca.GradeId, ca.GroupId)
                    })
                    .FirstOrDefaultAsync();

                if (assignment != null)
                {
                    _logger.LogInformation("Consejero encontrado: {UserFullName} para grupo {GroupName}", 
                        assignment.UserFullName, assignment.GroupName);
                }
                else
                {
                    _logger.LogWarning("No se encontró consejero para escuela {SchoolId} y grupo {GroupId}", 
                        schoolId, groupId);
                }

                return assignment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener consejero para escuela {SchoolId} y grupo {GroupId}", 
                    schoolId, groupId);
                throw;
            }
        }

        public async Task<CounselorAssignmentDto?> GetGeneralCounselorAsync(Guid schoolId)
        {
            try
            {
                _logger.LogInformation("Obteniendo consejero general para escuela: {SchoolId}", schoolId);
                
                var assignment = await _context.CounselorAssignments
                    .Include(ca => ca.School)
                    .Include(ca => ca.User)
                    .Include(ca => ca.GradeLevel)
                    .Include(ca => ca.Group)
                    .Where(ca => ca.SchoolId == schoolId && ca.GradeId == null && ca.GroupId == null && ca.IsActive)
                    .Select(ca => new CounselorAssignmentDto
                    {
                        Id = ca.Id,
                        SchoolId = ca.SchoolId,
                        SchoolName = ca.School != null ? ca.School.Name : "N/A",
                        UserId = ca.UserId,
                        UserName = ca.User.Name,
                        UserLastName = ca.User.LastName,
                        UserFullName = $"{ca.User.Name} {ca.User.LastName}",
                        UserEmail = ca.User.Email,
                        GradeId = ca.GradeId,
                        GradeName = ca.GradeLevel != null ? ca.GradeLevel.Name : null,
                        GroupId = ca.GroupId,
                        GroupName = ca.Group != null ? ca.Group.Name : null,
                        IsCounselor = ca.IsCounselor,
                        IsActive = ca.IsActive,
                        CreatedAt = ca.CreatedAt,
                        UpdatedAt = ca.UpdatedAt,
                        AssignmentType = GetAssignmentType(ca.GradeId, ca.GroupId)
                    })
                    .FirstOrDefaultAsync();

                if (assignment != null)
                {
                    _logger.LogInformation("Consejero general encontrado: {UserFullName}", assignment.UserFullName);
                }
                else
                {
                    _logger.LogWarning("No se encontró consejero general para escuela {SchoolId}", schoolId);
                }

                return assignment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener consejero general para escuela: {SchoolId}", schoolId);
                throw;
            }
        }

        public async Task<CounselorAssignmentDto> CreateAsync(CounselorAssignmentCreateDto dto)
        {
            try
            {
                var callerSchoolId = await _currentUserService.GetCurrentSchoolIdAsync();
                if (callerSchoolId.HasValue && callerSchoolId.Value != dto.SchoolId)
                    throw new UnauthorizedAccessException("No puede crear asignaciones en una institución diferente a la suya.");

                _logger.LogInformation("Creando nueva asignación de consejero para usuario {UserId} en escuela {SchoolId}",
                    dto.UserId, dto.SchoolId);

                // Validar que no exista una asignación duplicada
                var existingAssignment = await _context.CounselorAssignments
                    .Where(ca => ca.SchoolId == dto.SchoolId && ca.UserId == dto.UserId)
                    .FirstOrDefaultAsync();

                if (existingAssignment != null)
                {
                    throw new InvalidOperationException("Ya existe una asignación para este usuario en esta escuela");
                }

                var assignment = new CounselorAssignment
                {
                    Id = Guid.NewGuid(),
                    SchoolId = dto.SchoolId,
                    UserId = dto.UserId,
                    GradeId = dto.GradeId,
                    GroupId = dto.GroupId,
                    IsCounselor = dto.IsCounselor,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CounselorAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Asignación de consejero creada exitosamente con ID: {Id}", assignment.Id);

                // Retornar el DTO completo
                return await GetByIdAsync(assignment.Id) ?? throw new InvalidOperationException("Error al obtener la asignación creada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear asignación de consejero");
                
                // Manejar específicamente el error de constraint duplicado
                if (ex.InnerException?.Message?.Contains("counselor_assignments_school_grade_key") == true)
                {
                    throw new InvalidOperationException("Ya existe un consejero asignado para este grado. El sistema actual solo permite un consejero por grado. Contacte al administrador para configurar múltiples consejeros por grado-grupo.");
                }
                
                if (ex.InnerException?.Message?.Contains("counselor_assignments_school_group_key") == true)
                {
                    throw new InvalidOperationException("Ya existe un consejero asignado para este grupo. El sistema actual solo permite un consejero por grupo. Contacte al administrador para configurar múltiples consejeros por grado-grupo.");
                }
                
                throw;
            }
        }

        public async Task<CounselorAssignmentDto> UpdateAsync(Guid id, CounselorAssignmentUpdateDto dto)
        {
            try
            {
                _logger.LogInformation("Actualizando asignación de consejero con ID: {Id}", id);

                var assignment = await _context.CounselorAssignments.FindAsync(id);
                if (assignment == null)
                {
                    throw new InvalidOperationException("Asignación de consejero no encontrada");
                }

                assignment.GradeId = dto.GradeId;
                assignment.GroupId = dto.GroupId;
                assignment.IsCounselor = dto.IsCounselor;
                assignment.IsActive = dto.IsActive;
                assignment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Asignación de consejero actualizada exitosamente con ID: {Id}", id);

                return await GetByIdAsync(id) ?? throw new InvalidOperationException("Error al obtener la asignación actualizada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar asignación de consejero con ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"[SERVICE DEBUG] DeleteAsync called with ID: {id}");
                _logger.LogInformation("Eliminando asignación de consejero con ID: {Id}", id);

                // Verificar si el ID es válido
                if (id == Guid.Empty)
                {
                    Console.WriteLine("[SERVICE ERROR] ID is empty or invalid");
                    _logger.LogError("ID de asignación de consejero inválido en servicio: {Id}", id);
                    return false;
                }

                Console.WriteLine($"[SERVICE DEBUG] Searching for assignment with ID: {id}");
                var assignment = await _context.CounselorAssignments.FindAsync(id);
                if (assignment == null)
                {
                    Console.WriteLine($"[SERVICE ERROR] Assignment not found in database with ID: {id}");
                    _logger.LogWarning("Asignación de consejero no encontrada con ID: {Id}", id);
                    return false;
                }

                Console.WriteLine($"[SERVICE DEBUG] Assignment found: UserId={assignment.UserId}, SchoolId={assignment.SchoolId}, IsActive={assignment.IsActive}");
                _logger.LogInformation("Asignación encontrada en servicio: UserId={UserId}, SchoolId={SchoolId}, IsActive={IsActive}", 
                    assignment.UserId, assignment.SchoolId, assignment.IsActive);

                Console.WriteLine("[SERVICE DEBUG] Removing assignment from context");
                _context.CounselorAssignments.Remove(assignment);
                
                Console.WriteLine("[SERVICE DEBUG] Saving changes to database");
                var changesSaved = await _context.SaveChangesAsync();
                Console.WriteLine($"[SERVICE DEBUG] Changes saved: {changesSaved}");

                Console.WriteLine($"[SERVICE SUCCESS] Assignment deleted successfully with ID: {id}");
                _logger.LogInformation("Asignación de consejero eliminada exitosamente con ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVICE ERROR] Exception in DeleteAsync: {ex.Message}");
                Console.WriteLine($"[SERVICE ERROR] Stack trace: {ex.StackTrace}");
                _logger.LogError(ex, "Error al eliminar asignación de consejero con ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ToggleActiveAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Cambiando estado de asignación de consejero con ID: {Id}", id);

                var assignment = await _context.CounselorAssignments.FindAsync(id);
                if (assignment == null)
                {
                    _logger.LogWarning("Asignación de consejero no encontrada con ID: {Id}", id);
                    return false;
                }

                assignment.IsActive = !assignment.IsActive;
                assignment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Estado de asignación cambiado a {IsActive} para ID: {Id}", 
                    assignment.IsActive, id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado de asignación con ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> IsUserCounselorAsync(Guid userId, Guid schoolId)
        {
            try
            {
                _logger.LogInformation("Verificando si usuario {UserId} es consejero en escuela {SchoolId}", 
                    userId, schoolId);

                var isCounselor = await _context.CounselorAssignments
                    .AnyAsync(ca => ca.UserId == userId && ca.SchoolId == schoolId && ca.IsActive && ca.IsCounselor);

                _logger.LogInformation("Usuario {UserId} {IsCounselor} consejero en escuela {SchoolId}", 
                    userId, isCounselor ? "es" : "no es", schoolId);

                return isCounselor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar si usuario {UserId} es consejero en escuela {SchoolId}", 
                    userId, schoolId);
                throw;
            }
        }

        public async Task<CounselorAssignmentStatsDto> GetStatsAsync(Guid schoolId)
        {
            try
            {
                _logger.LogInformation("Obteniendo estadísticas de asignaciones para escuela: {SchoolId}", schoolId);

                var assignments = await _context.CounselorAssignments
                    .Include(ca => ca.School)
                    .Include(ca => ca.User)
                    .Include(ca => ca.GradeLevel)
                    .Include(ca => ca.Group)
                    .Where(ca => ca.SchoolId == schoolId)
                    .ToListAsync();

                var stats = new CounselorAssignmentStatsDto
                {
                    TotalAssignments = assignments.Count,
                    ActiveAssignments = assignments.Count(a => a.IsActive),
                    InactiveAssignments = assignments.Count(a => !a.IsActive),
                    GeneralCounselors = assignments.Count(a => a.GradeId == null && a.GroupId == null),
                    GradeCounselors = assignments.Count(a => a.GradeId != null),
                    GroupCounselors = assignments.Count(a => a.GroupId != null)
                };

                // Agrupar por tipo
                var assignmentsByType = assignments
                    .GroupBy(a => GetAssignmentType(a.GradeId, a.GroupId))
                    .Select(g => new CounselorAssignmentByTypeDto
                    {
                        Type = g.Key,
                        Count = g.Count(),
                        Assignments = g.Select(a => new CounselorAssignmentDto
                        {
                            Id = a.Id,
                            SchoolId = a.SchoolId,
                            SchoolName = a.School != null ? a.School.Name : "N/A",
                            UserId = a.UserId,
                            UserName = a.User.Name,
                            UserLastName = a.User.LastName,
                            UserFullName = $"{a.User.Name} {a.User.LastName}",
                            UserEmail = a.User.Email,
                            GradeId = a.GradeId,
                            GradeName = a.GradeLevel?.Name,
                            GroupId = a.GroupId,
                            GroupName = a.Group?.Name,
                            IsCounselor = a.IsCounselor,
                            IsActive = a.IsActive,
                            CreatedAt = a.CreatedAt,
                            UpdatedAt = a.UpdatedAt,
                            AssignmentType = GetAssignmentType(a.GradeId, a.GroupId)
                        }).ToList()
                    })
                    .ToList();

                stats.AssignmentsByType = assignmentsByType;

                _logger.LogInformation("Estadísticas obtenidas: {Total} total, {Active} activas, {Inactive} inactivas", 
                    stats.TotalAssignments, stats.ActiveAssignments, stats.InactiveAssignments);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas para escuela: {SchoolId}", schoolId);
                throw;
            }
        }

        public async Task<List<GradeGroupCombinationDto>> GetValidGradeGroupCombinationsAsync(Guid schoolId)
        {
            try
            {
                _logger.LogInformation("Obteniendo combinaciones válidas de grado-grupo para escuela: {SchoolId}", schoolId);

                var rawRows = await _context.StudentAssignments
                    .AsNoTracking()
                    .Where(sa => sa.Student != null && sa.Student.SchoolId == schoolId && sa.IsActive)
                    .Where(sa => !_context.CounselorAssignments.Any(ca =>
                        ca.SchoolId == schoolId &&
                        ca.IsActive &&
                        ca.GradeId == sa.GradeId &&
                        ca.GroupId == sa.GroupId))
                    .Select(sa => new
                    {
                        sa.GradeId,
                        sa.GroupId,
                        GradeName = sa.Grade.Name,
                        GroupName = sa.Group.Name,
                        GroupGrade = sa.Group.Grade ?? "",
                        ShiftName = sa.Shift != null ? sa.Shift.Name : (string?)null
                    })
                    .ToListAsync();

                var rows = rawRows
                    .Select(r => new GradeGroupFlatRow(r.GradeId, r.GroupId, r.GradeName, r.GroupName, r.GroupGrade, r.ShiftName))
                    .ToList();

                var combinations = ToGradeGroupCombinationDtos(rows);

                _logger.LogInformation("Se encontraron {Count} combinaciones válidas de grado-grupo", combinations?.Count ?? 0);
                return combinations ?? new List<GradeGroupCombinationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener combinaciones válidas de grado-grupo para escuela: {SchoolId}", schoolId);
                throw;
            }
        }

        public async Task<List<GradeGroupCombinationDto>> GetValidGradeGroupCombinationsForEditAsync(Guid schoolId, Guid? excludeAssignmentId = null)
        {
            try
            {
                _logger.LogInformation("Obteniendo combinaciones válidas de grado-grupo para edición en escuela: {SchoolId}, excluyendo asignación: {ExcludeId}", schoolId, excludeAssignmentId);
                
                var rawRows = await _context.StudentAssignments
                    .AsNoTracking()
                    .Where(sa => sa.Student != null && sa.Student.SchoolId == schoolId && sa.IsActive)
                    .Where(sa => !_context.CounselorAssignments.Any(ca =>
                        ca.SchoolId == schoolId &&
                        ca.IsActive &&
                        ca.GradeId == sa.GradeId &&
                        ca.GroupId == sa.GroupId &&
                        (excludeAssignmentId == null || ca.Id != excludeAssignmentId)))
                    .Select(sa => new
                    {
                        sa.GradeId,
                        sa.GroupId,
                        GradeName = sa.Grade.Name,
                        GroupName = sa.Group.Name,
                        GroupGrade = sa.Group.Grade ?? "",
                        ShiftName = sa.Shift != null ? sa.Shift.Name : (string?)null
                    })
                    .ToListAsync();

                var rows = rawRows
                    .Select(r => new GradeGroupFlatRow(r.GradeId, r.GroupId, r.GradeName, r.GroupName, r.GroupGrade, r.ShiftName))
                    .ToList();

                var combinations = ToGradeGroupCombinationDtos(rows);

                _logger.LogInformation("Se encontraron {Count} combinaciones válidas de grado-grupo para edición", combinations?.Count ?? 0);
                return combinations ?? new List<GradeGroupCombinationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener combinaciones válidas de grado-grupo para edición en escuela: {SchoolId}", schoolId);
                throw;
            }
        }

        public async Task<List<Guid>> GetAssignedCounselorUserIdsAsync(Guid schoolId)
        {
            try
            {
                _logger.LogInformation("Obteniendo IDs de usuarios ya asignados como consejeros para escuela: {SchoolId}", schoolId);
                
                var assignedUserIds = await _context.CounselorAssignments
                    .Where(ca => ca.SchoolId == schoolId && ca.IsActive)
                    .Select(ca => ca.UserId)
                    .Distinct()
                    .ToListAsync();

                _logger.LogInformation("Se encontraron {Count} usuarios ya asignados como consejeros", assignedUserIds.Count);
                return assignedUserIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener IDs de usuarios asignados como consejeros para escuela: {SchoolId}", schoolId);
                throw;
            }
        }

        public async Task<List<CounselorGroupDto>> GetCounselorGroupsAsync(Guid teacherId)
        {
            try
            {
                _logger.LogInformation("Obteniendo grupos de consejería para profesor {TeacherId}", teacherId);

                var assignments = await _context.CounselorAssignments
                    .Include(ca => ca.GradeLevel)
                    .Include(ca => ca.Group)
                    .Where(ca => ca.UserId == teacherId && ca.IsActive && ca.IsCounselor)
                    .ToListAsync();

                var groups = assignments.Select(assignment => new CounselorGroupDto
                {
                    Id = assignment.Id,
                    SchoolId = assignment.SchoolId,
                    UserId = assignment.UserId,
                    GradeId = assignment.GradeId,
                    GroupId = assignment.GroupId,
                    GradeName = assignment.GradeLevel?.Name ?? "Sin grado",
                    GroupName = assignment.Group?.Name ?? "Sin grupo",
                    DisplayName = $"{assignment.GradeLevel?.Name ?? "Sin grado"} - {assignment.Group?.Name ?? "Sin grupo"}",
                    IsActive = assignment.IsActive,
                    CreatedAt = assignment.CreatedAt,
                    UpdatedAt = assignment.UpdatedAt
                }).ToList();

                _logger.LogInformation("Se encontraron {Count} grupos de consejería para profesor {TeacherId}", groups.Count, teacherId);

                return groups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener grupos de consejería para profesor {TeacherId}", teacherId);
                throw;
            }
        }

        private static string GetAssignmentType(Guid? gradeId, Guid? groupId)
        {
            if (gradeId != null && groupId != null)
                return "Por Combinación";
            return "Por Combinación"; // Siempre será por combinación ahora
        }

        private readonly record struct GradeGroupFlatRow(
            Guid GradeId,
            Guid GroupId,
            string GradeName,
            string GroupName,
            string GroupGrade,
            string? ShiftName);

        private static List<GradeGroupCombinationDto> ToGradeGroupCombinationDtos(IReadOnlyList<GradeGroupFlatRow> rows)
        {
            return rows
                .GroupBy(r => (r.GradeId, r.GroupId))
                .Select(g =>
                {
                    var first = g.First();
                    var shifts = g
                        .Select(x => x.ShiftName)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    return new GradeGroupCombinationDto
                    {
                        GradeId = g.Key.GradeId,
                        GroupId = g.Key.GroupId,
                        GradeName = string.IsNullOrWhiteSpace(first.GradeName) ? "Sin grado" : first.GradeName,
                        GroupName = string.IsNullOrWhiteSpace(first.GroupName) ? "Sin grupo" : first.GroupName,
                        GroupGrade = first.GroupGrade ?? "",
                        StudentCount = g.Count(),
                        ShiftNamesSummary = shifts.Count > 0 ? string.Join(", ", shifts) : ""
                    };
                })
                .OrderBy(c => c.GradeName)
                .ThenBy(c => c.GroupName)
                .ToList();
        }
    }
}
