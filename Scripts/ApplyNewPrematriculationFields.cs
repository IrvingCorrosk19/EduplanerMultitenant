using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

public class ApplyNewPrematriculationFields
{
    private readonly SchoolDbContext _context;

    public ApplyNewPrematriculationFields(SchoolDbContext context)
    {
        _context = context;
    }

    public async Task ApplyChangesAsync()
    {
        Console.WriteLine("🔍 Verificando estructura de la base de datos...");

        // 1. Verificar y agregar campos a student_assignments
        await CheckAndAddColumn("student_assignments", "is_active", 
            "ALTER TABLE student_assignments ADD COLUMN is_active boolean NOT NULL DEFAULT true");
        
        await CheckAndAddColumn("student_assignments", "end_date", 
            "ALTER TABLE student_assignments ADD COLUMN end_date timestamp with time zone");

        // 2. Verificar y agregar campos de auditoría a prematriculations
        await CheckAndAddColumn("prematriculations", "confirmed_by", 
            "ALTER TABLE prematriculations ADD COLUMN confirmed_by uuid");
        
        await CheckAndAddColumn("prematriculations", "rejected_by", 
            "ALTER TABLE prematriculations ADD COLUMN rejected_by uuid");
        
        await CheckAndAddColumn("prematriculations", "cancelled_by", 
            "ALTER TABLE prematriculations ADD COLUMN cancelled_by uuid");

        // 3. Verificar y agregar required_amount a prematriculation_periods
        await CheckAndAddColumn("prematriculation_periods", "required_amount", 
            "ALTER TABLE prematriculation_periods ADD COLUMN required_amount numeric(18,2) NOT NULL DEFAULT 0");

        // 4. Crear tabla prematriculation_histories si no existe
        await CreatePrematriculationHistoriesTable();

        // 5. Crear índices
        await CreateIndexesIfNotExist();

        // 6. Crear foreign keys
        await CreateForeignKeysIfNotExist();

        Console.WriteLine("✅ Todos los cambios aplicados correctamente!");
    }

    private async Task<bool> ColumnExistsAsync(string tableName, string columnName)
    {
        var sql = @"
            SELECT COUNT(*) 
            FROM information_schema.columns 
            WHERE table_name = @p0 AND column_name = @p1";
        
        var result = await _context.Database.SqlQueryRaw<int>(
            sql, tableName.ToLower(), columnName.ToLower()).ToListAsync();
        
        return result.FirstOrDefault() > 0;
    }

    private async Task CheckAndAddColumn(string tableName, string columnName, string sql)
    {
        if (!await ColumnExistsAsync(tableName, columnName))
        {
            Console.WriteLine($"➕ Agregando columna {columnName} a {tableName}...");
            await _context.Database.ExecuteSqlRawAsync(sql);
            Console.WriteLine($"✅ Columna {columnName} agregada a {tableName}");
        }
        else
        {
            Console.WriteLine($"✓ Columna {columnName} ya existe en {tableName}");
        }
    }

    private async Task<bool> TableExistsAsync(string tableName)
    {
        var sql = @"
            SELECT COUNT(*) 
            FROM information_schema.tables 
            WHERE table_name = @p0";
        
        var result = await _context.Database.SqlQueryRaw<int>(
            sql, tableName.ToLower()).ToListAsync();
        
        return result.FirstOrDefault() > 0;
    }

    private async Task CreatePrematriculationHistoriesTable()
    {
        if (!await TableExistsAsync("prematriculation_histories"))
        {
            Console.WriteLine("➕ Creando tabla prematriculation_histories...");
            
            var sql = @"
                CREATE TABLE prematriculation_histories (
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
                )";
            
            await _context.Database.ExecuteSqlRawAsync(sql);
            Console.WriteLine("✅ Tabla prematriculation_histories creada");
        }
        else
        {
            Console.WriteLine("✓ Tabla prematriculation_histories ya existe");
        }
    }

