# ANALISIS_COMPATIBILIDAD_BDS_RENDER_VS_LOCAL

## Alcance

Comparación técnica entre:

- Render `eduplaner2` (`schoolmanager_hx5i`)
- Render `EduplanerIIC` (`schoolmanagement_xqks`)
- Local destino `eduplaner` (`EduplanerMultitenant`)

Todo en modo lectura sobre Render.

## Comparación estructural (alto nivel)

| Base | Tablas | Columnas | Constraints | Índices |
|---|---:|---:|---:|---:|
| eduplaner2_render | 51 | 595 | 472 | 209 |
| eduiic_render | 51 | 595 | 189 | 209 |
| multitenant_local (pre) | 50 | 582 | 457 | 207 |

Hallazgos:

1. `eduplaner2` y `EduplanerIIC` son estructuralmente muy similares.
2. Local no era 1:1: faltaba `EmailConfigurations` y había divergencias puntuales de naming (`SchoolId` vs `school_id` en `subject_assignments`).
3. Diferencia de versiones PostgreSQL entre orígenes (17.9 / 18.3) introduce diferencias de metadatos de constraints.

## Diferencias por tablas

- Tabla presente en Render y ausente en local: `EmailConfigurations`.
- En `subject_assignments`, columna tenant en origen remoto como `"SchoolId"`, en local como `school_id`.

## Diferencias por columnas / constraints con impacto de consolidación

1. **Usuarios**
   - Unicidad global en local por `email` y `document_id`.
   - Esto bloquea inserción de IDs distintos de usuarios de otra fuente si comparten identidad semántica.

2. **Catálogos académicos**
   - Se detectó conflicto operacional en `grade_levels`:
     - 6 IDs de `grade_levels` de `eduplaner2` no pudieron coexistir por conflicto de unicidad (sin key tenant-aware en ese constraint).
     - Consecuencia directa: 1391 filas de `student_assignments` de `eduplaner2` quedaron bloqueadas por FK a `grade_id`.

3. **Tablas dependientes**
   - `student_assignments` quedó como principal cuello de botella para completar consolidación total de eduplaner2.

## Conflictos de datos (reales)

### Usuarios de eduplaner2 no incorporados por ID

Quedaron 3 IDs de `src_edu2.users` no presentes en `public.users` tras carga segura.

- 2 tienen colisión de `email` con usuarios existentes en local (IDs distintos).
- 1 adicional quedó fuera del set final de inserción por cadena de dependencias/ciclos de FK durante la carga incremental.

### Asignaciones de estudiantes de eduplaner2

- `src_edu2.student_assignments` faltantes en local por ID: **1391**.
- Diagnóstico:
  - `missing_grade_fk`: **1391**
  - `missing_user_fk`: 2
  - `missing_group_fk`: 0

## Viabilidad real de convivencia

### Lo que sí es viable ahora

- Convivencia de una porción amplia del modelo (usuarios, pagos carnet, QR, catálogos parciales, datos de IIC faltantes en local).
- Integridad referencial crítica post-carga en checks ejecutados: sin violaciones en claves revisadas.

### Lo que no es viable sin mapeo adicional

- Consolidación completa de `eduplaner2.student_assignments` en el estado actual del esquema y constraints.
- Carga ciega de catálogos con IDs de origen preservados cuando existen reglas de unicidad global no tenant-aware.

## Conclusión de compatibilidad

Compatibilidad **parcial** para consolidación directa.

- **Compatible**: gran parte de entidades por UUID + `ON CONFLICT`.
- **No compatible** (sin reglas de mapeo explícito): asignaciones académicas de eduplaner2 dependientes de catálogos no insertables 1:1.

Estado: la base local puede operar con datos consolidados parciales, pero no puede declararse consolidación completa de ambos orígenes mientras persista el conflicto de `grade_levels`/`student_assignments`.
