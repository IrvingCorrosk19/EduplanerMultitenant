# Pruebas funcionales – Módulo de horarios

**Fecha:** 2026-02-12  
**Tipo:** Análisis estático de flujo (controlador + servicio). Sin ejecución de aplicación.  
**Objetivo:** Validar reglas de negocio y seguridad para los escenarios indicados.

---

## Metodología

Para cada escenario se describe:
- **Precondiciones / datos de entrada** (rol, parámetros).
- **Flujo en código** (controlador → servicio).
- **Resultado esperado** (comportamiento deseado).
- **Resultado actual** (comportamiento deducido del código).
- **Estado:** Correcto / Error.
- **Riesgo detectado** (si aplica).

---

## Escenario 1: Profesor crea horario válido

**Precondiciones:** Usuario con rol Teacher. `teacherAssignmentId` pertenece a ese docente. `timeSlotId`, `academicYearId` válidos. `dayOfWeek` 1–7. No existe otra entrada del mismo docente en ese año+día+bloque. No existe otra entrada del mismo grupo en ese año+día+bloque.

**Flujo:**
- `SaveEntry` recibe request válido y `currentUserId`.
- `CreateEntryAsync`: carga TA (con SubjectAssignment), TimeSlot, AcademicYear. Rol = teacher, `ta.TeacherId == currentUserId` → permitido. Consulta conflicto docente: no hay otra entrada con mismo año+día+bloque y mismo `TeacherId`. Consulta conflicto grupo: no hay otra entrada con mismo año+día+bloque y mismo `GroupId`. Crea `ScheduleEntry`, `SaveChanges`, devuelve entrada con Includes.

**Resultado esperado:** `success: true`, mensaje de guardado, `data` con la entrada creada (id, subjectName, timeSlotName, etc.).

**Resultado actual:** El código cumple el flujo anterior. El controlador devuelve `Json(new { success = true, message = "Entrada guardada.", data })` con el objeto mapeado.

**Estado:** Correcto  

**Riesgo detectado:** Ninguno.

---

## Escenario 2: Profesor intenta crear conflicto docente

**Precondiciones:** Profesor ya tiene una entrada para el mismo año académico, mismo día (dayOfWeek) y mismo bloque (timeSlotId). Intenta crear otra entrada (misma o distinta TeacherAssignment) que implique el mismo docente en ese mismo slot.

**Flujo:**
- `CreateEntryAsync`: pasa validación de pertenencia (TA del profesor). La consulta `teacherConflict` busca cualquier `ScheduleEntry` con mismo `AcademicYearId`, `DayOfWeek`, `TimeSlotId` y mismo `TeacherId` (vía `e.TeacherAssignment.TeacherId == ta.TeacherId`). Encuentra al menos una → lanza `InvalidOperationException` con mensaje "Conflicto de horario: el docente ya tiene una clase asignada en el mismo día y bloque para este año académico."

**Resultado esperado:** `success: false`, mensaje claro de conflicto docente. No se crea entrada.

**Resultado actual:** El servicio lanza `InvalidOperationException`. El controlador la captura en `catch (InvalidOperationException ex)` y devuelve `Json(new { success = false, message = ex.Message, data = (object?)null })`.

**Estado:** Correcto  

**Riesgo detectado:** Ninguno. Además, el índice único `(teacher_assignment_id, academic_year_id, time_slot_id, day_of_week)` evita duplicar la misma asignación en el mismo slot; la validación en aplicación evita que el mismo docente tenga dos asignaciones distintas en el mismo slot.

---

## Escenario 3: Profesor intenta crear conflicto grupo

**Precondiciones:** Ya existe una entrada para el mismo grupo (mismo `GroupId` vía otra TeacherAssignment) en el mismo año, día y bloque. El profesor intenta asignar otra materia al mismo grupo en ese slot.

