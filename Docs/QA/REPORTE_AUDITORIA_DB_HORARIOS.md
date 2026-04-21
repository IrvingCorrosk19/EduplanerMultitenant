# REPORTE AUDITORÍA DB – MÓDULO HORARIOS

**Fecha de auditoría:** 2026-02-12  
**Alcance:** Módulo Schedule (Horarios). Comparación Base de Datos esperada (según migraciones y scripts Ensure) vs DbContext y modelos.  
**Metodología:** Análisis estático de código (migraciones EF, scripts Ensure, SchoolDbContext.cs, modelos). No se ejecutaron consultas SQL en vivo; la “BD real” se infiere de las migraciones aplicables y de los scripts de creación/ajuste.

---

## 1. Estado general

| Aspecto | Estado |
|--------|--------|
| Tablas del módulo | Definidas en migraciones y/o scripts Ensure. |
| DbSet en DbContext | Presentes: `TimeSlots`, `ScheduleEntries`, `SchoolScheduleConfigurations`. |
| Modelos (clases) | Alineados con tablas en nombres y tipos de propiedad. |
| Inconsistencias críticas | **Sí:** DeleteBehavior en DbContext incoherente con columnas NOT NULL en `schedule_entries`. |
| Índices | Coinciden salvo índice `IX_schedule_entries_created_by` no declarado en DbContext. |
| Multi-tenant | `time_slots.school_id` NOT NULL; `schedule_entries` trazables a escuela vía relaciones. |

**Conclusión breve:** El esquema y el modelo son utilizables, pero existe **riesgo de integridad y comportamiento confuso** por el uso de `ClientSetNull` en FKs cuyas columnas son NOT NULL en BD. Se recomienda corregir DeleteBehavior y documentar política de borrado antes de considerar el módulo listo para producción sin reservas.

---

## 2. Tablas existentes

Fuente: migraciones `20260216194827_AddScheduleModule`, `20260216225855_AddSchoolScheduleConfiguration` y scripts `EnsureScheduleTables.cs`, `EnsureSchoolScheduleConfigurationTable.cs`.

Tablas propias del módulo de horarios:

| Tabla | Origen | Observación |
|-------|--------|-------------|
| **time_slots** | Migración AddScheduleModule / EnsureScheduleTables | Estructura alineada. |
| **schedule_entries** | Migración AddScheduleModule / EnsureScheduleTables | Estructura alineada. |
| **school_schedule_configurations** | Migración AddSchoolScheduleConfiguration / EnsureSchoolScheduleConfigurationTable | Columnas de tarde opcionales; script Ensure añade columnas si faltan. |

Tablas relacionadas (existentes en el proyecto, no creadas por este módulo):

- **teacher_assignments**, **subject_assignments**, **groups**, **academic_years**, **shifts**, **schools**, **users**.

---

## 3. Comparación Modelo vs DB

### 3.1 time_slots

| Columna (DB) | data_type | is_nullable (BD) | Modelo (TimeSlot) | ¿Coincide? |
|--------------|-----------|-------------------|--------------------|-------------|
| id | uuid | NOT NULL | Guid Id | Sí |
| school_id | uuid | NOT NULL | Guid SchoolId | Sí |
| shift_id | uuid | NULL | Guid? ShiftId | Sí |
| name | character varying(50) | NOT NULL | string Name | Sí |
| start_time | time | NOT NULL | TimeOnly StartTime | Sí |
| end_time | time | NOT NULL | TimeOnly EndTime | Sí |
| display_order | integer | NOT NULL (default 0) | int DisplayOrder | Sí |
| is_active | boolean | NOT NULL (default true) | bool IsActive | Sí |
| created_at | timestamp with time zone | NULL (default CURRENT_TIMESTAMP) | DateTime? CreatedAt | Sí |

**Resultado:** Sin columnas faltantes ni sobrantes. Tipos y nulabilidad coherentes.

### 3.2 schedule_entries

| Columna (DB) | data_type | is_nullable (BD) | Modelo (ScheduleEntry) | ¿Coincide? |
|--------------|-----------|-------------------|-------------------------|-------------|
| id | uuid | NOT NULL | Guid Id | Sí |
| teacher_assignment_id | uuid | NOT NULL | Guid TeacherAssignmentId | Sí |
| time_slot_id | uuid | NOT NULL | Guid TimeSlotId | Sí |
| day_of_week | smallint | NOT NULL | byte DayOfWeek | Sí |
| academic_year_id | uuid | NOT NULL | Guid AcademicYearId | Sí |
| created_at | timestamp with time zone | NULL (default) | DateTime? CreatedAt | Sí |
| created_by | uuid | NULL | Guid? CreatedBy | Sí |

