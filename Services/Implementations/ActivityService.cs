using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;            // ⇦ DTOs con get/set
using SchoolManager.Interfaces;      // ⇦ IActivityService, IFileStorage
using SchoolManager.Models;          // ⇦ SchoolDbContext, Activity
using SchoolManager.Services.Interfaces;
using SchoolManager.Services.Implementations;

namespace SchoolManager.Services
{
    public class ActivityService : IActivityService
    {
        private readonly SchoolDbContext _context;
        private readonly IFileStorage _fileStorage;
        private readonly IDocumentStorageService _documentStorage;
        private readonly ITrimesterService _trimesterService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ActivityService> _logger;

        public ActivityService(
            SchoolDbContext context,
            IFileStorage fileStorage,
            IDocumentStorageService documentStorage,
            ITrimesterService trimesterService,
            ICurrentUserService currentUserService,
            ILogger<ActivityService> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _documentStorage = documentStorage;
            _trimesterService = trimesterService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /* ────────────────────────────────────────
           1.  Métodos usados por el Portal Docente
           ────────────────────────────────────────*/

        public async Task<ActivityDto> CreateAsync(ActivityCreateDto dto)
        {
            try
            {
                _logger.LogInformation("[ActivityService] Iniciando creación de actividad");

            // Validar trimestre activo
            await _trimesterService.ValidateTrimesterActiveAsync(dto.TrimesterCode);
                _logger.LogDebug("[ActivityService] Trimestre validado: {TrimesterCode}", dto.TrimesterCode);

            // Obtener la escuela del usuario logueado
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }
                _logger.LogDebug("[ActivityService] Escuela obtenida: {SchoolId}", currentUserSchool.Id);

            // Buscar el trimestre por código y escuela
            var trimestre = await _context.Trimesters
                .FirstOrDefaultAsync(t => t.Name == dto.TrimesterCode && t.SchoolId == currentUserSchool.Id);
            
            if (trimestre == null)
            {
                throw new InvalidOperationException($"No se encontró el trimestre '{dto.TrimesterCode}' para la escuela actual.");
            }
                _logger.LogDebug("[ActivityService] Trimestre encontrado: {TrimesterId}", trimestre.Id);

            var activity = new Activity
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Type = dto.Type,          // 'tarea' | 'parcial' | 'examen'
                Trimester = dto.TrimesterCode, // '1T' | '2T' | '3T'
                TrimesterId = trimestre.Id,    // ← Asignar TrimesterId
                TeacherId = dto.TeacherId,
                SubjectId = dto.SubjectId,
                GroupId = dto.GroupId,
                GradeLevelId = dto.GradeLevelId,
                SchoolId = currentUserSchool.Id,  // ← Agregar SchoolId del usuario logueado
                DueDate = dto.DueDate.ToUniversalTime()
            };

            // Configurar campos de auditoría
            await AuditHelper.SetAuditFieldsForCreateAsync(activity, _currentUserService);

                _logger.LogDebug("[ActivityService] Actividad creada con ID: {ActivityId}, DueDate: {DueDate}", activity.Id, activity.DueDate);

            if (!string.IsNullOrWhiteSpace(dto.PersistedTeacherGradebookFileName))
            {
                activity.PdfUrl = dto.PersistedTeacherGradebookFileName;
                _logger.LogDebug("[ActivityService] Documento TeacherGradebook persistido para actividad: {ActivityId}", activity.Id);
            }
            else if (dto.Pdf is { Length: > 0 })
            {
                var path = $"activities/{activity.Id}/{dto.Pdf.FileName}";
                await using var stream = dto.Pdf.OpenReadStream();
                activity.PdfUrl = await _fileStorage.SaveAsync(path, stream);
                _logger.LogDebug("[ActivityService] PDF guardado para actividad: {ActivityId}", activity.Id);
            }

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();
                _logger.LogInformation("[ActivityService] Actividad guardada exitosamente en la base de datos: {ActivityId}", activity.Id);

            var subject = await _context.Subjects.Where(x => x.Id == dto.SubjectId).FirstOrDefaultAsync();
            var group = await _context.Groups.Where(x => x.Id == dto.GroupId).FirstOrDefaultAsync();

                var result = new ActivityDto
            {
                Id = activity.Id,
                Name = activity.Name,
                Type = activity.Type,
                Date = DateTime.UtcNow,
                TrimesterCode = activity.Trimester,
                SubjectName = subject?.Name ?? string.Empty,
                GroupDisplayName = group != null ? $"{group.Grade} – {group.Name}" : string.Empty,
                PdfUrl = _documentStorage.ToPublicDownloadUrl(activity.PdfUrl)
            };

                _logger.LogInformation("[ActivityService] Actividad creada exitosamente: {ActivityId}", result.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ActivityService] Error al crear actividad");
                throw; // Re-lanzar la excepción para que el controlador la maneje
            }
        }

