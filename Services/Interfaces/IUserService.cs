using SchoolManager.Models;

public interface IUserService
{
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Login y flujos anónimos: ignora filtros de tenant y resuelve email+escuela o detecta ambigüedad.
    /// Solo usuarios activos con escuela activa (o SuperAdmin sin escuela).
    /// </summary>
    Task<User?> GetByEmailForLoginAsync(string email, Guid? schoolId);

    /// <summary>
    /// Escuelas activas asociadas a un correo activo. Usado para el selector condicional del Login.
    /// </summary>
    Task<IReadOnlyList<SchoolInfo>> GetLoginSchoolsByEmailAsync(string email);

    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByIdWithRelationsAsync(Guid id);
    Task CreateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds, List<Guid> gradeLevelIds);
    Task CreateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds);
    Task UpdateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds, List<Guid> gradeLevelIds);
    Task UpdateAsync(User user, List<Guid> subjectIds, List<Guid> groupIds);
    Task<List<User>> GetAllStudentsAsync();
    Task<List<User>> GetAllWithAssignmentsByRoleAsync(string role);
    Task<List<User>> GetAllWithAssignmentsByRoleSA(string role);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
    Task<User?> AuthenticateAsync(string email, string password);
    Task<List<User>> GetAllTeachersAsync();
    Task<(bool success, string message)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<User?> GetByRoleAndSchoolAsync(string role, Guid schoolId);
}
