# Normalización `school_id` — único colegio activo (Eduplaner)

Fecha de ejecución local: **2026-04-20**  
Base de datos: **eduplaner** (PostgreSQL 18, host `localhost`)  
Herramienta: `psql` desde `C:\Program Files\PostgreSQL\18\bin` (usuario `postgres`, contraseña vía variable de entorno `PGPASSWORD`).

---

## 1. Colegio detectado

| Campo | Valor |
|--------|--------|
| **Validación** | `SELECT count(*) FROM schools WHERE is_active = true` → **1** (condición obligatoria cumplida; si fuera ≠ 1 el script aborta con `NORMALIZE_ABORT`). |
| **id** | `6e42399f-6f17-4585-b92e-fa4fff02cb65` |
| **name** | Instituto Profesional y Técnico San Miguelito |
| **is_active** | `true` |

No se hardcodeó el UUID en el script de ejecución: se obtiene en tiempo de ejecución con `SELECT id FROM schools WHERE is_active = true LIMIT 1`.

---

## 2. Tablas revisadas

Se tomaron **todas** las tablas del esquema `public` con columna `school_id` (snake_case), según `information_schema.columns`, **excluyendo** `users` del bucle genérico para aplicar reglas explícitas de rol.

Incluidas en el bucle dinámico (entre otras): `academic_years`, `activities`, `activity_types`, `area`, `attendance`, `audit_logs`, `counselor_assignments`, `discipline_reports`, `email_configurations`, `email_jobs`, `grade_levels`, `groups`, `id_card_template_fields`, `messages`, `orientation_reports`, `payment_concepts`, `payments`, `prematriculation_periods`, `prematriculations`, `school_id_card_settings`, `school_schedule_configurations`, `security_settings`, `shifts`, `specialties`, `student_activity_scores`, `student_payment_access`, `students`, `subjects`, `subject_assignments`, `teacher_work_plans`, `time_slots`, `trimester`.

**Alineación posterior (2026-04-21):** migraciones EF renombraron `subject_assignments."SchoolId"` → `school_id`, índice `IX_subject_assignments_school_id`, y se eliminó la tabla legacy vacía `"EmailConfigurations"`. El modelo `Area` ahora expone `SchoolId` / `School` acorde a la columna `area.school_id`.

**Además (tratamiento explícito):** `users` (ver sección 5).

**Sin columna `school_id` directa (tenant indirecto):** entre otras, `student_assignments` (FK `student_id` → `users`), `teacher_assignments` (vía `subject_assignments`), `schedule_entries` (vía `time_slots` / `academic_years` / `teacher_assignments`), `student_id_cards` (vía `students`), `scan_logs` (vía `students`), `email_queues` (vía `email_jobs` / `users`), `prematriculation_histories`, `teacher_work_plan_details`, `teacher_work_plan_review_logs`, `activity_attachments`, `student_qr_tokens`, `user_grades`, `user_groups`.

---

## 3. Tablas actualizadas

Todas las filas donde `school_id` era **NULL** o **distinto** del id del único colegio activo (incluye UUID huérfano respecto a `schools`) fueron alineadas al colegio detectado.

Archivo ejecutado: `Scripts/Normalization/02_normalize_school_id_execute.sql` (transacción `BEGIN` / `COMMIT`).

---

## 4. Cantidad de registros corregidos por tabla

| Tabla | Filas actualizadas |
|--------|---------------------|
| attendance | 6464 |
| area | 4 |
| subjects | 4 |
| activities | 3 |
| *(resto del bucle)* | 0 |
| users (no superadmin) | 0 |
| users (limpieza superadmin) | 0 |
| **Total aproximado** | **6475** |

Los avisos `NOTICE` de PostgreSQL registran el detalle por tabla al ejecutar el script.

---

## 5. Usuarios excluidos (superadmin)

- **Regla:** `lower(trim(role)) = 'superadmin'` → `school_id` debe permanecer **NULL** (sin colegio fijo).
- **Estado tras la migración:** 1 usuario `superadmin` con `school_id` NULL; **0** usuarios no superadmin con `school_id` NULL.
- **Limpieza:** si un superadmin hubiera tenido `school_id` no nulo, el script lo habría puesto a NULL (en esta BD: **0** filas afectadas).

---

## 6. Nulos restantes en `school_id`

Auditoría: `Scripts/Normalization/03_remaining_nulls.sql`.

