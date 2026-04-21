-- Verificar estructura y datos de configuración de jornada (mañana/tarde)
-- Ejecutar en la base de datos de la aplicación (por ejemplo con psql o pgAdmin).

-- 1) Estructura: columnas de la tabla
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'school_schedule_configurations'
ORDER BY ordinal_position;

-- 2) Datos actuales (una fila por escuela)
SELECT 
  id,
  school_id,
  morning_start_time,
  morning_block_duration_minutes,
  morning_block_count,
  afternoon_start_time,
  afternoon_block_duration_minutes,
  afternoon_block_count,
  created_at,
  updated_at
FROM school_schedule_configurations;

-- 3) Si las columnas de tarde no existen, añadirlas (PostgreSQL):
-- ALTER TABLE school_schedule_configurations ADD COLUMN IF NOT EXISTS afternoon_start_time time NULL;
-- ALTER TABLE school_schedule_configurations ADD COLUMN IF NOT EXISTS afternoon_block_duration_minutes integer NULL;
-- ALTER TABLE school_schedule_configurations ADD COLUMN IF NOT EXISTS afternoon_block_count integer NULL;
