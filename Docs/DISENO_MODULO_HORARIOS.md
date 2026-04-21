# Diseño e implementación: Módulo de Horarios (SchoolManager)

**Versión:** 1.0  
**Fecha:** 2026-02-12  
**Referencia:** `Views/Promts/promts.md`

---

## A) Análisis del proyecto real

### A.1 Estructura de carpetas (arquitectura actual)

El proyecto **no** tiene ensamblados separados Domain/Application/Infrastructure; todo vive bajo **SchoolManager** con carpetas lógicas:

| Carpeta | Uso |
|--------|-----|
| `Models/` | Entidades EF (partial class), DbContext, interceptores |
| `Services/Interfaces/` | Contratos de servicios (ej. IGroupService, ITeacherAssignmentService) |
| `Services/Implementations/` | Implementaciones de servicios |
| `Application.Interfaces/` | Algunos contratos (ICurrentUserService, IDateTimeHomologationService) |
| `Infrastructure.Services/` | Algunas implementaciones (DateTimeHomologationService) |
| `Controllers/` | MVC + endpoints JSON en el mismo controlador |
| `Views/` | Razor (por controlador) |
| `ViewModels/`, `Dtos/` | Modelos de entrada/salida y DTOs |
| `Migrations/` | EF Core, nombre `YYYYMMDDHHMMSS_Descripcion.cs` |
| `Program.cs` | DI: `AddScoped<IXXX, XXXService>()` |

**Ubicación del nuevo módulo:** mismo esquema: entidades en `Models/`, interfaces en `Services/Interfaces/`, implementaciones en `Services/Implementations/`, controladores en `Controllers/`, vistas en `Views/TimeSlot/` y `Views/Schedule/` (o un solo `Schedule` con subvistas).

### A.2 Modelado de entidades y OnModelCreating

- **PK:** `Guid`, generado con `HasDefaultValueSql("gen_random_uuid()")` o `uuid_generate_v4()`.
- **Nombres de tabla:** snake_case (ej. `academic_years`, `teacher_assignments`).
- **Nombres de columna:** snake_case (ej. `created_at`, `school_id`).
- **DateTime:** `HasColumnType("timestamp with time zone")`, `HasDefaultValueSql("CURRENT_TIMESTAMP")` donde aplica.
- **Configuración:** Todo en `SchoolDbContext.OnModelCreating`; no se usan IEntityTypeConfiguration en archivos separados.
- **Auditoría:** Muchas entidades tienen `CreatedAt`, y a veces `UpdatedAt`, `CreatedBy`, `UpdatedBy`. No hay soft-delete estándar (algún módulo puede tener `IsDeleted`).

Para **TimeSlot** y **ScheduleEntry** se seguirá: `Id` (Guid), `CreatedAt` (timestamp with time zone, default CURRENT_TIMESTAMP), tablas en snake_case.

### A.3 Servicios existentes

- **Patrón:** Interfaz en `Services/Interfaces/IXxxService.cs`, implementación en `Services/Implementations/XxxService.cs`.
- **Estilo:** Métodos async (GetAllAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync). No se usa AutoMapper en todos los servicios; el prompt pide **no usar AutoMapper** en el módulo de horarios: mapeo manual en servicios o en controlador.
- **Registro:** En `Program.cs`, `builder.Services.AddScoped<IXxxService, XxxService>()`.
- Servicios que el módulo reutilizará: **ITeacherAssignmentService**, **IShiftService**, **IAcademicYearService**, **IGroupService** (para validar Group.ShiftId).

### A.4 Estilo de controladores

- **Vista principal:** `Index()` → devuelve `View()`.
- **API JSON para listas:** `[HttpGet] ListJson()` → `return Json(new { success = true, data = list })`.
- **CRUD por AJAX:**  
  - Crear: `[HttpPost] Create([FromBody] Model model)` → `Json(new { success, id, ... })`.  
  - Editar: `[HttpPost] Edit([FromBody] Model model)` → `Json(new { success })`.  
  - Eliminar: `[HttpPost] Delete([FromBody] Request request)` o `Delete(Guid id)` → `Json(new { success, message })`.
- **Errores:** `Json(new { success = false, message = ex.Message })`.
- **Eliminación:** A veces GET `Delete(id)` + POST `DeleteConfirmed(id)` para formulario; para AJAX suele usarse un solo `Delete` con body.

Se mantendrá el mismo estilo: endpoints JSON (ListJson, Create, Edit, Delete) y respuestas dinámicas `{ success, data | message }` sin DTOs rígidos donde no aporten.

### A.5 DateTime / UTC

