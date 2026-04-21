# Auditoría arquitectónica – Horario estudiante (Student Schedule)

**Fecha:** 2026-02-12  
**Tipo:** Validación y análisis. Sin modificación de código.  
**Referencia:** Docs/QA/INVESTIGACION_RELACIONES_HORARIO_ESTUDIANTE.md  

---

## Resumen ejecutivo

Se validó la cadena de relaciones propuesta para el módulo de horario del estudiante y el uso de **User (rol Student) → StudentAssignment → Group → SubjectAssignment → TeacherAssignment → ScheduleEntry → TimeSlot**. La cadena es **correcta** y coherente con el modelo actual: no existe una ruta alternativa más correcta y la reutilización de `GetByGroupAsync` es adecuada. Se identifican **riesgos medios** por múltiples StudentAssignments activas y por la necesidad de validar explícitamente que el grupo pertenezca a la escuela del usuario. No se detectan fugas multi-tenant ni necesidad de añadir `school_id` a `schedule_entries`. El diseño es **enterprise-safe** siempre que la implementación fije una política clara para “asignación activa por año” y valide grupo contra escuela. A continuación se detallan hallazgos, riesgos y recomendaciones.

---

## Confirmación o rechazo de la cadena

**Confirmación: la cadena propuesta es correcta.**

| Eslabón | Verificación en código / modelo |
|--------|----------------------------------|
| User (rol Student) | `student_assignments.student_id` → `users.id` (constraint `fk_student`, migración 20251102175646). |
| StudentAssignment | Conecta User con Group: `GroupId` → `groups.id`, `AcademicYearId` opcional, `IsActive`. |
| Group | Sin relación directa con schedule_entries; acceso vía SubjectAssignment. |
| SubjectAssignment → TeacherAssignment → ScheduleEntry → TimeSlot | Confirmado en `ScheduleService.GetByGroupAsync`: filtra por `TeacherAssignment.SubjectAssignment.GroupId == groupId` y `AcademicYearId`. |

No existe una ruta alternativa más correcta: la única forma de pasar de “estudiante” (User) a horario es vía StudentAssignment (para obtener GroupId) y luego la misma lógica que ya usa el horario por grupo. La tabla `students` no participa en esta cadena y no debe usarse para el módulo Student Schedule.

---

## 1. Validación de modelo

### 1.1 StudentAssignment como conector

- **Correcto.** StudentAssignment es la entidad que vincula al estudiante (User) con el grupo y el año académico. No hay otra entidad que relacione directamente User (Student) con Group en el dominio de horarios.
- **Ruta alternativa:** No. Otras tablas (e.g. Attendance, DisciplineReport) referencian “student” y “group” pero no definen la asignación académica vigente; StudentAssignment es la fuente de verdad para “a qué grupo pertenece el estudiante” en un contexto de horario.

### 1.2 Duplicidad conceptual Student vs User(Student)

- **No hay duplicidad para el horario.** En el módulo de horario solo interviene User + StudentAssignment. La entidad `Student` (tabla `students`) es un concepto distinto (vínculo padre/hijo con User acudiente) y no tiene `group_id` ni relación con schedule.
- **Ambigüedad de diseño:** En otros módulos del sistema, “studentId” a veces denota `User.Id` (ej. StudentAssignmentService, GetAssignmentsByStudentIdAsync) y en otros podría referirse a `Student.Id` (ej. estudiantes como hijos del acudiente). Para el módulo Student Schedule debe usarse de forma explícita **User.Id** (el usuario autenticado con rol Student) y no el Id de la tabla `students`.

### 1.3 Posibles inconsistencias futuras

- Si en el futuro se añade una relación directa “estudiante → grupo” en otra tabla (o se migra Student a tener GroupId), podría generarse confusión sobre qué fuente usar. La recomendación es mantener una única fuente: **StudentAssignment** para “grupo actual del estudiante” en contexto académico/horario.

---

## 2. Riesgos de integridad

### 2.1 Múltiples StudentAssignments activas

- **Situación:** No existe constraint único (StudentId + AcademicYearId) ni (StudentId + IsActive). Un mismo User puede tener varias filas con `IsActive == true` (por ejemplo por migración de datos, cambios de grupo sin desactivar la anterior, o uso de AssignAsync que inactiva anteriores pero podría haber ventanas o errores).
- **Impacto:** Si el módulo toma “cualquier” asignación activa y hay dos con distinto GroupId, no queda definido qué horario mostrar. Podría mostrarse un horario que no corresponde al grupo actual del estudiante.
- **Mitigación:** En la implementación, definir una **política explícita**: por ejemplo “una sola asignación activa por StudentId y AcademicYearId” (filtrar por año académico seleccionado o actual) y tomar `FirstOrDefault` o la más reciente por `CreatedAt`. Documentar que el negocio debe evitar múltiples activas por año; opcionalmente en el futuro añadir constraint o regla de dominio.

