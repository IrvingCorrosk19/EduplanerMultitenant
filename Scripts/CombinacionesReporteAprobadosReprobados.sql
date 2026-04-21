-- =============================================================================
-- COMBINACIONES PARA PROBAR EL REPORTE APROBADOS/REPROBADOS
-- Ejecutar en PostgreSQL. Resultados indican qué filtros usar en la pantalla.
-- =============================================================================

-- 1) Escuelas y trimestres que tienen actividades (origen del dropdown Trimestre)
SELECT
  s.id AS school_id,
  s.name AS escuela,
  array_agg(DISTINCT a.trimester ORDER BY a.trimester) AS trimestres_disponibles
FROM schools s
LEFT JOIN activities a ON a.school_id = s.id AND a.trimester IS NOT NULL
GROUP BY s.id, s.name
ORDER BY s.name;

-- 2) Grupos por escuela y grado (valores para Nivel Premedia: 7°, 8°, 9° | Media: 10°, 11°, 12°)
SELECT
  g.school_id,
  s.name AS escuela,
  g.grade AS grado,
  g.name AS grupo,
  (SELECT COUNT(*) FROM student_assignments sa WHERE sa.group_id = g.id AND sa.is_active = true) AS estudiantes_activos
FROM groups g
JOIN schools s ON s.id = g.school_id
ORDER BY g.school_id, g.grade, g.name;

-- 3) Combinaciones con datos para el reporte: escuela + trimestre + grado + grupo
--    donde existen actividades en ese trimestre y estudiantes en el grupo (con o sin notas)
WITH trimestres_por_escuela AS (
  SELECT DISTINCT a.school_id, a.trimester
  FROM activities a
  WHERE a.trimester IS NOT NULL
),
grupos_con_estudiantes AS (
  SELECT g.id, g.school_id, g.grade, g.name,
         COUNT(sa.student_id) FILTER (WHERE sa.is_active = true) AS activos
  FROM groups g
  LEFT JOIN student_assignments sa ON sa.group_id = g.id
  GROUP BY g.id, g.school_id, g.grade, g.name
  HAVING COUNT(sa.student_id) FILTER (WHERE sa.is_active = true) > 0
)
SELECT
  s.name AS escuela,
  t.trimester AS trimestre,
  CASE
    WHEN g.grade IN ('7°','8°','9°') THEN 'Premedia'
    WHEN g.grade IN ('10°','11°','12°') THEN 'Media'
    ELSE 'Otro'
  END AS nivel_educativo,
  g.grade AS grado,
  g.name AS grupo,
  g.activos AS estudiantes_activos,
  (SELECT COUNT(DISTINCT sas.student_id)
   FROM student_activity_scores sas
   JOIN activities ac ON ac.id = sas.activity_id AND ac.school_id = g.school_id AND ac.trimester = t.trimester
   JOIN student_assignments sa2 ON sa2.student_id = sas.student_id AND sa2.group_id = g.id AND sa2.is_active = true
  ) AS estudiantes_con_al_menos_una_nota
FROM trimestres_por_escuela t
JOIN grupos_con_estudiantes g ON g.school_id = t.school_id
JOIN schools s ON s.id = g.school_id
ORDER BY s.name, t.trimester, g.grade, g.name;

-- 4) Resumen rápido: una fila por (escuela, trimestre, nivel) con total grupos y estudiantes
WITH trimestres_por_escuela AS (
  SELECT DISTINCT a.school_id, a.trimester FROM activities a WHERE a.trimester IS NOT NULL
),
grupos_con_estudiantes AS (
  SELECT g.school_id, g.grade, g.name, g.id,
         COUNT(sa.student_id) FILTER (WHERE sa.is_active = true) AS activos
  FROM groups g
  LEFT JOIN student_assignments sa ON sa.group_id = g.id
  GROUP BY g.id, g.school_id, g.grade, g.name
  HAVING COUNT(sa.student_id) FILTER (WHERE sa.is_active = true) > 0
)
SELECT
  s.name AS escuela,
  t.trimester AS trimestre,
  CASE WHEN g.grade IN ('7°','8°','9°') THEN 'Premedia' WHEN g.grade IN ('10°','11°','12°') THEN 'Media' ELSE 'Otro' END AS nivel,
  COUNT(DISTINCT g.id) AS grupos_con_estudiantes,
  SUM(g.activos) AS total_estudiantes
FROM trimestres_por_escuela t
JOIN grupos_con_estudiantes g ON g.school_id = t.school_id
JOIN schools s ON s.id = g.school_id
GROUP BY s.id, s.name, t.trimester,
  CASE WHEN g.grade IN ('7°','8°','9°') THEN 'Premedia' WHEN g.grade IN ('10°','11°','12°') THEN 'Media' ELSE 'Otro' END
ORDER BY s.name, t.trimester, nivel;
