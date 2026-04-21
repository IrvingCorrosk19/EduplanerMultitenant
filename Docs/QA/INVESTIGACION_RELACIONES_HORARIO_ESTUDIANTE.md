# Investigación arquitectónica – Módulo horario estudiante

**Fecha:** 2026-02-12  
**Objetivo:** Determinar con precisión las relaciones entre entidades para construir un módulo de visualización de horario para estudiantes basado en el módulo Schedule existente.  
**Regla:** Solo análisis estructural; no se ha modificado código.

---

## 1. Cadena completa de relaciones

### A) Estudiante (en el contexto de horario)

En el sistema existen **dos conceptos** relacionados con “estudiante”:

| Concepto | Tabla | Uso en horario |
|----------|--------|-----------------|
| **User (rol Student)** | `users` | **Sí.** Es el usuario que inicia sesión como estudiante. Su horario se obtiene vía `student_assignments`. |
| **Student** | `students` | **No.** Entidad separada (ej. hijos de un acudiente). No participa en la cadena de horarios. |

**Tabla `students` (modelo `Student`):**

- **school_id:** Sí, existe. `SchoolId` (Guid?, FK a `schools`).  
- **group_id:** No. No hay FK a grupos. Hay un campo de texto `GroupName` (legacy).  
- **grade_id:** No. Hay un campo de texto `Grade`.  
- **Relación directa con subject_assignments:** No.  
- **Relación con user:** Sí, como **padre**: `ParentId` → `users.id` (el estudiante en `students` es el “hijo” del User acudiente).  

**Tabla `student_assignments` (modelo `StudentAssignment`):**

- Aquí está el vínculo **real** del “estudiante” (User) con grupo y año académico.
- **student_id** → **users.id** (constraint `fk_student` en migración `20251102175646_AddPaymentModuleComplete.cs`: `principalTable: "users"`).
- **group_id** → groups.id (`fk_group`).
- **grade_id** → grade_levels.id (`fk_grade`).
- **academic_year_id** → academic_years.id (opcional).

**Conclusión A:** Para el horario del estudiante se usa **User (rol Student)** y su **StudentAssignment** activa (y opcionalmente el año académico actual). La tabla `students` no interviene en la cadena de horarios.

---

### B) Grupo

**Tabla `groups` (modelo `Group`):**

- **school_id:** Sí. `SchoolId` (Guid?, FK a `schools`).
- **shift_id:** Sí. `ShiftId` (Guid?, FK a `shifts`). También campo de texto `Shift` (compatibilidad).
- **Relación con subject_assignments:** Sí. `Group.SubjectAssignments` (ICollection). FK en `subject_assignments.group_id` → `groups.id`.
- **Relación con schedule_entries:** No directa. Es **indirecta**:  
  `Group` → `SubjectAssignment` → `TeacherAssignment` → `ScheduleEntry`.

**Cómo obtiene un grupo su horario hoy:**

- En código: `ScheduleService.GetByGroupAsync(groupId, academicYearId)`.
- Consulta: `ScheduleEntries` donde `TeacherAssignment.SubjectAssignment.GroupId == groupId` y `AcademicYearId == academicYearId`.
- Es decir: **Grupo → SubjectAssignment → TeacherAssignment → ScheduleEntry → TimeSlot.**

---

### C) SubjectAssignment

**Tabla `subject_assignments` (modelo `SubjectAssignment`):**

- **FK a group:** Sí. `GroupId` (Guid) → `groups.id`.
- **FK a subject:** Sí. `SubjectId` → `subjects.id`.
- **FK a teacher_assignment:** No. La relación es **inversa**: `SubjectAssignment` tiene `TeacherAssignments` (ICollection). Es decir, `teacher_assignments.subject_assignment_id` → `subject_assignments.id`.
- **school_id:** Sí, opcional. `SchoolId` (Guid?, FK a `schools`).

**Vinculación del horario:**

- El horario se vincula al grupo **vía SubjectAssignment**: cada entrada de horario (`ScheduleEntry`) referencia un `TeacherAssignment`, y cada `TeacherAssignment` referencia un `SubjectAssignment`, que a su vez tiene `GroupId`. Por tanto: **ScheduleEntry → TeacherAssignment → SubjectAssignment → Group.**

---

### D) TeacherAssignment

**Tabla `teacher_assignments` (modelo `TeacherAssignment`):**

- **FK a teacher (users):** Sí. `TeacherId` (Guid) → `users.id`.
- **FK a subject_assignment:** Sí. `SubjectAssignmentId` (Guid) → `subject_assignments.id`.