- **Interceptor:** `Models/DateTimeInterceptor.cs` convierte todos los `DateTime` a UTC en `SavingChanges`/`SavingChangesAsync`.
- **JSON:** `DateTimeJsonConverter` y `NullableDateTimeJsonConverter` en `Program.cs` (camelCase, conversión consistente).
- **PostgreSQL:** columnas `timestamp with time zone`.  
Para **TimeSlot**, `StartTime`/`EndTime` serán **TimeOnly** (solo hora), no DateTime; no pasan por el interceptor de UTC. En BD se pueden almacenar como `time` o como parte de un tipo hora.

### A.6 Migraciones

- Nombre: `YYYYMMDDHHMMSS_Descripcion.cs` (ej. `20251115115232_AddAcademicYearSupport`).
- Se generan con `dotnet ef migrations add NombreDescriptivo`.
- En el documento no se genera el script aún; solo se describe el alcance (tablas, índices, FKs).

---

## B) Diseño final (sin codificar)

### B.1 Entidades

**1) TimeSlot (bloques horarios)**

| Propiedad     | Tipo        | BD / Notas |
|--------------|-------------|------------|
| Id           | Guid        | PK, gen_random_uuid() |
| SchoolId     | Guid        | FK schools, NOT NULL |
| ShiftId      | Guid?       | FK shifts, NULL = bloque de toda la escuela |
| Name         | string      | ej. "Bloque 1", max 50 |
| StartTime    | TimeOnly    | hora inicio (PostgreSQL `time`) |
| EndTime      | TimeOnly    | hora fin |
| DisplayOrder | int         | orden visual, default 0 |
| IsActive     | bool        | default true |
| CreatedAt    | DateTime?   | timestamp with time zone, default CURRENT_TIMESTAMP |
| CreatedBy    | Guid?       | opcional |
| UpdatedAt    | DateTime?   | opcional |
| UpdatedBy    | Guid?       | opcional |

**Relaciones:** TimeSlot → School, TimeSlot → Shift (opcional).  
**Tabla:** `time_slots`.

**2) ScheduleEntry (entrada de horario)**

| Propiedad           | Tipo   | BD / Notas |
|--------------------|--------|------------|
| Id                 | Guid   | PK, gen_random_uuid() |
| TeacherAssignmentId| Guid   | FK teacher_assignments, NOT NULL |
| TimeSlotId         | Guid   | FK time_slots, NOT NULL |
| DayOfWeek          | byte   | 0=Dom, 1=Lun … 6=Sab (o 1-7 según convención); smallint |
| AcademicYearId     | Guid   | FK academic_years, NOT NULL |
| RoomId             | Guid?  | NULL por ahora (fase posterior si se añade Room) |
| SchoolId           | Guid?  | redundante pero útil para filtros por escuela |
| CreatedAt          | DateTime? | timestamp with time zone, default CURRENT_TIMESTAMP |
| CreatedBy          | Guid?  | opcional |

**Relaciones:** ScheduleEntry → TeacherAssignment, TimeSlot, AcademicYear; opcional Room y School.  
**Tabla:** `schedule_entries`.

**Navegación en entidades existentes (solo lectura, no cambian estructura de asignaciones):**

- TeacherAssignment: sin cambio (no añadir colección ScheduleEntries si se quiere cero impacto; se puede añadir después).
- AcademicYear: `ICollection<ScheduleEntry> ScheduleEntries` (nuevo).

### B.2 Índices y constraints

**TimeSlot**

- PK en `id`.
- IX_time_slots_school_id (school_id).
- IX_time_slots_shift_id (shift_id).
- Índice único opcional: (school_id, shift_id, start_time, end_time) para evitar bloques duplicados en el mismo contexto.

**ScheduleEntry**

- PK en `id`.
- IX_schedule_entries_teacher_assignment_id.
- IX_schedule_entries_time_slot_id.
- IX_schedule_entries_academic_year_id.
- IX_schedule_entries_school_id (si se añade school_id).

**Unicos (evitar conflictos):**

1. **Un docente no puede tener dos clases a la misma hora el mismo día y año:**  
   `UNIQUE (teacher_assignment_id, academic_year_id, time_slot_id, day_of_week)`.  
   Como `teacher_assignment_id` implica un único teacher_id, con esto se evita doble uso del mismo docente en el mismo (año, día, bloque).

