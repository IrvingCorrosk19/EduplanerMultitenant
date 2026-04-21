using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SchoolManager.Models;
using Npgsql;

namespace SchoolManager.Scripts;

/// <summary>
/// Consulta el estado real de la base de datos (tablas, columnas, migraciones)
/// para no especular. Ejecutar: dotnet run -- --check-db
/// </summary>
public static class CheckDatabaseState
{
    public static async Task RunAsync(SchoolDbContext context)
    {
        var conn = context.Database.GetDbConnection();
        await conn.OpenAsync();

        try
        {
            Console.WriteLine("=== Estado real de la base de datos (information_schema + __EFMigrationsHistory) ===\n");

            using var cmd = conn.CreateCommand();
            cmd.Connection = conn;

            // 1) Tablas que nos interesan: ¿existen?
            var tablesToCheck = new[] { "schools", "school_id_card_settings", "id_card_template_fields", "groups", "__EFMigrationsHistory" };
            Console.WriteLine("1. Tablas en public:");
            foreach (var table in tablesToCheck)
            {
                cmd.CommandText = @"
SELECT EXISTS (
  SELECT 1 FROM information_schema.tables 
  WHERE table_schema = 'public' AND table_name = $1
);";
                if (cmd is NpgsqlCommand npg)
                {
                    npg.Parameters.Clear();
                    npg.Parameters.AddWithValue(table);
                }
                var exists = (bool)(await cmd.ExecuteScalarAsync() ?? false);
                Console.WriteLine($"   {table}: {(exists ? "SÍ" : "NO")}");
            }

            // 2) Columnas de la tabla schools (para ver si tiene shift_id)
            Console.WriteLine("\n2. Columnas de la tabla 'schools':");
            cmd.CommandText = @"
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_schema = 'public' AND table_name = 'schools' 
ORDER BY ordinal_position;";
            if (cmd is NpgsqlCommand npg2)
                npg2.Parameters.Clear();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (!reader.HasRows)
                    Console.WriteLine("   (tabla no existe o sin columnas)");
                else
                    while (await reader.ReadAsync())
                        Console.WriteLine($"   {reader.GetString(0),-30} {reader.GetString(1)}");
            }

            // 3) Migraciones aplicadas (__EFMigrationsHistory)
            Console.WriteLine("\n3. Migraciones registradas en __EFMigrationsHistory:");
            cmd.CommandText = @"SELECT ""MigrationId"" FROM ""__EFMigrationsHistory"" ORDER BY ""MigrationId"";";
            if (cmd is NpgsqlCommand npg3)
                npg3.Parameters.Clear();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (!reader.HasRows)
                    Console.WriteLine("   (ninguna o tabla no existe)");
                else
                    while (await reader.ReadAsync())
                        Console.WriteLine($"   {reader.GetString(0)}");
            }

            // 4) Columna shift_id en groups (debería estar)
            Console.WriteLine("\n4. ¿Columna 'shift_id' en tabla 'groups'?");
            cmd.CommandText = @"
SELECT EXISTS (
  SELECT 1 FROM information_schema.columns 
  WHERE table_schema = 'public' AND table_name = 'groups' AND column_name = 'shift_id'
);";
            if (cmd is NpgsqlCommand npg4)
                npg4.Parameters.Clear();
            var groupsHasShiftId = (bool)(await cmd.ExecuteScalarAsync() ?? false);
            Console.WriteLine($"   {(groupsHasShiftId ? "SÍ" : "NO")}");

            Console.WriteLine("\n=== Fin del reporte ===\n");
        }
        finally
        {
            await conn.CloseAsync();
        }
    }
}
