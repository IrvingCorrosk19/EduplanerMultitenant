using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

public static class ApplyDatabaseChanges
{
    public static async Task ApplyPrematriculationChangesAsync(SchoolDbContext context)
    {
        Console.WriteLine("🔍 Verificando y aplicando cambios en la base de datos...");

        try
        {
            // 1. Verificar y agregar campos a student_assignments
            await ExecuteIfColumnNotExists(context, "student_assignments", "is_active",
                "ALTER TABLE student_assignments ADD COLUMN is_active boolean NOT NULL DEFAULT true");

            await ExecuteIfColumnNotExists(context, "student_assignments", "end_date",
                "ALTER TABLE student_assignments ADD COLUMN end_date timestamp with time zone");

            // 2. Verificar y agregar campos de auditoría a prematriculations
            await ExecuteIfColumnNotExists(context, "prematriculations", "confirmed_by",
                "ALTER TABLE prematriculations ADD COLUMN confirmed_by uuid");

            await ExecuteIfColumnNotExists(context, "prematriculations", "rejected_by",
                "ALTER TABLE prematriculations ADD COLUMN rejected_by uuid");

            await ExecuteIfColumnNotExists(context, "prematriculations", "cancelled_by",
                "ALTER TABLE prematriculations ADD COLUMN cancelled_by uuid");

            // 3. Verificar y agregar required_amount a prematriculation_periods
            await ExecuteIfColumnNotExists(context, "prematriculation_periods", "required_amount",
                "ALTER TABLE prematriculation_periods ADD COLUMN required_amount numeric(18,2) NOT NULL DEFAULT 0");

            // 4. Crear tabla prematriculation_histories si no existe
            await CreateTableIfNotExists(context, "prematriculation_histories",
                @"CREATE TABLE IF NOT EXISTS prematriculation_histories (
                    id uuid NOT NULL DEFAULT uuid_generate_v4(),
                    prematriculation_id uuid NOT NULL,
                    previous_status character varying(20) NOT NULL,
                    new_status character varying(20) NOT NULL,
                    changed_by uuid,
                    reason text,
                    changed_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    additional_info text,
                    CONSTRAINT prematriculation_histories_pkey PRIMARY KEY (id),
                    CONSTRAINT prematriculation_histories_prematriculation_id_fkey 
                        FOREIGN KEY (prematriculation_id) REFERENCES prematriculations(id) ON DELETE CASCADE,
                    CONSTRAINT prematriculation_histories_changed_by_fkey 
                        FOREIGN KEY (changed_by) REFERENCES users(id) ON DELETE SET NULL
                )");

            // 5. Crear índices
            await ExecuteIfIndexNotExists(context, "IX_prematriculation_histories_prematriculation_id",
                "CREATE INDEX IF NOT EXISTS IX_prematriculation_histories_prematriculation_id ON prematriculation_histories(prematriculation_id)");

            await ExecuteIfIndexNotExists(context, "IX_prematriculation_histories_changed_at",
                "CREATE INDEX IF NOT EXISTS IX_prematriculation_histories_changed_at ON prematriculation_histories(changed_at)");

            await ExecuteIfIndexNotExists(context, "IX_prematriculation_histories_changed_by",
                "CREATE INDEX IF NOT EXISTS IX_prematriculation_histories_changed_by ON prematriculation_histories(changed_by)");

            await ExecuteIfIndexNotExists(context, "IX_prematriculations_confirmed_by",
                "CREATE INDEX IF NOT EXISTS IX_prematriculations_confirmed_by ON prematriculations(confirmed_by)");

            await ExecuteIfIndexNotExists(context, "IX_prematriculations_rejected_by",
                "CREATE INDEX IF NOT EXISTS IX_prematriculations_rejected_by ON prematriculations(rejected_by)");

            await ExecuteIfIndexNotExists(context, "IX_prematriculations_cancelled_by",
                "CREATE INDEX IF NOT EXISTS IX_prematriculations_cancelled_by ON prematriculations(cancelled_by)");

            // 6. Crear foreign keys
            await ExecuteIfForeignKeyNotExists(context, "prematriculations_confirmed_by_fkey",
                "ALTER TABLE prematriculations ADD CONSTRAINT prematriculations_confirmed_by_fkey FOREIGN KEY (confirmed_by) REFERENCES users(id) ON DELETE SET NULL");

            await ExecuteIfForeignKeyNotExists(context, "prematriculations_rejected_by_fkey",
                "ALTER TABLE prematriculations ADD CONSTRAINT prematriculations_rejected_by_fkey FOREIGN KEY (rejected_by) REFERENCES users(id) ON DELETE SET NULL");

            await ExecuteIfForeignKeyNotExists(context, "prematriculations_cancelled_by_fkey",
                "ALTER TABLE prematriculations ADD CONSTRAINT prematriculations_cancelled_by_fkey FOREIGN KEY (cancelled_by) REFERENCES users(id) ON DELETE SET NULL");

            Console.WriteLine("✅ Todos los cambios aplicados correctamente!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al aplicar cambios: {ex.Message}");
            throw;
        }
    }

