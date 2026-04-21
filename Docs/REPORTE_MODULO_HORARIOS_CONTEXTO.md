# Reporte: Módulo de Horarios – Contexto para nuevas funcionalidades

Documento de contexto que resume **todo lo implementado** en el módulo de horarios para apoyar el desarrollo de nuevas funcionalidades.

---

## 1. Resumen ejecutivo

- **Objetivo del módulo:** Permitir definir bloques horarios por escuela (mañana/tarde), asignar a cada docente sus materias en bloques y días (cuadro horario), y consultar horario por docente o por grupo.
- **Roles:** Admin/Director configuran jornada y bloques; Teacher ve y asigna su propio horario; Admin/Director pueden ver horario de cualquier docente de su escuela.
- **Jornadas:** Se usan `shifts` (Mañana, Tarde). Los grupos tienen `shift_id`; los bloques (`time_slots`) pueden tener `shift_id`; la configuración de jornada genera bloques con jornada asignada.

---

## 2. Tablas de base de datos (horarios)

| Tabla | Descripción |
|-------|-------------|
| **time_slots** | Bloques horarios por escuela. Columnas: id, school_id, shift_id (opcional), name, start_time, end_time, display_order, is_active, created_at. |
| **schedule_entries** | Una celda del horario: asignación docente + bloque + día + año. Columnas: id, teacher_assignment_id, time_slot_id, day_of_week (1–7), academic_year_id, created_at, created_by. Índice único: (teacher_assignment_id, academic_year_id, time_slot_id, day_of_week). |
| **school_schedule_configurations** | Una fila por escuela. Define mañana (hora inicio, duración min, cantidad bloques) y opcionalmente tarde (igual). Al guardar se regeneran los TimeSlots. Columnas: afternoon_start_time, afternoon_block_duration_minutes, afternoon_block_count (nullable). |
| **shifts** | Catálogo de jornadas por escuela (Mañana, Tarde). Usado en groups.shift_id y time_slots.shift_id. |
| **teacher_assignments** | Vincula User (profesor) con SubjectAssignment (materia en un grupo). |
| **subject_assignments** | Materia en un grupo (group_id). |
| **groups** | Tiene shift_id (jornada del grupo). |

Documentación detallada:
- **ESTRUCTURA_BD_PROFESOR_Y_HORARIO.md** – Cadena profesor → jornada y profesor → horario (schedule_entries).
- **ESTRUCTURA_BD_JORNADAS.md** – Tablas shifts, groups, time_slots, school_schedule_configurations.

---

## 3. Controladores y rutas

### 3.1 ScheduleController

| Acción | Método | Ruta / uso | Descripción |
|--------|--------|------------|-------------|
| Index | GET | /Schedule | Página índice (puede redirigir o mostrar menú). |
| ByTeacher | GET | /Schedule/ByTeacher?teacherId=&academicYearId= | Vista principal: horario por docente. Teacher solo ve el suyo; Admin/Director eligen docente. Muestra jornada(s) del docente (ViewBag.TeacherShiftNames). |
| ListJsonTimeSlots | GET | /Schedule/ListJsonTimeSlots | JSON: bloques horarios activos de la escuela (para construir tabla). |
| ListJsonByTeacher | GET | /Schedule/ListJsonByTeacher?teacherId=&academicYearId= | JSON: entradas de horario del docente para el año. |
| ListJsonByGroup | GET | /Schedule/ListJsonByGroup?groupId=&academicYearId= | JSON: entradas de horario del grupo para el año. |
| SaveEntry | POST | /Schedule/SaveEntry (body: TeacherAssignmentId, TimeSlotId, DayOfWeek, AcademicYearId) | Crea una ScheduleEntry. Valida conflictos docente y grupo. |
| DeleteEntry | POST | /Schedule/DeleteEntry (body: Id) | Elimina una ScheduleEntry. Teacher solo puede eliminar propias. |

**Autorización:** `[Authorize(Roles = "Teacher,Admin,Director,teacher,admin,director")]`

### 3.2 ScheduleConfigurationController

| Acción | Método | Ruta | Descripción |
|--------|--------|-----|-------------|
| Index | GET | /ScheduleConfiguration/Index | Formulario de configuración de jornada (mañana + tarde opcional). Carga SchoolScheduleConfiguration por escuela o modelo por defecto. |
| SaveConfiguration | POST | /ScheduleConfiguration/SaveConfiguration | Guarda configuración y regenera TimeSlots. Parámetro forceRegenerate: si true, borra ScheduleEntries de la escuela y regenera bloques. |

**Autorización:** `[Authorize(Roles = "Admin,Director,admin,director")]`

### 3.3 TimeSlotController