        public async Task<ActivityDto> UpdateAsync(ActivityUpdateDto dto)
        {
            try
            {
                _logger.LogInformation("[ActivityService] Iniciando actualización de actividad: {ActivityId}", dto.ActivityId);
                
                // Buscar la actividad existente
                var activity = await _context.Activities.Where(x => x.Id == dto.ActivityId).FirstOrDefaultAsync();
                if (activity == null)
                {
                    throw new InvalidOperationException($"No se encontró la actividad con ID: {dto.ActivityId}");
                }

                // Validar trimestre activo
                await _trimesterService.ValidateTrimesterActiveAsync(dto.TrimesterCode);
                _logger.LogDebug("[ActivityService] Trimestre validado en actualización: {TrimesterCode}", dto.TrimesterCode);

                // Obtener la escuela del usuario logueado
                var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
                if (currentUserSchool == null)
                {
                    throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
                }

                // Buscar el trimestre por código y escuela
                var trimestre = await _context.Trimesters
                    .FirstOrDefaultAsync(t => t.Name == dto.TrimesterCode && t.SchoolId == currentUserSchool.Id);
                
                if (trimestre == null)
                {
                    throw new InvalidOperationException($"No se encontró el trimestre '{dto.TrimesterCode}' para la escuela actual.");
                }

                // Actualizar los campos
                activity.Name = dto.Name;
                activity.Type = dto.Type;
                activity.Trimester = dto.TrimesterCode;
                activity.TrimesterId = trimestre.Id;
                activity.TeacherId = dto.TeacherId;
                activity.SubjectId = dto.SubjectId;
                activity.GroupId = dto.GroupId;
                activity.GradeLevelId = dto.GradeLevelId;
                activity.DueDate = dto.DueDate.ToUniversalTime();

                if (!string.IsNullOrWhiteSpace(dto.PersistedTeacherGradebookFileName))
                {
                    if (_documentStorage.IsPersistedTeacherGradebookFileName(activity.PdfUrl))
                        await _documentStorage.TryDeleteTeacherGradebookFileAsync(activity.PdfUrl).ConfigureAwait(false);
                    activity.PdfUrl = dto.PersistedTeacherGradebookFileName;
                    _logger.LogDebug("[ActivityService] Documento TeacherGradebook actualizado para actividad: {ActivityId}", activity.Id);
                }
                else if (dto.Pdf is { Length: > 0 })
                {
                    if (_documentStorage.IsPersistedTeacherGradebookFileName(activity.PdfUrl))
                        await _documentStorage.TryDeleteTeacherGradebookFileAsync(activity.PdfUrl).ConfigureAwait(false);
                    var path = $"activities/{activity.Id}/{dto.Pdf.FileName}";
                    await using var stream = dto.Pdf.OpenReadStream();
                    activity.PdfUrl = await _fileStorage.SaveAsync(path, stream);
                    _logger.LogDebug("[ActivityService] PDF actualizado para actividad: {ActivityId}", activity.Id);
                }

                // Configurar campos de auditoría para actualización
                await AuditHelper.SetAuditFieldsForUpdateAsync(activity, _currentUserService);

                _context.Activities.Update(activity);
                await _context.SaveChangesAsync();
                _logger.LogInformation("[ActivityService] Actividad actualizada exitosamente en la base de datos: {ActivityId}", activity.Id);

                var subject = await _context.Subjects.Where(x => x.Id == dto.SubjectId).FirstOrDefaultAsync();
                var group = await _context.Groups.Where(x => x.Id == dto.GroupId).FirstOrDefaultAsync();

                var result = new ActivityDto
                {
                    Id = activity.Id,
                    Name = activity.Name,
                    Type = activity.Type,
                    Date = DateTime.UtcNow,
                    TrimesterCode = activity.Trimester,
                    SubjectName = subject?.Name ?? string.Empty,
                    GroupDisplayName = group != null ? $"{group.Grade} – {group.Name}" : string.Empty,
                    PdfUrl = _documentStorage.ToPublicDownloadUrl(activity.PdfUrl)
                };

                _logger.LogInformation("[ActivityService] Actividad actualizada exitosamente: {ActivityId}", result.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ActivityService] Error al actualizar actividad: {ActivityId}", dto.ActivityId);
                throw; // Re-lanzar la excepción para que el controlador la maneje
            }
        }

