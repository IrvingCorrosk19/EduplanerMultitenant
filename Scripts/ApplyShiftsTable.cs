using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Script para crear la tabla shifts si no existe
/// </summary>
public static class ApplyShiftsTable
{
    public static async Task ApplyAsync(SchoolDbContext context)
    {
        Console.WriteLine("🔍 Verificando y creando tabla shifts...");

        try
        {
            // Verificar si la tabla existe
            var tableSql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = {0}";
            var tableResult = await context.Database.SqlQueryRaw<int>(tableSql, "shifts").ToListAsync();
            var tableExists = tableResult.FirstOrDefault() > 0;

            if (!tableExists)
            {
                Console.WriteLine("   ➕ Creando tabla shifts...");
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE shifts (
                        id uuid NOT NULL DEFAULT gen_random_uuid(),
                        school_id uuid NOT NULL,
                        name character varying(50) NOT NULL,
                        description text,
                        is_active boolean NOT NULL DEFAULT true,
                        display_order integer NOT NULL DEFAULT 0,
                        created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at timestamp with time zone,
                        created_by uuid,
                        updated_by uuid,
                        CONSTRAINT shifts_pkey PRIMARY KEY (id)
                    );
                ");
                Console.WriteLine("   ✅ Tabla shifts creada");
            }
            else
            {
                Console.WriteLine("   ✓ Tabla shifts ya existe");
                
                // Verificar y agregar columna display_order si no existe
                var displayOrderSql = "SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'shifts' AND column_name = 'display_order'";
                var displayOrderResult = await context.Database.SqlQueryRaw<int>(displayOrderSql).ToListAsync();
                var displayOrderExists = displayOrderResult.FirstOrDefault() > 0;

                if (!displayOrderExists)
                {
                    Console.WriteLine("   ➕ Agregando columna display_order a shifts...");
                    await context.Database.ExecuteSqlRawAsync(@"
                        ALTER TABLE shifts 
                        ADD COLUMN display_order integer NOT NULL DEFAULT 0;
                    ");
                    Console.WriteLine("   ✅ Columna display_order agregada");
                }
            }

            // Verificar y crear índices
            var indexSql = "SELECT COUNT(*) FROM pg_indexes WHERE indexname = {0}";
            
            var idx1 = await context.Database.SqlQueryRaw<int>(indexSql, "ix_shifts_school_id").ToListAsync();
            if (idx1.FirstOrDefault() == 0)
            {
                await context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_shifts_school_id ON shifts(school_id);");
                Console.WriteLine("   ✅ Índice IX_shifts_school_id creado");
            }

            var idx2 = await context.Database.SqlQueryRaw<int>(indexSql, "ix_shifts_is_active").ToListAsync();
            if (idx2.FirstOrDefault() == 0)
            {
                await context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_shifts_is_active ON shifts(is_active);");
                Console.WriteLine("   ✅ Índice IX_shifts_is_active creado");
            }

            // Verificar y agregar foreign keys
            var fkSql = "SELECT COUNT(*) FROM information_schema.table_constraints WHERE constraint_name = {0}";
            
            var fk1 = await context.Database.SqlQueryRaw<int>(fkSql, "shifts_school_id_fkey").ToListAsync();
            if (fk1.FirstOrDefault() == 0)
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE shifts 
                    ADD CONSTRAINT shifts_school_id_fkey 
                    FOREIGN KEY (school_id) REFERENCES schools(id) ON DELETE CASCADE;
                ");
                Console.WriteLine("   ✅ Foreign key shifts_school_id_fkey agregada");
            }

            var fk2 = await context.Database.SqlQueryRaw<int>(fkSql, "shifts_created_by_fkey").ToListAsync();
            if (fk2.FirstOrDefault() == 0)
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE shifts 
                    ADD CONSTRAINT shifts_created_by_fkey 
                    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE SET NULL;
                ");
                Console.WriteLine("   ✅ Foreign key shifts_created_by_fkey agregada");
            }

            var fk3 = await context.Database.SqlQueryRaw<int>(fkSql, "shifts_updated_by_fkey").ToListAsync();
            if (fk3.FirstOrDefault() == 0)
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE shifts 
                    ADD CONSTRAINT shifts_updated_by_fkey 
                    FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE SET NULL;
                ");
                Console.WriteLine("   ✅ Foreign key shifts_updated_by_fkey agregada");
            }

            // Ahora que shifts existe, agregar la foreign key en groups si no existe
            var fk4 = await context.Database.SqlQueryRaw<int>(fkSql, "groups_shift_id_fkey").ToListAsync();
            if (fk4.FirstOrDefault() == 0)
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE groups 
                    ADD CONSTRAINT groups_shift_id_fkey 
                    FOREIGN KEY (shift_id) REFERENCES shifts(id) ON DELETE SET NULL;
                ");
                Console.WriteLine("   ✅ Foreign key groups_shift_id_fkey agregada");
            }

            Console.WriteLine("✅ Todos los cambios de shifts aplicados correctamente!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al aplicar cambios: {ex.Message}");
            throw;
        }
    }
}