**Resultado:** Sin columnas faltantes ni sobrantes. Tipos y nulabilidad coherentes.

### 3.3 school_schedule_configurations

| Columna (DB) | data_type | is_nullable (BD) | Modelo (SchoolScheduleConfiguration) | ¿Coincide? |
|--------------|-----------|-------------------|----------------------------------------|-------------|
| id | uuid | NOT NULL | Guid Id | Sí |
| school_id | uuid | NOT NULL | Guid SchoolId | Sí |
| morning_start_time | time | NOT NULL | TimeOnly MorningStartTime | Sí |
| morning_block_duration_minutes | integer | NOT NULL | int MorningBlockDurationMinutes | Sí |
| morning_block_count | integer | NOT NULL | int MorningBlockCount | Sí |
| afternoon_start_time | time | NULL | TimeOnly? AfternoonStartTime | Sí |
| afternoon_block_duration_minutes | integer | NULL | int? AfternoonBlockDurationMinutes | Sí |
| afternoon_block_count | integer | NULL | int? AfternoonBlockCount | Sí |
| created_at | timestamp with time zone | NULL | DateTime? CreatedAt | Sí |
| updated_at | timestamp with time zone | NULL | DateTime? UpdatedAt | Sí |

**Resultado:** Coincidencia completa. El script Ensure añade las columnas de tarde si la tabla existía sin ellas.

### 3.4 Tablas relacionadas (teacher_assignments, subject_assignments, groups, academic_years, shifts)

Los modelos `TeacherAssignment`, `SubjectAssignment`, `Group`, `AcademicYear`, `Shift` tienen propiedades que mapean a las columnas usadas por el módulo de horarios. No se detectaron columnas del módulo Schedule en estas tablas que falten en los modelos; las FKs del módulo apuntan a PKs existentes. No se realizó listado exhaustivo de todas las columnas de estas tablas (fuera del alcance estricto del módulo).

---

## 4. FKs detectadas

### 4.1 schedule_entries (BD según migración)

| constraint_name | table_name | column_name | foreign_table | foreign_column | ON DELETE (migración) |
|-----------------|------------|-------------|--------------|----------------|------------------------|
| schedule_entries_pkey | schedule_entries | id | — | — | — |
| schedule_entries_academic_year_id_fkey | schedule_entries | academic_year_id | academic_years | id | **no especificado** → NO ACTION |
| schedule_entries_created_by_fkey | schedule_entries | created_by | users | id | **SetNull** |
| schedule_entries_teacher_assignment_id_fkey | schedule_entries | teacher_assignment_id | teacher_assignments | id | **no especificado** → NO ACTION |
| schedule_entries_time_slot_id_fkey | schedule_entries | time_slot_id | time_slots | id | **no especificado** → NO ACTION |

### 4.2 schedule_entries – DbContext (SchoolDbContext.cs)

| FK | DeleteBehavior en DbContext | Columna | is_nullable |
|----|-----------------------------|---------|-------------|
| AcademicYearId | **ClientSetNull** | academic_year_id | NOT NULL |
| TeacherAssignmentId | **ClientSetNull** | teacher_assignment_id | NOT NULL |
| TimeSlotId | **ClientSetNull** | time_slot_id | NOT NULL |
| CreatedBy | SetNull | created_by | NULL |

### 4.3 time_slots (BD y DbContext)

| FK | foreign_table | ON DELETE (BD) | DeleteBehavior (DbContext) |
|----|----------------|----------------|----------------------------|
| time_slots_school_id_fkey | schools | CASCADE | Cascade |
| time_slots_shift_id_fkey | shifts | SET NULL | SetNull |

### 4.4 school_schedule_configurations (BD y DbContext)

| FK | foreign_table | ON DELETE (BD) | DeleteBehavior (DbContext) |
|----|----------------|----------------|----------------------------|
| school_schedule_configurations_school_id_fkey | schools | CASCADE | Cascade |

---

## 5. Índices detectados

### 5.1 schedule_entries

