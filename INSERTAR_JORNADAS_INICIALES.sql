-- Script para insertar jornadas iniciales
DO $$
DECLARE
    _school_id UUID := '6e42399f-6f17-4585-b92e-fa4fff02cb65'; -- ID de la escuela
    _admin_user_id UUID; -- ID de un usuario admin existente
BEGIN
    -- Obtener un usuario admin de la escuela
    SELECT id INTO _admin_user_id 
    FROM users 
    WHERE school_id = _school_id AND role = 'admin' 
    LIMIT 1;
    
    -- Si no hay admin, usar NULL
    IF _admin_user_id IS NULL THEN
        _admin_user_id := NULL;
    END IF;
    
    -- Insertar jornadas si no existen
    INSERT INTO shifts (id, school_id, name, description, is_active, display_order, created_by, created_at)
    VALUES
        ('a1b2c3d4-e5f6-4789-a012-345678901234', _school_id, 'Mañana', 'Jornada de la mañana', TRUE, 1, _admin_user_id, CURRENT_TIMESTAMP),
        ('b2c3d4e5-f6a7-4890-b123-456789012345', _school_id, 'Tarde', 'Jornada de la tarde', TRUE, 2, _admin_user_id, CURRENT_TIMESTAMP),
        ('c3d4e5f6-a7b8-4901-c234-567890123456', _school_id, 'Noche', 'Jornada nocturna', TRUE, 3, _admin_user_id, CURRENT_TIMESTAMP)
    ON CONFLICT (id) DO NOTHING;
    
    RAISE NOTICE 'Jornadas iniciales insertadas exitosamente';
END
$$;

