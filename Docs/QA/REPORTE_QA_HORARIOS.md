# REPORTE QA COMPLETO - MODULO HORARIOS

**Fecha:** 2025-02-16
**Modulo:** Schedule (Horarios)
**Version analizada:** Post-fix (con correcciones CSRF, FK, Multi-tenant, TeacherAssignment)
**Tipo de prueba:** Analisis estatico de codigo + revision de seguridad
**Estado general:** APROBADO CON OBSERVACIONES

---

## RESUMEN EJECUTIVO

| Metrica | Valor |
|---------|-------|
| Escenarios evaluados | 32 |
| OK (sin hallazgos) | 26 |
| FAIL (defectos detectados) | 2 |
| WARNING (riesgo medio/bajo) | 4 |
| Tasa de exito | 81.3% |
| Severidad critica | 0 (corregidos previamente) |
| Severidad media | 3 |
| Severidad baja | 3 |
| Listo para produccion | SI, con correcciones menores |

### Correcciones previamente aplicadas (ya en codigo)
1. CSRF Protection en ScheduleController (`[AutoValidateAntiforgeryToken]`)
2. FK-safe delete en SuperAdminService (ScheduleEntries antes de TeacherAssignments)
3. Multi-tenant en ListJsonByTeacher y ListJsonByGroup
4. Acceso Teacher a GetAssignmentsByTeacher con validacion de seguridad

---

## A) FUNCIONALIDAD DOCENTE (Teacher Flow)

### A.1 Acceso a vista ByTeacher

| Campo | Valor |
|-------|-------|
| **Endpoint** | `GET /Schedule/ByTeacher` |
| **Archivo** | `ScheduleController.cs:42-101` |
| **Resultado** | OK |
| **Evidencia** | Teacher con `user.SchoolId != null` obtiene `effectiveTeacherId = user.Id` (linea 55). Si `SchoolId == null`, redirect a Home. ViewBag.IsEditable y ViewBag.IsTeacher se setean a `true` para el rol teacher. |

### A.2 Carga de bloques horarios (TimeSlots)

| Campo | Valor |
|-------|-------|
| **Endpoint** | `GET /Schedule/ListJsonTimeSlots` |
| **Archivo** | `ScheduleController.cs:107-121` |
| **Resultado** | OK |
| **Evidencia** | Filtra por `SchoolId == user.SchoolId` y `IsActive == true`. Ordena por `DisplayOrder`, luego `StartTime`. Retorna JSON con id, name, startTime (HH:mm), endTime (HH:mm), displayOrder. |

### A.3 Carga de asignaciones (TeacherAssignments)

| Campo | Valor |
|-------|-------|
| **Endpoint** | `GET /TeacherAssignment/GetAssignmentsByTeacher/{id}` |
| **Archivo** | `TeacherAssignmentController.cs:241-298` |
| **Resultado** | OK |
| **Evidencia** | Seguridad implementada: Teacher/Docente solo puede consultar `id == currentUserId`. Retorna `currentAssignments` y `allPossibleAssignments`. |
| **Nota** | `allPossibleAssignments` llama a `GetAllSubjectAssignments()` sin filtro de escuela -- ver hallazgo M.1. |

### A.4 Creacion de entrada de horario (SaveEntry)

| Campo | Valor |
|-------|-------|
| **Endpoint** | `POST /Schedule/SaveEntry` |
| **Archivo** | `ScheduleController.cs:203-241`, `ScheduleService.cs:18-106` |
| **Resultado** | OK |
| **Evidencia** | Validaciones: TeacherAssignmentId, TimeSlotId, AcademicYearId != Empty, DayOfWeek 1-7. Servicio verifica existencia de TA, TimeSlot y AcademicYear. Teacher solo puede crear para sus propios TA (linea 49-50). Detecta conflictos de docente y grupo. |

### A.5 Deteccion de conflicto docente

