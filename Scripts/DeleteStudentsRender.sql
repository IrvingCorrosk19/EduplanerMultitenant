-- Elimina todos los usuarios con rol estudiante en Render (y sus dependencias).
-- Ejecutar contra BD Render. Idempotente en orden de FKs.

BEGIN;

WITH student_ids AS (
  SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante')
),
n AS (SELECT COUNT(*) AS total FROM student_ids)
SELECT total AS "Usuarios estudiante a eliminar" FROM n;

-- Orden según FKs (email_queues puede no existir en Render aún)
DELETE FROM student_activity_scores WHERE student_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante'));
DELETE FROM attendance WHERE student_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante'));
DELETE FROM discipline_reports WHERE student_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante'));
DELETE FROM orientation_reports WHERE student_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante'));
UPDATE payments SET student_id = NULL WHERE student_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante'));
DELETE FROM prematriculation_histories WHERE prematriculation_id IN (SELECT id FROM prematriculations WHERE student_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante')));
DELETE FROM prematriculations WHERE student_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante'));
DELETE FROM student_assignments WHERE student_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante'));
DELETE FROM student_payment_access WHERE student_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante'));
DELETE FROM student_id_cards WHERE student_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante'));
DELETE FROM student_qr_tokens WHERE student_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante'));
DELETE FROM scan_logs WHERE student_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante'));
DELETE FROM audit_logs WHERE user_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante'));
DELETE FROM user_grades WHERE user_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante'));
DELETE FROM user_groups WHERE user_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante'));
DELETE FROM user_subjects WHERE user_id IN (SELECT id FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante'));

DELETE FROM users WHERE LOWER(TRIM(role)) IN ('student', 'estudiante');

COMMIT;
