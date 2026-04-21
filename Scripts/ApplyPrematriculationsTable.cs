using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Script para crear la tabla prematriculations si no existe
/// </summary>
public static class ApplyPrematriculationsTable
{
    public static async Task ApplyAsync(SchoolDbContext context)
    {
        Console.WriteLine("🔍 Verificando y creando tabla prematriculations...");

        try
        {
            // Verificar si la tabla existe
            var tableSql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = {0}";
            var tableResult = await context.Database.SqlQueryRaw<int>(tableSql, "prematriculations").ToListAsync();
            var tableExists = tableResult.FirstOrDefault() > 0;

            if (!tableExists)
            {
                Console.WriteLine("➕ Creando tabla prematriculations...");
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE prematriculations (
                        id uuid NOT NULL DEFAULT gen_random_uuid(),
                        school_id uuid NOT NULL,
                        student_id uuid NOT NULL,
                        parent_id uuid,
                        grade_id uuid,
                        group_id uuid,
                        prematriculation_period_id uuid NOT NULL,
                        status character varying(20) NOT NULL DEFAULT 'Pendiente',
                        failed_subjects_count integer,
                        academic_condition_valid boolean,
                        rejection_reason text,
                        prematriculation_code character varying(50),
                        created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at timestamp with time zone,
                        payment_date timestamp with time zone,
                        matriculation_date timestamp with time zone,
                        confirmed_by uuid,
                        rejected_by uuid,
                        cancelled_by uuid,
                        CONSTRAINT prematriculations_pkey PRIMARY KEY (id)
                    );
                ");
                Console.WriteLine("✅ Tabla prematriculations creada");
            }
            else
            {
                Console.WriteLine("✓ Tabla prematriculations ya existe");
            }

            // Verificar y crear índices
            var indexSql = "SELECT COUNT(*) FROM pg_indexes WHERE indexname = {0}";
            
            var index1Result = await context.Database.SqlQueryRaw<int>(indexSql, "ix_prematriculations_school_id").ToListAsync();
            if (index1Result.FirstOrDefault() == 0)
            {
                Console.WriteLine("➕ Creando índice IX_prematriculations_school_id...");
                await context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_prematriculations_school_id ON prematriculations(school_id);");
                Console.WriteLine("✅ Índice creado");
            }

            var index2Result = await context.Database.SqlQueryRaw<int>(indexSql, "ix_prematriculations_student_id").ToListAsync();
            if (index2Result.FirstOrDefault() == 0)
            {
                Console.WriteLine("➕ Creando índice IX_prematriculations_student_id...");
                await context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_prematriculations_student_id ON prematriculations(student_id);");
                Console.WriteLine("✅ Índice creado");
            }

            var index3Result = await context.Database.SqlQueryRaw<int>(indexSql, "ix_prematriculations_period_id").ToListAsync();
            if (index3Result.FirstOrDefault() == 0)
            {
                Console.WriteLine("➕ Creando índice IX_prematriculations_period_id...");
                await context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_prematriculations_period_id ON prematriculations(prematriculation_period_id);");
                Console.WriteLine("✅ Índice creado");
            }

            var index4Result = await context.Database.SqlQueryRaw<int>(indexSql, "ix_prematriculations_code").ToListAsync();
            if (index4Result.FirstOrDefault() == 0)
            {
                Console.WriteLine("➕ Creando índice único IX_prematriculations_code...");
                await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_prematriculations_code ON prematriculations(prematriculation_code) WHERE prematriculation_code IS NOT NULL;");
                Console.WriteLine("✅ Índice único creado");
            }

            // Verificar y agregar foreign keys
            var fkSql = "SELECT COUNT(*) FROM information_schema.table_constraints WHERE constraint_name = {0}";
            
            var fk1Result = await context.Database.SqlQueryRaw<int>(fkSql, "prematriculations_school_id_fkey").ToListAsync();
            if (fk1Result.FirstOrDefault() == 0)
            {
                Console.WriteLine("➕ Agregando foreign key prematriculations_school_id_fkey...");
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE prematriculations 
                    ADD CONSTRAINT prematriculations_school_id_fkey 
                    FOREIGN KEY (school_id) REFERENCES schools(id) ON DELETE CASCADE;
                ");
                Console.WriteLine("✅ Foreign key agregada");
            }

            var fk2Result = await context.Database.SqlQueryRaw<int>(fkSql, "prematriculations_student_id_fkey").ToListAsync();
            if (fk2Result.FirstOrDefault() == 0)
            {
                Console.WriteLine("➕ Agregando foreign key prematriculations_student_id_fkey...");
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE prematriculations 
                    ADD CONSTRAINT prematriculations_student_id_fkey 
                    FOREIGN KEY (student_id) REFERENCES users(id) ON DELETE RESTRICT;
                ");
                Console.WriteLine("✅ Foreign key agregada");
            }

            var fk3Result = await context.Database.SqlQueryRaw<int>(fkSql, "prematriculations_period_id_fkey").ToListAsync();
            if (fk3Result.FirstOrDefault() == 0)
            {
                Console.WriteLine("➕ Agregando foreign key prematriculations_period_id_fkey...");
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE prematriculations 
                    ADD CONSTRAINT prematriculations_period_id_fkey 
                    FOREIGN KEY (prematriculation_period_id) REFERENCES prematriculation_periods(id) ON DELETE RESTRICT;
                ");
                Console.WriteLine("✅ Foreign key agregada");
            }

            Console.WriteLine("✅ Todos los cambios de prematriculations aplicados correctamente!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al aplicar cambios: {ex.Message}");
            throw;
        }
    }
}

