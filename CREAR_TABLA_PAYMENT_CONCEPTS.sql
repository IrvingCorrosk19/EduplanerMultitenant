-- ============================================
-- SCRIPT: Crear tabla payment_concepts
-- ============================================
-- Este script crea solo la tabla payment_concepts
-- que falta en la base de datos
-- ============================================

-- Verificar si la tabla ya existe
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_schema = 'public' 
        AND table_name = 'payment_concepts'
    ) THEN
        -- Crear tabla payment_concepts
        CREATE TABLE payment_concepts (
            id uuid NOT NULL DEFAULT uuid_generate_v4(),
            school_id uuid NOT NULL,
            name character varying(100) NOT NULL,
            description text,
            amount numeric(18,2) NOT NULL,
            periodicity character varying(50),
            is_active boolean NOT NULL DEFAULT true,
            created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at timestamp with time zone,
            created_by uuid,
            updated_by uuid,
            CONSTRAINT payment_concepts_pkey PRIMARY KEY (id)
        );

        -- Crear índices
        CREATE INDEX IX_payment_concepts_school_id ON payment_concepts(school_id);
        CREATE INDEX IX_payment_concepts_created_by ON payment_concepts(created_by);
        CREATE INDEX IX_payment_concepts_updated_by ON payment_concepts(updated_by);

        -- Crear foreign keys
        ALTER TABLE payment_concepts
            ADD CONSTRAINT payment_concepts_school_id_fkey 
            FOREIGN KEY (school_id) 
            REFERENCES schools(id) 
            ON DELETE CASCADE;

        ALTER TABLE payment_concepts
            ADD CONSTRAINT payment_concepts_created_by_fkey 
            FOREIGN KEY (created_by) 
            REFERENCES users(id) 
            ON DELETE SET NULL;

        ALTER TABLE payment_concepts
            ADD CONSTRAINT payment_concepts_updated_by_fkey 
            FOREIGN KEY (updated_by) 
            REFERENCES users(id) 
            ON DELETE SET NULL;

        RAISE NOTICE 'Tabla payment_concepts creada exitosamente';
    ELSE
        RAISE NOTICE 'La tabla payment_concepts ya existe';
    END IF;
END $$;

-- Verificar creación
SELECT 
    'Tabla payment_concepts: ' || 
    CASE 
        WHEN EXISTS (
            SELECT FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_name = 'payment_concepts'
        ) THEN 'EXISTE ✅'
        ELSE 'NO EXISTE ❌'
    END as estado;

