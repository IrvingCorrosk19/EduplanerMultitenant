-- Script para crear la tabla de jornadas (shifts)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'public' AND tablename = 'shifts') THEN
        CREATE TABLE shifts (
            id uuid NOT NULL DEFAULT uuid_generate_v4(),
            school_id uuid NULL,
            name character varying(50) NOT NULL,
            description text NULL,
            is_active boolean NOT NULL DEFAULT TRUE,
            display_order integer NOT NULL DEFAULT 0,
            created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at timestamp with time zone NULL,
            created_by uuid NULL,
            updated_by uuid NULL,
            CONSTRAINT shifts_pkey PRIMARY KEY (id)
        );

        -- Crear índices
        CREATE INDEX IX_shifts_school_id ON shifts (school_id);
        CREATE INDEX IX_shifts_name ON shifts (name);
        CREATE INDEX IX_shifts_is_active ON shifts (is_active);

        -- Agregar foreign keys
        ALTER TABLE shifts 
            ADD CONSTRAINT shifts_school_id_fkey 
            FOREIGN KEY (school_id) REFERENCES schools (id) ON DELETE CASCADE;
        
        ALTER TABLE shifts 
            ADD CONSTRAINT shifts_created_by_fkey 
            FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE SET NULL;
        
        ALTER TABLE shifts 
            ADD CONSTRAINT shifts_updated_by_fkey 
            FOREIGN KEY (updated_by) REFERENCES users (id) ON DELETE SET NULL;

        RAISE NOTICE 'Tabla shifts creada exitosamente';
    ELSE
        RAISE NOTICE 'Tabla shifts ya existe, omitiendo creación.';
    END IF;

    -- Agregar columna shift_id a groups si no existe
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'groups' AND column_name = 'shift_id') THEN
        ALTER TABLE groups ADD COLUMN shift_id uuid NULL;
        
        CREATE INDEX IX_groups_shift_id ON groups (shift_id);
        
        ALTER TABLE groups 
            ADD CONSTRAINT groups_shift_id_fkey 
            FOREIGN KEY (shift_id) REFERENCES shifts (id) ON DELETE SET NULL;
        
        RAISE NOTICE 'Columna shift_id agregada a groups exitosamente';
    ELSE
        RAISE NOTICE 'Columna shift_id ya existe en groups, omitiendo creación.';
    END IF;
END
$$;