2. **Un grupo no puede tener dos clases a la misma hora el mismo día y año:**  
   No se puede expresar solo con columnas de `schedule_entries` porque GroupId viene de TeacherAssignment → SubjectAssignment → GroupId. Opciones:
   - **En aplicación:** al crear/editar ScheduleEntry, consultar el GroupId de la TeacherAssignment y validar que no exista otra ScheduleEntry en el mismo (AcademicYearId, TimeSlotId, DayOfWeek) cuya TeacherAssignment tenga el mismo GroupId.
   - **En BD:** no hay FK a Group en schedule_entries; el unique por grupo sería un índice único “funcional” o trigger. **Recomendación:** validación en servicio (IScheduleService) leyendo GroupId vía TeacherAssignment → SubjectAssignment.

Resumen:

- **Unique en BD:** `(teacher_assignment_id, academic_year_id, time_slot_id, day_of_week)`.
- **Conflicto por grupo:** validación en capa de aplicación.

### B.3 Regla de compatibilidad Shift

- Si **TimeSlot.ShiftId** no es null, entonces la **TeacherAssignment** usada en ScheduleEntry debe ser para un **Group** cuyo **Group.ShiftId** coincida con **TimeSlot.ShiftId**.
- Validación en servicio al crear/actualizar ScheduleEntry:  
  - Obtener Group de TeacherAssignment → SubjectAssignment → Group.  
  - Si TimeSlot.ShiftId tiene valor, exigir Group.ShiftId == TimeSlot.ShiftId; si no, rechazar con mensaje claro.

---

## C) Plan de implementación por fases

| Fase | Contenido |
|------|------------|
| **1** | Entidades TimeSlot y ScheduleEntry en Models/; configuración en SchoolDbContext (OnModelCreating); migración EF (solo añadir tablas e índices, sin tocar TeacherAssignment/SubjectAssignment). |
| **2** | Interfaces: ITimeSlotService, IScheduleService en Services/Interfaces/. |
| **3** | Implementaciones: TimeSlotService, ScheduleService en Services/Implementations/ (sin AutoMapper; mapeo manual si hace falta). Registro en Program.cs. |
| **4** | Controladores: TimeSlotController y ScheduleController (o uno solo Schedule con subrutas). Endpoints JSON: ListJson, Create, Edit, Delete y los específicos de horario (por docente+año, por grupo+año). |
| **5** | UI Razor: vista para administrar TimeSlots (por escuela/jornada); editor de horario por docente+año y por grupo+año; grid “tabla horario” (selects o drag/drop si ya hay librería). Estilo: cards, DataTables, AJAX, SweetAlert. |
| **6** | Reglas y validaciones: conflictos docente y grupo; compatibilidad Shift; pruebas manuales o unitarias. |

---

## D) Salida esperada

### D.1 Lista de archivos nuevos y modificados

**Nuevos**

| Ruta | Descripción |
|------|-------------|
| `Models/TimeSlot.cs` | Entidad TimeSlot. |
| `Models/ScheduleEntry.cs` | Entidad ScheduleEntry. |
| `Services/Interfaces/ITimeSlotService.cs` | Contrato TimeSlot. |
| `Services/Interfaces/IScheduleService.cs` | Contrato Schedule (CRUD + por docente/grupo + validaciones). |
| `Services/Implementations/TimeSlotService.cs` | Implementación ITimeSlotService. |
| `Services/Implementations/ScheduleService.cs` | Implementación IScheduleService. |
| `Controllers/TimeSlotController.cs` | CRUD TimeSlot + ListJson. |
| `Controllers/ScheduleController.cs` | Editor horario + ListJson por docente/grupo + Create/Edit/Delete entradas. |
| `Views/TimeSlot/Index.cshtml` | Lista/bloques por jornada. |
| `Views/Schedule/Index.cshtml` | Punto de entrada (elegir docente o grupo + año). |
| `Views/Schedule/ByTeacher.cshtml` | Horario por docente + año. |
| `Views/Schedule/ByGroup.cshtml` | Horario por grupo + año. |
| `Migrations/YYYYMMDDHHMMSS_AddScheduleModule.cs` | Migración (generada por EF). |
| `Migrations/YYYYMMDDHHMMSS_AddScheduleModule.Designer.cs` | Designer. |

**Modificados**

| Ruta | Cambio |
|------|--------|
| `Models/SchoolDbContext.cs` | DbSet&lt;TimeSlot&gt;, DbSet&lt;ScheduleEntry&gt;, configuración en OnModelCreating (tablas, FKs, índices, único). |
| `Models/AcademicYear.cs` | Añadir `ICollection<ScheduleEntry> ScheduleEntries` (opcional, para navegación). |
| `Program.cs` | Registrar ITimeSlotService/TimeSlotService, IScheduleService/ScheduleService. |
| `Views/Shared/_AdminLayout.cshtml` | Añadir ítem de menú “Horarios” (por rol admin/director/secretaria según criterio del negocio). |