**ScheduleEntry referencia TeacherAssignment directamente:** Sí.  
`ScheduleEntry.TeacherAssignmentId` → `teacher_assignments.id`. No hay FK de ScheduleEntry a Group ni a SubjectAssignment; se llega a ellos por navegación desde TeacherAssignment.

---

### E) ScheduleEntry

**Tabla `schedule_entries` (modelo `ScheduleEntry`):**

- **FK a teacher_assignment:** Sí. `TeacherAssignmentId` → `teacher_assignments.id`.
- **FK a time_slot:** Sí. `TimeSlotId` → `time_slots.id`.
- **FK a academic_year:** Sí. `AcademicYearId` → `academic_years.id`.
- No hay `school_id` ni `group_id` en la tabla; el contexto de escuela/grupo se obtiene por joins.

**Cómo obtener el horario de un grupo:**

- Ruta: **Group** (groupId) → SubjectAssignments del grupo → TeacherAssignments de esas asignaciones → ScheduleEntries de esas TeacherAssignments para un `academicYearId`.
- En código: `GetByGroupAsync(groupId, academicYearId)` filtra por `e.TeacherAssignment.SubjectAssignment.GroupId == groupId`.

**Cómo obtener el horario de un estudiante:**

1. Estudiante = **User** con rol Student (Id = `userId`).
2. Obtener una **StudentAssignment** activa para ese User (y opcionalmente para el año académico actual): `StudentId == userId`, `IsActive == true`, `AcademicYearId == academicYearId` (o el año actual de la escuela).
3. De esa asignación obtener **GroupId**.
4. Obtener el horario del grupo: **GetByGroupAsync(groupId, academicYearId)** (misma lógica que ya existe).

Es decir: **User (student) → StudentAssignment → Group → [SubjectAssignment → TeacherAssignment → ScheduleEntry → TimeSlot].**

---

## 2. Ruta para horario del estudiante

La ruta correcta es:

```
User (estudiante, rol Student)
  → StudentAssignment (StudentId = User.Id, IsActive = true, AcademicYearId)
  → Group (GroupId)
  → SubjectAssignment (por GroupId)
  → TeacherAssignment (por SubjectAssignmentId)
  → ScheduleEntry (por TeacherAssignmentId + AcademicYearId)
  → TimeSlot
```

**No** es válido partir de la entidad `Student` (tabla `students`): esa entidad no tiene `group_id` ni relación con `schedule_entries`. La cadena real usa **User** + **StudentAssignment** + **Group**, y a partir de ahí se reutiliza la misma lógica que ya tiene el módulo Schedule por grupo.

**Resumen:**

- **Horario de un grupo:** `ScheduleService.GetByGroupAsync(groupId, academicYearId)`.
- **Horario de un estudiante:** Obtener `groupId` (y opcionalmente `academicYearId`) desde `StudentAssignment` del User actual; luego llamar a `GetByGroupAsync(groupId, academicYearId)`.

---

## 3. Multi-tenant

- **students.school_id:** Existe; los registros en `students` pertenecen a una escuela. No se usan para horario.
- **users.school_id:** El User (estudiante) tiene `SchoolId`; debe coincidir con la escuela del grupo al que está asignado vía StudentAssignment.
- **groups.school_id:** Existe. Un grupo pertenece a una escuela. En `ScheduleController.ListJsonByGroup` ya se valida: el grupo debe cumplir `g.SchoolId == currentUser.SchoolId`.
- **time_slots.school_id:** Existe. Los bloques son por escuela.
- **schedule_entries:** No tienen `school_id`; se filtran por escuela de forma indirecta:
  - Por **TimeSlot:** `TimeSlot.SchoolId`.
  - Por **AcademicYear:** `AcademicYear.SchoolId`.
  - Por **Group:** al obtener horario por grupo se exige que el grupo sea de la escuela del usuario (como en `ListJsonByGroup`).

**Conclusión multi-tenant:** No hay fuga entre escuelas si:

1. El estudiante solo ve su propio horario: se obtiene su `GroupId` desde **su** StudentAssignment (ya ligada a su User y por tanto a su escuela).
2. El año académico se limita a años de la escuela del usuario (como en el módulo actual).
3. En cualquier vista “por grupo” se valida que `group.SchoolId == currentUser.SchoolId` (ya implementado en Schedule).

---

## 4. Diagrama textual de relaciones

