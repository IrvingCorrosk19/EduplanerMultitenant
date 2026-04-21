-- Script para aplicar los nuevos campos del módulo de prematrícula
-- Ejecutar manualmente en la base de datos si las migraciones no funcionan

-- 1. Agregar campos a student_assignments
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'student_assignments' AND column_name = 'is_active') THEN
        ALTER TABLE student_assignments ADD COLUMN is_active boolean NOT NULL DEFAULT true;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'student_assignments' AND column_name = 'end_date') THEN
        ALTER TABLE student_assignments ADD COLUMN end_date timestamp with time zone;
    END IF;
END $$;

-- 2. Agregar campos de auditoría a prematriculations
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'prematriculations' AND column_name = 'confirmed_by') THEN
        ALTER TABLE prematriculations ADD COLUMN confirmed_by uuid;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'prematriculations' AND column_name = 'rejected_by') THEN
        ALTER TABLE prematriculations ADD COLUMN rejected_by uuid;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'prematriculations' AND column_name = 'cancelled_by') THEN
        ALTER TABLE prematriculations ADD COLUMN cancelled_by uuid;
    END IF;
END $$;

-- 3. Agregar required_amount a prematriculation_periods
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'prematriculation_periods' AND column_name = 'required_amount') THEN
        ALTER TABLE prematriculation_periods ADD COLUMN required_amount numeric(18,2) NOT NULL DEFAULT 0;
    END IF;
END $$;

-- 4. Crear tabla prematriculation_histories
CREATE TABLE IF NOT EXISTS prematriculation_histories (
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    prematriculation_id uuid NOT NULL,
    previous_status character varying(20) NOT NULL,
    new_status character varying(20) NOT NULL,
    changed_by uuid,
    reason text,
    changed_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    additional_info text,
    CONSTRAINT prematriculation_histories_pkey PRIMARY KEY (id),
    CONSTRAINT prematriculation_histories_prematriculation_id_fkey 
        FOREIGN KEY (prematriculation_id) REFERENCES prematriculations(id) ON DELETE CASCADE,
    CONSTRAINT prematriculation_histories_changed_by_fkey 
        FOREIGN KEY (changed_by) REFERENCES users(id) ON DELETE SET NULL
);

-- 5. Crear índices
CREATE INDEX IF NOT EXISTS IX_prematriculation_histories_prematriculation_id 
    ON prematriculation_histories(prematriculation_id);
CREATE INDEX IF NOT EXISTS IX_prematriculation_histories_changed_at 
    ON prematriculation_histories(changed_at);
CREATE INDEX IF NOT EXISTS IX_prematriculation_histories_changed_by 
    ON prematriculation_histories(changed_by);

CREATE INDEX IF NOT EXISTS IX_prematriculations_confirmed_by 
    ON prematriculations(confirmed_by);
CREATE INDEX IF NOT EXISTS IX_prematriculations_rejected_by 
    ON prematriculations(rejected_by);
CREATE INDEX IF NOT EXISTS IX_prematriculations_cancelled_by 
    ON prematriculations(cancelled_by);

-- 6. Agregar foreign keys
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints 
                   WHERE constraint_name = 'prematriculations_confirmed_by_fkey') THEN
        ALTER TABLE prematriculations 
            ADD CONSTRAINT prematriculations_confirmed_by_fkey 
            FOREIGN KEY (confirmed_by) REFERENCES users(id) ON DELETE SET NULL;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints 
                   WHERE constraint_name = 'prematriculations_rejected_by_fkey') THEN
        ALTER TABLE prematriculations 
            ADD CONSTRAINT prematriculations_rejected_by_fkey 
            FOREIGN KEY (rejected_by) REFERENCES users(id) ON DELETE SET NULL;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints 
                   WHERE constraint_name = 'prematriculations_cancelled_by_fkey') THEN
        ALTER TABLE prematriculations 
            ADD CONSTRAINT prematriculations_cancelled_by_fkey 
            FOREIGN KEY (cancelled_by) REFERENCES users(id) ON DELETE SET NULL;
    END IF;
END $$;