**Flujo:**
- `CreateEntryAsync`: obtiene `groupId = ta.SubjectAssignment.GroupId`. La consulta `groupConflict` busca cualquier `ScheduleEntry` con mismo `AcademicYearId`, `DayOfWeek`, `TimeSlotId` y mismo `GroupId` (vía `TeacherAssignment.SubjectAssignment.GroupId == groupId`). Encuentra al menos una → lanza `InvalidOperationException` con mensaje "Conflicto de horario: el grupo ya tiene una clase asignada en el mismo día y bloque para este año académico."

**Resultado esperado:** `success: false`, mensaje claro de conflicto de grupo. No se crea entrada.

**Resultado actual:** Igual que escenario 2: excepción capturada y devuelta como `success: false` con `message` del servicio.

**Estado:** Correcto  

**Riesgo detectado:** Ninguno.

---

## Escenario 4: Profesor intenta registrar horario de otro docente

**Precondiciones:** Usuario con rol Teacher. Envía `teacherAssignmentId` que corresponde a una asignación de otro docente (otro `TeacherId`).

**Flujo:**
- `CreateEntryAsync`: carga TA. Rol = teacher, `ta.TeacherId != currentUserId` → lanza `UnauthorizedAccessException` con mensaje "Solo puede asignar horarios a sus propias materias. La asignación docente no le pertenece."

**Resultado esperado:** `success: false`, mensaje de no autorizado. No se crea entrada.

**Resultado actual:** El controlador captura `UnauthorizedAccessException` y devuelve `Json(new { success = false, message = ex.Message, data = (object?)null })`.

**Estado:** Correcto  

**Riesgo detectado:** Ninguno. La validación se hace en servicio con el rol actual; un docente no puede bypassear desde la UI si el backend recibe un `teacherAssignmentId` ajeno.

---

## Escenario 5: Admin consulta horario de cualquier docente

**Precondiciones:** Usuario con rol Admin. Llama a `ListJsonByTeacher(teacherId: X, academicYearId: Y)` donde X es el id de otro docente.

**Flujo:**
- `ListJsonByTeacher`: `GetCurrentUserRoleAsync()` devuelve admin → `isTeacher` = false. `effectiveTeacherId = teacherId` (el pasado por parámetro). No se aplica la restricción `teacherId != currentUserId`. Se llama `GetByTeacherAsync(effectiveTeacherId, academicYearId)` y se devuelve el JSON con las entradas de ese docente.

**Resultado esperado:** `success: true`, `data` con la lista de entradas del docente X para el año Y.

**Resultado actual:** El código permite a Admin (y Director) consultar cualquier `teacherId`; solo Teacher queda restringido a su propio id.

**Estado:** Correcto  

**Riesgo detectado:** Ninguno.

---

## Escenario 6: Director consulta horario por grupo

**Precondiciones:** Usuario con rol Director. Llama a `ListJsonByGroup(groupId, academicYearId)` con ids válidos.

**Flujo:**
- `ListJsonByGroup`: comprueba usuario autenticado y que `groupId` y `academicYearId` no sean vacíos. No hay comprobación de rol que restrinja el acceso. Llama `GetByGroupAsync(groupId, academicYearId)` y devuelve las entradas de ese grupo para ese año.

**Resultado esperado:** `success: true`, `data` con la lista de entradas del grupo para el año.

**Resultado actual:** Cualquier usuario autorizado (Teacher, Admin, Director) puede llamar a `ListJsonByGroup`; el endpoint no filtra por escuela en el controlador (el servicio devuelve lo que existe en BD para ese grupo). Si los grupos son por escuela, la consistencia depende de que grupoId pertenezca a la escuela del usuario (no hay validación explícita de escuela en este endpoint).

**Estado:** Correcto  

**Riesgo detectado:** Bajo. Si en el futuro se requiere que un Director solo vea grupos de su escuela, habría que añadir validación (p. ej. que `groupId` pertenezca a la escuela del usuario). No es fallo del escenario actual “Director consulta por grupo”.

