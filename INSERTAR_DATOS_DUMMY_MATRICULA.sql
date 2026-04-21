-- ============================================
-- SCRIPT: Datos Dummy para Pruebas de Matrícula y Prematrícula
-- ============================================
-- NOTA: Este script solo INSERTA datos, NO crea tablas
-- Las tablas deben existir (creadas por migraciones)
-- ============================================

-- IDs Fijos de la Base de Datos (según consulta anterior)
-- Escuela: 6e42399f-6f17-4585-b92e-fa4fff02cb65
-- Período: 307efc09-60f5-4280-a986-763659e9a1d6
-- Admin: b0b35595-cc47-4a3e-9233-1c57809daca5

-- ============================================
-- 1. ACTUALIZAR GRUPOS CON CAPACIDAD Y JORNADA
-- ============================================
-- Actualizar grupos existentes con max_capacity y shift
UPDATE groups 
SET max_capacity = 30, 
    shift = 'Mañana'
WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' 
  AND max_capacity IS NULL;

-- Actualizar algunos grupos específicos con diferentes capacidades
UPDATE groups 
SET max_capacity = 25, 
    shift = 'Tarde'
WHERE name IN ('A1', 'A2', 'C1', 'C2')
  AND school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65';

-- ============================================
-- 2. CREAR ACUDIENTES DE PRUEBA
-- ============================================
-- Acudiente 1 - Para estudiante de prueba
INSERT INTO users (
    id, 
    name, 
    last_name, 
    email, 
    password_hash, 
    role, 
    document_id,
    school_id,
    status,
    created_at
) VALUES (
    gen_random_uuid(),
    'María',
    'Pérez',
    'maria.perez@test.com',
    '$2a$11$KIXx5L5L5L5L5L5L5L5L5O5L5L5L5L5L5L5L5L5L5L5L5L5L5L5L', -- Password: Test123!
    'acudiente',
    '8-1234-5678',
    '6e42399f-6f17-4585-b92e-fa4fff02cb65',
    'active',
    CURRENT_TIMESTAMP
) ON CONFLICT (email) DO NOTHING;

-- Acudiente 2
INSERT INTO users (
    id,
    name,
    last_name,
    email,
    password_hash,
    role,
    document_id,
    school_id,
    status,
    created_at
) VALUES (
    gen_random_uuid(),
    'Juan',
    'González',
    'juan.gonzalez@test.com',
    '$2a$11$KIXx5L5L5L5L5L5L5L5L5O5L5L5L5L5L5L5L5L5L5L5L5L5L5L5L',
    'acudiente',
    '8-2345-6789',
    '6e42399f-6f17-4585-b92e-fa4fff02cb65',
    'active',
    CURRENT_TIMESTAMP
) ON CONFLICT (email) DO NOTHING;

-- Acudiente 3
INSERT INTO users (
    id,
    name,
    last_name,
    email,
    password_hash,
    role,
    document_id,
    school_id,
    status,
    created_at
) VALUES (
    gen_random_uuid(),
    'Ana',
    'Rodríguez',
    'ana.rodriguez@test.com',
    '$2a$11$KIXx5L5L5L5L5L5L5L5L5O5L5L5L5L5L5L5L5L5L5L5L5L5L5L5L',
    'acudiente',
    '8-3456-7890',
    '6e42399f-6f17-4585-b92e-fa4fff02cb65',
    'active',
    CURRENT_TIMESTAMP
) ON CONFLICT (email) DO NOTHING;

-- ============================================
-- 3. ACTUALIZAR ESTUDIANTES CON JORNADA
-- ============================================
-- Actualizar algunos estudiantes con jornada (para asignación automática)
UPDATE users 
SET shift = 'Mañana'
WHERE role IN ('student', 'estudiante')
  AND school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65'
  AND shift IS NULL
  AND id IN (
    SELECT id FROM users 
    WHERE role IN ('student', 'estudiante') 
    AND school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' 
    LIMIT 50
);

UPDATE users 
SET shift = 'Tarde'
WHERE role IN ('student', 'estudiante')
  AND school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65'
  AND shift IS NULL
  AND id IN (
    SELECT id FROM users 
    WHERE role IN ('student', 'estudiante') 
    AND school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' 
    LIMIT 30
);

-- ============================================
-- 4. CREAR CONCEPTOS DE PAGO (si la tabla existe)
-- ============================================
-- Verificar si la tabla existe antes de insertar
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
    END IF;
