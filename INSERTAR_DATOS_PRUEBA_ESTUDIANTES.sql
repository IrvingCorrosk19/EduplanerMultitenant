-- Script para insertar datos de prueba de estudiantes y asignaciones
-- Este script inserta estudiantes con sus asignaciones (grado, grupo, jornada)

DO $$
DECLARE
    _school_id UUID := '6e42399f-6f17-4585-b92e-fa4fff02cb65'; -- ID de la escuela
    _shift_manana_id UUID;
    _shift_tarde_id UUID;
    _shift_noche_id UUID;
    _student_id UUID;
    _grade_id UUID;
    _group_id UUID;
    _assignment_id UUID;
    _counter INTEGER := 0;
BEGIN
    -- Obtener IDs de jornadas
    SELECT id INTO _shift_manana_id FROM shifts WHERE name = 'Mañana' AND school_id = _school_id LIMIT 1;
    SELECT id INTO _shift_tarde_id FROM shifts WHERE name = 'Tarde' AND school_id = _school_id LIMIT 1;
    SELECT id INTO _shift_noche_id FROM shifts WHERE name = 'Noche' AND school_id = _school_id LIMIT 1;

    -- Crear o obtener grados
    INSERT INTO grade_levels (id, school_id, name, description, created_at, updated_at)
    VALUES 
        ('11111111-1111-1111-1111-111111111111', _school_id, '6°', 'Sexto grado', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('22222222-2222-2222-2222-222222222222', _school_id, '7°', 'Séptimo grado', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('33333333-3333-3333-3333-333333333333', _school_id, '8°', 'Octavo grado', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('44444444-4444-4444-4444-444444444444', _school_id, '9°', 'Noveno grado', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('55555555-5555-5555-5555-555555555555', _school_id, '10°', 'Décimo grado', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('66666666-6666-6666-6666-666666666666', _school_id, '11°', 'Undécimo grado', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
    ON CONFLICT (id) DO NOTHING;

    -- Crear o obtener grupos
    INSERT INTO groups (id, school_id, name, description, shift_id, shift, created_at, updated_at)
    VALUES 
        ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', _school_id, 'A', 'Grupo A', _shift_manana_id, 'Mañana', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', _school_id, 'B', 'Grupo B', _shift_tarde_id, 'Tarde', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('cccccccc-cccc-cccc-cccc-cccccccccccc', _school_id, 'C', 'Grupo C', _shift_noche_id, 'Noche', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('dddddddd-dddd-dddd-dddd-dddddddddddd', _school_id, 'D', 'Grupo D', _shift_manana_id, 'Mañana', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
        ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', _school_id, 'E', 'Grupo E', _shift_tarde_id, 'Tarde', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
    ON CONFLICT (id) DO NOTHING;

    -- Obtener IDs de grados y grupos
    SELECT id INTO _grade_id FROM grade_levels WHERE name = '6°' AND school_id = _school_id LIMIT 1;
    SELECT id INTO _group_id FROM groups WHERE name = 'A' AND school_id = _school_id LIMIT 1;

    -- Insertar estudiantes de prueba con asignaciones
    -- Estudiante 1
    INSERT INTO users (id, email, name, last_name, document_id, date_of_birth, role, status, password_hash, school_id, created_at, updated_at, two_factor_enabled, shift, inclusivo)
    VALUES 
        ('11111111-1111-4111-8111-111111111111', 'juan.garcia1@estudiante.com', 'Juan', 'García', 'EST00011001', '2008-03-15', 'estudiante', 'active', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', _school_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, false, 'Mañana', false)
    ON CONFLICT (id) DO UPDATE SET shift = EXCLUDED.shift, inclusivo = EXCLUDED.inclusivo;
    
    SELECT id INTO _student_id FROM users WHERE email = 'juan.garcia1@estudiante.com';
    SELECT id INTO _grade_id FROM grade_levels WHERE name = '6°' AND school_id = _school_id LIMIT 1;
    SELECT id INTO _group_id FROM groups WHERE name = 'A' AND school_id = _school_id LIMIT 1;
    
    INSERT INTO student_assignments (id, student_id, grade_id, group_id, shift_id, created_at)
    VALUES (gen_random_uuid(), _student_id, _grade_id, _group_id, _shift_manana_id, CURRENT_TIMESTAMP)
    ON CONFLICT DO NOTHING;
    _counter := _counter + 1;

    -- Estudiante 2
    INSERT INTO users (id, email, name, last_name, document_id, date_of_birth, role, status, password_hash, school_id, created_at, updated_at, two_factor_enabled, shift, inclusivo)
    VALUES 
        ('22222222-2222-4222-8222-222222222222', 'maria.rodriguez2@estudiante.com', 'María', 'Rodríguez', 'EST00022002', '2007-07-22', 'estudiante', 'active', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', _school_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, false, 'Tarde', true)
    ON CONFLICT (id) DO UPDATE SET shift = EXCLUDED.shift, inclusivo = EXCLUDED.inclusivo;
    
    SELECT id INTO _student_id FROM users WHERE email = 'maria.rodriguez2@estudiante.com';
    SELECT id INTO _grade_id FROM grade_levels WHERE name = '7°' AND school_id = _school_id LIMIT 1;
    SELECT id INTO _group_id FROM groups WHERE name = 'B' AND school_id = _school_id LIMIT 1;
    
    INSERT INTO student_assignments (id, student_id, grade_id, group_id, shift_id, created_at)
    VALUES (gen_random_uuid(), _student_id, _grade_id, _group_id, _shift_tarde_id, CURRENT_TIMESTAMP)
    ON CONFLICT DO NOTHING;
    _counter := _counter + 1;

    -- Estudiante 3
    INSERT INTO users (id, email, name, last_name, document_id, date_of_birth, role, status, password_hash, school_id, created_at, updated_at, two_factor_enabled, shift, inclusivo)
    VALUES 
        ('33333333-3333-4333-8333-333333333333', 'carlos.lopez3@estudiante.com', 'Carlos', 'López', 'EST00033003', '2009-11-10', 'estudiante', 'active', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', _school_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, false, 'Noche', false)
    ON CONFLICT (id) DO UPDATE SET shift = EXCLUDED.shift, inclusivo = EXCLUDED.inclusivo;
    
    SELECT id INTO _student_id FROM users WHERE email = 'carlos.lopez3@estudiante.com';
    SELECT id INTO _grade_id FROM grade_levels WHERE name = '8°' AND school_id = _school_id LIMIT 1;
    SELECT id INTO _group_id FROM groups WHERE name = 'C' AND school_id = _school_id LIMIT 1;
    
    INSERT INTO student_assignments (id, student_id, grade_id, group_id, shift_id, created_at)
    VALUES (gen_random_uuid(), _student_id, _grade_id, _group_id, _shift_noche_id, CURRENT_TIMESTAMP)
    ON CONFLICT DO NOTHING;
    _counter := _counter + 1;

    -- Estudiante 4
    INSERT INTO users (id, email, name, last_name, document_id, date_of_birth, role, status, password_hash, school_id, created_at, updated_at, two_factor_enabled, shift, inclusivo)
    VALUES 
        ('44444444-4444-4444-8444-444444444444', 'ana.martinez4@estudiante.com', 'Ana', 'Martínez', 'EST00044004', '2006-01-05', 'estudiante', 'active', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', _school_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, false, 'Mañana', true)
    ON CONFLICT (id) DO UPDATE SET shift = EXCLUDED.shift, inclusivo = EXCLUDED.inclusivo;
    
    SELECT id INTO _student_id FROM users WHERE email = 'ana.martinez4@estudiante.com';
    SELECT id INTO _grade_id FROM grade_levels WHERE name = '9°' AND school_id = _school_id LIMIT 1;
    SELECT id INTO _group_id FROM groups WHERE name = 'D' AND school_id = _school_id LIMIT 1;
    
    INSERT INTO student_assignments (id, student_id, grade_id, group_id, shift_id, created_at)
    VALUES (gen_random_uuid(), _student_id, _grade_id, _group_id, _shift_manana_id, CURRENT_TIMESTAMP)
    ON CONFLICT DO NOTHING;
    _counter := _counter + 1;

    -- Estudiante 5
    INSERT INTO users (id, email, name, last_name, document_id, date_of_birth, role, status, password_hash, school_id, created_at, updated_at, two_factor_enabled, shift, inclusivo)
    VALUES 
        ('55555555-5555-4555-8555-555555555555', 'luis.gonzalez5@estudiante.com', 'Luis', 'González', 'EST00055005', '2008-05-18', 'estudiante', 'active', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', _school_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, false, 'Tarde', false)
    ON CONFLICT (id) DO UPDATE SET shift = EXCLUDED.shift, inclusivo = EXCLUDED.inclusivo;
    
    SELECT id INTO _student_id FROM users WHERE email = 'luis.gonzalez5@estudiante.com';
    SELECT id INTO _grade_id FROM grade_levels WHERE name = '10°' AND school_id = _school_id LIMIT 1;
    SELECT id INTO _group_id FROM groups WHERE name = 'A' AND school_id = _school_id LIMIT 1;
    
    INSERT INTO student_assignments (id, student_id, grade_id, group_id, shift_id, created_at)
    VALUES (gen_random_uuid(), _student_id, _grade_id, _group_id, _shift_tarde_id, CURRENT_TIMESTAMP)
    ON CONFLICT DO NOTHING;
    _counter := _counter + 1;

    -- Estudiante 6
    INSERT INTO users (id, email, name, last_name, document_id, date_of_birth, role, status, password_hash, school_id, created_at, updated_at, two_factor_enabled, shift, inclusivo)
    VALUES 
        ('66666666-6666-4666-8666-666666666666', 'laura.perez6@estudiante.com', 'Laura', 'Pérez', 'EST00066006', '2007-09-30', 'estudiante', 'active', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', _school_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, false, 'Noche', true)
    ON CONFLICT (id) DO UPDATE SET shift = EXCLUDED.shift, inclusivo = EXCLUDED.inclusivo;
    
    SELECT id INTO _student_id FROM users WHERE email = 'laura.perez6@estudiante.com';
    SELECT id INTO _grade_id FROM grade_levels WHERE name = '11°' AND school_id = _school_id LIMIT 1;
    SELECT id INTO _group_id FROM groups WHERE name = 'B' AND school_id = _school_id LIMIT 1;
    
    INSERT INTO student_assignments (id, student_id, grade_id, group_id, shift_id, created_at)
    VALUES (gen_random_uuid(), _student_id, _grade_id, _group_id, _shift_noche_id, CURRENT_TIMESTAMP)
    ON CONFLICT DO NOTHING;
    _counter := _counter + 1;

    -- Estudiante 7
    INSERT INTO users (id, email, name, last_name, document_id, date_of_birth, role, status, password_hash, school_id, created_at, updated_at, two_factor_enabled, shift, inclusivo)
    VALUES 
        ('77777777-7777-4777-8777-777777777777', 'pedro.sanchez7@estudiante.com', 'Pedro', 'Sánchez', 'EST00077007', '2009-04-12', 'estudiante', 'active', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', _school_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, false, 'Mañana', false)
    ON CONFLICT (id) DO UPDATE SET shift = EXCLUDED.shift, inclusivo = EXCLUDED.inclusivo;
    
    SELECT id INTO _student_id FROM users WHERE email = 'pedro.sanchez7@estudiante.com';
    SELECT id INTO _grade_id FROM grade_levels WHERE name = '6°' AND school_id = _school_id LIMIT 1;
    SELECT id INTO _group_id FROM groups WHERE name = 'C' AND school_id = _school_id LIMIT 1;
    
    INSERT INTO student_assignments (id, student_id, grade_id, group_id, shift_id, created_at)
    VALUES (gen_random_uuid(), _student_id, _grade_id, _group_id, _shift_manana_id, CURRENT_TIMESTAMP)
    ON CONFLICT DO NOTHING;
    _counter := _counter + 1;

    -- Estudiante 8
    INSERT INTO users (id, email, name, last_name, document_id, date_of_birth, role, status, password_hash, school_id, created_at, updated_at, two_factor_enabled, shift, inclusivo)
    VALUES 
        ('88888888-8888-4888-8888-888888888888', 'sofia.ramirez8@estudiante.com', 'Sofía', 'Ramírez', 'EST00088008', '2008-08-25', 'estudiante', 'active', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', _school_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, false, 'Tarde', false)
    ON CONFLICT (id) DO UPDATE SET shift = EXCLUDED.shift, inclusivo = EXCLUDED.inclusivo;
    
    SELECT id INTO _student_id FROM users WHERE email = 'sofia.ramirez8@estudiante.com';
    SELECT id INTO _grade_id FROM grade_levels WHERE name = '7°' AND school_id = _school_id LIMIT 1;
    SELECT id INTO _group_id FROM groups WHERE name = 'D' AND school_id = _school_id LIMIT 1;
    
    INSERT INTO student_assignments (id, student_id, grade_id, group_id, shift_id, created_at)
    VALUES (gen_random_uuid(), _student_id, _grade_id, _group_id, _shift_tarde_id, CURRENT_TIMESTAMP)
    ON CONFLICT DO NOTHING;
    _counter := _counter + 1;

    -- Estudiante 9
    INSERT INTO users (id, email, name, last_name, document_id, date_of_birth, role, status, password_hash, school_id, created_at, updated_at, two_factor_enabled, shift, inclusivo)
    VALUES 
        ('99999999-9999-4999-8999-999999999999', 'diego.torres9@estudiante.com', 'Diego', 'Torres', 'EST00099009', '2007-12-14', 'estudiante', 'active', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', _school_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, false, 'Noche', true)
    ON CONFLICT (id) DO UPDATE SET shift = EXCLUDED.shift, inclusivo = EXCLUDED.inclusivo;
    
    SELECT id INTO _student_id FROM users WHERE email = 'diego.torres9@estudiante.com';
    SELECT id INTO _grade_id FROM grade_levels WHERE name = '8°' AND school_id = _school_id LIMIT 1;
    SELECT id INTO _group_id FROM groups WHERE name = 'E' AND school_id = _school_id LIMIT 1;
    
    INSERT INTO student_assignments (id, student_id, grade_id, group_id, shift_id, created_at)
    VALUES (gen_random_uuid(), _student_id, _grade_id, _group_id, _shift_noche_id, CURRENT_TIMESTAMP)
    ON CONFLICT DO NOTHING;
    _counter := _counter + 1;

    -- Estudiante 10
    INSERT INTO users (id, email, name, last_name, document_id, date_of_birth, role, status, password_hash, school_id, created_at, updated_at, two_factor_enabled, shift, inclusivo)
    VALUES 
        ('10101010-0101-4010-8101-010101010101', 'carmen.flores10@estudiante.com', 'Carmen', 'Flores', 'EST00101010', '2006-02-08', 'estudiante', 'active', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', _school_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, false, 'Mañana', false)
    ON CONFLICT (id) DO UPDATE SET shift = EXCLUDED.shift, inclusivo = EXCLUDED.inclusivo;
    
    SELECT id INTO _student_id FROM users WHERE email = 'carmen.flores10@estudiante.com';
    SELECT id INTO _grade_id FROM grade_levels WHERE name = '9°' AND school_id = _school_id LIMIT 1;
    SELECT id INTO _group_id FROM groups WHERE name = 'A' AND school_id = _school_id LIMIT 1;
    
    INSERT INTO student_assignments (id, student_id, grade_id, group_id, shift_id, created_at)
    VALUES (gen_random_uuid(), _student_id, _grade_id, _group_id, _shift_manana_id, CURRENT_TIMESTAMP)
    ON CONFLICT DO NOTHING;
    _counter := _counter + 1;

    RAISE NOTICE 'Total de estudiantes y asignaciones creadas: %', _counter;
END
$$;

