-- ============================================
-- SCRIPT DE CONSULTA: Estructura de Tablas
-- Módulo: Matrícula y Prematrícula
-- ============================================

-- 1. Consultar estructura de tabla: schools
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_name = 'schools' 
ORDER BY ordinal_position;

-- 2. Consultar estructura de tabla: users
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_name = 'users' 
ORDER BY ordinal_position;

-- 3. Consultar estructura de tabla: grade_levels
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_name = 'grade_levels' 
ORDER BY ordinal_position;

-- 4. Consultar estructura de tabla: groups
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_name = 'groups' 
ORDER BY ordinal_position;

-- 5. Consultar estructura de tabla: prematriculation_periods
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_name = 'prematriculation_periods' 
ORDER BY ordinal_position;

-- 6. Consultar estructura de tabla: prematriculations
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_name = 'prematriculations' 
ORDER BY ordinal_position;

-- 7. Consultar estructura de tabla: payment_concepts
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_name = 'payment_concepts' 
ORDER BY ordinal_position;

-- 8. Consultar estructura de tabla: payments
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_name = 'payments' 
ORDER BY ordinal_position;

-- 9. Consultar estructura de tabla: student_assignments
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_name = 'student_assignments' 
ORDER BY ordinal_position;

-- 10. Consultar estructura de tabla: subjects (para validación académica)
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_name = 'subjects' 
ORDER BY ordinal_position;

-- 11. Consultar estructura de tabla: activities (para validación académica)
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_name = 'activities' 
ORDER BY ordinal_position;

-- 12. Consultar estructura de tabla: student_activity_scores (para validación académica)
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns 
WHERE table_name = 'student_activity_scores' 
ORDER BY ordinal_position;

-- ============================================
-- CONSULTAS DE DATOS EXISTENTES
-- ============================================

-- Verificar si hay escuelas
SELECT COUNT(*) as total_schools, 
       string_agg(id::text, ', ') as school_ids
FROM schools;

-- Verificar si hay usuarios
SELECT 
    role, 
    COUNT(*) as total,
    string_agg(id::text, ', ') as user_ids
FROM users 
GROUP BY role;

-- Verificar si hay grados
SELECT COUNT(*) as total_grades, 
       string_agg(id::text, ', ') as grade_ids
FROM grade_levels;

-- Verificar si hay grupos
SELECT COUNT(*) as total_groups, 
       string_agg(id::text, ', ') as group_ids
FROM groups;

-- Verificar si hay períodos de prematrícula
SELECT 
    id,
    school_id,
    start_date,
    end_date,
    is_active,
    max_capacity_per_group,
    auto_assign_by_shift
FROM prematriculation_periods;

-- Verificar si hay prematrículas existentes
SELECT 
    COUNT(*) as total_prematriculations,
    status,
    COUNT(*) as count_by_status
FROM prematriculations 
GROUP BY status;

-- Verificar si hay conceptos de pago
SELECT 
    id,
    name,
    amount,
    is_active
FROM payment_concepts;

-- Verificar si hay pagos
SELECT 
    COUNT(*) as total_payments,
    payment_status,
    COUNT(*) as count_by_status
FROM payments 
GROUP BY payment_status;

-- Verificar si hay asignaciones de estudiantes
SELECT COUNT(*) as total_student_assignments 
FROM student_assignments;

-- Verificar si hay materias
SELECT 
    COUNT(*) as total_subjects,
    string_agg(id::text, ', ') as subject_ids
FROM subjects;

-- Verificar si hay actividades
SELECT 
    COUNT(*) as total_activities,
    string_agg(id::text, ', ') as activity_ids
FROM activities;

-- Verificar si hay calificaciones
SELECT 
    COUNT(*) as total_scores,
    COUNT(DISTINCT student_id) as students_with_scores
FROM student_activity_scores;

-- ============================================
-- CONSULTAS DE RELACIONES (Foreign Keys)
-- ============================================

-- Verificar relaciones de prematriculations
SELECT 
    p.id as prematriculation_id,
    p.status,
    p.student_id,
    u.name as student_name,
    p.grade_id,
    gl.name as grade_name,
    p.group_id,
    g.name as group_name,
    p.prematriculation_period_id,
    pp.start_date as period_start,
    pp.end_date as period_end
FROM prematriculations p
LEFT JOIN users u ON p.student_id = u.id
LEFT JOIN grade_levels gl ON p.grade_id = gl.id
LEFT JOIN groups g ON p.group_id = g.id
LEFT JOIN prematriculation_periods pp ON p.prematriculation_period_id = pp.id
LIMIT 10;

-- Verificar relaciones de payments
SELECT 
    pay.id as payment_id,
    pay.payment_status,
    pay.amount,
    pay.prematriculation_id,
    p.status as prematriculation_status,
    pay.payment_concept_id,
    pc.name as concept_name
FROM payments pay
LEFT JOIN prematriculations p ON pay.prematriculation_id = p.id
LEFT JOIN payment_concepts pc ON pay.payment_concept_id = pc.id
LIMIT 10;

-- Verificar calificaciones por estudiante (para validación académica)
SELECT 
    u.id as student_id,
    u.name as student_name,
    s.name as subject_name,
    AVG(sas.score) as promedio_materia,
    CASE WHEN AVG(sas.score) < 3.0 THEN 'REPROBADA' ELSE 'APROBADA' END as estado
FROM users u
INNER JOIN student_activity_scores sas ON u.id = sas.student_id
INNER JOIN activities a ON sas.activity_id = a.id
INNER JOIN subjects s ON a.subject_id = s.id
GROUP BY u.id, u.name, s.id, s.name
ORDER BY u.name, s.name;