    private async Task<bool> IndexExistsAsync(string indexName)
    {
        var sql = @"
            SELECT COUNT(*) 
            FROM pg_indexes 
            WHERE indexname = @p0";
        
        var result = await _context.Database.SqlQueryRaw<int>(
            sql, indexName.ToLower()).ToListAsync();
        
        return result.FirstOrDefault() > 0;
    }

    private async Task CreateIndexesIfNotExist()
    {
        var indexes = new[]
        {
            ("IX_prematriculation_histories_prematriculation_id", 
             "CREATE INDEX IF NOT EXISTS IX_prematriculation_histories_prematriculation_id ON prematriculation_histories(prematriculation_id)"),
            ("IX_prematriculation_histories_changed_at", 
             "CREATE INDEX IF NOT EXISTS IX_prematriculation_histories_changed_at ON prematriculation_histories(changed_at)"),
            ("IX_prematriculation_histories_changed_by", 
             "CREATE INDEX IF NOT EXISTS IX_prematriculation_histories_changed_by ON prematriculation_histories(changed_by)"),
            ("IX_prematriculations_confirmed_by", 
             "CREATE INDEX IF NOT EXISTS IX_prematriculations_confirmed_by ON prematriculations(confirmed_by)"),
            ("IX_prematriculations_rejected_by", 
             "CREATE INDEX IF NOT EXISTS IX_prematriculations_rejected_by ON prematriculations(rejected_by)"),
            ("IX_prematriculations_cancelled_by", 
             "CREATE INDEX IF NOT EXISTS IX_prematriculations_cancelled_by ON prematriculations(cancelled_by)")
        };

        foreach (var (indexName, sql) in indexes)
        {
            if (!await IndexExistsAsync(indexName))
            {
                Console.WriteLine($"➕ Creando índice {indexName}...");
                await _context.Database.ExecuteSqlRawAsync(sql);
                Console.WriteLine($"✅ Índice {indexName} creado");
            }
            else
            {
                Console.WriteLine($"✓ Índice {indexName} ya existe");
            }
        }
    }

    private async Task<bool> ForeignKeyExistsAsync(string constraintName)
    {
        var sql = @"
            SELECT COUNT(*) 
            FROM information_schema.table_constraints 
            WHERE constraint_name = @p0";
        
        var result = await _context.Database.SqlQueryRaw<int>(
            sql, constraintName.ToLower()).ToListAsync();
        
        return result.FirstOrDefault() > 0;
    }

    private async Task CreateForeignKeysIfNotExist()
    {
        var foreignKeys = new[]
        {
            ("prematriculations_confirmed_by_fkey",
             "ALTER TABLE prematriculations ADD CONSTRAINT prematriculations_confirmed_by_fkey FOREIGN KEY (confirmed_by) REFERENCES users(id) ON DELETE SET NULL"),
            ("prematriculations_rejected_by_fkey",
             "ALTER TABLE prematriculations ADD CONSTRAINT prematriculations_rejected_by_fkey FOREIGN KEY (rejected_by) REFERENCES users(id) ON DELETE SET NULL"),
            ("prematriculations_cancelled_by_fkey",
             "ALTER TABLE prematriculations ADD CONSTRAINT prematriculations_cancelled_by_fkey FOREIGN KEY (cancelled_by) REFERENCES users(id) ON DELETE SET NULL")
        };

        foreach (var (constraintName, sql) in foreignKeys)
        {
            if (!await ForeignKeyExistsAsync(constraintName))
            {
                Console.WriteLine($"➕ Creando foreign key {constraintName}...");
                await _context.Database.ExecuteSqlRawAsync(sql);
                Console.WriteLine($"✅ Foreign key {constraintName} creada");
            }
            else
            {
                Console.WriteLine($"✓ Foreign key {constraintName} ya existe");
            }
        }
    }
}

