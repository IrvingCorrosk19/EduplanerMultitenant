using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Script completo para aplicar todas las migraciones faltantes relacionadas con matrícula y prematrícula
/// </summary>
public static class ApplyCompletePrematriculationModule
{
    public static async Task ApplyAsync(SchoolDbContext context)
    {
        Console.WriteLine("═══════════════════════════════════════════════════");
        Console.WriteLine("   APLICANDO MÓDULO COMPLETO DE MATRÍCULA/PREMATRÍCULA");
        Console.WriteLine("═══════════════════════════════════════════════════\n");

        try
        {
            // 1. Agregar foreign keys faltantes en prematriculations
            Console.WriteLine("🔧 Paso 1: Agregando foreign keys faltantes en prematriculations...");
            await ApplyPrematriculationsForeignKeys(context);
            
            // 2. Completar prematriculation_histories (índices y foreign keys)
            Console.WriteLine("🔧 Paso 2: Completando prematriculation_histories...");
            await CompletePrematriculationHistories(context);
            
            // 3. Crear tabla payments si no existe
            Console.WriteLine("🔧 Paso 3: Creando tabla payments...");
            await ApplyPaymentsTable(context);
            
            // 4. Crear tabla payment_concepts si no existe
            Console.WriteLine("🔧 Paso 4: Creando tabla payment_concepts...");
            await ApplyPaymentConceptsTable(context);
            
            Console.WriteLine("\n═══════════════════════════════════════════════════");
            Console.WriteLine("✅ MÓDULO COMPLETO APLICADO EXITOSAMENTE");
            Console.WriteLine("═══════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ ERROR: {ex.Message}");
            throw;
        }
    }

    private static async Task ApplyPrematriculationsForeignKeys(SchoolDbContext context)
    {
        var fkSql = "SELECT COUNT(*) FROM information_schema.table_constraints WHERE constraint_name = {0}";
        
        // parent_id_fkey
        var fk1 = await context.Database.SqlQueryRaw<int>(fkSql, "prematriculations_parent_id_fkey").ToListAsync();
        if (fk1.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE prematriculations 
                ADD CONSTRAINT prematriculations_parent_id_fkey 
                FOREIGN KEY (parent_id) REFERENCES users(id) ON DELETE SET NULL;
            ");
            Console.WriteLine("   ✅ Foreign key prematriculations_parent_id_fkey agregada");
        }

        // grade_id_fkey
        var fk2 = await context.Database.SqlQueryRaw<int>(fkSql, "prematriculations_grade_id_fkey").ToListAsync();
        if (fk2.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE prematriculations 
                ADD CONSTRAINT prematriculations_grade_id_fkey 
                FOREIGN KEY (grade_id) REFERENCES grade_levels(id) ON DELETE SET NULL;
            ");
            Console.WriteLine("   ✅ Foreign key prematriculations_grade_id_fkey agregada");
        }

        // group_id_fkey
        var fk3 = await context.Database.SqlQueryRaw<int>(fkSql, "prematriculations_group_id_fkey").ToListAsync();
        if (fk3.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE prematriculations 
                ADD CONSTRAINT prematriculations_group_id_fkey 
                FOREIGN KEY (group_id) REFERENCES groups(id) ON DELETE SET NULL;
            ");
            Console.WriteLine("   ✅ Foreign key prematriculations_group_id_fkey agregada");
        }

        // confirmed_by_fkey
        var fk4 = await context.Database.SqlQueryRaw<int>(fkSql, "prematriculations_confirmed_by_fkey").ToListAsync();
        if (fk4.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE prematriculations 
                ADD CONSTRAINT prematriculations_confirmed_by_fkey 
                FOREIGN KEY (confirmed_by) REFERENCES users(id) ON DELETE SET NULL;
            ");
            Console.WriteLine("   ✅ Foreign key prematriculations_confirmed_by_fkey agregada");
        }

        // rejected_by_fkey
        var fk5 = await context.Database.SqlQueryRaw<int>(fkSql, "prematriculations_rejected_by_fkey").ToListAsync();
        if (fk5.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE prematriculations 
                ADD CONSTRAINT prematriculations_rejected_by_fkey 
                FOREIGN KEY (rejected_by) REFERENCES users(id) ON DELETE SET NULL;
            ");
            Console.WriteLine("   ✅ Foreign key prematriculations_rejected_by_fkey agregada");
        }

        // cancelled_by_fkey
        var fk6 = await context.Database.SqlQueryRaw<int>(fkSql, "prematriculations_cancelled_by_fkey").ToListAsync();
        if (fk6.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE prematriculations 
                ADD CONSTRAINT prematriculations_cancelled_by_fkey 
                FOREIGN KEY (cancelled_by) REFERENCES users(id) ON DELETE SET NULL;
            ");
            Console.WriteLine("   ✅ Foreign key prematriculations_cancelled_by_fkey agregada");
        }

        // Crear índices para confirmed_by, rejected_by, cancelled_by si no existen
        var indexSql = "SELECT COUNT(*) FROM pg_indexes WHERE indexname = {0}";
        
        var idx1 = await context.Database.SqlQueryRaw<int>(indexSql, "ix_prematriculations_confirmed_by").ToListAsync();
        if (idx1.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_prematriculations_confirmed_by ON prematriculations(confirmed_by);");
            Console.WriteLine("   ✅ Índice IX_prematriculations_confirmed_by creado");
        }

        var idx2 = await context.Database.SqlQueryRaw<int>(indexSql, "ix_prematriculations_rejected_by").ToListAsync();
        if (idx2.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_prematriculations_rejected_by ON prematriculations(rejected_by);");
            Console.WriteLine("   ✅ Índice IX_prematriculations_rejected_by creado");
        }

        var idx3 = await context.Database.SqlQueryRaw<int>(indexSql, "ix_prematriculations_cancelled_by").ToListAsync();
        if (idx3.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_prematriculations_cancelled_by ON prematriculations(cancelled_by);");
            Console.WriteLine("   ✅ Índice IX_prematriculations_cancelled_by creado");
        }
    }

