# MAPEO_CATALOGOS_Y_EQUIVALENCIAS

## Principio aplicado

- Mismo nombre **no** implica misma entidad.
- Mapeo por ID fuente y contexto de escuela (tenant).
- En caso de duda, separación por tenant.

## 1) Schools

Tabla: `migration_map.school_map`

Regla:

- Mapeo por `legacy_id` -> `target_id`.
- Estado esperado: `mapped`.

Resultado resumido:

- `eduplaner2_render`: 1 escuela mapeada.
- `eduiic_render`: 1 escuela mapeada.

## 2) Users

Tabla: `migration_map.user_map`

Regla de resolución:

1. `mapped_by_id` si existe mismo UUID.
2. `mapped_by_email` si no hay UUID pero coincide `(school_id, lower(email))`.
3. `mapped_by_document` si no hay lo anterior y coincide `(school_id, document_id)`.
4. `unmapped` si no existe correspondencia.

Resultado resumido:

- `eduplaner2_render`: 1359 `mapped_by_id`
- `eduiic_render`: 1984 `mapped_by_id`

## 3) Grade Levels

Tabla: `migration_map.grade_level_map`

Regla:

- Se preserva `legacy_id` como `target_id` cuando es insertable.
- Convivencia habilitada por uniqueness `(school_id, name)`.

Resultado resumido:

- `eduplaner2_render`: 7 `mapped_by_id`
- `eduiic_render`: 6 `mapped_by_id`

## 4) Student Assignments

Tabla: `migration_map.student_assignment_map`

Regla:

- Mapeo por `legacy_id` -> `target_id`.
- Solo se considera válido si padres FK existen en tenant correcto.

Resultado resumido:

- `eduplaner2_render`: 1393 `mapped_by_id`
- `eduiic_render`: 1855 `mapped_by_id`

## 5) Evidencia

Archivo de resumen:

- `migration_artifacts/reports/mapping_summary_phase2b.csv`

Campos trazables en tablas de mapeo:

- `source_system`, `source_table`, `legacy_id`, `target_id`, `school_id`, `map_status`, `notes`, `created_at`