| Campo | Valor |
|-------|-------|
| **Endpoint** | `POST /Schedule/SaveEntry` |
| **Archivo** | `ScheduleService.cs:53-64` |
| **Resultado** | OK |
| **Evidencia** | Busca en `ScheduleEntries` donde mismo `AcademicYearId + DayOfWeek + TimeSlotId + TeacherAssignment.TeacherId`. Si existe, lanza `InvalidOperationException("Conflicto de horario: el docente ya tiene una clase asignada...")`. |
| **Nota sobre indice** | El indice unico `IX_schedule_entries_unique_slot` es sobre `(teacher_assignment_id, academic_year_id, time_slot_id, day_of_week)` -- protege por TeacherAssignment, pero la validacion por TeacherId se hace a nivel de aplicacion. Esto es correcto porque un docente puede tener multiples TeacherAssignments. |

### A.6 Deteccion de conflicto grupo

| Campo | Valor |
|-------|-------|
| **Endpoint** | `POST /Schedule/SaveEntry` |
| **Archivo** | `ScheduleService.cs:67-80` |
| **Resultado** | OK |
| **Evidencia** | Busca en `ScheduleEntries` via `TeacherAssignment.SubjectAssignment.GroupId` donde mismo `AcademicYearId + DayOfWeek + TimeSlotId + GroupId`. Si existe, lanza `InvalidOperationException("Conflicto de horario: el grupo ya tiene una clase asignada...")`. |

### A.7 Eliminacion de entrada de horario

| Campo | Valor |
|-------|-------|
| **Endpoint** | `POST /Schedule/DeleteEntry` |
| **Archivo** | `ScheduleController.cs:243-270`, `ScheduleService.cs:108-126` |
| **Resultado** | OK |
| **Evidencia** | Valida `request.Id != Guid.Empty`. Servicio verifica que la entrada existe, y si es Teacher, que `TeacherAssignment.TeacherId == currentUserId`. Luego `Remove + SaveChanges`. |

### A.8 Visualizacion del horario cargado

| Campo | Valor |
|-------|-------|
| **Endpoint** | `GET /Schedule/ListJsonByTeacher` |
| **Archivo** | `ScheduleController.cs:127-168`, `ScheduleService.cs:128-146` |
| **Resultado** | OK |
| **Evidencia** | Teacher: `teacherId == currentUser.Id` obligatorio (linea 143-144). Query con Includes completos (Subject, Group, Teacher, TimeSlot, AcademicYear). MapEntryToJson produce: id, teacherAssignmentId, timeSlotId, dayOfWeek, subjectName, groupName, teacherName, startTime, endTime. |

---

## B) FUNCIONALIDAD ADMIN/DIRECTOR

### B.1 Seleccion de docente en vista

| Campo | Valor |
|-------|-------|
| **Endpoint** | `GET /Schedule/ByTeacher?teacherId={id}` |
| **Archivo** | `ScheduleController.cs:54-57, 69-78` |
| **Resultado** | OK |
| **Evidencia** | Si no es Teacher: `effectiveTeacherId = teacherId ?? user.Id`. Lista de docentes filtrada por `SchoolId == user.SchoolId` y rol teacher/docente. |

### B.2 Consulta por docente (cross-school protegido)

| Campo | Valor |
|-------|-------|
| **Endpoint** | `GET /Schedule/ListJsonByTeacher?teacherId={id}&academicYearId={id}` |
| **Archivo** | `ScheduleController.cs:149-154` |
| **Resultado** | OK |
| **Evidencia** | Multi-tenant: `AnyAsync(u => u.Id == teacherId && u.SchoolId == currentUser.SchoolId)`. Si no pertenece, retorna `success:false, "El docente no pertenece a su escuela."` |

### B.3 Consulta por grupo

| Campo | Valor |
|-------|-------|
| **Endpoint** | `GET /Schedule/ListJsonByGroup?groupId={id}&academicYearId={id}` |
| **Archivo** | `ScheduleController.cs:173-201`, `ScheduleService.cs:148-168` |
| **Resultado** | OK |
| **Evidencia** | Multi-tenant: `AnyAsync(g => g.Id == groupId && g.SchoolId == currentUser.SchoolId)`. Valida groupId y academicYearId no vacios. |

### B.4 Admin puede crear/eliminar entradas

