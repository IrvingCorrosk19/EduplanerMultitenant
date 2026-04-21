-- Crear tabla student_payment_access en Render (módulo Club de Padres). Idempotente (IF NOT EXISTS).
CREATE TABLE IF NOT EXISTS student_payment_access (
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    student_id uuid NOT NULL,
    school_id uuid NOT NULL,
    carnet_status character varying(20) NOT NULL DEFAULT 'Pendiente',
    platform_access_status character varying(20) NOT NULL DEFAULT 'Pendiente',
    carnet_status_updated_at timestamp with time zone NULL,
    platform_status_updated_at timestamp with time zone NULL,
    carnet_updated_by_user_id uuid NULL,
    platform_updated_by_user_id uuid NULL,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NULL,
    CONSTRAINT student_payment_access_pkey PRIMARY KEY (id),
    CONSTRAINT student_payment_access_student_id_fkey FOREIGN KEY (student_id) REFERENCES users (id) ON DELETE RESTRICT,
    CONSTRAINT student_payment_access_school_id_fkey FOREIGN KEY (school_id) REFERENCES schools (id) ON DELETE RESTRICT,
    CONSTRAINT student_payment_access_carnet_updated_by_fkey FOREIGN KEY (carnet_updated_by_user_id) REFERENCES users (id) ON DELETE SET NULL,
    CONSTRAINT student_payment_access_platform_updated_by_fkey FOREIGN KEY (platform_updated_by_user_id) REFERENCES users (id) ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS IX_student_payment_access_student_id ON student_payment_access (student_id);
CREATE INDEX IF NOT EXISTS IX_student_payment_access_school_id ON student_payment_access (school_id);
CREATE INDEX IF NOT EXISTS IX_student_payment_access_carnet_status_school_id ON student_payment_access (carnet_status, school_id);
CREATE UNIQUE INDEX IF NOT EXISTS IX_student_payment_access_student_id_school_id ON student_payment_access (student_id, school_id);
CREATE INDEX IF NOT EXISTS IX_student_payment_access_carnet_updated_by_user_id ON student_payment_access (carnet_updated_by_user_id);
CREATE INDEX IF NOT EXISTS IX_student_payment_access_platform_updated_by_user_id ON student_payment_access (platform_updated_by_user_id);
