-- =============================================================================
-- DATOS PARA QUE EL REPORTE APROBADOS/REPROBADOS MUESTRE FILAS (3T + Media/Premedia)
-- Ejecutar en la MISMA base de datos que usa la aplicación (local o Render).
-- Si usa otra escuela, reemplace el GUID siguiente por el id de su escuela.
-- Para obtener el school_id:  SELECT id, name FROM schools;
-- =============================================================================

-- Reemplace este GUID por el de su escuela si es distinto:
-- 6e42399f-6f17-4585-b92e-fa4fff02cb65 = Instituto Profesional y Técnico San Miguelito

-- Poblar activities con 3T (tercer trimestre activo) para que el reporte traiga datos al elegir 3T
UPDATE activities
SET trimester = '3T',
    "TrimesterId" = (SELECT id FROM trimester t WHERE t.school_id = activities.school_id AND t.name = '3T' LIMIT 1)
WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65';

-- Premedia: 7°, 8°, 9°
UPDATE groups SET grade = '7°' WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' AND name IN ('A','A1','A2') AND (grade IS NULL OR grade = '');
UPDATE groups SET grade = '8°' WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' AND name IN ('B','C','C1','C2') AND (grade IS NULL OR grade = '');
UPDATE groups SET grade = '9°' WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' AND name IN ('D','E','E1','E2') AND (grade IS NULL OR grade = '');

-- Media: 10°, 11°, 12° (para que al elegir "Media" en el reporte aparezcan grupos)
UPDATE groups SET grade = '10°' WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' AND name IN ('F','G','H') AND (grade IS NULL OR grade = '');
UPDATE groups SET grade = '11°' WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' AND name IN ('I','J','K') AND (grade IS NULL OR grade = '');
UPDATE groups SET grade = '12°' WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65' AND name IN ('L','M','N') AND (grade IS NULL OR grade = '');
