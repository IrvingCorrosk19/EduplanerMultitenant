# CAMBIOS_ESTRUCTURALES_LOCALES_MULTITENANT

## Alcance

Cambios aplicados **solo en local** (`eduplaner`).
Render no fue modificado.

## 1) Índices/constraints modificados

### grade_levels

- Removido índice único global:
  - `grade_levels_name_key` sobre `(name)`
- Agregado índice único tenant-safe:
  - `uq_grade_levels_school_name` sobre `(school_id, name)`

Motivo:

- Permitir que dos escuelas tengan etiquetas de grado iguales (por ejemplo `7`, `8`, etc.) sin colisión global.

### users

- Removidos índices únicos globales:
  - `users_email_key` sobre `(email)`
  - `users_document_id_key` sobre `(document_id)`
- Agregados índices únicos tenant-safe:
  - `uq_users_school_email_ci` sobre `(school_id, lower(email))`
  - `uq_users_school_document` sobre `(school_id, document_id)` con filtro `document_id is not null`

Motivo:

- Evitar colisiones entre escuelas con identidad semántica similar.
- Mantener unicidad dentro del tenant.

## 2) Nuevas estructuras persistentes de trazabilidad

### Esquema

- `migration_map`

### Tablas creadas

- `migration_map.school_map`
- `migration_map.user_map`
- `migration_map.grade_level_map`
- `migration_map.student_assignment_map`

### Campos clave comunes

- `source_system`
- `source_table`
- `legacy_id`
- `target_id`
- `school_id`
- `map_status`
- `notes`
- `created_at`

Motivo:

- Trazabilidad completa origen -> destino por entidad crítica.
- Auditoría de mapeos y conflictos.

## 3) Motivo de negocio de los cambios

Sin estos ajustes, la consolidación 100% de `eduplaner2` no era viable:

- 3 `users` bloqueados,
- 6 `grade_levels` bloqueados,
- 1391 `student_assignments` bloqueados por FK.

Los cambios habilitaron coexistencia multi-tenant sin mezclar catálogos entre escuelas.

## 4) Reversibilidad

- Backup local previo documentado:
  - `migration_artifacts/backups/eduplaner_pre_phase2b_20260423_073144.dump`
- DDL versionado en script:
  - `migration_artifacts/phase2b_structural.sql`