        public async Task<IEnumerable<ActivityHeaderDto>> GetByTeacherGroupTrimesterAsync(
            Guid teacherId, Guid groupId, string trimesterCode, Guid subjectId, Guid gradeLevelId)
        {
            if (subjectId == Guid.Empty || gradeLevelId == Guid.Empty)
                return new List<ActivityHeaderDto>();

            // Obtener la escuela del usuario logueado para filtrar
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            // Buscar el trimestre por código y escuela
            var trimestre = await _context.Trimesters
                .FirstOrDefaultAsync(t => t.Name == trimesterCode && t.SchoolId == currentUserSchool.Id);

            if (trimestre == null)
            {
                // Si no existe el trimestre, devolver lista vacía
                return new List<ActivityHeaderDto>();
            }

            var query = _context.Activities
                .Where(a => a.TeacherId == teacherId
                         && a.GroupId == groupId
                         && a.Trimester == trimesterCode
                         && a.SchoolId == currentUserSchool.Id
                         && a.TrimesterId == trimestre.Id
                         && a.SubjectId == subjectId
                         && a.GradeLevelId == gradeLevelId);

            var list = await query
                .OrderBy(a => a.CreatedAt)
                .Select(a => new ActivityHeaderDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Type = a.Type,
                    Date = DateTime.UtcNow,
                    HasPdf = a.PdfUrl != null,
                    PdfUrl = a.PdfUrl,
                    DueDate = a.DueDate
                })
                .ToListAsync();

            foreach (var item in list)
                item.PdfUrl = _documentStorage.ToPublicDownloadUrl(item.PdfUrl);

            return list;
        }

        public async Task UploadPdfAsync(Guid activityId, string fileName, Stream content)
        {
            var activity = await _context.Activities.Where(x => x.Id == activityId).FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Actividad no encontrada.");

            // Validar trimestre activo antes de subir PDF
            await _trimesterService.ValidateTrimesterActiveAsync(activity.Trimester);

            var path = $"activities/{activityId}/{fileName}";
            activity.PdfUrl = await _fileStorage.SaveAsync(path, content);

            await _context.SaveChangesAsync();
        }

        /* ────────────────────────────────────────
           2.  CRUD "legacy" que aún usa tu proyecto
           ────────────────────────────────────────*/

        public async Task<List<Activity>> GetAllAsync() =>
            await _context.Activities.ToListAsync();

        public async Task<Activity?> GetByIdAsync(Guid id) =>
            await _context.Activities.Where(x => x.Id == id).FirstOrDefaultAsync();

        public async Task UpdateAsync(Activity activity)
        {
            // Validar trimestre activo antes de actualizar
            await _trimesterService.ValidateTrimesterActiveAsync(activity.Trimester);

            // Obtener la escuela del usuario logueado para validación
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            // Verificar que la actividad pertenece a la misma escuela
            if (activity.SchoolId != currentUserSchool.Id)
            {
                throw new UnauthorizedAccessException("No tiene permisos para modificar actividades de otra escuela.");
            }

            // Verificar que el trimestre existe y pertenece a la misma escuela
            if (activity.TrimesterId.HasValue)
            {
                var trimestre = await _context.Trimesters
                    .FirstOrDefaultAsync(t => t.Id == activity.TrimesterId && t.SchoolId == currentUserSchool.Id);
                
                if (trimestre == null)
                {
                    throw new InvalidOperationException("El trimestre asociado no existe o no pertenece a su escuela.");
                }
            }

            // Asegurar que el SchoolId se mantenga
            activity.SchoolId = currentUserSchool.Id;

            // Configurar campos de auditoría para actualización
            await AuditHelper.SetAuditFieldsForUpdateAsync(activity, _currentUserService);

            _context.Activities.Update(activity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Activities.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (entity is null) return;

            // Validar trimestre activo antes de eliminar
            await _trimesterService.ValidateTrimesterActiveAsync(entity.Trimester);

            // Obtener la escuela del usuario logueado para validación
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            // Verificar que la actividad pertenece a la misma escuela
            if (entity.SchoolId != currentUserSchool.Id)
            {
                throw new UnauthorizedAccessException("No tiene permisos para eliminar actividades de otra escuela.");
            }

            await _documentStorage.TryDeleteTeacherGradebookFileAsync(entity.PdfUrl).ConfigureAwait(false);

            _context.Activities.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Activity>> GetByGroupAndSubjectAsync(Guid groupId, Guid subjectId)
        {
            // Obtener la escuela del usuario logueado para filtrar
            var currentUserSchool = await _currentUserService.GetCurrentUserSchoolAsync();
            if (currentUserSchool == null)
            {
                throw new InvalidOperationException("No se pudo determinar la escuela del usuario actual.");
            }

            return await _context.Activities
                .Where(a => a.GroupId == groupId 
                         && a.SubjectId == subjectId
                         && a.SchoolId == currentUserSchool.Id)  // ← Filtrar por escuela
                .ToListAsync();
        }
    }
}
