using SchoolManager.Models;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Enums;
using BCrypt.Net;
using SchoolManager.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolManager.Services.Implementations
{
public class UserService : IUserService
{
    private readonly SchoolDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UserService> _logger;
    private readonly IAuditLogService _auditLog;

    public UserService(SchoolDbContext context, ICurrentUserService currentUserService, ILogger<UserService> logger, IAuditLogService auditLog)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
        _auditLog = auditLog;
    }

    private async Task WriteAuditAsync(string action, string resource, string details)
    {
        try
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
            await _auditLog.LogActionAsync(new SchoolManager.Models.AuditLog
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                UserId = user?.Id,
                UserName = user != null ? $"{user.Name} {user.LastName}" : null,
                UserRole = user?.Role,
                Action = action,
                Resource = resource,
                Details = details,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo escribir audit log: {Action} {Resource}", action, resource);
        }
    }

    public async Task<List<User>> GetAllStudentsAsync()
    {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null || currentUser.SchoolId == null)
                return new List<User>();

        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Role.ToLower() == "student" || u.Role.ToLower() == "estudiante")
                .Where(u => u.SchoolId == currentUser.SchoolId)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task UpdateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds)
    {
        try
        {
            _logger.LogInformation("[UserService] UpdateAsync - UserId: {UserId}, Subjects: {SubjectCount}, Groups: {GroupCount}",
                user.Id, subjectIds?.Count ?? 0, groupIds?.Count ?? 0);

            _logger.LogDebug("[UserService] Actualizando usuario en contexto...");
            
            // En lugar de usar Update() que puede causar problemas con claves únicas,
            // vamos a actualizar solo los campos específicos
            // Usar Where() en lugar de FindAsync() para que el Global Query Filter de tenant se aplique
            var existingUser = await _context.Users
                .Include(u => u.Subjects)
                .Include(u => u.Groups)
                .Where(u => u.Id == user.Id)
                .FirstOrDefaultAsync();
            if (existingUser != null)
            {
                existingUser.Name = user.Name;
                existingUser.LastName = user.LastName;
                existingUser.Email = user.Email;
                existingUser.DocumentId = user.DocumentId;
                existingUser.Role = user.Role;
                existingUser.Status = user.Status;
                existingUser.DateOfBirth = user.DateOfBirth;
                existingUser.CellphonePrimary = user.CellphonePrimary;
                existingUser.CellphoneSecondary = user.CellphoneSecondary;
                existingUser.Disciplina = user.Disciplina;
                existingUser.Inclusion = user.Inclusion;
                existingUser.Orientacion = user.Orientacion;
                existingUser.Inclusivo = user.Inclusivo;
                // El formulario Edit no envía PasswordHash; no sobrescribir con null.
                if (!string.IsNullOrWhiteSpace(user.PasswordHash))
                    existingUser.PasswordHash = user.PasswordHash;
                existingUser.UpdatedAt = DateTime.UtcNow;
                
                // Actualizar Subjects
                existingUser.Subjects.Clear();
                if (subjectIds.Any())
                {
                    var subjectsQuery = _context.Subjects.Where(s => subjectIds.Contains(s.Id));
                    if (existingUser.SchoolId.HasValue)
                        subjectsQuery = subjectsQuery.Where(s => s.SchoolId == existingUser.SchoolId);
                    var subjects = await subjectsQuery.ToListAsync();
                    foreach (var subject in subjects)
                    {
                        existingUser.Subjects.Add(subject);
                    }
                }

                // Actualizar Groups
                existingUser.Groups.Clear();
                if (groupIds.Any())
                {
                    var groupsQuery = _context.Groups.Where(g => groupIds.Contains(g.Id));
                    if (existingUser.SchoolId.HasValue)
                        groupsQuery = groupsQuery.Where(g => g.SchoolId == existingUser.SchoolId);
                    var groups = await groupsQuery.ToListAsync();
                    foreach (var group in groups)
                    {
                        existingUser.Groups.Add(group);
                    }
                }
            }
            
            _logger.LogDebug("[UserService] Guardando cambios en base de datos...");
            await _context.SaveChangesAsync();

            _logger.LogInformation("[UserService] Usuario actualizado exitosamente: {UserId}", user.Id);
            await WriteAuditAsync("USUARIO_EDITADO", "Usuario", $"UserId: {user.Id}, Rol: {user.Role}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserService] Error en UpdateAsync para usuario: {UserId}", user.Id);
            throw;
        }
    }
    public async Task UpdateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds, List<Guid> gradeLevelIds)
    {
        // Actualizar Subjects
        user.Subjects.Clear();
        if (subjectIds.Any())
        {
            var subjectsQuery = _context.Subjects.Where(s => subjectIds.Contains(s.Id));
            if (user.SchoolId.HasValue)
                subjectsQuery = subjectsQuery.Where(s => s.SchoolId == user.SchoolId);
            var subjects = await subjectsQuery.ToListAsync();
            foreach (var subject in subjects)
            {
                user.Subjects.Add(subject);
            }
        }


        // Actualizar Groups
        user.Groups.Clear();
        if (groupIds.Any())
        {
            var groupsQuery = _context.Groups.Where(g => groupIds.Contains(g.Id));
            if (user.SchoolId.HasValue)
                groupsQuery = groupsQuery.Where(g => g.SchoolId == user.SchoolId);
            var groups = await groupsQuery.ToListAsync();
            foreach (var group in groups)
            {
                user.Groups.Add(group);
            }
        }

        // Actualizar GradeLevels
        user.Grades.Clear();
        if (gradeLevelIds.Any())
        {
            var gradesQuery = _context.GradeLevels.Where(g => gradeLevelIds.Contains(g.Id));
            if (user.SchoolId.HasValue)
                gradesQuery = gradesQuery.Where(g => g.SchoolId == user.SchoolId);
            var grades = await gradesQuery.ToListAsync();
            foreach (var grade in grades)
            {
                user.Grades.Add(grade);
            }
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
    public async Task<List<User>> GetAllTeachersAsync()
    {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null || currentUser.SchoolId == null)
                return new List<User>();

        return await _context.Users
            .Where(u => u.Role == "teacher")
                .Where(u => u.SchoolId == currentUser.SchoolId)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }
    public async Task CreateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds)
    {
        try
        {
                var currentUser = await _currentUserService.GetCurrentUserAsync();
                if (currentUser == null || currentUser.SchoolId == null)
                    throw new InvalidOperationException("No se puede crear el usuario porque no hay un usuario actual o no tiene un colegio asignado.");

                // Asignar el SchoolId del usuario actual
                user.SchoolId = currentUser.SchoolId;

            // La contraseña ya viene hasheada desde el controlador, no necesitamos hashearla de nuevo
            
            // Cargar las entidades completas desde la base de datos
            var schoolId = currentUser.SchoolId!.Value;
            var subjects = await _context.Subjects
                .Where(s => subjectIds.Contains(s.Id) && s.SchoolId == schoolId)
                .ToListAsync();
            var groups = await _context.Groups
                .Where(g => groupIds.Contains(g.Id) && g.SchoolId == schoolId)
                .ToListAsync();

            // Asignar las relaciones
            user.Subjects = subjects;
            user.Groups = groups;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await WriteAuditAsync("USUARIO_CREADO", "Usuario", $"Email: {user.Email}, Rol: {user.Role}");
        }
        catch (Exception ex)
        {
            throw new Exception("Error al crear el usuario y asignar relaciones.", ex);
        }
    }
    public async Task<User?> GetByIdWithRelationsAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[UserService] GetByIdWithRelations - UserId: {UserId}", id);

            var user = await _context.Users
                .Include(u => u.Subjects)
                .Include(u => u.Groups)
                .Include(u => u.Grades)
                .Include(u => u.SchoolNavigation)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user != null)
            {
                _logger.LogDebug("[UserService] Usuario encontrado: {UserId}, Subjects: {SubjectCount}, Groups: {GroupCount}",
                    id, user.Subjects?.Count ?? 0, user.Groups?.Count ?? 0);
            }
            else
            {
                _logger.LogWarning("[UserService] Usuario no encontrado: {UserId}", id);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserService] Error en GetByIdWithRelations para usuario: {UserId}", id);
            throw;
        }
    }

        public async Task<List<User>> GetAllAsync()
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null || currentUser.SchoolId == null)
                return new List<User>();

            return await _context.Users
                .Where(u => u.SchoolId == currentUser.SchoolId)
                .ToListAsync();
        }

    public async Task<List<User>> GetAllWithAssignmentsByRoleAsync(string role)
    {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null || currentUser.SchoolId == null)
                return new List<User>();

        return await _context.Users
            .Where(u => u.Role == role)
                .Where(u => u.SchoolId == currentUser.SchoolId)
            .Include(u => u.TeacherAssignments)
                .ThenInclude(ta => ta.SubjectAssignment)
                    .ThenInclude(sa => sa.Subject)
            .Include(u => u.TeacherAssignments)
                .ThenInclude(ta => ta.SubjectAssignment)
                    .ThenInclude(sa => sa.Group)
            .Include(u => u.TeacherAssignments)
                .ThenInclude(ta => ta.SubjectAssignment)
                    .ThenInclude(sa => sa.GradeLevel)
            .Include(u => u.TeacherAssignments)
                .ThenInclude(ta => ta.SubjectAssignment)
                    .ThenInclude(sa => sa.Area)
            .Include(u => u.TeacherAssignments)
                .ThenInclude(ta => ta.SubjectAssignment)
                    .ThenInclude(sa => sa.Specialty)
            .ToListAsync();
    }

        public async Task<List<User>> GetAllWithAssignmentsByRoleSA(string role)
        {
            return await _context.Users
                .Where(u => u.Role == role)                   
                .Include(u => u.TeacherAssignments)
                    .ThenInclude(ta => ta.SubjectAssignment)
                        .ThenInclude(sa => sa.Subject)
                .Include(u => u.TeacherAssignments)
                    .ThenInclude(ta => ta.SubjectAssignment)
                        .ThenInclude(sa => sa.Group)
                .Include(u => u.TeacherAssignments)
                    .ThenInclude(ta => ta.SubjectAssignment)
                        .ThenInclude(sa => sa.GradeLevel)
                .Include(u => u.TeacherAssignments)
                    .ThenInclude(ta => ta.SubjectAssignment)
                        .ThenInclude(sa => sa.Area)
                .Include(u => u.TeacherAssignments)
                    .ThenInclude(ta => ta.SubjectAssignment)
                        .ThenInclude(sa => sa.Specialty)
                .ToListAsync();
        }
        public async Task<User?> GetByIdAsync(Guid id)
        {
            // Where() aplica el Global Query Filter de tenant automáticamente (FindAsync lo bypasaba)
            return await _context.Users.Where(u => u.Id == id).FirstOrDefaultAsync();
        }

    public async Task CreateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds, List<Guid> gradeLevelIds)
    {
        try
        {
                var currentUser = await _currentUserService.GetCurrentUserAsync();
                if (currentUser == null || currentUser.SchoolId == null)
                    throw new InvalidOperationException("No se puede crear el usuario porque no hay un usuario actual o no tiene un colegio asignado.");

                // Asignar el SchoolId del usuario actual
                user.SchoolId = currentUser.SchoolId;

            // La contraseña ya viene hasheada desde el controlador, no necesitamos hashearla de nuevo
            
            var schoolId = currentUser.SchoolId!.Value;
            var subjects = await _context.Subjects
                .Where(s => subjectIds.Contains(s.Id) && s.SchoolId == schoolId)
                .ToListAsync();
            var groups = await _context.Groups
                .Where(g => groupIds.Contains(g.Id) && g.SchoolId == schoolId)
                .ToListAsync();
            var grades = await _context.GradeLevels
                .Where(g => gradeLevelIds.Contains(g.Id) && g.SchoolId == schoolId)
                .ToListAsync();

            user.Subjects = subjects;
            user.Groups = groups;
            user.Grades = grades;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await WriteAuditAsync("USUARIO_CREADO", "Usuario", $"Email: {user.Email}, Rol: {user.Role}");
        }
        catch (Exception ex)
        {
            throw new Exception("Error al crear el usuario y asignar relaciones.", ex);
        }
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }


