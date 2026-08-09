# PLAN_UNIFICACION_LOCAL_EDUPLANER

## Resumen ejecutivo

Se ejecutó una consolidación **solo local** hacia `eduplaner` (proyecto `EduplanerMultitenant`) leyendo `eduplaner2` y `EduplanerIIC` vía `postgres_fdw` desde Render en modo extracción.

Regla aplicada: **Render fue solo lectura** (sin DDL/DML en origen).

Resultado operativo:

- Carga de `EduplanerIIC` completada para los deltas faltantes en local.
- Carga de `eduplaner2` completada parcialmente.
- Se **detuvo** la carga total al identificar conflicto estructural crítico de consolidación (IDs de `grade_levels` no coexistentes + constraints de unicidad global que impiden coexistencia limpia sin mapeo explícito).

## Estrategia de consolidación aplicada

1. **Análisis previo** (estructura y datos) en las 3 bases.
2. **Respaldo lógico local** antes de carga.
3. **Conectividad federada local** (`postgres_fdw`) a ambas fuentes remotas.
4. **Carga incremental auditable** (`ON CONFLICT DO NOTHING`) con bitácora en `public.unification_audit_log`.
5. **Remediación local no destructiva** para referencias cíclicas (`users`/`schools`) usando inserción por fases y backfill de FK nullable cuando fue seguro.
6. **Validación post-carga** (conteos y checks de integridad críticos).

## Tablas involucradas (núcleo)

- Tenant/core: `schools`, `users`
- Académico: `grade_levels`, `groups`, `subjects`, `subject_assignments`, `student_assignments`, `teacher_assignments`, `academic_years`, `shifts`, `time_slots`
- Operación: `attendance`, `activities`, `student_activity_scores`, `messages`
- Carnet/pagos: `student_payment_access`, `student_id_cards`, `student_qr_tokens`, `school_id_card_settings`

## Orden de carga aplicado

1. `users` (fase segura con `school_id`, `created_by`, `updated_by` temporalmente null para romper ciclo)
2. `schools` (fase segura con `admin_id`, `created_by`, `updated_by` temporalmente null)
3. Backfill de FK nullable (`users.school_id`, `schools.admin_id`, `created_by/updated_by`) cuando referencia existía
4. Catálogos y dimensiones
5. Relaciones académicas
6. Transaccionales (asistencia, pagos, carnet, etc.)

## Reglas de mapeo usadas

- **Sin tocar IDs de origen** (UUID preservado en inserciones exitosas).
- `subject_assignments`: mapeo de columna fuente `"SchoolId"` a destino `school_id`.
- En tablas con FK metadata nullable (`created_by`, `updated_by`, `admin_id`) se usó null temporal para permitir carga y luego backfill seguro.

## Manejo de duplicados aplicado

- Clave técnica: `ON CONFLICT DO NOTHING`.
- Duplicados semánticos detectados en usuarios por `email` y/o `document_id` globales en destino provocaron no inserción de algunos IDs fuente (sin sobrescritura).

## Riesgos encontrados durante ejecución

1. **Bloqueo de consolidación total de `eduplaner2.student_assignments`**:
   - Faltan en destino 6 IDs de `grade_levels` del tenant eduplaner2.
   - Causa: conflicto por unicidad global (no tenant-aware) y desacople de IDs entre catálogos.
2. **Colisión de identidad de usuario** (`users_email_key` / `users_document_id_key`) entre tenants.
3. **Integridad referencial estricta** impide carga ciega sin mapeo (correcto desde perspectiva de seguridad de datos).

## Estado final del plan

- Consolidación **parcial y segura** ejecutada.
- Consolidación **total** detenida por riesgo de corrupción lógica (mezcla/mapeo implícito de grados entre tenants).
- Se requiere fase siguiente de **mapeo explícito de catálogos multi-tenant** antes de completar la carga de `eduplaner2.student_assignments`.

## Evidencia técnica (artefactos)

- Backup: `migration_artifacts/backups/eduplaner_pre_unification_20260423_070118.dump`
- Reportes: `migration_artifacts/reports/*`
- SQL de ejecución local:
  - `migration_artifacts/fdw_setup.sql`
  - `migration_artifacts/load_unification.sql`