### 2.2 AcademicYear no coincide

- **Situación:** StudentAssignment.AcademicYearId es **nullable**. Puede haber asignaciones activas sin año académico.
- **Impacto:** Si se filtra solo por `AcademicYearId == academicYearId`, se excluirían esas asignaciones y el estudiante no vería horario. Si no se filtra por año, podría mezclarse horario de varios años.
- **Mitigación:** Definir regla: (1) Si la asignación tiene AcademicYearId, usarla solo cuando coincida con el año seleccionado (o “año actual” de la escuela). (2) Si AcademicYearId es null, asociar al “año académico actual” de la escuela del usuario para la consulta de horario. Así se evita ambigüedad y se alinea con el selector de año que usará la vista.

### 2.3 Cambio de grupo y ScheduleEntries de años anteriores

- **Situación:** Cuando un estudiante cambia de grupo, las ScheduleEntries siguen ligadas a TeacherAssignment (y por tanto al grupo antiguo) por AcademicYearId. No se borran entradas históricas.
- **Impacto:** No es un problema de integridad. Para un año dado, el horario que debe verse es el del **grupo en que el estudiante está asignado para ese año**. Si la asignación activa para ese año apunta al grupo nuevo, GetByGroupAsync(groupIdNuevo, academicYearId) devuelve el horario correcto. Las entradas del grupo antiguo siguen existiendo para ese año pero no se muestran al estudiante porque no se consultan con el GroupId del grupo viejo.
- **Histórico:** Si se quiere “ver mi horario del año pasado”, se necesitaría una asignación (activa o no) para ese StudentId + AcademicYearId pasado; la política de “una activa por año” no impide conservar asignaciones inactivas para años anteriores. La arquitectura soporta histórico siempre que la capa de aplicación resuelva asignación por año (activa o última conocida para ese año).

---

## 3. Multi-tenant

### 3.1 ¿Puede un estudiante consultar horario de otra escuela?

- **No**, siempre que la implementación:
  - Use **solo** el usuario autenticado (currentUser.Id) para buscar StudentAssignments, y no acepte ningún parámetro “studentUserId” desde el cliente.
  - Tras obtener GroupId desde la asignación del usuario, **valide** que `Group.SchoolId == currentUser.SchoolId` antes de llamar a `GetByGroupAsync`. Si los datos están correctos, la asignación del estudiante ya será de su escuela; la validación protege frente a datos corruptos o migraciones incorrectas.

### 3.2 Joins e aislamiento

- GetByGroupAsync no hace join con `schools`; filtra por `groupId` y `academicYearId`. El aislamiento se garantiza porque el `groupId` solo se obtiene de una StudentAssignment del usuario actual y, con la validación anterior, ese grupo es de la misma escuela. No hay joins implícitos que permitan “filtrar por otra escuela”.

### 3.3 ¿Agregar school_id a schedule_entries?

- **No necesario.** La escuela queda determinada por:
  - AcademicYear.SchoolId (cada entrada tiene AcademicYearId),
  - TimeSlot.SchoolId (vía TimeSlotId),
  - y por el Group (SubjectAssignment → Group.SchoolId) usado para filtrar.
- Añadir `school_id` a `schedule_entries` sería redundante y podría desincronizarse; la arquitectura actual es suficiente para multi-tenant.

---

## 4. Performance

### 4.1 Cadena de consultas

Flujo propuesto:

1. Obtener asignación(es) del estudiante: `StudentAssignments.Where(sa => sa.StudentId == userId && sa.IsActive && [AcademicYearId])`.
2. Obtener GroupId de la asignación elegida.
3. Llamar a `GetByGroupAsync(groupId, academicYearId)`.

### 4.2 N+1

- **No hay N+1.** Paso 1 es una sola consulta. Paso 3 es una sola consulta con Include (TeacherAssignment → SubjectAssignment → Subject/Group, Teacher, TimeSlot, AcademicYear). No se itera sobre colecciones cargando relaciones en bucle.

### 4.3 Includes

- GetByGroupAsync ya incluye las navegaciones necesarias para mostrar horario (materia, grupo, docente, bloque, año). Para el estudiante no se requieren includes adicionales en esa llamada; solo hace falta incluir en el paso 1 lo necesario para GroupId (y opcionalmente Group para validar SchoolId), que puede ser un Select con GroupId o un Include(Group) mínimo.

### 4.4 Índices

- Existentes y útiles:
  - `IX_student_assignments_student_id`
  - `IX_student_assignments_student_active` (StudentId, IsActive)
  - `IX_student_assignments_student_academic_year` (StudentId, AcademicYearId)
- La consulta por StudentId + IsActive (y opcionalmente AcademicYearId) está cubierta. No se identifica necesidad de índices adicionales para el flujo de horario estudiante.

---

## 5. Arquitectura futura

### 5.1 GetByStudentUserAsync