```
[User (Student)]  ←student_id—  [StudentAssignment]  —group_id→  [Group]
                                      |                            |
                                      | academic_year_id            | school_id
                                      ↓                            ↓
                              [AcademicYear]                  [School]
                                      |                            ↑
                                      | school_id                   |
[ScheduleEntry] —teacher_assignment_id→ [TeacherAssignment]       |
       |                                        |                   |
       | time_slot_id                    subject_assignment_id      |
       ↓                                        |                  |
[TimeSlot] —school_id→ [School]                 ↓                  |
       |                              [SubjectAssignment] —group_id→+
       | academic_year_id                             |
       ↓                                              |
[AcademicYear] —school_id→ [School]                  +— school_id (opcional)
```

**Cadena horario estudiante (flujo de datos):**

```
User.Id (estudiante)
  → StudentAssignment (StudentId, GroupId, AcademicYearId)
  → Group.Id
  → ScheduleEntry vía TeacherAssignment.SubjectAssignment.GroupId == Group.Id
  → TimeSlot (StartTime, EndTime, Name), DayOfWeek
```

---

## 5. Posibles inconsistencias

| Riesgo | Descripción | Mitigación |
|--------|-------------|------------|
| Varias StudentAssignment activas | Un User (estudiante) podría tener más de una asignación activa (ej. distinto año o cambio de grupo). | Definir política: usar la asignación del año académico actual o la única activa; filtrar siempre por `IsActive == true` y por `AcademicYearId` cuando corresponda. |
| StudentAssignment sin grupo válido | GroupId apunta a un grupo de otra escuela (error de datos). | Validar en servicio que `Group.SchoolId == User.SchoolId` antes de devolver horario. |
| Año académico no alineado | El estudiante tiene asignación a un año que ya no está activo o no es el “actual”. | Usar el mismo criterio que en Schedule (año actual o selector) y documentar qué año se muestra. |

---

## 6. Riesgos

- **Duplicar lógica:** No crear una cadena alternativa de consultas; reutilizar `GetByGroupAsync` una vez obtenido el `groupId` del estudiante.
- **Confundir Student vs User:** La tabla `students` no debe usarse para el módulo de horario del estudiante; el horario es por **User (rol Student)** y **StudentAssignment**.
- **Filtro por escuela:** Cualquier nuevo endpoint que exponga horario por estudiante debe obtener el grupo solo desde las StudentAssignments del usuario actual y validar implícita o explícitamente que el grupo sea de la misma escuela.

---

## 7. Confirmación de coherencia de la cadena

La cadena **User (estudiante) → StudentAssignment → Group → SubjectAssignment → TeacherAssignment → ScheduleEntry → TimeSlot** es coherente con:

- El modelo de datos (FKs en DbContext y migraciones).
- El uso actual en `ScheduleService.GetByGroupAsync` y en `ScheduleController.ListJsonByGroup`.
- La restricción multi-tenant (grupo de la escuela del usuario, años académicos de la escuela).

---

## 8. Recomendación arquitectónica para módulo StudentSchedule

1. **No introducir nuevas tablas ni FKs** para el horario del estudiante; la información ya está en las entidades existentes.
2. **Servicio:** Añadir en `IScheduleService` / `ScheduleService` un método del tipo `GetByStudentUserAsync(Guid studentUserId, Guid academicYearId)` que:
   - Obtenga una asignación activa del estudiante para ese año (o el año actual de la escuela).
   - Si no hay asignación o grupo, devolver lista vacía o mensaje claro.
   - Valide que el grupo pertenezca a la escuela del User (studentUserId debe ser el usuario actual en contexto web).
   - Llame a `GetByGroupAsync(groupId, academicYearId)` y devuelva el mismo tipo de datos que ya usa la vista/API por grupo.
3. **Controlador:** En un controlador accesible solo para rol Student (o vista “Mi horario”), obtener el usuario actual, el año académico (selector o actual) y llamar a `GetByStudentUserAsync(currentUser.Id, academicYearId)`. No aceptar `studentUserId` desde la petición para evitar que un estudiante consulte el horario de otro.
4. **Vista:** Reutilizar la misma estructura de datos que usa la vista/API por grupo (por ejemplo la misma forma de “grilla” por día y bloque), alimentada por el resultado de `GetByStudentUserAsync`, para no duplicar clases ni lógica de presentación.
5. **Multi-tenant:** No exponer nunca un listado de “todos los estudiantes” para elegir; el estudiante solo ve su propio horario derivado de su propia StudentAssignment.

Con esto, el módulo de horario del estudiante queda alineado con la arquitectura actual, sin fugas entre escuelas y con una sola cadena de relaciones bien definida.