    private static async Task CompletePrematriculationHistories(SchoolDbContext context)
    {
        var indexSql = "SELECT COUNT(*) FROM pg_indexes WHERE indexname = {0}";
        
        // Índice en prematriculation_id
        var idx1 = await context.Database.SqlQueryRaw<int>(indexSql, "ix_prematriculation_histories_prematriculation_id").ToListAsync();
        if (idx1.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_prematriculation_histories_prematriculation_id ON prematriculation_histories(prematriculation_id);");
            Console.WriteLine("   ✅ Índice IX_prematriculation_histories_prematriculation_id creado");
        }

        // Índice en changed_at
        var idx2 = await context.Database.SqlQueryRaw<int>(indexSql, "ix_prematriculation_histories_changed_at").ToListAsync();
        if (idx2.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_prematriculation_histories_changed_at ON prematriculation_histories(changed_at);");
            Console.WriteLine("   ✅ Índice IX_prematriculation_histories_changed_at creado");
        }

        // Índice en changed_by
        var idx3 = await context.Database.SqlQueryRaw<int>(indexSql, "ix_prematriculation_histories_changed_by").ToListAsync();
        if (idx3.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_prematriculation_histories_changed_by ON prematriculation_histories(changed_by);");
            Console.WriteLine("   ✅ Índice IX_prematriculation_histories_changed_by creado");
        }

        // Foreign key a prematriculations
        var fkSql = "SELECT COUNT(*) FROM information_schema.table_constraints WHERE constraint_name = {0}";
        var fk1 = await context.Database.SqlQueryRaw<int>(fkSql, "prematriculation_histories_prematriculation_id_fkey").ToListAsync();
        if (fk1.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE prematriculation_histories 
                ADD CONSTRAINT prematriculation_histories_prematriculation_id_fkey 
                FOREIGN KEY (prematriculation_id) REFERENCES prematriculations(id) ON DELETE CASCADE;
            ");
            Console.WriteLine("   ✅ Foreign key prematriculation_histories_prematriculation_id_fkey agregada");
        }

        // Foreign key a users (changed_by)
        var fk2 = await context.Database.SqlQueryRaw<int>(fkSql, "prematriculation_histories_changed_by_fkey").ToListAsync();
        if (fk2.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE prematriculation_histories 
                ADD CONSTRAINT prematriculation_histories_changed_by_fkey 
                FOREIGN KEY (changed_by) REFERENCES users(id) ON DELETE SET NULL;
            ");
            Console.WriteLine("   ✅ Foreign key prematriculation_histories_changed_by_fkey agregada");
        }
    }

    private static async Task ApplyPaymentsTable(SchoolDbContext context)
    {
        var tableSql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = {0}";
        var tableResult = await context.Database.SqlQueryRaw<int>(tableSql, "payments").ToListAsync();
        var tableExists = tableResult.FirstOrDefault() > 0;

        if (!tableExists)
        {
            Console.WriteLine("   ➕ Creando tabla payments...");
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE payments (
                    id uuid NOT NULL DEFAULT gen_random_uuid(),
                    school_id uuid NOT NULL,
                    prematriculation_id uuid NOT NULL,
                    registered_by uuid,
                    amount decimal(18,2) NOT NULL,
                    payment_date timestamp with time zone NOT NULL,
                    receipt_number character varying(100) NOT NULL,
                    payment_status character varying(20) NOT NULL DEFAULT 'Pendiente',
                    payment_method character varying(50),
                    receipt_image text,
                    payment_concept_id uuid,
                    student_id uuid,
                    notes text,
                    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_at timestamp with time zone,
                    confirmed_at timestamp with time zone,
                    CONSTRAINT payments_pkey PRIMARY KEY (id)
                );
            ");
            Console.WriteLine("   ✅ Tabla payments creada");

            // Crear índices
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX IX_payments_school_id ON payments(school_id);
                CREATE INDEX IX_payments_prematriculation_id ON payments(prematriculation_id);
                CREATE UNIQUE INDEX IX_payments_receipt_number ON payments(receipt_number);
            ");
            Console.WriteLine("   ✅ Índices de payments creados");

            // Foreign keys
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE payments 
                ADD CONSTRAINT payments_school_id_fkey 
                FOREIGN KEY (school_id) REFERENCES schools(id) ON DELETE CASCADE;

                ALTER TABLE payments 
                ADD CONSTRAINT payments_prematriculation_id_fkey 
                FOREIGN KEY (prematriculation_id) REFERENCES prematriculations(id) ON DELETE RESTRICT;

                ALTER TABLE payments 
                ADD CONSTRAINT payments_registered_by_fkey 
                FOREIGN KEY (registered_by) REFERENCES users(id) ON DELETE SET NULL;
            ");
            Console.WriteLine("   ✅ Foreign keys de payments creadas");
        }
        else
        {
            Console.WriteLine("   ✓ Tabla payments ya existe");
            
            // Verificar y agregar columnas faltantes
            await AddPaymentsColumnsIfMissing(context);
        }
    }

