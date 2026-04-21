-- Crea la tabla school_schedule_configurations si no existe.
-- Ejecutar contra la base de datos del proyecto (ej. psql o cliente SQL).
-- Si usas migraciones EF: dotnet ef database update

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS school_schedule_configurations (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    school_id uuid NOT NULL,
    morning_start_time time NOT NULL,
    morning_block_duration_minutes integer NOT NULL,
    morning_block_count integer NOT NULL,
    recess_duration_minutes integer NOT NULL DEFAULT 30,
    recess_after_morning_block_number integer NOT NULL DEFAULT 4,
    recess_after_afternoon_block_number integer NOT NULL DEFAULT 2,
    afternoon_start_time time NULL,
    afternoon_block_duration_minutes integer NULL,
    afternoon_block_count integer NULL,
    created_at timestamp with time zone NULL,
    updated_at timestamp with time zone NULL,
    CONSTRAINT school_schedule_configurations_pkey PRIMARY KEY (id),
    CONSTRAINT school_schedule_configurations_school_id_fkey
        FOREIGN KEY (school_id) REFERENCES schools (id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_school_schedule_configurations_school_id
    ON school_schedule_configurations (school_id);
