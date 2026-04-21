using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Script para agregar las columnas faltantes a la tabla groups si no existen
/// </summary>
public static class ApplyGroupsColumns
{
    public static async Task ApplyAsync(SchoolDbContext context)
    {
        Console.WriteLine("🔍 Verificando y aplicando columnas faltantes a groups...");

        try
        {
            // Verificar y agregar max_capacity
            var maxCapacitySql = "SELECT COUNT(*) FROM information_schema.columns WHERE table_name = {0} AND column_name = {1}";
            var maxCapacityResult = await context.Database.SqlQueryRaw<int>(maxCapacitySql, "groups", "max_capacity").ToListAsync();
            var maxCapacityExists = maxCapacityResult.FirstOrDefault() > 0;

            if (!maxCapacityExists)
            {
                Console.WriteLine("➕ Agregando columna max_capacity a groups...");
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE groups ADD COLUMN max_capacity integer;");
                Console.WriteLine("✅ Columna max_capacity agregada");
            }
            else
            {
                Console.WriteLine("✓ Columna max_capacity ya existe en groups");
            }

            // Verificar y agregar shift
            var shiftResult = await context.Database.SqlQueryRaw<int>(maxCapacitySql, "groups", "shift").ToListAsync();
            var shiftExists = shiftResult.FirstOrDefault() > 0;

            if (!shiftExists)
            {
                Console.WriteLine("➕ Agregando columna shift a groups...");
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE groups ADD COLUMN shift character varying(20);");
                Console.WriteLine("✅ Columna shift agregada");
            }
            else
            {
                Console.WriteLine("✓ Columna shift ya existe en groups");
            }

            // Verificar y agregar shift_id
            var shiftIdResult = await context.Database.SqlQueryRaw<int>(maxCapacitySql, "groups", "shift_id").ToListAsync();
            var shiftIdExists = shiftIdResult.FirstOrDefault() > 0;

            if (!shiftIdExists)
            {
                Console.WriteLine("➕ Agregando columna shift_id a groups...");
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE groups ADD COLUMN shift_id uuid;");
                Console.WriteLine("✅ Columna shift_id agregada");
            }
            else
            {
                Console.WriteLine("✓ Columna shift_id ya existe en groups");
            }

            // Verificar y agregar foreign key para shift_id (solo si la tabla shifts existe)
            var shiftsTableSql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = {0}";
            var shiftsTableResult = await context.Database.SqlQueryRaw<int>(shiftsTableSql, "shifts").ToListAsync();
            var shiftsTableExists = shiftsTableResult.FirstOrDefault() > 0;

            if (shiftsTableExists)
            {
                var fkSql = "SELECT COUNT(*) FROM information_schema.table_constraints WHERE constraint_name = {0}";
                var fkResult = await context.Database.SqlQueryRaw<int>(fkSql, "groups_shift_id_fkey").ToListAsync();
                var fkExists = fkResult.FirstOrDefault() > 0;

                if (!fkExists)
                {
                    Console.WriteLine("➕ Agregando foreign key groups_shift_id_fkey...");
                    await context.Database.ExecuteSqlRawAsync(@"
                        ALTER TABLE groups 
                        ADD CONSTRAINT groups_shift_id_fkey 
                        FOREIGN KEY (shift_id) REFERENCES shifts(id) ON DELETE SET NULL;
                    ");
                    Console.WriteLine("✅ Foreign key groups_shift_id_fkey agregada");
                }
                else
                {
                    Console.WriteLine("✓ Foreign key groups_shift_id_fkey ya existe");
                }
            }
            else
            {
                Console.WriteLine("⚠️  Tabla shifts no existe aún, la foreign key se creará cuando shifts sea creada");
            }

            Console.WriteLine("✅ Todos los cambios de groups aplicados correctamente!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al aplicar cambios: {ex.Message}");
            throw;
        }
    }
}

