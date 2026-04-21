-- ============================================
-- SCRIPT: Insertar Conceptos de Pago
-- ============================================

-- IDs Fijos
-- Escuela: 6e42399f-6f17-4585-b92e-fa4fff02cb65
-- Admin: b0b35595-cc47-4a3e-9233-1c57809daca5

-- Verificar si la tabla existe
DO $$
BEGIN
    IF EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_schema = 'public' 
        AND table_name = 'payment_concepts'
    ) THEN
        -- Concepto: Matrícula
        INSERT INTO payment_concepts (
            id,
            school_id,
            name,
            description,
            amount,
            periodicity,
            is_active,
            created_at,
            created_by
        ) VALUES (
            gen_random_uuid(),
            '6e42399f-6f17-4585-b92e-fa4fff02cb65',
            'Matrícula',
            'Pago de matrícula para el año académico',
            100.00,
            'Unico',
            true,
            CURRENT_TIMESTAMP,
            'b0b35595-cc47-4a3e-9233-1c57809daca5'
        ) ON CONFLICT DO NOTHING;

        -- Concepto: Mensualidad
        INSERT INTO payment_concepts (
            id,
            school_id,
            name,
            description,
            amount,
            periodicity,
            is_active,
            created_at,
            created_by
        ) VALUES (
            gen_random_uuid(),
            '6e42399f-6f17-4585-b92e-fa4fff02cb65',
            'Mensualidad',
            'Pago mensual de colegiatura',
            50.00,
            'Mensual',
            true,
            CURRENT_TIMESTAMP,
            'b0b35595-cc47-4a3e-9233-1c57809daca5'
        ) ON CONFLICT DO NOTHING;

        -- Concepto: Materiales
        INSERT INTO payment_concepts (
            id,
            school_id,
            name,
            description,
            amount,
            periodicity,
            is_active,
            created_at,
            created_by
        ) VALUES (
            gen_random_uuid(),
            '6e42399f-6f17-4585-b92e-fa4fff02cb65',
            'Materiales',
            'Pago de materiales escolares',
            25.00,
            'Unico',
            true,
            CURRENT_TIMESTAMP,
            'b0b35595-cc47-4a3e-9233-1c57809daca5'
        ) ON CONFLICT DO NOTHING;

        RAISE NOTICE 'Conceptos de pago insertados exitosamente';
    ELSE
        RAISE NOTICE 'La tabla payment_concepts no existe. Ejecuta primero CREAR_TABLA_PAYMENT_CONCEPTS.sql';
    END IF;
END $$;

-- Verificar conceptos creados
SELECT 
    id,
    name,
    amount,
    periodicity,
    is_active
FROM payment_concepts
WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65'
ORDER BY name;