| Situación | Filas |
|-----------|--------|
| `users` con `school_id` NULL y rol **superadmin** | 1 (esperado) |
| `users` con `school_id` NULL y rol **distinto** de superadmin | 0 |
| Resto de tablas con columna `school_id` nullable en la consulta | 0 nulos donde aplica |

**Nota técnica (resuelta):** `subject_assignments` usaba **`"SchoolId"`** en PostgreSQL; quedó unificada a **`school_id`** con migraciones `20260421043240_FixSubjectAssignmentSchoolIdColumnName` y `20260421043343_RenameSubjectAssignmentSchoolIdIndex`.

---

## 7. Riesgos encontrados

1. **Ejecución en otro entorno:** si existiera más de una escuela con `is_active = true`, el script **no modifica datos** y lanza excepción. Antes de producción: backup completo y verificación del conteo de escuelas activas.
2. **Áreas globales (`area.is_global`):** en este entorno todas las `area` con `school_id` inconsistente se asignaron al colegio único. Si en el futuro se usan áreas verdaderamente globales multi-colegio, habrá que revisar reglas de negocio antes de endurecer `NOT NULL`.
3. **`email_jobs.school_id` nullable:** se normalizaron valores inconsistentes; los NULL restantes (0 tras esta corrida) podrían ser válidos en diseños futuros “plataforma”; documentar política al endurecer.
4. **Reversibilidad:** no hay “undo” automático salvo restaurar backup o re-ejecutar lógica inversa manual. El script está encapsulado en una transacción única por ejecución (éxito → `COMMIT`).
5. **Contraseña:** no versionar contraseñas en scripts; usar `PGPASSWORD` u otro mecanismo seguro en CI/CD.
6. **Índice único en `area`:** en PostgreSQL puede existir `area_name_school_key` (nombre + `school_id`); el snapshot de EF aún refleja `area_name_key` solo sobre `name`. Con un solo colegio no choca; al habilitar varios colegios conviene alinear modelo y migración para unicidad por tenant.

---

## 8. Estado final para hardening multi-tenant

### Datos

- **6475** registros con `school_id` NULL o inconsistente corregidos hacia el único colegio activo.
- **Aislamiento lógico:** no quedan nulos “accidentales” en `school_id` en las tablas auditadas, excepto el **superadmin** en `users`.

### Columnas ya `NOT NULL` en BD (no requieren migración a NOT NULL)

Incluyen: `academic_years`, `counselor_assignments`, `email_configurations`, `id_card_template_fields`, `payment_concepts`, `payments`, `prematriculation_periods`, `prematriculations`, `school_id_card_settings`, `school_schedule_configurations`, `shifts`, `student_payment_access`, `time_slots` (todas con `school_id NOT NULL` en el esquema actual).

### Candidatas a `NOT NULL` en una fase posterior (tras validar negocio y migraciones EF)

Donde hoy `is_nullable = YES`: `activities`, `activity_types`, `area`, `attendance`, `audit_logs`, `discipline_reports`, `email_jobs`, `grade_levels`, `groups`, `messages`, `orientation_reports`, `security_settings`, `specialties`, `student_activity_scores`, `students`, `subjects`, `teacher_work_plans`, `trimester`, `users` (solo roles con colegio obligatorio), `subject_assignments.school_id`.

**Recomendación:** no aplicar `NOT NULL` en `users` hasta definir política explícita para roles sin escuela (p. ej. superadmin, soporte).

### Relaciones indirectas (sin acción SQL en esta fase)

- `student_assignments`: sin `school_id`; coherencia de tenant vía `users` y `groups`.
- Verificación rápida post-corrección: **0** asistencias con estudiante inexistente en `users`; **0** `student_assignments` huérfanos de `users`.

### Artefactos en el repositorio

| Archivo | Uso |
|---------|-----|
| `Scripts/Normalization/01_pre_counts.sql` | Conteos “a corregir” antes/después. |
| `Scripts/Normalization/02_normalize_school_id_execute.sql` | Ejecución idempotente (2ª corrida → 0 filas salvo nuevos datos sucios). |
| `Scripts/Normalization/03_remaining_nulls.sql` | Auditoría de nulos residuales. |

Ejemplo (PowerShell):

```powershell
$env:PGPASSWORD = '<su_password>'
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -p 5432 -U postgres -d eduplaner -v ON_ERROR_STOP=1 -f "Scripts\Normalization\02_normalize_school_id_execute.sql"
```

---

**Resumen:** la base local quedó con todos los `school_id` alineados al único colegio real, preservando al **superadmin** sin `school_id`, y con scripts reproducibles y aborto seguro si aparece más de un colegio activo.
