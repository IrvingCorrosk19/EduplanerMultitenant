-- Repara actividades creadas sin school_id y/o sin "TrimesterId" (guardado masivo del libro).
-- Resuelve escuela: COALESCE(activities.school_id, users.school_id del docente, groups.school_id).
-- Resuelve trimestre: fila en trimester con mismo school_id y name = activities.trimester (TRIM).
--
-- Requisitos: psql con SSL, por ejemplo:
--   set PGSSLMODE=require
--   psql -h ... -U ... -d ... -f Scripts/backfill_activities_school_trimester.sql

BEGIN;

-- Vista previa (opcional): descomentar para revisar antes de aplicar en otra BD
-- SELECT a.id, a.name, a.trimester, a.school_id, a."TrimesterId",
--        COALESCE(a.school_id, u.school_id, g.school_id) AS resolved_school,
--        t.id AS new_trimester_id
-- FROM activities a
-- LEFT JOIN users u ON u.id = a.teacher_id
-- LEFT JOIN groups g ON g.id = a.group_id
-- LEFT JOIN trimester t ON t.school_id = COALESCE(a.school_id, u.school_id, g.school_id)
--   AND t.name = TRIM(a.trimester)
-- WHERE (a.school_id IS NULL OR a."TrimesterId" IS NULL);

UPDATE activities AS a
SET
  school_id = COALESCE(a.school_id, x.resolved_school_id),
  "TrimesterId" = COALESCE(a."TrimesterId", x.trimester_id)
FROM (
  SELECT
    a2.id,
    COALESCE(a2.school_id, u.school_id, g.school_id) AS resolved_school_id,
    t.id AS trimester_id
  FROM activities a2
  LEFT JOIN users u ON u.id = a2.teacher_id
  LEFT JOIN groups g ON g.id = a2.group_id
  INNER JOIN trimester t
    ON t.school_id = COALESCE(a2.school_id, u.school_id, g.school_id)
   AND t.name = TRIM(BOTH FROM a2.trimester)
  WHERE (a2.school_id IS NULL OR a2."TrimesterId" IS NULL)
    AND COALESCE(a2.school_id, u.school_id, g.school_id) IS NOT NULL
    AND a2.trimester IS NOT NULL
    AND TRIM(BOTH FROM a2.trimester) <> ''
) AS x
WHERE a.id = x.id;

COMMIT;

-- Verificación posterior:
-- SELECT COUNT(*) FROM activities WHERE school_id IS NULL;
-- SELECT COUNT(*) FROM activities WHERE "TrimesterId" IS NULL;
