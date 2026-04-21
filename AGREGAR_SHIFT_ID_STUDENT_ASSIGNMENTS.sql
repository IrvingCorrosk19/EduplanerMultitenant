-- Script para agregar columna shift_id a student_assignments
DO $$
BEGIN
    -- Agregar columna shift_id si no existe
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'student_assignments' AND column_name = 'shift_id') THEN
        ALTER TABLE student_assignments ADD COLUMN shift_id uuid NULL;
        
        -- Crear índice
        CREATE INDEX IX_student_assignments_shift_id ON student_assignments (shift_id);
        
        -- Agregar foreign key
        ALTER TABLE student_assignments 
            ADD CONSTRAINT student_assignments_shift_id_fkey 
            FOREIGN KEY (shift_id) REFERENCES shifts (id) ON DELETE SET NULL;
        
        RAISE NOTICE 'Columna shift_id agregada a student_assignments exitosamente';
    ELSE
        RAISE NOTICE 'Columna shift_id ya existe en student_assignments, omitiendo creación.';
    END IF;
    
    -- Actualizar shift_id basado en el ShiftId del grupo relacionado
    -- Esto migra los datos existentes: si el grupo tiene una jornada, se asigna a la asignación
    UPDATE student_assignments sa
    SET shift_id = g.shift_id
    FROM groups g
    WHERE sa.group_id = g.id 
      AND g.shift_id IS NOT NULL 
      AND sa.shift_id IS NULL;
    
    RAISE NOTICE 'Datos de jornada migrados desde grupos a asignaciones de estudiantes';
END
$$;

