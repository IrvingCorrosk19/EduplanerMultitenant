-- Verificación de años académicos para Schedule/ByTeacher
-- Ejecutar en la BD (pgAdmin, psql, etc.) para comprobar por qué no aparece el desplegable "Año académico".

-- 1) ¿Existe la tabla?
SELECT EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'academic_years'
) AS tabla_academic_years_existe;

-- 2) Total de años académicos y por escuela
SELECT school_id, COUNT(*) AS cantidad
FROM academic_years
GROUP BY school_id
ORDER BY school_id;

-- 3) Listado de años académicos (para cruzar con school_id del usuario)
SELECT id, name, school_id, start_date, end_date, is_active
FROM academic_years
ORDER BY school_id, start_date DESC;

-- 4) school_id de los usuarios que usan la app (para comparar con 2 y 3)
SELECT id, name, last_name, email, school_id, role
FROM users
WHERE role IN ('teacher', 'docente', 'admin', 'director')
ORDER BY school_id, email
LIMIT 20;

-- Si en (2) o (3) no hay filas para el school_id de tu usuario, el desplegable estará vacío.
-- Solución: insertar al menos un año académico para esa escuela (p. ej. desde Prematrícula o Administración).
