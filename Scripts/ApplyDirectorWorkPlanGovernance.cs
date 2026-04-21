using Microsoft.EntityFrameworkCore;

namespace SchoolManager.Scripts;

/// <summary>
/// Añade columnas de gobernanza a teacher_work_plans y crea teacher_work_plan_review_logs.
/// Ejecutar con: dotnet run -- --apply-director-work-plan-governance
/// </summary>
public static class ApplyDirectorWorkPlanGovernance
{
    public static async Task RunAsync(DbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE teacher_work_plans ADD COLUMN IF NOT EXISTS submitted_at timestamp with time zone;
ALTER TABLE teacher_work_plans ADD COLUMN IF NOT EXISTS approved_at timestamp with time zone;
ALTER TABLE teacher_work_plans ADD COLUMN IF NOT EXISTS approved_by_user_id uuid REFERENCES users(id) ON DELETE SET NULL;
ALTER TABLE teacher_work_plans ADD COLUMN IF NOT EXISTS rejected_at timestamp with time zone;
ALTER TABLE teacher_work_plans ADD COLUMN IF NOT EXISTS rejected_by_user_id uuid REFERENCES users(id) ON DELETE SET NULL;
ALTER TABLE teacher_work_plans ADD COLUMN IF NOT EXISTS review_comment text;
");
        await context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS teacher_work_plan_review_logs (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    teacher_work_plan_id uuid NOT NULL REFERENCES teacher_work_plans(id) ON DELETE CASCADE,
    action character varying(50) NOT NULL,
    performed_by_user_id uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    performed_at timestamp with time zone NOT NULL,
    comment text,
    summary character varying(500),
    CONSTRAINT teacher_work_plan_review_logs_pkey PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS IX_teacher_work_plan_review_logs_plan_id ON teacher_work_plan_review_logs(teacher_work_plan_id);
");
        Console.WriteLine("✅ Columnas de gobernanza y tabla teacher_work_plan_review_logs aplicadas.");
    }
}