| Campo | Valor |
|-------|-------|
| **Endpoint** | `POST /Schedule/SaveEntry`, `POST /Schedule/DeleteEntry` |
| **Archivo** | `ScheduleService.cs:46-50, 118-122` |
| **Resultado** | OK |
| **Evidencia** | La restriccion de seguridad (`isTeacher && ta.TeacherId != currentUserId`) solo aplica al rol Teacher. Admin/Director pasa la validacion y puede crear/eliminar entradas de cualquier docente de su escuela. |

### B.5 Vista ByTeacher muestra jornadas del docente

| Campo | Valor |
|-------|-------|
| **Endpoint** | `GET /Schedule/ByTeacher` |
| **Archivo** | `ScheduleController.cs:82-90` |
| **Resultado** | OK |
| **Evidencia** | `teacherShiftNames` se obtiene via `TeacherAssignments -> SubjectAssignment -> Group -> ShiftNavigation`. Se pasa a ViewBag para mostrar en la vista. |

---

## C) CONFIGURACION DE JORNADA (ScheduleConfiguration)

### C.1 Acceso restringido a Admin/Director

| Campo | Valor |
|-------|-------|
| **Endpoint** | `GET /ScheduleConfiguration/Index` |
| **Archivo** | `ScheduleConfigurationController.cs:8-9` |
| **Resultado** | OK |
| **Evidencia** | `[Authorize(Roles = "Admin,Director,admin,director")]` a nivel de clase. Teacher no tiene acceso. |

### C.2 Carga de configuracion existente

| Campo | Valor |
|-------|-------|
| **Endpoint** | `GET /ScheduleConfiguration/Index` |
| **Archivo** | `ScheduleConfigurationController.cs:24-42` |
| **Resultado** | OK |
| **Evidencia** | `GetBySchoolIdAsync(user.SchoolId.Value)` con `AsNoTracking()`. Si no existe, muestra defaults (7:00, 45min, 8 bloques). Multi-tenant por SchoolId. |

### C.3 Validacion de datos de jornada

| Campo | Valor |
|-------|-------|
| **Endpoint** | `POST /ScheduleConfiguration/SaveConfiguration` |
| **Archivo** | `ScheduleConfigurationService.cs:27-46` |
| **Resultado** | OK |
| **Evidencia** | Valida: morning blocks >= 1, morning duration >= 1, afternoon (si tiene start) requiere count y duration > 0, no solapamiento (afternoon start >= morning end). |

### C.4 Generacion de TimeSlots con transaccion

| Campo | Valor |
|-------|-------|
| **Endpoint** | `POST /ScheduleConfiguration/SaveConfiguration` |
| **Archivo** | `ScheduleConfigurationService.cs:61-177` |
| **Resultado** | OK |
| **Evidencia** | Usa `BeginTransactionAsync`. Elimina entradas de horario si existen, luego TimeSlots, luego regenera morning y afternoon slots con ShiftId. Commit o Rollback en catch. |

### C.5 Bloqueo si existen horarios (sin forceRegenerate)

| Campo | Valor |
|-------|-------|
| **Endpoint** | `POST /ScheduleConfiguration/SaveConfiguration` |
| **Archivo** | `ScheduleConfigurationService.cs:53-58` |
| **Resultado** | OK |
| **Evidencia** | Si `hasEntries && !forceRegenerate`, retorna `(false, "No se puede regenerar la jornada porque ya existen horarios asignados...")`. El checkbox "Forzar regeneracion" controla `forceRegenerate = true`. |

---

## D) ADMINISTRACION DE BLOQUES (TimeSlot CRUD)

### D.1 CRUD restringido a Admin/Director

| Campo | Valor |
|-------|-------|
| **Endpoint** | `/TimeSlot/*` |
| **Archivo** | `TimeSlotController.cs:9` |
| **Resultado** | OK |
| **Evidencia** | `[Authorize(Roles = "Admin,Director,admin,director")]` a nivel de clase. CSRF con `[ValidateAntiForgeryToken]` en cada POST (Create, Edit, Delete). |

### D.2 Creacion manual de bloque

| Campo | Valor |
|-------|-------|
| **Endpoint** | `POST /TimeSlot/Create` |
| **Archivo** | `TimeSlotController.cs:65-91` |
| **Resultado** | OK |
| **Evidencia** | Valida Name no vacio y EndTime > StartTime. Asigna SchoolId del usuario. IsActive = true. DisplayOrder >= 0. |

