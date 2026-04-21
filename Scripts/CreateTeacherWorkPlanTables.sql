-- Crear tablas del módulo Plan de Trabajo Trimestral (docente)
-- Ejecutar en la base de datos si la migración EF no se aplicó (ej. historial desincronizado).

-- Tabla principal
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

CREATE UNIQUE INDEX IF NOT EXISTS ix_teacher_work_plans_teacher_year_trim_subj_group
    ON teacher_work_plans (teacher_id, academic_year_id, trimester, subject_id, group_id);

CREATE INDEX IF NOT EXISTS IX_teacher_work_plans_academic_year_id ON teacher_work_plans (academic_year_id);
CREATE INDEX IF NOT EXISTS IX_teacher_work_plans_grade_level_id ON teacher_work_plans (grade_level_id);
CREATE INDEX IF NOT EXISTS IX_teacher_work_plans_group_id ON teacher_work_plans (group_id);
CREATE INDEX IF NOT EXISTS IX_teacher_work_plans_school_id ON teacher_work_plans (school_id);
CREATE INDEX IF NOT EXISTS IX_teacher_work_plans_subject_id ON teacher_work_plans (subject_id);

-- Tabla de detalles (bloques de contenido)
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

CREATE INDEX IF NOT EXISTS IX_teacher_work_plan_details_teacher_work_plan_id ON teacher_work_plan_details (teacher_work_plan_id);

-- Registrar la migración en el historial de EF (opcional, para que "dotnet ef database update" no intente aplicarla de nuevo)
-- INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20260217142501_AddTeacherWorkPlanModule', '8.0.0');