| Índice (BD – migración) | DbContext declarado | Observación |
|-------------------------|---------------------|-------------|
| schedule_entries_pkey (PK) | Sí | — |
| IX_schedule_entries_academic_year_id | Sí | — |
| IX_schedule_entries_created_by | **No** | Índice creado por migración; no aparece en OnModelCreating para ScheduleEntry. |
| IX_schedule_entries_teacher_assignment_id | Sí | — |
| IX_schedule_entries_time_slot_id | Sí | — |
| IX_schedule_entries_unique_slot (UNIQUE) | Sí | — |

### 5.2 time_slots

| Índice (BD – migración) | DbContext declarado |
|-------------------------|---------------------|
| time_slots_pkey (PK) | Sí |
| IX_time_slots_school_id | Sí |
| IX_time_slots_shift_id | Sí |

### 5.3 school_schedule_configurations

| Índice (BD – migración) | DbContext declarado |
|-------------------------|---------------------|
| school_schedule_configurations_pkey (PK) | Sí |
| IX_school_schedule_configurations_school_id (UNIQUE) | Sí (HasIndex SchoolId IsUnique) |

---

## 6. Inconsistencias encontradas

### 6.1 Crítica: DeleteBehavior vs NOT NULL en schedule_entries

- **Hecho:** En `SchoolDbContext.cs`, las tres FKs obligatorias de `schedule_entries` (AcademicYearId, TeacherAssignmentId, TimeSlotId) están configuradas con **OnDelete(DeleteBehavior.ClientSetNull)**.
- **En BD:** Las columnas `academic_year_id`, `teacher_assignment_id`, `time_slot_id` son **NOT NULL**. La migración no define ON DELETE para estas FKs (comportamiento por defecto: NO ACTION).
- **Problema:** ClientSetNull indica que, al borrar el principal, EF intentará poner la FK en null. Como la columna no acepta null, cualquier borrado del principal que EF intente “propagar” con SetNull fallará en la BD (constraint violation). Además, el comportamiento real en BD es NO ACTION: no se puede borrar el principal si existen filas dependientes.
- **Conclusión:** Incoherencia entre intención (ClientSetNull), capacidad del esquema (NOT NULL) y comportamiento real en BD (NO ACTION). No hay huérfanos por borrado en BD, pero la configuración del DbContext es engañosa y podría provocar excepciones en tiempo de ejecución si se borra un AcademicYear, TeacherAssignment o TimeSlot con entradas asociadas.

### 6.2 Menor: Índice CreatedBy no declarado en DbContext

- El índice **IX_schedule_entries_created_by** existe en la migración y por tanto en la BD.
- No está declarado en `modelBuilder.Entity<ScheduleEntry>(...)` en SchoolDbContext.
- Impacto: bajo. EF no depende de que todos los índices estén declarados; el índice sigue existiendo en la BD y mejora consultas por `created_by`. Solo hay una pequeña desincronización documental entre modelo EF y esquema.

### 6.3 Script Ensure vs migración (schedule_entries)

- **EnsureScheduleTables:** Crea FKs sin ON DELETE para academic_year_id, teacher_assignment_id, time_slot_id (igual que la migración).
- **Migración:** Igual. No hay discrepancia de ON DELETE entre script y migración.

---

## 7. Riesgos críticos

| # | Riesgo | Descripción |
|---|--------|-------------|
| 1 | **ClientSetNull con columnas NOT NULL** | Si en la aplicación se elimina un `TeacherAssignment`, `TimeSlot` o `AcademicYear` que tiene `ScheduleEntry` asociados, EF puede intentar poner las FK en null y provocar error de constraint en la BD. Además, la BD rechazará el DELETE del principal por NO ACTION, por lo que el comportamiento real es “no permitir borrado”, pero la configuración del DbContext sugiere otra cosa. |
| 2 | **Borrado de School** | Al borrar una escuela, `time_slots` y `school_schedule_configurations` se eliminan en cascada (correcto). Las `schedule_entries` referencian `time_slots` y `teacher_assignments`; si no hay ON DELETE CASCADE desde `schedule_entries` hacia `time_slots`, el borrado de la escuela elimina primero los `time_slots` y las FKs de `schedule_entries` a `time_slots` provocarían violación. **Comprobación:** La migración no define ON DELETE en `schedule_entries_time_slot_id_fkey`, por tanto es NO ACTION. Al borrar School → se borran TimeSlots (CASCADE) → quedarían ScheduleEntries con time_slot_id huérfano → **error de FK**. Por tanto, borrar una escuela sin antes eliminar o reasignar las `schedule_entries` que usan `time_slots` de esa escuela puede producir error de integridad referencial. |

