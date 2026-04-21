-- Diagnóstico de login: jaime.ramos20@meduca.edu.pa
-- Ejecutar en la BD donde intenta entrar (local o Render).

\echo '=== Usuario por email (jaime.ramos20@meduca.edu.pa) ==='
SELECT
  id,
  name,
  last_name,
  email,
  role,
  status,
  school_id,
  CASE WHEN password_hash IS NULL THEN 'NULL'
       WHEN password_hash LIKE '$2%' THEN 'BCrypt (OK)'
       ELSE 'Texto plano o otro'
  END AS password_tipo,
  LENGTH(password_hash) AS password_len,
  last_login
FROM users
WHERE LOWER(TRIM(email)) = LOWER(TRIM('jaime.ramos20@meduca.edu.pa'));

\echo ''
\echo '=== Si tiene school_id: estado de la escuela ==='
SELECT
  u.email,
  u.school_id,
  s.name AS school_name,
  s.is_active AS school_activa
FROM users u
LEFT JOIN schools s ON s.id = u.school_id
WHERE LOWER(TRIM(u.email)) = LOWER(TRIM('jaime.ramos20@meduca.edu.pa'));

\echo ''
\echo '=== Conclusión para login ==='
\echo '  - Si no sale fila: usuario no existe o email distinto.'
\echo '  - status debe ser active.'
\echo '  - Si school_id no es null, schools.is_active debe ser true.'
