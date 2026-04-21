-- ============================================
-- SCRIPT PARA VERIFICAR DATOS EXISTENTES
-- Módulo: Matrícula y Prematrícula
-- ============================================

-- Configurar formato de salida
\pset format aligned
\pset tuples_only off

-- ============================================
-- 1. VERIFICAR ESCUELAS
-- ============================================
\echo '========================================'
\echo '1. ESCUELAS EXISTENTES'
\echo '========================================'
SELECT 
    id,
    name,
    address,
    phone,
    admin_id,
    created_at
FROM schools
ORDER BY created_at;

-- ============================================
-- 2. VERIFICAR USUARIOS (por rol)
-- ============================================
\echo ''
\echo '========================================'
\echo '2. USUARIOS EXISTENTES (por rol)'
\echo '========================================'
SELECT 
    role,
    COUNT(*) as total,
    string_agg(name || ' ' || last_name, ', ') as nombres
FROM users 
GROUP BY role
ORDER BY role;

-- Ver usuarios específicos para matrícula
\echo ''
\echo '--- Estudiantes ---'
SELECT id, name, last_name, email, document_id, school_id, shift
FROM users 
WHERE role IN ('student', 'estudiante')
LIMIT 10;

\echo ''
\echo '--- Acudientes ---'
SELECT id, name, last_name, email, document_id, school_id
FROM users 
WHERE role IN ('parent', 'acudiente')
LIMIT 10;

\echo ''
\echo '--- Administradores ---'
SELECT id, name, last_name, email, school_id
FROM users 
WHERE role IN ('admin', 'superadmin')
LIMIT 10;

-- ============================================
-- 3. VERIFICAR GRADOS
-- ============================================
\echo ''
\echo '========================================'
\echo '3. GRADOS EXISTENTES'
\echo '========================================'
SELECT 
    id,
    name,
    description,
    school_id,
    created_at
FROM grade_levels
ORDER BY name;

-- ============================================
-- 4. VERIFICAR GRUPOS
-- ============================================
\echo ''
\echo '========================================'
\echo '4. GRUPOS EXISTENTES'
\echo '========================================'
SELECT 
    id,
    name,
    grade,
    school_id,
    max_capacity,
    shift,
    created_at,
    (SELECT COUNT(*) FROM student_assignments WHERE group_id = groups.id) as estudiantes_actuales
FROM groups
ORDER BY grade, name;

-- ============================================
-- 5. VERIFICAR PERÍODOS DE PREMATRÍCULA
-- ============================================
\echo ''
\echo '========================================'
\echo '5. PERÍODOS DE PREMATRÍCULA'
\echo '========================================'
SELECT 
    id,
    school_id,
    start_date,
    end_date,
    is_active,
    max_capacity_per_group,
    auto_assign_by_shift,
    created_at
FROM prematriculation_periods
ORDER BY created_at DESC;

-- ============================================
-- 6. VERIFICAR PREMATRÍCULAS
-- ============================================
\echo ''
\echo '========================================'
\echo '6. PREMATRÍCULAS EXISTENTES'
\echo '========================================'
SELECT 
    COUNT(*) as total_prematriculations,
    status,
    COUNT(*) as count_by_status
FROM prematriculations 
GROUP BY status;

\echo ''
\echo '--- Detalle de Prematrículas ---'
SELECT 
    p.id,
    p.status,
    u.name || ' ' || u.last_name as estudiante,
    gl.name as grado,
    g.name as grupo,
    p.failed_subjects_count,
    p.academic_condition_valid,
    p.prematriculation_code,
    p.created_at
FROM prematriculations p
LEFT JOIN users u ON p.student_id = u.id
LEFT JOIN grade_levels gl ON p.grade_id = gl.id
LEFT JOIN groups g ON p.group_id = g.id
ORDER BY p.created_at DESC
LIMIT 10;

-- ============================================
-- 7. VERIFICAR CONCEPTOS DE PAGO
-- ============================================
\echo ''
\echo '========================================'
\echo '7. CONCEPTOS DE PAGO'
\echo '========================================'
SELECT 
    id,
    name,
    description,
    amount,
    periodicity,
    is_active,
    school_id
FROM payment_concepts
ORDER BY name;

-- ============================================
-- 8. VERIFICAR PAGOS
-- ============================================
\echo ''
\echo '========================================'
\echo '8. PAGOS EXISTENTES'
\echo '========================================'
SELECT 
    COUNT(*) as total_payments,
    payment_status,
    COUNT(*) as count_by_status
