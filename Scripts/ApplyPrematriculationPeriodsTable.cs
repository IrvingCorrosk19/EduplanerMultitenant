using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Script para crear la tabla prematriculation_periods si no existe
/// </summary>
public static class ApplyPrematriculationPeriodsTable
{
    public static async Task ApplyAsync(SchoolDbContext context)
    {
        Console.WriteLine("🔍 Verificando y creando tabla prematriculation_periods...");

        try
        {
            // Verificar si la tabla existe
            var tableSql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = {0}";
            var tableResult = await context.Database.SqlQueryRaw<int>(tableSql, "prematriculation_periods").ToListAsync();
            var tableExists = tableResult.FirstOrDefault() > 0;

            if (!tableExists)
            {
                Console.WriteLine("➕ Creando tabla prematriculation_periods...");
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE prematriculation_periods (
                        id uuid NOT NULL DEFAULT gen_random_uuid(),
                        school_id uuid NOT NULL,
                        start_date timestamp with time zone NOT NULL,
                        end_date timestamp with time zone NOT NULL,
                        is_active boolean NOT NULL DEFAULT true,
                        max_capacity_per_group integer NOT NULL DEFAULT 30,
                        auto_assign_by_shift boolean NOT NULL DEFAULT true,
                        required_amount decimal(18,2) DEFAULT 0,
                        created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at timestamp with time zone,
                        created_by uuid,
                        updated_by uuid,
                        CONSTRAINT prematriculation_periods_pkey PRIMARY KEY (id)
                    );
                ");
                Console.WriteLine("✅ Tabla prematriculation_periods creada");
            }
            else
            {
                Console.WriteLine("✓ Tabla prematriculation_periods ya existe");
            }

            // Verificar y crear índices
            var index1Sql = "SELECT COUNT(*) FROM pg_indexes WHERE indexname = {0}";
            var index1Result = await context.Database.SqlQueryRaw<int>(index1Sql, "ix_prematriculation_periods_school_id").ToListAsync();
            if (index1Result.FirstOrDefault() == 0)
            {
                Console.WriteLine("➕ Creando índice IX_prematriculation_periods_school_id...");
                await context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_prematriculation_periods_school_id ON prematriculation_periods(school_id);");
                Console.WriteLine("✅ Índice creado");
            }

            var index2Result = await context.Database.SqlQueryRaw<int>(index1Sql, "ix_prematriculation_periods_dates").ToListAsync();
            if (index2Result.FirstOrDefault() == 0)
            {
                Console.WriteLine("➕ Creando índice IX_prematriculation_periods_dates...");
                await context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_prematriculation_periods_dates ON prematriculation_periods(school_id, start_date, end_date);");
                Console.WriteLine("✅ Índice creado");
            }

            // Verificar y agregar foreign keys
            var fkSql = "SELECT COUNT(*) FROM information_schema.table_constraints WHERE constraint_name = {0}";
            
            var fk1Result = await context.Database.SqlQueryRaw<int>(fkSql, "prematriculation_periods_school_id_fkey").ToListAsync();
            if (fk1Result.FirstOrDefault() == 0)
            {
                Console.WriteLine("➕ Agregando foreign key prematriculation_periods_school_id_fkey...");
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE prematriculation_periods 
                    ADD CONSTRAINT prematriculation_periods_school_id_fkey 
                    FOREIGN KEY (school_id) REFERENCES schools(id) ON DELETE CASCADE;
                ");
                Console.WriteLine("✅ Foreign key agregada");
            }

            var fk2Result = await context.Database.SqlQueryRaw<int>(fkSql, "prematriculation_periods_created_by_fkey").ToListAsync();
            if (fk2Result.FirstOrDefault() == 0)
            {
                Console.WriteLine("➕ Agregando foreign key prematriculation_periods_created_by_fkey...");
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE prematriculation_periods 
                    ADD CONSTRAINT prematriculation_periods_created_by_fkey 
                    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE SET NULL;
                ");
                Console.WriteLine("✅ Foreign key agregada");
            }

            var fk3Result = await context.Database.SqlQueryRaw<int>(fkSql, "prematriculation_periods_updated_by_fkey").ToListAsync();
            if (fk3Result.FirstOrDefault() == 0)
            {
                Console.WriteLine("➕ Agregando foreign key prematriculation_periods_updated_by_fkey...");
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE prematriculation_periods 
                    ADD CONSTRAINT prematriculation_periods_updated_by_fkey 
                    FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE SET NULL;
                ");
                Console.WriteLine("✅ Foreign key agregada");
            }

            Console.WriteLine("✅ Todos los cambios de prematriculation_periods aplicados correctamente!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al aplicar cambios: {ex.Message}");
            throw;
        }
    }
}