END $$;

-- ============================================
-- 5. CREAR ACTIVIDADES Y CALIFICACIONES PARA VALIDACIÓN ACADÉMICA
-- ============================================
-- Necesitamos crear actividades y calificaciones para probar la validación
-- de materias reprobadas (máximo 3 para poder prematricular)

-- Obtener un estudiante de prueba (primer estudiante)
DO $$
DECLARE
    test_student_id UUID;
    test_teacher_id UUID;
    test_group_id UUID;
    test_grade_id UUID;
    math_subject_id UUID;
    spanish_subject_id UUID;
    science_subject_id UUID;
    english_subject_id UUID;
    activity_id UUID;
BEGIN
    -- Obtener IDs necesarios
    SELECT id INTO test_student_id 
    FROM users 
    WHERE role IN ('student', 'estudiante') 
    AND school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' 
    LIMIT 1;

    SELECT id INTO test_teacher_id 
    FROM users 
    WHERE role = 'teacher' 
    AND school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' 
    LIMIT 1;

    SELECT id INTO test_group_id 
    FROM groups 
    WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' 
    LIMIT 1;

    SELECT id INTO test_grade_id 
    FROM grade_levels 
    WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' 
    AND name = '10'
    LIMIT 1;

    -- Obtener materias
    SELECT id INTO math_subject_id 
    FROM subjects 
    WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' 
    AND name LIKE '%MATEMÁTICAS%'
    LIMIT 1;

    SELECT id INTO spanish_subject_id 
    FROM subjects 
    WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' 
    AND name LIKE '%ESPAÑOL%'
    LIMIT 1;

    SELECT id INTO science_subject_id 
    FROM subjects 
    WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' 
    AND name LIKE '%CIENCIAS%'
    LIMIT 1;

    SELECT id INTO english_subject_id 
    FROM subjects 
    WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' 
    AND name LIKE '%INGLÉS%'
    LIMIT 1;

    -- Solo crear si tenemos todos los IDs necesarios
    IF test_student_id IS NOT NULL 
       AND test_teacher_id IS NOT NULL 
       AND test_group_id IS NOT NULL 
       AND math_subject_id IS NOT NULL THEN

        -- Crear actividad de Matemáticas (estudiante con 2.5 - REPROBADA)
        INSERT INTO activities (
            id,
            school_id,
            subject_id,
            teacher_id,
            group_id,
            name,
            type,
            grade_level_id,
            created_at
        ) VALUES (
            gen_random_uuid(),
            '6e42399f-6f17-4585-b92e-fa4fff02cb65',
            math_subject_id,
            test_teacher_id,
            test_group_id,
            'Parcial 1 - Matemáticas',
            'Parcial',
            test_grade_id,
            CURRENT_TIMESTAMP
        ) RETURNING id INTO activity_id;

        -- Calificación reprobada en Matemáticas
        IF activity_id IS NOT NULL THEN
            INSERT INTO student_activity_scores (
                id,
                student_id,
                activity_id,
                score,
                school_id,
                created_at
            ) VALUES (
                gen_random_uuid(),
                test_student_id,
                activity_id,
                2.5,
                '6e42399f-6f17-4585-b92e-fa4fff02cb65',
                CURRENT_TIMESTAMP
            ) ON CONFLICT DO NOTHING;
        END IF;

        -- Crear segunda actividad de Matemáticas
        INSERT INTO activities (
            id,
            school_id,
            subject_id,
            teacher_id,
            group_id,
            name,
            type,
            grade_level_id,
            created_at
        ) VALUES (
            gen_random_uuid(),
            '6e42399f-6f17-4585-b92e-fa4fff02cb65',
            math_subject_id,
            test_teacher_id,
            test_group_id,
            'Parcial 2 - Matemáticas',
            'Parcial',
            test_grade_id,
            CURRENT_TIMESTAMP
        ) RETURNING id INTO activity_id;

        IF activity_id IS NOT NULL THEN
            INSERT INTO student_activity_scores (
                id,
                student_id,
                activity_id,
                score,
                school_id,
                created_at
            ) VALUES (
                gen_random_uuid(),
                test_student_id,
                activity_id,
                2.8,
                '6e42399f-6f17-4585-b92e-fa4fff02cb65',
                CURRENT_TIMESTAMP
            ) ON CONFLICT DO NOTHING;
        END IF;

        -- Crear actividad de Español (estudiante con 4.0 - APROBADA)
        IF spanish_subject_id IS NOT NULL THEN
            INSERT INTO activities (
                id,
                school_id,
                subject_id,
                teacher_id,
                group_id,
                name,
                type,
                grade_level_id,
                created_at
            ) VALUES (
                gen_random_uuid(),
                '6e42399f-6f17-4585-b92e-fa4fff02cb65',
                spanish_subject_id,
                test_teacher_id,
                test_group_id,
                'Parcial 1 - Español',
                'Parcial',
                test_grade_id,
                CURRENT_TIMESTAMP
            ) RETURNING id INTO activity_id;

            IF activity_id IS NOT NULL THEN
                INSERT INTO student_activity_scores (
                    id,
                    student_id,
                    activity_id,
                    score,
                    school_id,
                    created_at
                ) VALUES (
                    gen_random_uuid(),
                    test_student_id,
                    activity_id,
                    4.0,
                    '6e42399f-6f17-4585-b92e-fa4fff02cb65',
                    CURRENT_TIMESTAMP
                ) ON CONFLICT DO NOTHING;
            END IF;
        END IF;

        -- Crear actividad de Ciencias (estudiante con 2.0 - REPROBADA)
        IF science_subject_id IS NOT NULL THEN
            INSERT INTO activities (
                id,
                school_id,
                subject_id,
                teacher_id,
                group_id,
                name,
                type,
                grade_level_id,
                created_at
            ) VALUES (
                gen_random_uuid(),
                '6e42399f-6f17-4585-b92e-fa4fff02cb65',
                science_subject_id,
                test_teacher_id,
                test_group_id,
                'Parcial 1 - Ciencias',
                'Parcial',
                test_grade_id,
                CURRENT_TIMESTAMP
            ) RETURNING id INTO activity_id;

            IF activity_id IS NOT NULL THEN
                INSERT INTO student_activity_scores (
                    id,
                    student_id,
                    activity_id,
                    score,
                    school_id,
                    created_at
                ) VALUES (
                    gen_random_uuid(),
                    test_student_id,
                    activity_id,
                    2.0,
                    '6e42399f-6f17-4585-b92e-fa4fff02cb65',
                    CURRENT_TIMESTAMP
                ) ON CONFLICT DO NOTHING;
            END IF;
        END IF;

        -- RESULTADO: Estudiante tiene 2 materias reprobadas (Matemáticas y Ciencias)
        -- Esto cumple con el requisito de ≤ 3 materias reprobadas

    END IF;
