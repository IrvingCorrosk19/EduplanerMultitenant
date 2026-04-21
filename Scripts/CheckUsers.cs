using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Consulta la tabla users: roles, conteos y estudiantes por school_id.
/// Ejecutar: dotnet run -- --check-users
/// </summary>
public static class CheckUsers
{
    public static async Task RunAsync(SchoolDbContext context)
    {
        var conn = context.Database.GetDbConnection();
        await conn.OpenAsync();

        try
        {
            Console.WriteLine("=== USERS - Conteo por rol ===\n");

            using var cmd = conn.CreateCommand();
            cmd.Connection = conn;

            cmd.CommandText = @"
SELECT role, COUNT(*) as cnt 
FROM users 
GROUP BY role 
ORDER BY role;";
            if (cmd is NpgsqlCommand npg) npg.Parameters.Clear();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    Console.WriteLine($"  role='{reader.GetString(0)}'  ->  {reader.GetInt64(1)} usuarios");
            }

            Console.WriteLine("\n=== ESTUDIANTES (role student/estudiante) por school_id ===\n");

            cmd.CommandText = @"
SELECT u.school_id, s.name as school_name, COUNT(*) as cnt
FROM users u
LEFT JOIN schools s ON s.id = u.school_id
WHERE LOWER(u.role) IN ('student', 'estudiante')
GROUP BY u.school_id, s.name
ORDER BY u.school_id NULLS LAST;";
            if (cmd is NpgsqlCommand npg2) npg2.Parameters.Clear();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (!reader.HasRows)
                    Console.WriteLine("  (ningún estudiante encontrado)");
                else
                    while (await reader.ReadAsync())
                    {
                        var schoolId = reader.IsDBNull(0) ? "NULL" : reader.GetGuid(0).ToString();
                        var schoolName = reader.IsDBNull(1) ? "(sin escuela)" : reader.GetString(1);
                        Console.WriteLine($"  school_id={schoolId}  |  {schoolName}  ->  {reader.GetInt64(2)} estudiantes");
                    }
            }

            Console.WriteLine("\n=== Muestra de 5 estudiantes (id, name, role, school_id) ===\n");

            cmd.CommandText = @"
SELECT id, name, last_name, role, school_id
FROM users
WHERE LOWER(role) IN ('student', 'estudiante')
LIMIT 5;";
            if (cmd is NpgsqlCommand npg3) npg3.Parameters.Clear();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (!reader.HasRows)
                    Console.WriteLine("  (ningún estudiante)");
                else
                    while (await reader.ReadAsync())
                    {
                        var schoolId = reader.IsDBNull(4) ? "NULL" : reader.GetGuid(4).ToString();
                        Console.WriteLine($"  {reader.GetGuid(0)}  |  {reader.GetString(1)} {reader.GetString(2)}  |  role='{reader.GetString(3)}'  |  school_id={schoolId}");
                    }
            }

            Console.WriteLine("\n=== Admin/Prof. Jaime Ramos - school_id ===\n");

            cmd.CommandText = @"
SELECT id, name, last_name, email, role, school_id
FROM users
WHERE LOWER(name) LIKE '%jaime%' AND LOWER(last_name) LIKE '%ramos%'
   OR role IN ('admin', 'superadmin', 'director')
ORDER BY role
LIMIT 10;";
            if (cmd is NpgsqlCommand npg4) npg4.Parameters.Clear();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (!reader.HasRows)
                    Console.WriteLine("  (ningún admin encontrado con ese nombre)");
                else
                    while (await reader.ReadAsync())
                    {
                        var schoolId = reader.IsDBNull(5) ? "NULL" : reader.GetGuid(5).ToString();
                        Console.WriteLine($"  {reader.GetString(1)} {reader.GetString(2)}  |  role='{reader.GetString(4)}'  |  school_id={schoolId}");
                    }
            }

            Console.WriteLine("\n=== Fin ===\n");
        }
        finally
        {
            await conn.CloseAsync();
        }
    }
}
