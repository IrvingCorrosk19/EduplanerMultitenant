using Npgsql;

namespace SchoolManager.Scripts;

/// <summary>
/// Utility to query test data for smoke tests. Run via: dotnet run -- --smoke-query
/// </summary>
public static class SmokeQuery
{
    public static async Task RunAsync(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        Console.WriteLine("=== TEACHERS ===");
        await using (var cmd = new NpgsqlCommand(@"
            SELECT u.id, u.email, u.role, u.school_id, u.name, u.last_name
            FROM users u
            WHERE u.role IN ('teacher','docente','Teacher')
              AND u.status = 'active'
            LIMIT 5", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
                Console.WriteLine($"  ID={r[0]} EMAIL={r[1]} ROLE={r[2]} SCHOOL={r[3]} NAME={r[4]} {r[5]}");
        }

        Console.WriteLine("=== ADMINS ===");
        await using (var cmd2 = new NpgsqlCommand(@"
            SELECT u.id, u.email, u.role, u.school_id, u.name
            FROM users u
            WHERE u.role IN ('admin','superadmin','Admin','director')
              AND u.status = 'active'
            LIMIT 5", conn))
        await using (var r2 = await cmd2.ExecuteReaderAsync())
        {
            while (await r2.ReadAsync())
                Console.WriteLine($"  ID={r2[0]} EMAIL={r2[1]} ROLE={r2[2]} SCHOOL={r2[3]} NAME={r2[4]}");
        }

        Console.WriteLine("=== ACADEMIC YEARS ===");
        await using (var cmd3 = new NpgsqlCommand("SELECT id, school_id, name, is_active FROM academic_years LIMIT 5", conn))
        await using (var r3 = await cmd3.ExecuteReaderAsync())
        {
            while (await r3.ReadAsync())
                Console.WriteLine($"  ID={r3[0]} SCHOOL={r3[1]} NAME={r3[2]} ACTIVE={r3[3]}");
        }

        Console.WriteLine("=== TIME SLOTS ===");
        await using (var cmd4 = new NpgsqlCommand("SELECT id, school_id, name, is_active FROM time_slots WHERE is_active = true LIMIT 5", conn))
        await using (var r4 = await cmd4.ExecuteReaderAsync())
        {
            while (await r4.ReadAsync())
                Console.WriteLine($"  ID={r4[0]} SCHOOL={r4[1]} NAME={r4[2]} ACTIVE={r4[3]}");
        }

        Console.WriteLine("=== TEACHER ASSIGNMENTS ===");
        await using (var cmd5 = new NpgsqlCommand(@"
            SELECT ta.id, ta.teacher_id, u.email, sa.subject_id, s.name as subject, sa.group_id, g.name as grp
            FROM teacher_assignments ta
            JOIN users u ON u.id = ta.teacher_id
            JOIN subject_assignments sa ON sa.id = ta.subject_assignment_id
            LEFT JOIN subjects s ON s.id = sa.subject_id
            LEFT JOIN groups g ON g.id = sa.group_id
            LIMIT 10", conn))
        await using (var r5 = await cmd5.ExecuteReaderAsync())
        {
            while (await r5.ReadAsync())
                Console.WriteLine($"  TA_ID={r5[0]} TEACHER={r5[1]} EMAIL={r5[2]} SUBJECT={r5[4]} GROUP={r5[6]}");
        }

        Console.WriteLine("=== SCHEDULE ENTRIES ===");
        await using (var cmd6 = new NpgsqlCommand("SELECT id, teacher_assignment_id, time_slot_id, day_of_week, academic_year_id FROM schedule_entries LIMIT 5", conn))
        await using (var r6 = await cmd6.ExecuteReaderAsync())
        {
            while (await r6.ReadAsync())
                Console.WriteLine($"  ID={r6[0]} TA={r6[1]} TS={r6[2]} DAY={r6[3]} AY={r6[4]}");
        }
    }
}