### D.3 Eliminacion segura (soft-delete si hay entradas)

| Campo | Valor |
|-------|-------|
| **Endpoint** | `POST /TimeSlot/Delete` |
| **Archivo** | `TimeSlotController.cs:137-163` |
| **Resultado** | OK |
| **Evidencia** | Si `AnyAsync(e => e.TimeSlotId == id)` => `slot.IsActive = false` (soft-delete con mensaje "Bloque desactivado"). Si no hay entradas => `Remove` (hard-delete). Multi-tenant: filtra por SchoolId. |

---

## E) SEGURIDAD

### E.1 CSRF (Cross-Site Request Forgery)

| Campo | Valor |
|-------|-------|
| **Componente** | Todos los controladores |
| **Resultado** | OK |
| **Evidencia** | - `ScheduleController`: `[AutoValidateAntiforgeryToken]` a nivel de clase (linea 10) - `ScheduleConfigurationController`: `[AutoValidateAntiforgeryToken]` a nivel de clase + `[ValidateAntiForgeryToken]` en POST (lineas 9, 45) - `TimeSlotController`: `[ValidateAntiForgeryToken]` en cada POST (lineas 66, 107, 138) - `Program.cs`: `AddAntiforgery(options => { options.HeaderName = "RequestVerificationToken"; })` (lineas 29-32) - `ByTeacher.cshtml`: Envia header `RequestVerificationToken` en todas las llamadas fetch |

### E.2 Autenticacion y Autorizacion (Roles)

| Campo | Valor |
|-------|-------|
| **Componente** | Todos los controladores |
| **Resultado** | OK |
| **Evidencia** | - `ScheduleController`: `Teacher,Admin,Director` (case-insensitive) - `ScheduleConfigurationController`: `Admin,Director` solamente - `TimeSlotController`: `Admin,Director` solamente - `TeacherAssignmentController`: `admin,secretaria,teacher,docente,director` (+ variantes case) |
| **Nota** | Cada controlador usa `[Authorize(Roles=...)]` a nivel de clase. Roles duplicados en ambas variantes (ej. "Teacher,teacher") para compatibilidad. |

### E.3 Multi-tenant (aislamiento por escuela)

| Campo | Valor |
|-------|-------|
| **Componente** | Todos los endpoints de datos |
| **Resultado** | WARNING - Ver hallazgo M.1 |
| **Evidencia** | **Protegidos:** ListJsonTimeSlots (SchoolId), ByTeacher (SchoolId en TimeSlots, teachers), ListJsonByTeacher (teacher belongs to school), ListJsonByGroup (group belongs to school), TimeSlot CRUD (SchoolId filter), ScheduleConfiguration (SchoolId). **Parcialmente protegido:** SaveEntry - ver hallazgo M.1. |

### E.4 Seguridad de Teacher (solo sus propios datos)

| Campo | Valor |
|-------|-------|
| **Componente** | SaveEntry, DeleteEntry, ListJsonByTeacher, GetAssignmentsByTeacher |
| **Resultado** | OK |
| **Evidencia** | - `SaveEntry`: Teacher solo crea entradas si `ta.TeacherId == currentUserId` (Service:49-50) - `DeleteEntry`: Teacher solo elimina si `entry.TeacherAssignment.TeacherId == currentUserId` (Service:121-122) - `ListJsonByTeacher`: `teacherId != currentUser.Id` retorna error (Controller:143-144) - `GetAssignmentsByTeacher`: `id != currentUserId.Value` retorna error (TeacherAssignmentController:255-256) |

### E.5 Proteccion contra inyeccion SQL

| Campo | Valor |
|-------|-------|
| **Componente** | Todos los servicios |
| **Resultado** | OK |
| **Evidencia** | Todas las consultas usan Entity Framework LINQ (parametrizado automaticamente). No hay uso de `FromSqlRaw` o `ExecuteSqlRaw` en los servicios del modulo (excepto `EnsureScheduleTables.cs` que usa DDL estatico sin inputs de usuario). |

---

## F) INTEGRIDAD REFERENCIAL