---

## Escenario 7: Eliminar entrada válida

**Precondiciones:** Usuario Teacher elimina una entrada que le pertenece (su `TeacherAssignment`), o Admin/Director elimina cualquier entrada. Se envía `DeleteEntry` con el `id` de esa entrada.

**Flujo:**
- `DeleteEntryAsync`: carga la entrada con Include(TeacherAssignment). Si es Teacher, comprueba `entry.TeacherAssignment.TeacherId == currentUserId`; si es Admin/Director, no se exige coincidencia. Elimina la entrada y hace `SaveChanges`.

**Resultado esperado:** `success: true`, mensaje de entrada eliminada. La fila desaparece de la base de datos.

**Resultado actual:** El controlador devuelve `Json(new { success = true, message = "Entrada eliminada.", data = (object?)null })` tras `DeleteEntryAsync` sin excepción.

**Estado:** Correcto  

**Riesgo detectado:** Ninguno.

---

## Escenario 8: Intentar eliminar entrada de otro docente

**Precondiciones:** Usuario con rol Teacher. Envía `DeleteEntry` con el `id` de una entrada cuya `TeacherAssignment` pertenece a otro docente.

**Flujo:**
- `DeleteEntryAsync`: carga la entrada. Rol = teacher y `entry.TeacherAssignment.TeacherId != currentUserId` → lanza `UnauthorizedAccessException` con mensaje "Solo puede eliminar horarios de sus propias asignaciones." No se ejecuta `Remove` ni `SaveChanges`.

**Resultado esperado:** `success: false`, mensaje de no autorizado. La entrada no se elimina.

**Resultado actual:** El controlador captura `UnauthorizedAccessException` y devuelve `success: false` con el mensaje de la excepción.

**Estado:** Correcto  

**Riesgo detectado:** Ninguno.

---

## Resumen de resultados

| # | Escenario                              | Resultado esperado     | Resultado actual | Estado   | Riesgo   |
|---|----------------------------------------|------------------------|------------------|----------|----------|
| 1 | Profesor crea horario válido           | success, entrada creada| Cumple           | Correcto | Ninguno  |
| 2 | Profesor intenta conflicto docente     | success false, mensaje | Cumple           | Correcto | Ninguno  |
| 3 | Profesor intenta conflicto grupo      | success false, mensaje | Cumple           | Correcto | Ninguno  |
| 4 | Profesor registra horario de otro      | success false, no auth | Cumple           | Correcto | Ninguno  |
| 5 | Admin consulta horario de cualquier   | success, datos de X    | Cumple           | Correcto | Ninguno  |
| 6 | Director consulta por grupo            | success, datos grupo   | Cumple           | Correcto | Bajo*    |
| 7 | Eliminar entrada válida                | success, eliminada     | Cumple           | Correcto | Ninguno  |
| 8 | Eliminar entrada de otro docente       | success false, no auth | Cumple           | Correcto | Ninguno  |

\* Riesgo bajo: posible mejora futura de validar que el grupo pertenezca a la escuela del usuario en `ListJsonByGroup`.

---

## Conclusiones

- Los ocho escenarios se comportan según las reglas de negocio y seguridad definidas: creación válida, rechazo de conflictos docente y grupo, restricción de Teacher a sus propias asignaciones en crear/eliminar, y permisos de Admin/Director para consultar cualquier docente y por grupo.
- Las excepciones del servicio se traducen correctamente en respuestas JSON con `success: false` y mensaje adecuado, sin filtrar información sensible.
- No se han detectado errores en la lógica analizada. La única recomendación es la validación opcional de escuela en la consulta por grupo (Director/Admin) si el modelo de datos lo requiere en el futuro.

**Veredicto:** Comportamiento funcional y de seguridad correcto para los escenarios revisados. Recomendable complementar con pruebas de integración/E2E contra base de datos y UI cuando se ejecuten en entorno real.
