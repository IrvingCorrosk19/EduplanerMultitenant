using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

public static class ApplyAcademicYearChanges
{
    public static async Task ApplyAsync(SchoolDbContext context)
    {
        Console.WriteLine("🔍 Verificando y aplicando cambios de Año Académico...");

        try
        {
            // 1. Verificar y crear tabla academic_years
            await CreateTableIfNotExists(context, "academic_years", @"
                CREATE TABLE IF NOT EXISTS academic_years (
                    id uuid NOT NULL DEFAULT gen_random_uuid(),
                    school_id uuid NOT NULL,
                    name character varying(50) NOT NULL,
                    description text,
                    start_date timestamp with time zone NOT NULL,
                    end_date timestamp with time zone NOT NULL,
                    is_active boolean NOT NULL DEFAULT false,
                    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_at timestamp with time zone,
                    created_by uuid,
                    updated_by uuid,
                    CONSTRAINT academic_years_pkey PRIMARY KEY (id),
                    CONSTRAINT academic_years_school_id_fkey FOREIGN KEY (school_id) REFERENCES schools(id) ON DELETE CASCADE
                )
            ");

            // 2. Verificar y crear índices para academic_years
            await ExecuteIfIndexNotExists(context, "ix_academic_years_school_id",
                "CREATE INDEX IF NOT EXISTS IX_academic_years_school_id ON academic_years(school_id)");
            await ExecuteIfIndexNotExists(context, "ix_academic_years_is_active",
                "CREATE INDEX IF NOT EXISTS IX_academic_years_is_active ON academic_years(is_active)");
            await ExecuteIfIndexNotExists(context, "ix_academic_years_school_active",
                "CREATE INDEX IF NOT EXISTS IX_academic_years_school_active ON academic_years(school_id, is_active)");

            // 3. Verificar y agregar foreign keys para academic_years
            await ExecuteIfForeignKeyNotExists(context, "academic_years_created_by_fkey",
                "ALTER TABLE academic_years ADD CONSTRAINT academic_years_created_by_fkey FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE SET NULL");
            await ExecuteIfForeignKeyNotExists(context, "academic_years_updated_by_fkey",
                "ALTER TABLE academic_years ADD CONSTRAINT academic_years_updated_by_fkey FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE SET NULL");

            // 4. Verificar y agregar columna academic_year_id a trimesters
            await ExecuteIfColumnNotExists(context, "trimester", "academic_year_id",
                "ALTER TABLE trimester ADD COLUMN academic_year_id uuid");

            await ExecuteIfIndexNotExists(context, "ix_trimester_academic_year_id",
                "CREATE INDEX IF NOT EXISTS IX_trimester_academic_year_id ON trimester(academic_year_id)");

            await ExecuteIfForeignKeyNotExists(context, "trimester_academic_year_id_fkey",
                "ALTER TABLE trimester ADD CONSTRAINT trimester_academic_year_id_fkey FOREIGN KEY (academic_year_id) REFERENCES academic_years(id) ON DELETE SET NULL");

            // 5. Verificar y agregar columna academic_year_id a student_assignments
            await ExecuteIfColumnNotExists(context, "student_assignments", "academic_year_id",
                "ALTER TABLE student_assignments ADD COLUMN academic_year_id uuid");

            await ExecuteIfIndexNotExists(context, "ix_student_assignments_academic_year_id",
                "CREATE INDEX IF NOT EXISTS IX_student_assignments_academic_year_id ON student_assignments(academic_year_id)");
            await ExecuteIfIndexNotExists(context, "ix_student_assignments_student_active",
                "CREATE INDEX IF NOT EXISTS IX_student_assignments_student_active ON student_assignments(student_id, is_active)");
            await ExecuteIfIndexNotExists(context, "ix_student_assignments_student_academic_year",
                "CREATE INDEX IF NOT EXISTS IX_student_assignments_student_academic_year ON student_assignments(student_id, academic_year_id)");

            await ExecuteIfForeignKeyNotExists(context, "student_assignments_academic_year_id_fkey",
                "ALTER TABLE student_assignments ADD CONSTRAINT student_assignments_academic_year_id_fkey FOREIGN KEY (academic_year_id) REFERENCES academic_years(id) ON DELETE SET NULL");

            // 6. Verificar y agregar columna academic_year_id a student_activity_scores
            await ExecuteIfColumnNotExists(context, "student_activity_scores", "academic_year_id",
                "ALTER TABLE student_activity_scores ADD COLUMN academic_year_id uuid");

            await ExecuteIfIndexNotExists(context, "ix_student_activity_scores_academic_year_id",
                "CREATE INDEX IF NOT EXISTS IX_student_activity_scores_academic_year_id ON student_activity_scores(academic_year_id)");
            await ExecuteIfIndexNotExists(context, "ix_student_activity_scores_student_academic_year",
                "CREATE INDEX IF NOT EXISTS IX_student_activity_scores_student_academic_year ON student_activity_scores(student_id, academic_year_id)");

            await ExecuteIfForeignKeyNotExists(context, "student_activity_scores_academic_year_id_fkey",
                "ALTER TABLE student_activity_scores ADD CONSTRAINT student_activity_scores_academic_year_id_fkey FOREIGN KEY (academic_year_id) REFERENCES academic_years(id) ON DELETE SET NULL");

            Console.WriteLine("✅ Todos los cambios de Año Académico aplicados correctamente!");
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
            await context.Database.ExecuteSqlRawAsync(sql);
            Console.WriteLine($"✅ Foreign key {constraintName} creada");
        }
        else
        {
            Console.WriteLine($"✓ Foreign key {constraintName} ya existe");
        }
    }
}

