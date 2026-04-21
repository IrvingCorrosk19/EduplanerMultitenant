using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Script para agregar la columna shift a la tabla users si no existe
/// </summary>
public static class ApplyUserShiftColumn
{
    public static async Task ApplyAsync(SchoolDbContext context)
    {
        Console.WriteLine("🔍 Verificando y aplicando columna shift a users...");

        try
        {
            // Verificar si la columna existe
            var sql = "SELECT COUNT(*) FROM information_schema.columns WHERE table_name = {0} AND column_name = {1}";
            var result = await context.Database.SqlQueryRaw<int>(sql, "users", "shift").ToListAsync();
            var exists = result.FirstOrDefault() > 0;

            if (!exists)
            {
                Console.WriteLine("➕ Agregando columna shift a users...");
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE users 
                    ADD COLUMN shift character varying(20);
                ");
                Console.WriteLine("✅ Columna shift agregada a users");
            }
            else
            {
                Console.WriteLine("✓ Columna shift ya existe en users");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al aplicar cambios: {ex.Message}");
            throw;
        }
    }
}