public async Task DeleteAsync(Guid id)
{
        await using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        var schoolId = await _currentUserService.GetCurrentSchoolIdAsync();
        var user = await _context.Users
            .Include(u => u.Subjects)
            .Include(u => u.Groups)
            .Include(u => u.Grades)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null || user.SchoolId != schoolId)
            throw new InvalidOperationException($"No se encontró el usuario con ID: {id}");

        // Validar el rol usando enum
        if (!Enum.TryParse<UserRole>(user.Role, true, out var parsedRole))
            throw new InvalidOperationException($"Rol no válido o no soportado: {user.Role}");

        switch (parsedRole)
        {
            case UserRole.Student:
            case UserRole.Estudiante:
                // MEJORADO: Inactivar asignaciones en lugar de eliminarlas (preserva historial)
                var studentAssignments = await _context.StudentAssignments
                    .Where(sa => sa.StudentId == id && sa.IsActive)
                    .ToListAsync();
                
                if (studentAssignments.Any())
                {
                    foreach (var assignment in studentAssignments)
                    {
                        assignment.IsActive = false;
                        assignment.EndDate = DateTime.UtcNow;
                    }
                    _context.StudentAssignments.UpdateRange(studentAssignments);
                }
                break;

            case UserRole.Teacher:
                var teacherAssignments = await _context.TeacherAssignments
                    .Where(ta => ta.TeacherId == id)
                    .ToListAsync();
                _context.TeacherAssignments.RemoveRange(teacherAssignments);
                break;

            case UserRole.Admin:
            case UserRole.Director:
            case UserRole.Secretaria:
                // No asignaciones específicas
                break;

            default:
                throw new InvalidOperationException($"Rol no manejado: {parsedRole}");
        }

        // Limpieza de relaciones M:M
        //user.Subjects.Clear();
        //user.Groups.Clear();
        //user.Grades.Clear();

        await _context.SaveChangesAsync();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();
        await WriteAuditAsync("USUARIO_ELIMINADO", "Usuario", $"UserId: {id}");
    }
    catch (DbUpdateException dbEx)
    {
        await transaction.RollbackAsync();
        throw new Exception("No se puede eliminar el usuario porque tiene dependencias en otras entidades.", dbEx);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        throw new Exception("Error inesperado al eliminar el usuario.", ex);
    }
    }

    public async Task<User?> GetByRoleAndSchoolAsync(string role, Guid schoolId)
    {
        try
        {
            return await _context.Users
                .Where(u => u.Role.ToLower() == role.ToLower() && u.SchoolId == schoolId && u.Status == "active")
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuario por rol {Role} y escuela {SchoolId}", role, schoolId);
            return null;
        }
    }
