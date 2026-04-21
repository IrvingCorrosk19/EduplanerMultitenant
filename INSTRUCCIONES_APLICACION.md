# 🚀 Instrucciones para Aplicar los Cambios

## Paso 1: Aplicar Cambios a la Base de Datos

### Opción A: Desde la Terminal (Recomendado)

1. **Detener la aplicación** si está corriendo (Ctrl+C)

2. **Ejecutar el script de aplicación**:
   ```bash
   dotnet run -- --apply-academic-year
   ```

3. **Verificar la salida**: Deberías ver mensajes como:
   ```
   🔍 Verificando y aplicando cambios de Año Académico...
   ➕ Creando tabla academic_years...
   ✅ Tabla academic_years creada
   ➕ Agregando columna academic_year_id a trimester...
   ✅ Columna academic_year_id agregada
   ...
   ✅ Todos los cambios de Año Académico aplicados correctamente!
   ```

### Opción B: Desde la Aplicación (Si tienes endpoint)

Si creaste un endpoint en el controlador, puedes acceder a:
```
http://localhost:5172/Prematriculation/ApplyDatabaseChanges
```

---

## Paso 2: Crear el Primer Año Académico

### Opción A: Desde SQL Directo

```sql
-- 1. Obtener el ID de tu escuela
SELECT id, name FROM schools;

-- 2. Crear el año académico (reemplaza SCHOOL_ID con el ID de tu escuela)
INSERT INTO academic_years (
    id, 
    school_id, 
    name, 
    description, 
    start_date, 
    end_date, 
    is_active, 
    created_at
) VALUES (
    gen_random_uuid(),
    'TU_SCHOOL_ID_AQUI',  -- ⚠️ REEMPLAZAR
    '2024-2025',
    'Año académico 2024-2025',
    '2024-01-15 00:00:00+00',
    '2024-12-15 23:59:59+00',
    true,
    CURRENT_TIMESTAMP
);
```

### Opción B: Desde la Aplicación (Cuando tengas UI)

Puedes crear un controlador para gestionar años académicos o usar el servicio directamente.

---

## Paso 3: Vincular Trimestres al Año Académico

```sql
-- Vincular todos los trimestres de la escuela al año académico activo
UPDATE trimester 
SET academic_year_id = (
    SELECT id FROM academic_years 
    WHERE is_active = true 
    AND school_id = (SELECT school_id FROM academic_years WHERE is_active = true LIMIT 1)
    LIMIT 1
)
WHERE school_id = (
    SELECT school_id FROM academic_years WHERE is_active = true LIMIT 1
);
```

---

## Paso 4: (Opcional) Vincular Datos Históricos

Si tienes datos históricos y quieres vincularlos a años académicos anteriores:

```sql
-- 1. Crear año académico histórico 2023-2024
INSERT INTO academic_years (
    id, school_id, name, start_date, end_date, is_active, created_at
) VALUES (
    gen_random_uuid(),
    (SELECT id FROM schools LIMIT 1),
    '2023-2024',
    '2023-01-15 00:00:00+00',
    '2023-12-15 23:59:59+00',
    false,  -- Inactivo porque es histórico
    CURRENT_TIMESTAMP
);

-- 2. Vincular notas históricas (basado en fecha de creación)
UPDATE student_activity_scores
SET academic_year_id = (
    SELECT id FROM academic_years WHERE name = '2023-2024' LIMIT 1
)
WHERE created_at >= '2023-01-01' 
  AND created_at < '2024-01-01'
  AND academic_year_id IS NULL;

-- 3. Vincular asignaciones históricas
UPDATE student_assignments
SET academic_year_id = (
    SELECT id FROM academic_years WHERE name = '2023-2024' LIMIT 1
)
WHERE created_at >= '2023-01-01' 
  AND created_at < '2024-01-01'
  AND academic_year_id IS NULL;
```

---

## ✅ Verificación

Después de aplicar los cambios, verifica que todo esté correcto:

```sql
-- Verificar que la tabla existe
SELECT * FROM academic_years;

-- Verificar que las columnas fueron agregadas
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name IN ('academic_years', 'trimester', 'student_assignments', 'student_activity_scores')
AND column_name = 'academic_year_id';

-- Verificar índices
SELECT indexname FROM pg_indexes 
WHERE tablename IN ('academic_years', 'trimester', 'student_assignments', 'student_activity_scores')
AND indexname LIKE '%academic_year%';
```

---

## 🎯 Listo para Usar

Una vez completados estos pasos, el sistema estará completamente funcional:

- ✅ Las nuevas notas se asignarán automáticamente al año académico activo
- ✅ Las nuevas asignaciones se asignarán automáticamente al año académico activo
- ✅ Las consultas filtrarán por año académico activo
- ✅ El historial se preservará completamente

---

**Nota**: Si no creas un año académico, el sistema seguirá funcionando normalmente (compatibilidad hacia atrás), pero las nuevas notas y asignaciones no tendrán `AcademicYearId` asignado hasta que crees uno.

