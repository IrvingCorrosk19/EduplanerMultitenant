using Microsoft.EntityFrameworkCore;

namespace SchoolManager.Scripts;

/// <summary>
/// Crea las tablas teacher_work_plans y teacher_work_plan_details.
/// Ejecutar con: dotnet run -- --apply-teacher-work-plan-tables
/// </summary>
public static class ApplyTeacherWorkPlanTables
{
    public static async Task RunAsync(DbContext context)
    {
        var sql = @"
CREATE TABLE IF NOT EXISTS teacher_work_plans (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    teacher_id uuid NOT NULL,
    subject_id uuid NOT NULL,
    grade_level_id uuid NOT NULL,
    group_id uuid NOT NULL,
    academic_year_id uuid NOT NULL,
    trimester integer NOT NULL,
    objectives text,
    status character varying(20) NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp with time zone,
    school_id uuid,
    CONSTRAINT teacher_work_plans_pkey PRIMARY KEY (id),
    CONSTRAINT teacher_work_plans_teacher_id_fkey FOREIGN KEY (teacher_id) REFERENCES users (id) ON DELETE RESTRICT,
    CONSTRAINT teacher_work_plans_subject_id_fkey FOREIGN KEY (subject_id) REFERENCES subjects (id) ON DELETE RESTRICT,
    CONSTRAINT teacher_work_plans_grade_level_id_fkey FOREIGN KEY (grade_level_id) REFERENCES grade_levels (id) ON DELETE RESTRICT,
    CONSTRAINT teacher_work_plans_group_id_fkey FOREIGN KEY (group_id) REFERENCES groups (id) ON DELETE RESTRICT,
    CONSTRAINT teacher_work_plans_academic_year_id_fkey FOREIGN KEY (academic_year_id) REFERENCES academic_years (id) ON DELETE RESTRICT,
    CONSTRAINT teacher_work_plans_school_id_fkey FOREIGN KEY (school_id) REFERENCES schools (id) ON DELETE SET NULL
);
";
        await context.Database.ExecuteSqlRawAsync(sql);

        await context.Database.ExecuteSqlRawAsync(@"
CREATE UNIQUE INDEX IF NOT EXISTS ix_teacher_work_plans_teacher_year_trim_subj_group
    ON teacher_work_plans (teacher_id, academic_year_id, trimester, subject_id, group_id);");
        await context.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS IX_teacher_work_plans_academic_year_id ON teacher_work_plans (academic_year_id);");
        await context.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS IX_teacher_work_plans_grade_level_id ON teacher_work_plans (grade_level_id);");
        await context.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS IX_teacher_work_plans_group_id ON teacher_work_plans (group_id);");
        await context.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS IX_teacher_work_plans_school_id ON teacher_work_plans (school_id);");
        await context.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS IX_teacher_work_plans_subject_id ON teacher_work_plans (subject_id);");

        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS teacher_work_plan_details (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    teacher_work_plan_id uuid NOT NULL,
    weeks_range character varying(20) NOT NULL,
    topic text,
    conceptual_content text,
    procedural_content text,
    attitudinal_content text,
    basic_competencies text,
    achievement_indicators text,
    display_order integer NOT NULL,
    CONSTRAINT teacher_work_plan_details_pkey PRIMARY KEY (id),
    CONSTRAINT teacher_work_plan_details_plan_id_fkey FOREIGN KEY (teacher_work_plan_id) REFERENCES teacher_work_plans (id) ON DELETE CASCADE
);
");
        await context.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS IX_teacher_work_plan_details_teacher_work_plan_id ON teacher_work_plan_details (teacher_work_plan_id);");

        Console.WriteLine("✅ Tablas teacher_work_plans y teacher_work_plan_details creadas o ya existían.");
    }
}