//public async Task DeleteAsync(Guid id)
//{
//    try
//    {
//        var user = await _context.Users
//            .Include(u => u.Subjects)
//            .Include(u => u.Groups)
//            .Include(u => u.Grades)
//            .FirstOrDefaultAsync(u => u.Id == id);

//        if (user == null)
//            throw new InvalidOperationException($"No se encontró el usuario con ID: {id}");

//        // Eliminar relaciones explícitas
//        //user.Subjects.Clear();
//        //user.Groups.Clear();
//        //user.Grades.Clear();

//        var role = user.Role.ToLower();


//        // Eliminar asignaciones de profesor
//        var assignments = await _context.TeacherAssignments
//            .Where(ta => ta.TeacherId == id)
//            .ToListAsync();

//        _context.TeacherAssignments.RemoveRange(assignments);

//        await _context.SaveChangesAsync();

//        // Eliminar el usuario
//        _context.Users.Remove(user);
//        await _context.SaveChangesAsync();
//    }
//    catch (DbUpdateException dbEx)
//    {
//        throw new Exception("No se puede eliminar el usuario porque tiene dependencias en otras entidades (como asignaciones de docentes).", dbEx);
//    }
//    catch (Exception ex)
//    {
//        throw new Exception("Error inesperado al eliminar el usuario.", ex);
//    }
//}