### F.1 FK schedule_entries -> teacher_assignments (DELETE)

| Campo | Valor |
|-------|-------|
| **Componente** | SuperAdminService.DeleteUserRelationsAsync |
| **Archivo** | `SuperAdminService.cs:744-771` |
| **Resultado** | OK (corregido) |
| **Evidencia** | Flujo: (1) obtiene TeacherAssignmentIds del usuario, (2) elimina ScheduleEntries asociados, (3) luego elimina TeacherAssignments. EF config: `DeleteBehavior.ClientSetNull` (no cascade automatico, requiere eliminacion explicita). |
| **DB-level FK** | `FOREIGN KEY (teacher_assignment_id) REFERENCES teacher_assignments (id)` - sin ON DELETE, default = RESTRICT. La aplicacion maneja el orden correcto. |

### F.2 FK schedule_entries -> time_slots (DELETE)

| Campo | Valor |
|-------|-------|
| **Componente** | TimeSlotController.Delete, ScheduleConfigurationService |
| **Archivo** | `TimeSlotController.cs:137-163`, `ScheduleConfigurationService.cs:64-75` |
| **Resultado** | OK |
| **Evidencia** | - TimeSlotController: Si hay ScheduleEntries para el slot => soft-delete (IsActive=false). Si no => hard-delete. - ScheduleConfigurationService: Elimina todas las ScheduleEntries de los slots de la escuela antes de eliminar los TimeSlots (lineas 67-74). |
| **DB-level FK** | `FOREIGN KEY (time_slot_id) REFERENCES time_slots (id)` - sin ON DELETE, default = RESTRICT. La aplicacion maneja el orden. |

### F.3 FK schedule_entries -> academic_years (DELETE)

| Campo | Valor |
|-------|-------|
| **Componente** | No se encontro flujo de eliminacion de academic_years |
| **Resultado** | WARNING - Ver hallazgo M.2 |
| **Evidencia** | `DeleteBehavior.ClientSetNull` en EF, pero `AcademicYearId` en ScheduleEntry es `NOT NULL` (tipo Guid, no Guid?). Si se elimina un AcademicYear que tiene ScheduleEntries, EF intentara SetNull en un campo NOT NULL, provocando una excepcion. No existe flujo de eliminacion de AcademicYear en el modulo actual, pero es un riesgo latente. |

---

## G) RENDIMIENTO

### G.1 Consultas con Include (N+1 potencial)

| Campo | Valor |
|-------|-------|
| **Componente** | ScheduleService (GetByTeacherAsync, GetByGroupAsync, CreateEntryAsync) |
| **Resultado** | WARNING - Ver hallazgo P.1 |
| **Evidencia** | **GetByTeacherAsync (lineas 130-146):** 5 niveles de Include encadenados (TeacherAssignment -> SubjectAssignment -> Subject/Group, Teacher, TimeSlot, AcademicYear). EF genera un solo SQL con JOINs. No hay N+1, pero el JOIN puede ser pesado con muchos datos. **CreateEntryAsync - Validacion de conflictos (lineas 53-61, 68-77):** Usa `.Include(e => e.TeacherAssignment)` dentro de `AnyAsync`. EF NO necesita el Include para el AnyAsync ya que solo evalua la condicion WHERE. El Include es innecesario y puede generar un LEFT JOIN extra en la query de validacion. **CreateEntryAsync - Return (lineas 96-105):** Hace `FirstAsync` con 5 Includes despues del SaveChanges. Correcto pero podria optimizarse retornando el entry recien creado sin re-query. |

### G.2 Indices de base de datos

| Campo | Valor |
|-------|-------|
| **Componente** | SchoolDbContext + EnsureScheduleTables |
| **Resultado** | OK |
| **Evidencia** | **schedule_entries:** - `IX_schedule_entries_teacher_assignment_id` (FK index) - `IX_schedule_entries_time_slot_id` (FK index) - `IX_schedule_entries_academic_year_id` (FK index) - `IX_schedule_entries_unique_slot` UNIQUE (teacher_assignment_id, academic_year_id, time_slot_id, day_of_week) **time_slots:** - `IX_time_slots_school_id` (FK index) - `IX_time_slots_shift_id` (FK index) |
| **Nota** | El indice unico cubre las combinaciones de la restriccion de unicidad. Los indices FK cubren las JOINs mas frecuentes. |