- **Conviene crearlo.** Centraliza la resolución “usuario estudiante → asignación → grupo → horario”, aplica la política de “asignación activa por año” en un solo lugar y garantiza que nunca se use un studentUserId distinto del usuario actual (el método recibe el Id del usuario en contexto, no desde la petición). Así se evita duplicar lógica y se reduce el riesgo de que en el futuro alguien exponga un parámetro “studentId” desde el cliente.

### 5.2 Horario del estudiante solo lectura

- **Sí.** El estudiante no debe crear, editar ni eliminar ScheduleEntries; solo consultar. Los permisos de escritura deben seguir en Admin/Director/Teacher como en el módulo Schedule actual. No se requiere nueva regla de negocio más allá de “rol Student = solo lectura en horario”.

### 5.3 Reutilización completa de GetByGroupAsync

- **Sí.** Una vez resuelto el GroupId (y validado contra la escuela), la llamada a GetByGroupAsync(groupId, academicYearId) es suficiente y devuelve la misma estructura que ya usa la vista/API por grupo. No es necesario duplicar la query ni crear un DTO distinto para el estudiante; se puede reutilizar el mismo formato de respuesta (por ejemplo MapEntryToJson en el controlador) para la vista “Mi horario”.

---

## Hallazgos críticos

**Ninguno** que invalide la arquitectura o impida un diseño enterprise-safe. La cadena es correcta y el multi-tenant se puede garantizar con las validaciones indicadas.

---

## Hallazgos medios

| ID | Hallazgo | Mitigación |
|----|----------|------------|
| M1 | Múltiples StudentAssignments activas para el mismo User (y posiblemente mismo año) sin constraint único. | Definir y documentar política: una asignación “vigente” por año (por ejemplo filtrar por AcademicYearId y tomar una sola); considerar en el futuro constraint o regla de negocio para evitar múltiples activas por (StudentId, AcademicYearId). |
| M2 | StudentAssignment.AcademicYearId nullable; sin regla clara, podría no mostrarse horario o mezclarse años. | En GetByStudentUserAsync (o equivalente): si la asignación tiene AcademicYearId, usarla solo cuando coincida con el año solicitado; si es null, usar el año académico actual de la escuela para la consulta de horario. |
| M3 | Si por error de datos el Group de la StudentAssignment es de otra escuela, sin validación se podría devolver horario de esa escuela. | Validar explícitamente Group.SchoolId == currentUser.SchoolId (o que el grupo pertenezca a la escuela del usuario) antes de llamar a GetByGroupAsync; en caso contrario, no devolver horario y registrar/alertar. |

---

## Hallazgos bajos

| ID | Hallazgo | Comentario |
|----|----------|------------|
| B1 | Ambigüedad de “studentId” en el sistema (a veces User.Id, a veces Student.Id). | Para Student Schedule usar siempre User.Id (estudiante logueado) y nombrar claramente en código y documentación. |
| B2 | GetAssignmentsByStudentIdAsync (StudentAssignmentService) no devuelve AcademicYearId en el Select actual. | Para horario, la consulta de asignaciones puede hacerse en el propio GetByStudentUserAsync (o servicio de horario) incluyendo AcademicYearId y Group para no depender de ese método. |

---

## Recomendaciones arquitectónicas

1. **Implementar GetByStudentUserAsync** (o equivalente) que: (a) reciba solo el userId del usuario en contexto (nunca desde cliente); (b) resuelva una asignación activa para el año académico indicado (o año actual); (c) valide que el grupo sea de la escuela del usuario; (d) llame a GetByGroupAsync y devuelva el mismo tipo de datos.
2. **Fijar política de “asignación vigente”:** Una sola asignación activa considerada por (StudentId, AcademicYearId) para la vista de horario; criterio de desempate documentado (ej. más reciente por CreatedAt) si hubiera más de una.
3. **Manejar AcademicYearId null:** Tratar asignaciones sin año como “vigentes para el año actual” de la escuela a efectos de mostrar horario, o excluirlas si la política es “solo asignaciones con año”; documentar la decisión.
4. **Vista de solo lectura:** El rol Student no debe tener acciones de creación/edición/eliminación de ScheduleEntries; reutilizar la misma estructura de presentación que por grupo (misma API o mismo modelo de vista).
5. **No añadir school_id a schedule_entries** ni duplicar lógica de consulta de horario; mantener el filtro por grupo y año académico.

---

## Veredicto final

**Student Schedule Architecture Safe: YES**

La cadena User → StudentAssignment → Group → SubjectAssignment → TeacherAssignment → ScheduleEntry → TimeSlot es correcta, no existe una ruta alternativa más correcta, y la reutilización de GetByGroupAsync es adecuada. Con una política clara para asignación activa por año, validación de grupo contra escuela y horario en solo lectura para el estudiante, el diseño es apto para un entorno enterprise. Los hallazgos medios son mitigables en la implementación sin cambios estructurales ni de modelo de datos.
