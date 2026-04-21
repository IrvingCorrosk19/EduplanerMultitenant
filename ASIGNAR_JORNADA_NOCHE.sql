-- ============================================
-- SCRIPT: Asignar Jornada Noche a algunos grupos
-- ============================================
-- Distribuir algunos grupos a la jornada "Noche"
-- para tener una distribución más balanceada
-- ============================================

-- IDs Fijos
-- Escuela: 6e42399f-6f17-4585-b92e-fa4fff02cb65

-- Asignar jornada "Noche" a algunos grupos
-- (Tomando algunos grupos de "Mañana" para balancear)

DO $$
DECLARE
    grupos_mañana CURSOR FOR 
        SELECT id, name 
        FROM groups 
        WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65'
          AND shift = 'Mañana'
        ORDER BY name
        LIMIT 9; -- Tomar 9 grupos de Mañana para asignar a Noche
    grupo_rec RECORD;
    contador INTEGER := 0;
BEGIN
    -- Asignar jornada "Noche" a algunos grupos
    FOR grupo_rec IN grupos_mañana LOOP
        UPDATE groups 
        SET shift = 'Noche'
        WHERE id = grupo_rec.id;
        
        contador := contador + 1;
        RAISE NOTICE 'Grupo % asignado a jornada Noche', grupo_rec.name;
    END LOOP;
    
    RAISE NOTICE 'Total de grupos asignados a Noche: %', contador;
END $$;

-- Verificar distribución final
SELECT 
    shift,
    COUNT(*) as cantidad
FROM groups
WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65'
GROUP BY shift
ORDER BY shift;

-- Mostrar algunos grupos con jornada Noche
SELECT 
    id,
    name,
    shift
FROM groups
WHERE school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65'
  AND shift = 'Noche'
ORDER BY name
LIMIT 10;