public async Task<User?> AuthenticateAsync(string email, string password)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == password);
    }
    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower().Trim() == email.ToLower().Trim());
    }

    public async Task<User?> GetByEmailForLoginAsync(string email, Guid? schoolId)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalized = email.Trim().ToLowerInvariant();

        // Solo cuentas activas; escuela activa o SuperAdmin (SchoolId null).
        // Tracking ON: AuthService actualiza LastLogin / rehash de password.
        var query = _context.Users
            .IgnoreQueryFilters()
            .Where(u =>
                u.Email != null &&
                u.Email.ToLower().Trim() == normalized &&
                u.Status != null &&
                u.Status.ToLower() == "active" &&
                (u.SchoolId == null || _context.Schools.IgnoreQueryFilters()
                    .Any(s => s.Id == u.SchoolId && s.IsActive)));

        if (schoolId.HasValue && schoolId.Value != Guid.Empty)
        {
            return await query.FirstOrDefaultAsync(u => u.SchoolId == schoolId);
        }

        var matches = await query.ToListAsync();
        if (matches.Count > 1)
            return null; // Ambigüedad: el cliente debe enviar SchoolId
        return matches.FirstOrDefault();
    }

    public async Task<IReadOnlyList<SchoolInfo>> GetLoginSchoolsByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Array.Empty<SchoolInfo>();

        var normalized = email.Trim().ToLowerInvariant();

        // Una sola consulta: usuarios activos del correo + escuelas activas.
        // SuperAdmin (SchoolId null) no entra al selector de institución.
        var schools = await (
            from u in _context.Users.AsNoTracking().IgnoreQueryFilters()
            where u.Email != null
                  && u.Email.ToLower().Trim() == normalized
                  && u.Status != null
                  && u.Status.ToLower() == "active"
                  && u.SchoolId != null
            join s in _context.Schools.AsNoTracking().IgnoreQueryFilters()
                on u.SchoolId equals s.Id
            where s.IsActive
            orderby s.Name
            select new SchoolInfo
            {
                Id = s.Id,
                Name = s.Name,
                LogoUrl = s.LogoUrl,
                Address = s.Address,
                IsActive = s.IsActive
            })
            .Distinct()
            .ToListAsync();

        return schools;
    }

    public async Task<(bool success, string message)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        _logger.LogInformation("[UserService.ChangePasswordAsync] Iniciando para userId: {UserId}", userId);

        try
        {
            // Where() aplica GQF de tenant (FindAsync lo bypasaba — IDOR potencial)
            var user = await _context.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("[UserService.ChangePasswordAsync] Usuario no encontrado: {UserId}", userId);
                return (false, "Usuario no encontrado");
            }

            _logger.LogDebug("[UserService.ChangePasswordAsync] IsPasswordHashed: {IsHashed}", IsPasswordHashed(user.PasswordHash));

            // Verificar contraseña actual
            bool currentPasswordValid = false;
            if (IsPasswordHashed(user.PasswordHash))
            {
                _logger.LogDebug("[UserService.ChangePasswordAsync] Verificando contraseña hasheada con BCrypt");
                currentPasswordValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);
            }
            else
            {
                _logger.LogDebug("[UserService.ChangePasswordAsync] Verificando contraseña en texto plano");
                currentPasswordValid = currentPassword == user.PasswordHash;
            }

            if (!currentPasswordValid)
            {
                _logger.LogWarning("[UserService.ChangePasswordAsync] Contraseña actual incorrecta para userId: {UserId}", userId);
                return (false, "La contraseña actual que ingresaste no es correcta. Por favor, verifica e intenta nuevamente.");
            }

            // Validar nueva contraseña
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                _logger.LogWarning("[UserService.ChangePasswordAsync] Nueva contraseña inválida (longitud insuficiente) para userId: {UserId}", userId);
                return (false, "La nueva contraseña debe tener al menos 8 caracteres");
            }

            // Verificar que la nueva contraseña sea diferente a la actual
            if (currentPassword == newPassword)
            {
                _logger.LogWarning("[UserService.ChangePasswordAsync] Nueva contraseña igual a la actual para userId: {UserId}", userId);
                return (false, "La nueva contraseña debe ser diferente a la actual");
            }

            _logger.LogDebug("[UserService.ChangePasswordAsync] Hasheando nueva contraseña...");
            // Hashear y actualizar contraseña
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("[UserService.ChangePasswordAsync] Contraseña actualizada exitosamente para userId: {UserId}", userId);
            await WriteAuditAsync("CONTRASENA_CAMBIADA", "Usuario", $"UserId: {userId}");
            return (true, "Contraseña actualizada exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserService.ChangePasswordAsync] Error inesperado para userId: {UserId}", userId);
            return (false, $"Error inesperado al cambiar la contraseña: {ex.Message}");
        }
    }

    private bool IsPasswordHashed(string passwordHash)
    {
        // BCrypt hashes start with $2a$, $2b$, or $2y$
        return passwordHash.StartsWith("$2") && passwordHash.Length > 20;
    }
}
}