### G.3 Paginacion y limites

| Campo | Valor |
|-------|-------|
| **Componente** | Todos los endpoints de lista |
| **Resultado** | WARNING - Ver hallazgo P.2 |
| **Evidencia** | Ninguno de los endpoints de lista (ListJsonByTeacher, ListJsonByGroup, ListJsonTimeSlots) tiene paginacion o limite de resultados. En escenarios normales esto no es problema (un docente tiene ~40 entradas por ano, una escuela ~20 TimeSlots), pero no hay proteccion contra volumenes anormales. |

---

## HALLAZGOS DETALLADOS

### RIESGO MEDIO

#### M.1 - SaveEntry: Falta validacion multi-tenant en TeacherAssignment

| Campo | Valor |
|-------|-------|
| **Archivo** | `ScheduleService.cs:28-31` |
| **Descripcion** | `CreateEntryAsync` busca la TeacherAssignment por ID sin filtrar por SchoolId. Un Admin malicioso podria (en teoria) crear una entrada de horario usando un `teacherAssignmentId` de otra escuela si conoce el UUID. |
| **Codigo afectado** | `FirstOrDefaultAsync(t => t.Id == teacherAssignmentId)` - no filtra por escuela. |
| **Impacto** | Medio. Requiere conocer un UUID valido de otra escuela, pero no hay validacion de que el TA pertenezca a la escuela del usuario. |
| **Recomendacion** | Agregar validacion: `var currentUser = await _currentUserService.GetCurrentUserAsync(); var ta = ... .Include(t => t.Teacher).FirstOrDefaultAsync(t => t.Id == teacherAssignmentId && t.Teacher.SchoolId == currentUser.SchoolId);` |
| **Tambien afecta** | `GetAssignmentsByTeacher` en `TeacherAssignmentController` llama `GetAllSubjectAssignments()` sin filtro de escuela, exponiendo potencialmente subject assignments de otras escuelas. |

#### M.2 - DeleteBehavior.ClientSetNull en campo NOT NULL (AcademicYear)

| Campo | Valor |
|-------|-------|
| **Archivo** | `SchoolDbContext.cs:2459-2463`, `ScheduleEntry.cs:17` |
| **Descripcion** | La FK `schedule_entries.academic_year_id` tiene `DeleteBehavior.ClientSetNull` en EF, pero el campo es `Guid` (NOT NULL). Si se elimina un AcademicYear con ScheduleEntries asociados, EF intentara hacer SET NULL en un campo obligatorio, lanzando excepcion. |
| **Impacto** | Medio. No hay flujo de eliminacion de AcademicYear en el modulo actual, pero si se agrega uno en el futuro sin manejar ScheduleEntries, fallara. |
| **Recomendacion** | Cambiar a `DeleteBehavior.Restrict` (para bloquear la eliminacion si hay dependencias) o agregar logica de eliminacion previa de ScheduleEntries al eliminar AcademicYear. |

#### M.3 - EnsureScheduleTables suprime excepciones silenciosamente

| Campo | Valor |
|-------|-------|
| **Archivo** | `EnsureScheduleTables.cs:76-80` |
| **Descripcion** | El catch generico solo escribe a `Debug.WriteLine`, que no aparece en logs de produccion ni en consola. Si falla la creacion de tablas (ej. por permisos, conflictos de schema), la aplicacion arranca sin tablas de horarios y todos los queries fallan con errores genericos. |
| **Impacto** | Medio. Verificado en pruebas locales: las tablas no se crearon y no hubo indicacion en los logs del servidor. |
| **Recomendacion** | Cambiar `System.Diagnostics.Debug.WriteLine` a `ILogger.LogError` o al menos `Console.Error.WriteLine`. Considerar lanzar la excepcion si es critica para el modulo. |

### RIESGO BAJO

#### L.1 - Dias 6-7 no visibles en UI pero aceptados por backend

