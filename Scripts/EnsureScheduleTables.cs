using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Crea las tablas del módulo de horarios (time_slots, schedule_entries) si no existen.
/// Útil cuando la BD se gestiona con scripts y no con EF migrations.
/// </summary>
public static class EnsureScheduleTables
{
    public static async Task EnsureAsync(SchoolDbContext context)
    {
        try
        {
            var exists = await context.Database
                .SqlQueryRaw<int>(
                    @"SELECT 1 FROM information_schema.tables 
                      WHERE table_schema = 'public' AND table_name = 'time_slots' 
                      LIMIT 1")
                .ToListAsync();

            if (exists.Count > 0)
                return;

            Console.WriteLine("[EnsureScheduleTables] Creando time_slots y schedule_entries...");
            await context.Database.ExecuteSqlRawAsync(@"
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";

CREATE TABLE time_slots (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    school_id uuid NOT NULL,
    shift_id uuid NULL,
    name character varying(50) NOT NULL,
    start_time time NOT NULL,
    end_time time NOT NULL,
    display_order integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT time_slots_pkey PRIMARY KEY (id),
    CONSTRAINT time_slots_school_id_fkey FOREIGN KEY (school_id)
        REFERENCES schools (id) ON DELETE CASCADE,
    CONSTRAINT time_slots_shift_id_fkey FOREIGN KEY (shift_id)
        REFERENCES shifts (id) ON DELETE SET NULL
);

CREATE INDEX IX_time_slots_school_id ON time_slots (school_id);
CREATE INDEX IX_time_slots_shift_id ON time_slots (shift_id);

CREATE TABLE schedule_entries (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    teacher_assignment_id uuid NOT NULL,
    time_slot_id uuid NOT NULL,
    day_of_week smallint NOT NULL,
    academic_year_id uuid NOT NULL,
    created_at timestamp with time zone NULL DEFAULT CURRENT_TIMESTAMP,
    created_by uuid NULL,
    CONSTRAINT schedule_entries_pkey PRIMARY KEY (id),
    CONSTRAINT schedule_entries_academic_year_id_fkey FOREIGN KEY (academic_year_id)
        REFERENCES academic_years (id),
    CONSTRAINT schedule_entries_created_by_fkey FOREIGN KEY (created_by)
        REFERENCES users (id) ON DELETE SET NULL,
    CONSTRAINT schedule_entries_teacher_assignment_id_fkey FOREIGN KEY (teacher_assignment_id)
        REFERENCES teacher_assignments (id),
    CONSTRAINT schedule_entries_time_slot_id_fkey FOREIGN KEY (time_slot_id)
        REFERENCES time_slots (id)
);

CREATE INDEX IX_schedule_entries_academic_year_id ON schedule_entries (academic_year_id);
CREATE INDEX IX_schedule_entries_created_by ON schedule_entries (created_by);
CREATE INDEX IX_schedule_entries_teacher_assignment_id ON schedule_entries (teacher_assignment_id);
CREATE INDEX IX_schedule_entries_time_slot_id ON schedule_entries (time_slot_id);
CREATE UNIQUE INDEX IX_schedule_entries_unique_slot ON schedule_entries (teacher_assignment_id, academic_year_id, time_slot_id, day_of_week);
");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EnsureScheduleTables] Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[EnsureScheduleTables] Error: {ex.Message}");
        }
    }
}