**No se tocan:** TeacherAssignment, SubjectAssignment, Group, Shift (solo lectura desde el módulo).

### D.2 Modelo final en texto

```
TimeSlot
  Id (Guid, PK)
  SchoolId (Guid, FK -> School)
  ShiftId (Guid?, FK -> Shift)
  Name, StartTime (TimeOnly), EndTime (TimeOnly), DisplayOrder, IsActive
  CreatedAt, CreatedBy, UpdatedAt, UpdatedBy

ScheduleEntry
  Id (Guid, PK)
  TeacherAssignmentId (Guid, FK -> TeacherAssignment)
  TimeSlotId (Guid, FK -> TimeSlot)
  DayOfWeek (byte, 0-6)
  AcademicYearId (Guid, FK -> AcademicYear)
  RoomId (Guid?, nullable)
  SchoolId (Guid?, nullable)
  CreatedAt, CreatedBy

TeacherAssignment (existente)
  Id, TeacherId, SubjectAssignmentId
  -> SubjectAssignment (Subject, GradeLevel, Group, Specialty, Area)
  -> User (Teacher)

Regla: Unique (TeacherAssignmentId, AcademicYearId, TimeSlotId, DayOfWeek).
Validación en servicio: mismo (AcademicYearId, TimeSlotId, DayOfWeek) + mismo GroupId (vía TA->SA->Group) => conflicto.
Validación en servicio: si TimeSlot.ShiftId != null => Group.ShiftId == TimeSlot.ShiftId.
```

### D.3 Script de migración (alcance estimado, sin generar)

- **Crear tabla `time_slots`:** columnas anteriores, PK, FK a `schools` y `shifts`, índices en school_id y shift_id.
- **Crear tabla `schedule_entries`:** columnas anteriores, PK, FKs a `teacher_assignments`, `time_slots`, `academic_years`; opcional FK a `schools`; índice único (teacher_assignment_id, academic_year_id, time_slot_id, day_of_week).
- **No** modificar `teacher_assignments` ni `subject_assignments`.  
El script concreto se obtendrá con `dotnet ef migrations add AddScheduleModule` y revisando el Up() generado.

### D.4 Endpoints mínimos recomendados

**TimeSlot**

- `GET /TimeSlot/Index` → Vista lista de bloques.
- `GET /TimeSlot/ListJson` → JSON { success, data: TimeSlot[] } (opcional por schoolId, shiftId).
- `POST /TimeSlot/Create` [FromBody] → crear bloque.
- `POST /TimeSlot/Edit` [FromBody] → editar bloque.
- `POST /TimeSlot/Delete` [FromBody] { id } → eliminar bloque (validar que no tenga ScheduleEntries).

**Schedule**

- `GET /Schedule/Index` → Vista para elegir “por docente” o “por grupo” y año.
- `GET /Schedule/ByTeacher?teacherId=&academicYearId=` → Vista horario docente.
- `GET /Schedule/ByGroup?groupId=&academicYearId=` → Vista horario grupo.
- `GET /Schedule/ListJsonByTeacher?teacherId=&academicYearId=` → JSON con entradas del docente para el año (para rellenar grid).
- `GET /Schedule/ListJsonByGroup?groupId=&academicYearId=` → JSON con entradas del grupo para el año.
- `POST /Schedule/SaveEntry` [FromBody] { teacherAssignmentId, timeSlotId, dayOfWeek, academicYearId } → crear entrada (validar conflictos y Shift).
- `POST /Schedule/DeleteEntry` [FromBody] { id } → eliminar entrada.

Opcional: `GET /Schedule/ListTeacherAssignmentsForYear?academicYearId=&groupId=` para combos (asignaciones disponibles para ese grupo en el año, por convención de “asignaciones vigentes”).

---

## Restricciones respetadas

- No se modifican TeacherAssignment ni SubjectAssignment (solo lectura).
- Horario siempre ligado a AcademicYearId en ScheduleEntry.
- SubjectAssignment no tiene AcademicYearId (no se asume).
- Sin AutoMapper en el nuevo módulo.
- Misma arquitectura (Models, Services, Controllers, Views) y estilo (JSON dinámico, ListJson, Create/Edit/Delete con success/message).
- UI coherente con el resto: cards, DataTables, AJAX, SweetAlert.

---

*Documento listo para usar como base de la Fase 1 (entidades + migración + DbContext) y siguientes.*