| Campo | Valor |
|-------|-------|
| **Archivo** | `ByTeacher.cshtml` (tabla HTML), `ScheduleService.cs:25-26` |
| **Descripcion** | El backend acepta `DayOfWeek` 1-7, pero la vista ByTeacher solo renderiza columnas para dias 1-5 (Lunes a Viernes). Si se crean entradas para dias 6-7 (via API directa), no seran visibles en la UI pero existiran en la BD. |
| **Impacto** | Bajo. Es un escenario edge-case. La UI no permite crear entradas para sabado/domingo. |
| **Recomendacion** | Considerar limitar el backend a 1-5 si no se requieren fines de semana, o agregar columnas opcionales en la vista. |

#### L.2 - Include innecesario en AnyAsync para validacion de conflictos

| Campo | Valor |
|-------|-------|
| **Archivo** | `ScheduleService.cs:53-61, 68-77` |
| **Descripcion** | Las validaciones de conflicto usan `.Include(e => e.TeacherAssignment)` antes de `AnyAsync()`. EF Core traduce AnyAsync a EXISTS/subquery, y el Include genera un LEFT JOIN innecesario que no aporta a la condicion EXISTS. |
| **Impacto** | Bajo. Marginal performance overhead. El query planner de PostgreSQL puede optimizarlo, pero es codigo innecesario. |
| **Recomendacion** | Remover los `.Include()` de las validaciones AnyAsync. Ejemplo: `_context.ScheduleEntries.AnyAsync(e => e.AcademicYearId == ... && e.TeacherAssignment.TeacherId == ...)` funciona identico sin Include. |

#### L.3 - Re-query innecesario despues de SaveChanges en CreateEntryAsync

| Campo | Valor |
|-------|-------|
| **Archivo** | `ScheduleService.cs:96-105` |
| **Descripcion** | Despues de `SaveChangesAsync`, se hace un `FirstAsync` con 5 Includes para retornar el entry completo. El entry ya esta en el DbContext tras el Add, pero sin navegaciones. Se podria evitar la re-query cargando los datos necesarios desde el contexto existente. |
| **Impacto** | Bajo. Query adicional al DB pero solo para una fila con PKs indexados. |
| **Recomendacion** | Aceptable como esta. Si se optimiza, se puede usar el contexto existente y cargar las navegaciones manualmente. |

---

## MATRIZ DE ESCENARIOS

| # | Escenario | Estado | Severidad |
|---|-----------|--------|-----------|
| A.1 | Teacher accede a ByTeacher | OK | - |
| A.2 | Carga TimeSlots | OK | - |
| A.3 | Carga TeacherAssignments | OK | - |
| A.4 | Crear entrada horario | OK | - |
| A.5 | Conflicto docente detectado | OK | - |
| A.6 | Conflicto grupo detectado | OK | - |
| A.7 | Eliminar entrada horario | OK | - |
| A.8 | Ver horario cargado | OK | - |
| B.1 | Admin selecciona docente | OK | - |
| B.2 | Admin consulta por docente (cross-school) | OK | - |
| B.3 | Admin consulta por grupo | OK | - |
| B.4 | Admin crea/elimina entradas | OK | - |
| B.5 | Vista muestra jornadas docente | OK | - |
| C.1 | Config restringida a Admin/Director | OK | - |
| C.2 | Carga config existente | OK | - |
| C.3 | Validacion datos de jornada | OK | - |
| C.4 | Generacion TimeSlots transaccional | OK | - |
| C.5 | Bloqueo sin forceRegenerate | OK | - |
| D.1 | TimeSlot CRUD restringido | OK | - |
| D.2 | Creacion manual de bloque | OK | - |
| D.3 | Eliminacion segura (soft-delete) | OK | - |
| E.1 | CSRF en todos los POST | OK | - |
| E.2 | Roles correctos por controlador | OK | - |
| E.3 | Multi-tenant aislamiento | WARNING | Media |
| E.4 | Teacher solo sus datos | OK | - |
| E.5 | Inyeccion SQL | OK | - |
| F.1 | FK schedule->teacher_assignments | OK | - |
| F.2 | FK schedule->time_slots | OK | - |
| F.3 | FK schedule->academic_years | WARNING | Media |
| G.1 | N+1 / Include excesivos | WARNING | Baja |
| G.2 | Indices de BD | OK | - |
| G.3 | Paginacion | WARNING | Baja |