| Acción | Método | Ruta | Descripción |
|--------|--------|-----|-------------|
| Index | GET | /TimeSlot/Index | Lista de bloques horarios de la escuela. |
| Manage | GET | /TimeSlot/Manage | Misma lista; entrada desde menú “Ajustar bloques horarios”. |
| Create | GET/POST | /TimeSlot/Create | Crear bloque manual (nombre, inicio, fin, orden). |
| Edit | GET/POST | /TimeSlot/Edit/{id} | Editar bloque. |
| Delete | POST | /TimeSlot/Delete/{id} | Eliminar bloque; si tiene ScheduleEntries se desactiva (IsActive = false). |

**Autorización:** Admin/Director (Manage y CRUD); Index accesible según menú.

---

## 4. Servicios

### 4.1 IScheduleService / ScheduleService

- **CreateEntryAsync(teacherAssignmentId, timeSlotId, dayOfWeek, academicYearId, currentUserId):** Crea ScheduleEntry. Valida: asignación existe, bloque existe, año existe; Teacher solo sus asignaciones; sin conflicto mismo docente mismo año+día+bloque; sin conflicto mismo grupo mismo año+día+bloque.
- **DeleteEntryAsync(id, currentUserId):** Elimina entrada; Teacher solo puede eliminar propias.
- **GetByTeacherAsync(teacherId, academicYearId):** Lista ScheduleEntry del docente para el año (con TeacherAssignment, SubjectAssignment, Subject, Group, TimeSlot).
- **GetByGroupAsync(groupId, academicYearId):** Lista ScheduleEntry del grupo para el año.

### 4.2 IScheduleConfigurationService / ScheduleConfigurationService

- **GetBySchoolIdAsync(schoolId):** Devuelve SchoolScheduleConfiguration de la escuela o null.
- **SaveAndGenerateBlocksAsync(model, schoolId, forceRegenerate):** Valida mañana (al menos 1 bloque y duración); tarde opcional (si hay hora de inicio, duración y cantidad > 0); tarde debe empezar después del último bloque de mañana. Si hay ScheduleEntries y no forceRegenerate, devuelve error. Si forceRegenerate, elimina todas las ScheduleEntries de la escuela, luego actualiza o crea configuración, elimina todos los TimeSlots de la escuela, obtiene/crea jornadas Mañana y Tarde (IShiftService.GetOrCreateBySchoolAndNameAsync), genera bloques de mañana con ShiftId = Mañana, genera bloques de tarde (si se configuró) con ShiftId = Tarde, y hace commit.

### 4.3 IShiftService

- **GetOrCreateBySchoolAndNameAsync(schoolId, name):** Usado al generar bloques para asignar ShiftId a cada TimeSlot (Mañana/Tarde).

---

## 5. Vistas principales

| Vista | Descripción |
|-------|-------------|
| **Schedule/ByTeacher.cshtml** | Página de horario por docente: filtros (docente, año académico), botón “Cargar horario”, tabla dinámica (días 1–7, bloques en filas). Muestra “Usted imparte en jornada(s): …” (ViewBag.TeacherShiftNames). Si no hay años académicos (HasNoAcademicYears), solo mensaje de aviso. Script: carga entradas vía ListJsonByTeacher, guarda con SaveEntry, elimina con DeleteEntry. |
| **ScheduleConfiguration/Index.cshtml** | Formulario: Mañana (hora inicio, duración min, cantidad bloques), Tarde opcional (igual), checkbox “Forzar regeneración”, botón “Guardar y generar bloques”, enlace “Ver bloques horarios” (TimeSlot/Index). Cálculo en JS: fin de mañana y sugerencia de inicio de tarde. |
| **TimeSlot/Index.cshtml** | Lista de TimeSlots de la escuela (nombre, hora inicio/fin, orden). Enlaces Crear, Editar, Eliminar. |
| **TimeSlot/Create.cshtml, Edit.cshtml** | Formularios para crear/editar bloque (Name, StartTime, EndTime, DisplayOrder, IsActive en Edit). |

---

## 6. Menú (Horarios)

En **Views/Shared/_AdminLayout.cshtml**:

- **Admin/Director:** Menú “Horarios” con:
  - **Horario por Docente** → Schedule/ByTeacher
  - **Configuración de jornada** → ScheduleConfiguration/Index
  - **Ajustar bloques horarios** → TimeSlot/Manage
- **Teacher:** Enlace directo “Horarios” → Schedule/ByTeacher.

---

## 7. Scripts de arranque y verificación

| Script | Dónde se llama | Qué hace |
|--------|----------------|----------|
| **EnsureScheduleTables** | Program.cs (al arranque) | Crea tablas time_slots y schedule_entries si no existen. |
| **EnsureSchoolScheduleConfigurationTable** | Program.cs (al arranque) | Crea tabla school_schedule_configurations si no existe; si existe, asegura columnas de tarde (afternoon_*) con ALTER TABLE ADD COLUMN IF NOT EXISTS. |
| **EnsureDefaultTimeSlots** | Program.cs (tras login/escuela) | Si la escuela no tiene ningún TimeSlot, crea 8 bloques de 45 min (07:00–13:00). |

