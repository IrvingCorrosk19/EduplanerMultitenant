-- Crear un año académico por defecto para cada escuela que no tenga ninguno.
-- Así se evita el mensaje "No hay años académicos configurados para su escuela".
-- Ejecutar en pgAdmin (o psql) contra la misma BD que usa la app.

-- Requiere que la tabla academic_years exista (migración o ApplyAcademicYearChanges).

INSERT INTO academic_years (
    id,
    school_id,
    name,
    description,
    start_date,
    end_date,
    is_active,
    created_at
)
SELECT
    gen_random_uuid(),
    s.id,
    EXTRACT(YEAR FROM CURRENT_DATE)::text,
    'Año académico creado por script',
    date_trunc('year', CURRENT_DATE)::date,
    (date_trunc('year', CURRENT_DATE) + interval '1 year - 1 day')::date,
    true,
    CURRENT_TIMESTAMP
FROM schools s
WHERE NOT EXISTS (
    SELECT 1 FROM academic_years ay WHERE ay.school_id = s.id
);

-- Ver cuántas filas se insertaron (opcional):
-- SELECT COUNT(*) FROM academic_years;