    private static async Task<bool> ColumnExistsAsync(SchoolDbContext context, string tableName, string columnName)
    {
        var sql = "SELECT COUNT(*) FROM information_schema.columns WHERE table_name = {0} AND column_name = {1}";
        var result = await context.Database.SqlQueryRaw<int>(sql, tableName.ToLower(), columnName.ToLower()).ToListAsync();
        return result.FirstOrDefault() > 0;
    }

    private static async Task ExecuteIfColumnNotExists(SchoolDbContext context, string tableName, string columnName, string sql)
    {
        if (!await ColumnExistsAsync(context, tableName, columnName))
        {
            Console.WriteLine($"➕ Agregando columna {columnName} a {tableName}...");
            await context.Database.ExecuteSqlRawAsync(sql);
            Console.WriteLine($"✅ Columna {columnName} agregada");
        }
        else
        {
            Console.WriteLine($"✓ Columna {columnName} ya existe en {tableName}");
        }
    }

    private static async Task<bool> TableExistsAsync(SchoolDbContext context, string tableName)
    {
        var sql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = {0}";
        var result = await context.Database.SqlQueryRaw<int>(sql, tableName.ToLower()).ToListAsync();
        return result.FirstOrDefault() > 0;
    }

    private static async Task CreateTableIfNotExists(SchoolDbContext context, string tableName, string sql)
    {
        if (!await TableExistsAsync(context, tableName))
        {
            Console.WriteLine($"➕ Creando tabla {tableName}...");
            await context.Database.ExecuteSqlRawAsync(sql);
            Console.WriteLine($"✅ Tabla {tableName} creada");
        }
        else
        {
            Console.WriteLine($"✓ Tabla {tableName} ya existe");
        }
    }

    private static async Task<bool> IndexExistsAsync(SchoolDbContext context, string indexName)
    {
        var sql = "SELECT COUNT(*) FROM pg_indexes WHERE indexname = {0}";
        var result = await context.Database.SqlQueryRaw<int>(sql, indexName.ToLower()).ToListAsync();
        return result.FirstOrDefault() > 0;
    }

    private static async Task ExecuteIfIndexNotExists(SchoolDbContext context, string indexName, string sql)
    {
        if (!await IndexExistsAsync(context, indexName))
        {
            Console.WriteLine($"➕ Creando índice {indexName}...");
            await context.Database.ExecuteSqlRawAsync(sql);
            Console.WriteLine($"✅ Índice {indexName} creado");
        }
        else
        {
            Console.WriteLine($"✓ Índice {indexName} ya existe");
        }
    }

    private static async Task<bool> ForeignKeyExistsAsync(SchoolDbContext context, string constraintName)
    {
        var sql = "SELECT COUNT(*) FROM information_schema.table_constraints WHERE constraint_name = {0}";
        var result = await context.Database.SqlQueryRaw<int>(sql, constraintName.ToLower()).ToListAsync();
        return result.FirstOrDefault() > 0;
    }

    private static async Task ExecuteIfForeignKeyNotExists(SchoolDbContext context, string constraintName, string sql)
    {
        if (!await ForeignKeyExistsAsync(context, constraintName))
        {
            Console.WriteLine($"➕ Creando foreign key {constraintName}...");
            try
            {
                await context.Database.ExecuteSqlRawAsync(sql);
                Console.WriteLine($"✅ Foreign key {constraintName} creada");
            }
            catch (Exception ex)
            {
                // Si falla, puede ser que ya exista pero con otro nombre
                Console.WriteLine($"⚠️ No se pudo crear {constraintName}: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"✓ Foreign key {constraintName} ya existe");
        }
    }
}

