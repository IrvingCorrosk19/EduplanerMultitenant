using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Asegura que el constraint users_role_check incluya 'secretaria' y el resto de roles permitidos.
/// Se ejecuta al arranque para que funcione en cualquier BD (local o producción).
/// </summary>
public static class EnsureUsersRoleCheck
{
    public static async Task EnsureAsync(SchoolDbContext context)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE public.users DROP CONSTRAINT IF EXISTS users_role_check;");
            await context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE public.users 
ADD CONSTRAINT users_role_check 
CHECK (role::text = ANY (ARRAY[
    'superadmin'::text, 
    'admin'::text, 
    'director'::text, 
    'teacher'::text, 
    'parent'::text, 
    'student'::text, 
    'estudiante'::text, 
    'acudiente'::text, 
    'contable'::text, 
    'contabilidad'::text,
    'secretaria'::text,
    'clubparentsadmin'::text,
    'qlservices'::text,
    'inspector'::text
]));");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EnsureUsersRoleCheck] {ex.Message}");
        }
    }
}
