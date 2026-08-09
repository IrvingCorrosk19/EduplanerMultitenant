using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class ProductionTenantAssignmentsAndDisciplineFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sombra EF (Cascade) puede existir en algunas BDs aunque en otras no; idempotente.
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_discipline_reports_schools_school_id') THEN
    ALTER TABLE discipline_reports DROP CONSTRAINT ""FK_discipline_reports_schools_school_id"";
  END IF;
END $$;");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'discipline_reports_school_id_fkey') THEN
    ALTER TABLE discipline_reports
      ADD CONSTRAINT discipline_reports_school_id_fkey
      FOREIGN KEY (school_id) REFERENCES schools(id) ON DELETE RESTRICT;
  END IF;
END $$;");

            migrationBuilder.Sql(@"ALTER TABLE student_assignments ADD COLUMN IF NOT EXISTS school_id uuid;");
            migrationBuilder.Sql(@"
UPDATE student_assignments sa
SET school_id = u.school_id
FROM users u
WHERE u.id = sa.student_id AND sa.school_id IS NULL AND u.school_id IS NOT NULL;");
            migrationBuilder.Sql(@"
UPDATE student_assignments sa
SET school_id = g.school_id
FROM groups g
WHERE g.id = sa.group_id AND sa.school_id IS NULL AND g.school_id IS NOT NULL;");
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM student_assignments WHERE school_id IS NULL) THEN
    RAISE EXCEPTION 'Migración: student_assignments sin school_id resoluble. Revise integridad users.school_id / groups.school_id.';
  END IF;
END $$;
ALTER TABLE student_assignments ALTER COLUMN school_id SET NOT NULL;");

            migrationBuilder.Sql(@"ALTER TABLE teacher_assignments ADD COLUMN IF NOT EXISTS school_id uuid;");
            migrationBuilder.Sql(@"
UPDATE teacher_assignments ta
SET school_id = sa.school_id
FROM subject_assignments sa
WHERE sa.id = ta.subject_assignment_id AND ta.school_id IS NULL AND sa.school_id IS NOT NULL;");
            migrationBuilder.Sql(@"
UPDATE teacher_assignments ta
SET school_id = g.school_id
FROM subject_assignments sa
JOIN groups g ON g.id = sa.group_id
WHERE sa.id = ta.subject_assignment_id AND ta.school_id IS NULL AND g.school_id IS NOT NULL;");
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM teacher_assignments WHERE school_id IS NULL) THEN
    RAISE EXCEPTION 'Migración: teacher_assignments sin school_id resoluble. Revise subject_assignments / groups.';
  END IF;
END $$;
ALTER TABLE teacher_assignments ALTER COLUMN school_id SET NOT NULL;");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_student_assignments_school_id') THEN
    CREATE INDEX ""IX_student_assignments_school_id"" ON student_assignments (school_id);
  END IF;
END $$;");
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_student_assignments_school_student_active') THEN
    CREATE INDEX ""IX_student_assignments_school_student_active"" ON student_assignments (school_id, student_id, is_active);
  END IF;
END $$;");
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'student_assignments_school_id_fkey') THEN
    ALTER TABLE student_assignments
      ADD CONSTRAINT student_assignments_school_id_fkey
      FOREIGN KEY (school_id) REFERENCES schools(id) ON DELETE RESTRICT;
  END IF;
END $$;");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_teacher_assignments_school_id') THEN
    CREATE INDEX ""IX_teacher_assignments_school_id"" ON teacher_assignments (school_id);
  END IF;
END $$;");
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_teacher_assignments_school_teacher') THEN
    CREATE INDEX ""IX_teacher_assignments_school_teacher"" ON teacher_assignments (school_id, teacher_id);
  END IF;
END $$;");
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'teacher_assignments_school_id_fkey') THEN
    ALTER TABLE teacher_assignments
      ADD CONSTRAINT teacher_assignments_school_id_fkey
      FOREIGN KEY (school_id) REFERENCES schools(id) ON DELETE RESTRICT;
  END IF;
END $$;");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS ""IX_prematriculations_grade_id"" ON prematriculations (grade_id);
CREATE INDEX IF NOT EXISTS ""IX_prematriculations_group_id"" ON prematriculations (group_id);
CREATE INDEX IF NOT EXISTS ""IX_prematriculations_parent_id"" ON prematriculations (parent_id);
CREATE INDEX IF NOT EXISTS ""IX_subject_assignments_specialty_id"" ON subject_assignments (specialty_id);
CREATE INDEX IF NOT EXISTS ""IX_subjects_area_id"" ON subjects (""AreaId"");
CREATE INDEX IF NOT EXISTS ""IX_email_jobs_created_by_user_id"" ON email_jobs (created_by_user_id);
CREATE INDEX IF NOT EXISTS ""IX_teacher_work_plan_review_logs_performed_by_user_id"" ON teacher_work_plan_review_logs (performed_by_user_id);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE teacher_assignments DROP CONSTRAINT IF EXISTS teacher_assignments_school_id_fkey;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_teacher_assignments_school_teacher"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_teacher_assignments_school_id"";");

            migrationBuilder.Sql(@"ALTER TABLE student_assignments DROP CONSTRAINT IF EXISTS student_assignments_school_id_fkey;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_student_assignments_school_student_active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_student_assignments_school_id"";");

            migrationBuilder.Sql(@"ALTER TABLE student_assignments DROP COLUMN IF EXISTS school_id;");
            migrationBuilder.Sql(@"ALTER TABLE teacher_assignments DROP COLUMN IF EXISTS school_id;");

            migrationBuilder.Sql(@"ALTER TABLE discipline_reports DROP CONSTRAINT IF EXISTS discipline_reports_school_id_fkey;");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_discipline_reports_schools_school_id') THEN
    ALTER TABLE discipline_reports
      ADD CONSTRAINT ""FK_discipline_reports_schools_school_id""
      FOREIGN KEY (school_id) REFERENCES schools(id) ON DELETE CASCADE;
  END IF;
END $$;");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_prematriculations_grade_id"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_prematriculations_group_id"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_prematriculations_parent_id"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_subject_assignments_specialty_id"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_subjects_area_id"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_email_jobs_created_by_user_id"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_teacher_work_plan_review_logs_performed_by_user_id"";");
        }
    }
}