FROM payments 
GROUP BY payment_status;

\echo ''
\echo '--- Detalle de Pagos ---'
SELECT 
    pay.id,
    pay.payment_status,
    pay.amount,
    pay.payment_method,
    pay.receipt_number,
    pc.name as concepto,
    p.status as prematriculation_status,
    pay.created_at
FROM payments pay
LEFT JOIN payment_concepts pc ON pay.payment_concept_id = pc.id
LEFT JOIN prematriculations p ON pay.prematriculation_id = p.id
ORDER BY pay.created_at DESC
LIMIT 10;

-- ============================================
-- 9. VERIFICAR ASIGNACIONES DE ESTUDIANTES
-- ============================================
\echo ''
\echo '========================================'
\echo '9. ASIGNACIONES DE ESTUDIANTES'
\echo '========================================'
SELECT 
    COUNT(*) as total_assignments
FROM student_assignments;

\echo ''
\echo '--- Estudiantes por Grupo ---'
SELECT 
    g.name as grupo,
    gl.name as grado,
    COUNT(*) as total_estudiantes
FROM student_assignments sa
JOIN groups g ON sa.group_id = g.id
JOIN grade_levels gl ON sa.grade_id = gl.id
GROUP BY g.name, gl.name
ORDER BY gl.name, g.name;

-- ============================================
-- 10. VERIFICAR MATERIAS (para validación académica)
-- ============================================
\echo ''
\echo '========================================'
\echo '10. MATERIAS EXISTENTES'
\echo '========================================'
SELECT 
    COUNT(*) as total_subjects
FROM subjects;

\echo ''
\echo '--- Materias ---'
SELECT id, name, code, school_id
FROM subjects
LIMIT 10;

-- ============================================
-- 11. VERIFICAR ACTIVIDADES (para validación académica)
-- ============================================
\echo ''
\echo '========================================'
\echo '11. ACTIVIDADES EXISTENTES'
\echo '========================================'
SELECT 
    COUNT(*) as total_activities
FROM activities;

-- ============================================
-- 12. VERIFICAR CALIFICACIONES (para validación académica)
-- ============================================
\echo ''
\echo '========================================'
\echo '12. CALIFICACIONES EXISTENTES'
\echo '========================================'
SELECT 
    COUNT(*) as total_scores,
    COUNT(DISTINCT student_id) as estudiantes_con_calificaciones
FROM student_activity_scores;

\echo ''
\echo '--- Calificaciones por Estudiante ---'
SELECT 
    u.id as student_id,
    u.name || ' ' || u.last_name as estudiante,
    COUNT(DISTINCT a.subject_id) as materias_con_calificaciones,
    COUNT(sas.id) as total_calificaciones
FROM users u
LEFT JOIN student_activity_scores sas ON u.id = sas.student_id
LEFT JOIN activities a ON sas.activity_id = a.id
WHERE u.role IN ('student', 'estudiante')
GROUP BY u.id, u.name, u.last_name
HAVING COUNT(sas.id) > 0
ORDER BY u.name
LIMIT 10;

-- ============================================
-- 13. RESUMEN GENERAL
-- ============================================
\echo ''
\echo '========================================'
\echo '13. RESUMEN GENERAL'
\echo '========================================'
SELECT 
    'Escuelas' as tabla,
    COUNT(*)::text as total
FROM schools
UNION ALL
SELECT 
    'Usuarios',
    COUNT(*)::text
FROM users
UNION ALL
SELECT 
    'Grados',
    COUNT(*)::text
FROM grade_levels
UNION ALL
SELECT 
    'Grupos',
    COUNT(*)::text
FROM groups
UNION ALL
SELECT 
    'Períodos Prematrícula',
    COUNT(*)::text
FROM prematriculation_periods
UNION ALL
SELECT 
    'Prematrículas',
    COUNT(*)::text
FROM prematriculations
UNION ALL
SELECT 
    'Conceptos de Pago',
    COUNT(*)::text
FROM payment_concepts
UNION ALL
SELECT 
    'Pagos',
    COUNT(*)::text
FROM payments
UNION ALL
SELECT 
    'Asignaciones Estudiantes',
    COUNT(*)::text
FROM student_assignments
UNION ALL
SELECT 
    'Materias',
    COUNT(*)::text
FROM subjects
UNION ALL
SELECT 
    'Actividades',
    COUNT(*)::text
FROM activities
UNION ALL
SELECT 
    'Calificaciones',
    COUNT(*)::text
FROM student_activity_scores;