---

## 8. Riesgos medios

| # | Riesgo | Descripción |
|---|--------|-------------|
| 1 | **Borrado de TeacherAssignment** | Si se elimina una asignación docente que tiene entradas en `schedule_entries`, la BD (NO ACTION) impedirá el borrado. Correcto para integridad. La aplicación debe eliminar o reasignar antes las entradas de horario. |
| 2 | **Borrado de AcademicYear** | Igual: NO ACTION sobre `academic_year_id` impide borrar un año con entradas de horario. Comportamiento deseable; la aplicación debe gestionar la eliminación o el archivado. |
| 3 | **Borrado de TimeSlot** | NO ACTION impide borrar un bloque con entradas. Coherente con la lógica de negocio (en la UI se desactiva el bloque en lugar de borrarlo si tiene entradas). |
| 4 | **Índice CreatedBy** | No declarado en DbContext; solo impacto en documentación y en posible futura generación de migraciones desde un modelo que no refleje ese índice. |

---

## 9. Recomendaciones priorizadas

1. **Alta – Ajustar DeleteBehavior en ScheduleEntry (DbContext)**  
   Cambiar las tres FKs NOT NULL (AcademicYearId, TeacherAssignmentId, TimeSlotId) de `DeleteBehavior.ClientSetNull` a `DeleteBehavior.Restrict` (o no especificar, que en EF Core suele ser Restrict). Así el modelo refleja que la BD no permite null y que no se debe borrar el principal si hay dependientes. No exige cambios en la BD si ya está en NO ACTION.

2. **Alta – Política de borrado de School**  
   Definir y documentar: antes de borrar una escuela, eliminar o mover las `schedule_entries` que usan `time_slots` de esa escuela, o implementar borrado en cascada desde `time_slots` hacia `schedule_entries` (ON DELETE CASCADE en `schedule_entries.time_slot_id`). La primera opción es más segura; la segunda requiere migración y decisión de negocio.

3. **Media – Declarar índice CreatedBy en ScheduleEntry**  
   En `OnModelCreating`, para `ScheduleEntry`, añadir algo equivalente a:  
   `entity.HasIndex(e => e.CreatedBy, "IX_schedule_entries_created_by");`  
   para alinear el modelo con la BD (solo documental/consistencia; no cambia comportamiento).

4. **Baja – Verificación en BD real**  
   Ejecutar en la base real las consultas de la Fase 1 (listado de tablas, columnas, FKs, índices) y contrastar con este reporte para detectar desvíos (por ejemplo, migraciones no aplicadas o scripts Ensure aplicados en distinto orden).

---

## 10. ¿Listo para producción?

**Respuesta: No, con reservas.**

**Motivos:**

1. **DeleteBehavior incoherente:** La configuración actual (ClientSetNull en FKs NOT NULL) puede provocar excepciones o comportamientos confusos al borrar entidades relacionadas. Es un defecto de modelo que conviene corregir antes de dar por cerrado el módulo.
2. **Borrado de School:** Sin política clara o sin CASCADE controlado, borrar una escuela con horarios asignados puede generar violaciones de FK. Debe definirse y, si aplica, implementarse (p. ej. borrado previo de entradas o CASCADE documentado).
3. **No se ha validado la BD real:** Este reporte se basa en migraciones y scripts. La BD en uso podría diferir (migraciones parciales, scripts aplicados a mano). Se recomienda ejecutar las comprobaciones SQL indicadas en la Fase 1 contra la instancia real y actualizar este documento si hay diferencias.

**Condiciones para considerar “listo para producción” (módulo horarios):**

- DeleteBehavior de `schedule_entries` para AcademicYearId, TeacherAssignmentId y TimeSlotId alineado con NOT NULL (p. ej. Restrict).
- Política documentada (y si aplica implementada) para borrado de School cuando existan schedule_entries / time_slots.
- Opcional pero recomendable: verificación en BD real de tablas, columnas, FKs e índices según este informe.

---

*Fin del reporte. No se ha modificado código; solo análisis y documentación.*