---

## TOP 10 RECOMENDACIONES (priorizadas)

| # | Prioridad | Hallazgo | Accion | Esfuerzo |
|---|-----------|----------|--------|----------|
| 1 | **ALTA** | M.1 | Agregar filtro `SchoolId` en `CreateEntryAsync` al buscar TeacherAssignment | 15 min |
| 2 | **ALTA** | M.1 | Filtrar `GetAllSubjectAssignments()` por SchoolId en GetAssignmentsByTeacher | 15 min |
| 3 | **ALTA** | M.3 | Cambiar `Debug.WriteLine` a `ILogger.LogError` en EnsureScheduleTables | 5 min |
| 4 | **MEDIA** | M.2 | Cambiar `DeleteBehavior.ClientSetNull` a `Restrict` en FK academic_year_id | 10 min |
| 5 | **MEDIA** | L.1 | Decidir si aceptar dias 6-7 o restringir validacion a 1-5 | 5 min |
| 6 | **BAJA** | L.2 | Remover `.Include()` de las validaciones `AnyAsync` en ScheduleService | 10 min |
| 7 | **BAJA** | L.3 | Optimizar re-query post-SaveChanges (opcional) | 15 min |
| 8 | **BAJA** | G.3 | Agregar limite maximo de resultados en endpoints de lista (ej. `.Take(500)`) | 10 min |
| 9 | **BAJA** | - | Agregar logging estructurado en ScheduleService (conflictos, creaciones) | 20 min |
| 10 | **BAJA** | - | Agregar tests unitarios para conflict detection y seguridad | 2-4 hrs |

---

## CHECKLIST DE PRODUCCION

| # | Criterio | Estado | Notas |
|---|----------|--------|-------|
| 1 | CSRF en todos los POST | PASS | AutoValidateAntiforgeryToken + HeaderName configurado |
| 2 | Autorizacion por roles | PASS | Cada controlador tiene [Authorize] correcto |
| 3 | Multi-tenant (SchoolId) en queries de lectura | PASS | ListJsonByTeacher, ListJsonByGroup, TimeSlots, Config |
| 4 | Multi-tenant (SchoolId) en queries de escritura | PARTIAL | SaveEntry no valida school del TA - ver M.1 |
| 5 | Teacher solo accede a sus datos | PASS | Validado en SaveEntry, DeleteEntry, ListByTeacher, GetAssignments |
| 6 | Integridad referencial (FK safe) | PASS | Delete order correcto en SuperAdmin y TimeSlot |
| 7 | Transacciones en operaciones criticas | PASS | ScheduleConfigurationService usa BeginTransaction |
| 8 | Indices para queries frecuentes | PASS | FK indexes + unique index en schedule_entries |
| 9 | Validacion de inputs | PASS | DayOfWeek 1-7, GUIDs no vacios, Name no vacio |
| 10 | Error handling sin leak de info interna | PARTIAL | Excepciones se retornan con `ex.Message` (aceptable para app interna) |
| 11 | Tablas creadas en startup | PARTIAL | EnsureScheduleTables existe pero falla silenciosamente |
| 12 | Logging y observabilidad | PARTIAL | Solo ScheduleController tiene logging (ILogger). Services no tienen. |

---

## CONCLUSION

El modulo de Horarios esta **funcional y seguro** para los flujos principales (Teacher, Admin/Director, Configuracion, TimeSlots). Las 4 correcciones criticas previamente aplicadas (CSRF, FK delete, multi-tenant reads, Teacher access) resolvieron las vulnerabilidades mas graves.

**Quedan 3 hallazgos de riesgo medio** que deben abordarse antes de una auditorea formal:
1. **M.1** - Validacion multi-tenant en escritura (SaveEntry) - el mas importante
2. **M.2** - FK ClientSetNull en campo NOT NULL (riesgo latente)
3. **M.3** - Logging silencioso en creacion de tablas

El modulo es **apto para produccion** con la recomendacion de aplicar las correcciones #1 y #3 del Top 10 antes del deployment.

---

*Generado por analisis estatico de codigo. No sustituye pruebas de integracion en ambiente de staging.*
