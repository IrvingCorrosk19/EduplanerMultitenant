using SchoolManager.Models;

namespace SchoolManager.Helpers;

/// <summary>Filtros traducibles a SQL sin <c>ToLower()</c> para permitir uso de índice en <c>users.role</c>.</summary>
public static class StudentRoleFilter
{
    public static IQueryable<User> WhereIsStudent(IQueryable<User> query) =>
        query.Where(u => u.Role != null && (
            u.Role == "student" || u.Role == "Student" ||
            u.Role == "estudiante" || u.Role == "Estudiante" ||
            u.Role == "alumno" || u.Role == "Alumno"));
}