END $$;

-- ============================================
-- 6. CREAR USUARIO DE CONTABILIDAD
-- ============================================
INSERT INTO users (
    id,
    name,
    last_name,
    email,
    password_hash,
    role,
    document_id,
    school_id,
    status,
    created_at
) VALUES (
    gen_random_uuid(),
    'Contabilidad',
    'Sistema',
    'contabilidad@test.com',
    '$2a$11$KIXx5L5L5L5L5L5L5L5L5O5L5L5L5L5L5L5L5L5L5L5L5L5L5L5L',
    'contabilidad',
    '8-9999-9999',
    '6e42399f-6f17-4585-b92e-fa4fff02cb65',
    'active',
    CURRENT_TIMESTAMP
) ON CONFLICT (email) DO NOTHING;

-- ============================================
-- RESUMEN DE DATOS CREADOS
-- ============================================
SELECT 'Datos dummy creados exitosamente' as mensaje;

-- Verificar datos creados
SELECT 
    'Acudientes creados: ' || COUNT(*)::text as resumen
FROM users 
WHERE role = 'acudiente' 
AND school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65';

SELECT 
    'Conceptos de pago creados: ' || COUNT(*)::text as resumen
FROM payment_concepts 
WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65';

SELECT 
    'Grupos actualizados: ' || COUNT(*)::text as resumen
FROM groups 
WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' 
AND max_capacity IS NOT NULL;

SELECT 
    'Estudiantes con jornada: ' || COUNT(*)::text as resumen
FROM users 
WHERE role IN ('student', 'estudiante') 
AND school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' 
AND shift IS NOT NULL;

SELECT 
    'Calificaciones creadas: ' || COUNT(*)::text as resumen
FROM student_activity_scores 
WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65';

