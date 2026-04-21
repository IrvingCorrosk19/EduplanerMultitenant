-- ============================================
-- SCRIPT: Agregar campos faltantes a tabla payments
-- ============================================
-- La migración incluye estos campos pero no están en la tabla
-- payment_method, receipt_image, payment_concept_id, student_id
-- ============================================

-- Verificar si los campos existen y agregarlos si faltan
DO $$
BEGIN
    -- Agregar payment_method si no existe
    IF NOT EXISTS (
        SELECT FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'payments' 
        AND column_name = 'payment_method'
    ) THEN
        ALTER TABLE payments 
        ADD COLUMN payment_method character varying(50);
        RAISE NOTICE 'Campo payment_method agregado';
    END IF;

    -- Agregar receipt_image si no existe
    IF NOT EXISTS (
        SELECT FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'payments' 
        AND column_name = 'receipt_image'
    ) THEN
        ALTER TABLE payments 
        ADD COLUMN receipt_image text;
        RAISE NOTICE 'Campo receipt_image agregado';
    END IF;

    -- Agregar payment_concept_id si no existe
    IF NOT EXISTS (
        SELECT FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'payments' 
        AND column_name = 'payment_concept_id'
    ) THEN
        ALTER TABLE payments 
        ADD COLUMN payment_concept_id uuid;
        
        -- Agregar foreign key si la tabla payment_concepts existe
        IF EXISTS (
            SELECT FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_name = 'payment_concepts'
        ) THEN
            ALTER TABLE payments
                ADD CONSTRAINT payments_payment_concept_id_fkey 
                FOREIGN KEY (payment_concept_id) 
                REFERENCES payment_concepts(id) 
                ON DELETE SET NULL;
        END IF;
        
        RAISE NOTICE 'Campo payment_concept_id agregado';
    END IF;

    -- Agregar student_id si no existe
    IF NOT EXISTS (
        SELECT FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'payments' 
        AND column_name = 'student_id'
    ) THEN
        ALTER TABLE payments 
        ADD COLUMN student_id uuid;
        
        -- Agregar foreign key
        ALTER TABLE payments
            ADD CONSTRAINT payments_student_id_fkey 
            FOREIGN KEY (student_id) 
            REFERENCES users(id) 
            ON DELETE SET NULL;
        
        RAISE NOTICE 'Campo student_id agregado';
    END IF;

    -- Crear índices si no existen
    IF NOT EXISTS (
        SELECT FROM pg_indexes 
        WHERE tablename = 'payments' 
        AND indexname = 'ix_payments_payment_concept_id'
    ) THEN
        CREATE INDEX ix_payments_payment_concept_id ON payments(payment_concept_id);
    END IF;

    IF NOT EXISTS (
        SELECT FROM pg_indexes 
        WHERE tablename = 'payments' 
        AND indexname = 'ix_payments_student_id'
    ) THEN
        CREATE INDEX ix_payments_student_id ON payments(student_id);
    END IF;
END $$;

-- Verificar estructura final
SELECT 
    column_name, 
    data_type, 
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'payments' 
ORDER BY ordinal_position;

