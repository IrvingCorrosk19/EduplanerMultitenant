# FASE_2B_PLAN_CONSOLIDACION_TENANT_SAFE

## Resumen ejecutivo

Se ejecutó Fase 2B para completar la consolidación de `eduplaner2` en la base local `eduplaner`, manteniendo Render en solo lectura y reforzando el modelo tenant-safe en local.

Objetivo de cierre alcanzado en pendientes críticos:

- `users` faltantes (`edu2`): 3 -> 0
- `grade_levels` faltantes (`edu2`): 6 -> 0
- `student_assignments` faltantes (`edu2`): 1391 -> 0

## Problema real detectado

Bloqueos de Fase previa:

1. Unicidad global de `grade_levels.name` impedía coexistencia de grados homónimos entre escuelas.
2. Unicidad global de `users.email` y `users.document_id` impedía coexistencia tenant-safe de identidades entre escuelas.
3. `student_assignments` de `edu2` no cargaban por FK a `grade_levels` no insertados.

## Estrategia tenant-safe aplicada

1. **Hardening estructural local** (sin tocar Render):
   - `grade_levels`: unicidad por `(school_id, name)`.
   - `users`: unicidad por tenant:
     - `(school_id, lower(email))`
     - `(school_id, document_id)` (parcial `document_id is not null`).
2. **Tablas de trazabilidad persistentes** en esquema `migration_map`.
3. **Carga pendiente controlada** de `edu2` por fases:
   - usuarios faltantes,
   - catálogos faltantes,
   - asignaciones faltantes con validación de FK.
4. **Validación de aislamiento y FK** post-carga.

## Riesgos y controles

- Riesgo de mezcla semántica entre escuelas: mitigado con unicidad tenant-safe y checks cruzados escuela-alumno-grado-grupo.
- Riesgo de pérdida de trazabilidad: mitigado con `migration_map.*` por entidad.
- Riesgo de regresión de integridad: mitigado con validaciones FK críticas (huérfanos = 0 en checks auditados).

## Orden de ejecución aplicado

1. Backup local pre-Fase2B.
2. Cambios estructurales locales (`phase2b_structural.sql`).
3. Carga de pendientes `edu2` (`users`, `grade_levels`, `student_assignments`).
4. Población de tablas de mapeo.
5. Validación final y documentación.

## Scripts y evidencia

- `migration_artifacts/phase2b_structural.sql`
- `migration_artifacts/reports/local_counts_delta_nonzero_phase2b.csv`
- `migration_artifacts/reports/mapping_summary_phase2b.csv`
- `migration_artifacts/backups/eduplaner_pre_phase2b_20260423_073144.dump`
