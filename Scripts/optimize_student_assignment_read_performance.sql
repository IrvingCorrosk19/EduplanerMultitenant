-- Optimización lectura: StudentAssignment/Index + GetAllStudentsAsync
-- Ejecutar con PostgreSQL 15+ (soporta IF NOT EXISTS en CONCURRENTLY):
--   set PGPASSWORD=...
--   "C:\Program Files\PostgreSQL\18\bin\psql.exe" -h HOST -p 5432 -U USER -d DB -f optimize_student_assignment_read_performance.sql
--
-- CONCURRENTLY: no bloquea escrituras prolongadas; cada CREATE es su propia transacción.

-- Lista de estudiantes por escuela + filtro LOWER(role) (alinea con UserService.GetAllStudentsAsync)
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_users_school_id_lower_role
  ON users (school_id, lower(role))
  WHERE school_id IS NOT NULL;

-- Carga masiva: WHERE student_id IN (...) AND is_active ORDER BY student_id, created_at DESC
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_student_assignments_active_student_created_at
  ON student_assignments (student_id, created_at DESC NULLS LAST)
  WHERE is_active = true;
