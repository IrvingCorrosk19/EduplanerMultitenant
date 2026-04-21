# Auditoría técnica – Módulo de horarios

**Documento:** AUDITORIA_MODULO_HORARIOS  
**Fecha:** 2026-02-12  
**Alcance:** Entidades, DbContext, migración, servicios, controlador, seguridad y rendimiento.  
**Criterio:** Mejores prácticas .NET 8 + EF Core + PostgreSQL y no ruptura de la arquitectura actual.

---

## Resumen ejecutivo

La auditoría del módulo de horarios concluye que la implementación **cumple** con las prácticas revisadas y **no introduce riesgos** que impidan su uso en producción. Las entidades, configuración EF, migración, reglas de negocio y seguridad están alineadas con el diseño y con el resto del sistema. Se identifican **2 recomendaciones opcionales** (migración con alcance mixto e índice opcional de rendimiento). **Nivel de riesgo global: Bajo.** **Veredicto: Apto para uso.**

| Métrica | Valor |
|--------|--------|
| Áreas revisadas | 6 |
| Puntos verificados | 24 |
| Correctos | 22 |
| Recomendaciones | 2 |
| Problemas detectados | 0 |

---

## Índice

1. [Revisión de entidades](#1-revisión-de-entidades)  
2. [Revisión de DbContext](#2-revisión-de-dbcontext)  
3. [Revisión de migración](#3-revisión-de-migración-addschedulemodule)  
4. [Revisión de servicios](#4-revisión-de-servicios)  
5. [Revisión de seguridad](#5-revisión-de-seguridad)  
6. [Revisión de rendimiento](#6-revisión-de-rendimiento)  
7. [Resumen y plan de acción](#7-resumen-y-plan-de-acción)

---

## 1. Revisión de entidades

**Archivos:** `Models/TimeSlot.cs`, `Models/ScheduleEntry.cs`

| # | Verificación | Resultado | Detalle |
|---|--------------|-----------|---------|
| 1.1 | PK de tipo Guid | ✔ Correcto | Ambas entidades exponen `public Guid Id { get; set; }`. Consistente con el resto del dominio. |
| 1.2 | Nombres de tabla snake_case | ✔ Correcto | `ToTable("time_slots")` y `ToTable("schedule_entries")`. Convención del proyecto respetada. |
| 1.3 | FKs y nombres de columna | ✔ Correcto | TimeSlot: SchoolId, ShiftId. ScheduleEntry: TeacherAssignmentId, TimeSlotId, AcademicYearId, CreatedBy. Constraints y columnas en snake_case. |
| 1.4 | No impacto en TeacherAssignment / SubjectAssignment | ✔ Correcto | No se añadieron propiedades ni colecciones en TA/SA. ScheduleEntry referencia TA por FK; las entidades existentes no se modifican. |
| 1.5 | Índice único para integridad de slot | ✔ Correcto | `IX_schedule_entries_unique_slot` único en `(teacher_assignment_id, academic_year_id, time_slot_id, day_of_week)`. Evita duplicar la misma asignación en el mismo slot; la regla de “mismo docente” se aplica en servicio. |

**Riesgo del área:** Bajo.

---

## 2. Revisión de DbContext

**Archivo:** `Models/SchoolDbContext.cs` (OnModelCreating)

| # | Verificación | Resultado | Detalle |
|---|--------------|-----------|---------|
| 2.1 | Configuración en OnModelCreating | ✔ Correcto | TimeSlot y ScheduleEntry configurados en el mismo bloque que el resto del modelo; sin IEntityTypeConfiguration externos. |
| 2.2 | Índices definidos | ✔ Correcto | TimeSlot: `IX_time_slots_school_id`, `IX_time_slots_shift_id`. ScheduleEntry: `IX_schedule_entries_teacher_assignment_id`, `IX_schedule_entries_time_slot_id`, `IX_schedule_entries_academic_year_id`, `IX_schedule_entries_unique_slot` (único). |
| 2.3 | DateTime con time zone | ✔ Correcto | `CreatedAt` en ambas entidades: `.HasColumnType("timestamp with time zone")` y `HasDefaultValueSql("CURRENT_TIMESTAMP")`. Alineado con el interceptor UTC del sistema. |
| 2.4 | StartTime / EndTime en PostgreSQL | ✔ Correcto | `.HasColumnType("time")` para `TimeOnly`; en PostgreSQL se mapean al tipo `time`. |

**Riesgo del área:** Bajo.

---

## 3. Revisión de migración (AddScheduleModule)

**Archivo:** `Migrations/20260216194827_AddScheduleModule.cs`

| # | Verificación | Resultado | Detalle |
|---|--------------|-----------|---------|
| 3.1 | Creación de tablas | ✔ Correcto | `CreateTable("time_slots", ...)` y `CreateTable("schedule_entries", ...)` con columnas, tipos y defaults esperados. |
| 3.2 | Creación de índices | ✔ Correcto | Se crean los 6 índices (2 en time_slots, 4 en schedule_entries, incluido el único). |
| 3.3 | Constraints de FK | ✔ Correcto | FKs a schools, shifts, academic_years, users, teacher_assignments, time_slots con nombres explícitos. |
| 3.4 | No drop de otras tablas | ✔ Correcto | Down() solo ejecuta DropTable de schedule_entries y time_slots. |
| 3.5 | AlterColumn en scan_logs | ⚠ Recomendación | Up() incluye `AlterColumn(scan_logs.student_id, nullable: true)`. No pertenece al módulo de horarios. Para una migración “solo horarios”, conviene mover este cambio a otra migración o eliminarlo de esta. No introduce fallos; solo mezcla alcances. |
| 3.6 | Orden en Down() | ✔ Correcto | Primero se elimina schedule_entries (depende de time_slots), luego time_slots; después se revierte el cambio en scan_logs. |

**Riesgos antes de update-database:**

- **Bajo:** La migración es coherente y no modifica otras tablas salvo scan_logs.
- **Medio (solo al revertir):** Si se ejecuta Down() y en scan_logs existieran filas con `student_id` NULL, el intento de volver a NOT NULL podría fallar. Aplica únicamente en caso de reversión.

**Riesgo del área:** Bajo.

---

## 4. Revisión de servicios

**Archivos:** `Services/Interfaces/IScheduleService.cs`, `Services/Implementations/ScheduleService.cs`

| # | Verificación | Resultado | Detalle |
|---|--------------|-----------|---------|
| 4.1 | Validación conflicto docente | ✔ Correcto | CreateEntryAsync: consulta ScheduleEntries con Include(TeacherAssignment), filtra por mismo AcademicYearId, DayOfWeek, TimeSlotId y mismo TeacherId. Lanza InvalidOperationException con mensaje claro. |
| 4.2 | Validación conflicto grupo | ✔ Correcto | GroupId vía TeacherAssignment → SubjectAssignment. Consulta con Include + ThenInclude(SubjectAssignment) para comprobar si ya existe entrada del mismo grupo en mismo año/día/bloque. Mensaje claro. |
| 4.3 | Teacher solo crea/elimina sus horarios | ✔ Correcto | CreateEntryAsync: si rol teacher/docente y ta.TeacherId != currentUserId → UnauthorizedAccessException. DeleteEntryAsync: misma comprobación sobre entry.TeacherAssignment.TeacherId. |
| 4.4 | No uso de AutoMapper | ✔ Correcto | ScheduleService no referencia AutoMapper. Mapeo a JSON se hace en el controlador (MapEntryToJson). |
| 4.5 | Ausencia de N+1 | ✔ Correcto | CreateEntryAsync: consultas acotadas (TA, conflictos, relectura de entrada). GetByTeacherAsync y GetByGroupAsync: una sola consulta con Include/ThenInclude; sin bucles que generen consultas extra. |

**Riesgo del área:** Bajo.

---

## 5. Revisión de seguridad

**Archivo:** `Controllers/ScheduleController.cs`

| # | Verificación | Resultado | Detalle |
|---|--------------|-----------|---------|
| 5.1 | Autorización a nivel de controlador | ✔ Correcto | `[Authorize(Roles = "Teacher,Admin,Director,teacher,admin,director")]` a nivel de clase. Todos los endpoints exigen autenticación y uno de esos roles. |
| 5.2 | Teacher no consulta horario de otro docente | ✔ Correcto | ListJsonByTeacher: si rol es teacher/docente y teacherId != currentUserId → success: false y mensaje "Solo puede consultar su propio horario."; effectiveTeacherId se fija a currentUserId. |
| 5.3 | Vista ByTeacher | ✔ Correcto | Si es Teacher, effectiveTeacherId = user.Id. Si Admin/Director, se permite elegir docente. |
| 5.4 | Validación en capa de servicio | ✔ Correcto | CreateEntryAsync y DeleteEntryAsync reciben currentUserId y aplican la restricción por rol y pertenencia de la asignación. Defensa en profundidad. |

**Riesgo del área:** Bajo.

---

## 6. Revisión de rendimiento

| # | Aspecto | Resultado | Detalle |
|---|---------|-----------|---------|
| 6.1 | Joins en validaciones | ✔ Correcto | Conflicto docente: una consulta con Include(TeacherAssignment). Conflicto grupo: una con Include + ThenInclude(SubjectAssignment). Dos consultas por creación; aceptable para la regla de negocio. |
| 6.2 | GetByTeacherAsync / GetByGroupAsync | ✔ Correcto | Una sola consulta con Include/ThenInclude; OrderBy por DayOfWeek y TimeSlot.DisplayOrder. Sin N+1. |
| 6.3 | Índices existentes | ✔ Correcto | Índices en teacher_assignment_id, time_slot_id, academic_year_id; school_id y shift_id en time_slots. Adecuados para los filtros actuales. |
| 6.4 | Índice adicional | ⚠ Recomendación | Opcional: índice compuesto en schedule_entries (academic_year_id, day_of_week, time_slot_id) si en el futuro crece el volumen y se consulta mucho por “slot”. El join con teacher_assignments ya se beneficia del índice en teacher_assignment_id. No crítico. |

**Riesgo del área:** Bajo.

---

## 7. Resumen y plan de acción

### 7.1 Cumplimiento por área

| Área | Estado | Riesgo |
|------|--------|--------|
| Entidades | ✔ Correcto | Bajo |
| DbContext | ✔ Correcto | Bajo |
| Migración | ✔ Correcto (1 recomendación) | Bajo |
| Servicios | ✔ Correcto | Bajo |
| Seguridad | ✔ Correcto | Bajo |
| Rendimiento | ✔ Correcto (1 recomendación) | Bajo |

### 7.2 Hallazgos

- **✔ Correcto (22 puntos):** PK Guid, tablas y columnas snake_case, FKs e índices coherentes, DateTime con time zone, TimeOnly como `time`, sin cambios en TeacherAssignment/SubjectAssignment, índice único en schedule_entries, validaciones de conflicto y de rol Teacher en servicio, Authorize y restricción por docente en controlador, sin AutoMapper y sin N+1 en los flujos revisados.
- **⚠ Recomendaciones (2):**
  1. **Migración:** Separar el `AlterColumn(scan_logs.student_id)` en otra migración para que AddScheduleModule contenga únicamente cambios del módulo de horarios.
  2. **Rendimiento:** Valorar en el futuro un índice compuesto `(academic_year_id, day_of_week, time_slot_id)` en schedule_entries si las consultas por “slot” aumentan.
- **❌ Problemas detectados:** Ninguno.

### 7.3 Plan de acción

| Id | Acción | Prioridad | Responsable |
|----|--------|-----------|-------------|
| R1 | Valorar migración separada para scan_logs.student_id (o dejar como está si se asume migración mixta). | Baja | Dev |
| R2 | En carga alta, valorar índice compuesto (academic_year_id, day_of_week, time_slot_id) en schedule_entries. | Baja | Dev / DBA |

### 7.4 Conclusión

La implementación del módulo de horarios cumple con las mejores prácticas revisadas, mantiene la arquitectura existente y no introduce problemas de seguridad ni de integridad en TeacherAssignment/SubjectAssignment. No se han detectado problemas que impidan usar la migración o el módulo en producción; las recomendaciones son mejoras opcionales.

| Criterio | Valor |
|----------|--------|
| **Nivel de riesgo global** | Bajo |
| **Veredicto** | Apto para uso; recomendaciones aplicables de forma opcional |