**Verificación en BD:** Script **Scripts/VerifySchoolScheduleConfiguration.sql** (listar columnas y datos de school_schedule_configurations).

---

## 8. Registro en Program.cs

- `AddScoped<IScheduleService, ScheduleService>();`
- `AddScoped<IScheduleConfigurationService, ScheduleConfigurationService>();`
- ScheduleConfigurationService depende de SchoolDbContext e IShiftService.
- ScheduleService depende de SchoolDbContext e ICurrentUserService.

---

## 9. Modelos clave (entidades)

- **TimeSlot:** Id, SchoolId, ShiftId?, Name, StartTime, EndTime, DisplayOrder, IsActive, CreatedAt. Navegación: Shift, ScheduleEntries.
- **ScheduleEntry:** Id, TeacherAssignmentId, TimeSlotId, DayOfWeek (byte 1–7), AcademicYearId, CreatedAt, CreatedBy. Navegación: TeacherAssignment, TimeSlot, AcademicYear.
- **SchoolScheduleConfiguration:** Id, SchoolId, MorningStartTime, MorningBlockDurationMinutes, MorningBlockCount, AfternoonStartTime?, AfternoonBlockDurationMinutes?, AfternoonBlockCount?, CreatedAt, UpdatedAt.
- **TeacherAssignment:** Id, TeacherId, SubjectAssignmentId. Navegación: Teacher (User), SubjectAssignment → Group → ShiftNavigation (Shift).
- **Group:** ShiftId, Shift (string legacy), ShiftNavigation (Shift).

---

## 10. Flujos de negocio resumidos

1. **Configurar jornada (Admin/Director):** Ir a Configuración de jornada, definir mañana y opcionalmente tarde, guardar → se crean/actualizan TimeSlots con ShiftId (Mañana/Tarde). Si ya hay horarios asignados, hay que marcar “Forzar regeneración”.
2. **Ajustar bloques a mano (Admin/Director):** Ajustar bloques horarios (TimeSlot/Manage): crear, editar o desactivar bloques.
3. **Ver/armar horario docente:** Horario por Docente → elegir año (y docente si Admin), Cargar horario → se rellenan celdas; el docente puede asignar materia en celda (SaveEntry) o quitar (DeleteEntry). Conflictos: mismo docente o mismo grupo no pueden tener dos clases en el mismo año+día+bloque.
4. **Jornada del profesor:** Se calcula por los grupos que imparte (TeacherAssignment → SubjectAssignment → Group → ShiftNavigation). Se muestra en ByTeacher como “Usted imparte en jornada(s): Mañana, Tarde”.

---

## 11. Documentación adicional en Docs/

| Documento | Contenido |
|-----------|-----------|
| DISENO_MODULO_HORARIOS.md | Diseño y convenciones del módulo. |
| ESTRUCTURA_BD_PROFESOR_Y_HORARIO.md | Cómo identificar a qué horario/jornada pertenece un profesor. |
| ESTRUCTURA_BD_JORNADAS.md | Tablas y columnas de jornadas (shifts, groups, time_slots, school_schedule_configurations). |
| VERIFICACION_JORNADAS.md | Verificación de jornadas en el sistema. |
| PRUEBAS_* / CONFIRMACION_* / AUDITORIA_* | Pruebas y auditoría del módulo. |
| IMPLEMENTACION_BLINDADO_ACADEMIC_YEAR.md | Año académico por defecto y mensaje en ByTeacher cuando no hay años. |

---

## 12. Puntos a tener en cuenta para nuevas funcionalidades

- **Multi-tenant:** Todas las consultas filtran por `user.SchoolId` (TimeSlots, ScheduleEntries vía TimeSlot, configuración, docentes, grupos).
- **Año académico:** Las entradas de horario son por `academic_year_id`. Si no hay años académicos, ByTeacher muestra solo mensaje de aviso (blindado).
- **Conflictos:** Al crear ScheduleEntry se validan conflictos de docente y de grupo; no hay validación por “aula” (no hay RoomId en ScheduleEntry en la implementación actual).
- **Jornadas:** Los bloques pueden tener `shift_id`; los grupos tienen `shift_id`. Para “filtrar por jornada” en listados o reportes, usar Group.ShiftId o TimeSlot.ShiftId según el caso.
- **Borrado de bloques:** Si un TimeSlot tiene ScheduleEntries, Delete lo desactiva (IsActive = false) en lugar de eliminarlo.
- **Formato de hora:** 24 h en configuración de jornada y en la app (TimeOnly, HH:mm).

Con este contexto se puede extender el módulo (por ejemplo: horario por grupo en vista, reportes, restricciones por aula, o integración con otras áreas) manteniendo la misma arquitectura y reglas de negocio.
