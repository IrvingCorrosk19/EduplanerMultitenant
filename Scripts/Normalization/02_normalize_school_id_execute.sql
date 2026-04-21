-- Normalización controlada de school_id al único colegio activo.
-- Requisito: exactamente 1 fila en schools con is_active = true.
-- users: role superadmin → school_id NULL; resto → id del colegio.
-- Reversible: ejecutar dentro de transacción; hacer backup antes en producción.

BEGIN;

DO $$
DECLARE
  sid uuid;
  active_count int;
  r record;
  n bigint;
  sql text;
BEGIN
  SELECT count(*)::int INTO active_count FROM schools WHERE is_active = true;
  IF active_count <> 1 THEN
    RAISE EXCEPTION 'NORMALIZE_ABORT: se requiere exactamente 1 escuela activa (is_active = true). Encontradas: %',
      active_count;
  END IF;

  SELECT id INTO sid FROM schools WHERE is_active = true LIMIT 1;

  FOR r IN
    SELECT c.table_name, c.column_name
    FROM information_schema.columns c
    WHERE c.table_schema = 'public'
      AND c.table_name <> 'users'
      AND c.column_name = 'school_id'
    ORDER BY c.table_name
  LOOP
    sql := format(
      'UPDATE %I SET %I = $1 WHERE %I IS DISTINCT FROM $1',
      r.table_name,
      r.column_name,
      r.column_name
    );
    EXECUTE sql USING sid;
    GET DIAGNOSTICS n = ROW_COUNT;
    RAISE NOTICE 'Updated % column %: % rows', r.table_name, r.column_name, n;
  END LOOP;

  -- Usuarios de colegio: asignar tenant; no tocar superadmin (sigue sin colegio fijo)
  UPDATE users
  SET school_id = sid
  WHERE lower(trim(role)) IS DISTINCT FROM 'superadmin'
    AND school_id IS DISTINCT FROM sid;
  GET DIAGNOSTICS n = ROW_COUNT;
  RAISE NOTICE 'Updated users (non-superadmin): % rows', n;

  UPDATE users
  SET school_id = NULL
  WHERE lower(trim(role)) = 'superadmin'
    AND school_id IS NOT NULL;
  GET DIAGNOSTICS n = ROW_COUNT;
  RAISE NOTICE 'Cleared school_id for superadmin: % rows', n;
END $$;

COMMIT;
