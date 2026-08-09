# VALIDACION_POST_CARGA_EDUPLANER

## 1) Estado de respaldo y trazabilidad

- Respaldo previo generado:
  - `migration_artifacts/backups/eduplaner_pre_unification_20260423_070118.dump`
- Evidencias de ejecución:
  - `migration_artifacts/fdw_setup.sql`
  - `migration_artifacts/load_unification.sql`
  - `migration_artifacts/reports/*.csv`

## 2) Conteos pre vs post (local)

Cambios no cero (extracto):

| Tabla | Pre | Post | Delta |
|---|---:|---:|---:|
| users | 1983 | 3339 | +1356 |
| student_activity_scores | 11278 | 14765 | +3487 |
| attendance | 6464 | 6980 | +516 |
| student_payment_access | 699 | 1123 | +424 |
| activities | 756 | 985 | +229 |
| groups | 27 | 46 | +19 |
| schools | 1 | 2 | +1 |
| subject_assignments | 1142 | 1143 | +1 |
| student_assignments | 1854 | 1857 | +3 |

Fuente: `migration_artifacts/reports/local_counts_delta_nonzero.csv`

## 3) Validación de integridad referencial crítica

Checks ejecutados (post-carga):

- `student_assignments.student_id` huérfanos: 0
- `student_assignments.grade_id` huérfanos: 0
- `student_assignments.group_id` huérfanos: 0
- `student_payment_access.student_id` huérfanos: 0
- `student_payment_access.school_id` huérfanos: 0
- `student_id_cards.student_id` huérfanos: 0
- `student_qr_tokens.student_id` huérfanos: 0
- `subject_assignments.grade_level_id` huérfanos: 0
- `subject_assignments.group_id` huérfanos: 0
- `subject_assignments.subject_id` huérfanos: 0

Fuente: `migration_artifacts/reports/fk_critical_checks_post_load.csv`

## 4) Cobertura de carga por origen

### EduplanerIIC -> local

Validación por IDs (muestras críticas):

- `users` faltantes: 0
- `student_assignments` faltantes: 0
- `student_activity_scores` faltantes: 0
- `attendance` faltantes: 0

Resultado: **deltas pendientes de IIC absorbidos**.

### eduplaner2 -> local

Validación por IDs (tablas clave):

- `schools` faltantes: 0
- `users` faltantes: 3
- `grade_levels` faltantes: 6
- `subject_assignments` faltantes: 0
- `student_payment_access` faltantes: 0
- `student_id_cards` faltantes: 0
- `student_qr_tokens` faltantes: 0
- `student_assignments` faltantes: 1391

## 5) Registros conflictivos / bloqueos detectados

### Conflictos de identidad de usuario

De los usuarios no incorporados por ID desde `eduplaner2`, se confirmaron colisiones por `email` con IDs distintos ya existentes en local.

### Bloqueo estructural de asignaciones académicas

`student_assignments` de `eduplaner2` quedó incompleto por dependencia a `grade_levels` no insertables 1:1 (conflicto por unicidad/catálogo).

Diagnóstico numérico de faltantes de `student_assignments` (`eduplaner2`):

- Total faltantes: 1391
- Causa FK `grade_id` no resoluble en estado actual: 1391

## 6) Decisión operativa aplicada

Se aplicó la regla de oro de migración segura:

- **Se detuvo la carga total** al detectar riesgo de mezclar/mal-mapear grados entre tenants para forzar la inserción de 1391 `student_assignments`.
- No se realizaron cambios destructivos ni en Render ni en local.

## 7) Conclusión post-carga

- La consolidación quedó **parcial, consistente y auditable**.
- `EduplanerIIC` quedó consolidado con cobertura práctica completa en tablas críticas.
- `eduplaner2` quedó consolidado en varias tablas clave (incluyendo usuarios, escuela, pagos/carnet), pero **no completa** en `student_assignments` por bloqueo estructural de compatibilidad.

Estado final: **no hay pérdida por borrado ni corrupción referencial detectada**, pero sí hay **incompletitud controlada** de `eduplaner2` documentada y pendiente de resolución de mapeo de catálogo.