    private static async Task AddPaymentsColumnsIfMissing(SchoolDbContext context)
    {
        var columnSql = "SELECT COUNT(*) FROM information_schema.columns WHERE table_name = {0} AND column_name = {1}";
        
        // Verificar payment_concept_id
        var col1 = await context.Database.SqlQueryRaw<int>(columnSql, "payments", "payment_concept_id").ToListAsync();
        if (col1.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE payments ADD COLUMN payment_concept_id uuid;");
            Console.WriteLine("   ✅ Columna payment_concept_id agregada");
        }

        // Verificar student_id
        var col2 = await context.Database.SqlQueryRaw<int>(columnSql, "payments", "student_id").ToListAsync();
        if (col2.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE payments ADD COLUMN student_id uuid;");
            Console.WriteLine("   ✅ Columna student_id agregada");
        }

        // Foreign keys faltantes
        var fkSql = "SELECT COUNT(*) FROM information_schema.table_constraints WHERE constraint_name = {0}";
        
        var fk1 = await context.Database.SqlQueryRaw<int>(fkSql, "payments_payment_concept_id_fkey").ToListAsync();
        if (fk1.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE payments 
                ADD CONSTRAINT payments_payment_concept_id_fkey 
                FOREIGN KEY (payment_concept_id) REFERENCES payment_concepts(id) ON DELETE SET NULL;
            ");
            Console.WriteLine("   ✅ Foreign key payments_payment_concept_id_fkey agregada");
        }

        var fk2 = await context.Database.SqlQueryRaw<int>(fkSql, "payments_student_id_fkey").ToListAsync();
        if (fk2.FirstOrDefault() == 0)
        {
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE payments 
                ADD CONSTRAINT payments_student_id_fkey 
                FOREIGN KEY (student_id) REFERENCES users(id) ON DELETE SET NULL;
            ");
            Console.WriteLine("   ✅ Foreign key payments_student_id_fkey agregada");
        }
    }

    private static async Task ApplyPaymentConceptsTable(SchoolDbContext context)
    {
        var tableSql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = {0}";
        var tableResult = await context.Database.SqlQueryRaw<int>(tableSql, "payment_concepts").ToListAsync();
        var tableExists = tableResult.FirstOrDefault() > 0;

        if (!tableExists)
        {
            Console.WriteLine("   ➕ Creando tabla payment_concepts...");
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE payment_concepts (
                    id uuid NOT NULL DEFAULT gen_random_uuid(),
                    school_id uuid NOT NULL,
                    name character varying(100) NOT NULL,
                    description text,
                    amount decimal(18,2) NOT NULL,
                    periodicity character varying(50),
                    is_active boolean NOT NULL DEFAULT true,
                    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_at timestamp with time zone,
                    created_by uuid,
                    updated_by uuid,
                    CONSTRAINT payment_concepts_pkey PRIMARY KEY (id)
                );
            ");
            Console.WriteLine("   ✅ Tabla payment_concepts creada");

            // Crear índice
            await context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_payment_concepts_school_id ON payment_concepts(school_id);");
            Console.WriteLine("   ✅ Índice de payment_concepts creado");

            // Foreign keys
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE payment_concepts 
                ADD CONSTRAINT payment_concepts_school_id_fkey 
                FOREIGN KEY (school_id) REFERENCES schools(id) ON DELETE CASCADE;

                ALTER TABLE payment_concepts 
                ADD CONSTRAINT payment_concepts_created_by_fkey 
                FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE SET NULL;

                ALTER TABLE payment_concepts 
                ADD CONSTRAINT payment_concepts_updated_by_fkey 
                FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE SET NULL;
            ");
            Console.WriteLine("   ✅ Foreign keys de payment_concepts creadas");
        }
        else
        {
            Console.WriteLine("   ✓ Tabla payment_concepts ya existe");
        }
    }
}

