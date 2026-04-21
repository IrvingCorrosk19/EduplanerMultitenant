ROL:
Actúa como Arquitecto Senior .NET 8 / EF Core / PostgreSQL, experto en Clean Architecture y sistemas académicos.

CONTEXTO:
Estoy construyendo SchoolManager con arquitectura limpia (Domain, Application, Infrastructure, API/MVC).
Ya tenemos entidades como:
- Users (roles: admin, teacher, student, secretaria)
- TeacherAssignment (TeacherId + SubjectAssignmentId)
- SubjectAssignment (Subject + GradeLevel + Group + Specialty + Area + School)
- Group (tiene ShiftId)
- Shift (jornada)
- AcademicYear (usado en StudentAssignment, Trimester, StudentActivityScore)

OBJETIVO:
Crear el Módulo de Horarios usando el enfoque recomendado:
- Reutilizar TeacherAssignment como “quién da qué”
- Crear TimeSlot (bloques por School y opcionalmente por Shift)
- Crear ScheduleEntry (TeacherAssignmentId + TimeSlotId + DayOfWeek + AcademicYearId)
- Validaciones de conflicto: docente / grupo / bloque / día / año

IMPORTANTE:
Antes de generar código, analiza el proyecto real y dime:
1) Archivos exactos a tocar y dónde ubicarlos en la arquitectura (Domain/Application/Infrastructure/MVC).
2) Patrones ya existentes (naming, auditoría de CreatedAt, soft delete, servicios, controllers JSON).
3) Convenciones usadas para DateTime/UTC (ya tenemos interceptores).
4) Cómo están modeladas GUID PKs y migraciones actuales.
5) Cómo se manejan los servicios (interfaces primero), sin AutoMapper y con respuestas JSON dinámicas.

TAREAS ESPECÍFICAS:

A) ANÁLISIS DE PROYECTO
- Encuentra cómo se modelan las entidades y su configuración en OnModelCreating.
- Identifica cómo están estructurados los servicios actuales (ej. TeacherAssignmentService, GroupService, etc).
- Identifica el estilo de controllers: endpoints JSON como ListJson/Create/Edit/Delete.

B) DISEÑO FINAL (SIN CODIFICAR AÚN)
- Confirma el diseño final de entidades:
  1) TimeSlot: Id, SchoolId, ShiftId?, Name, StartTime, EndTime, DisplayOrder, IsActive?, CreatedAt
  2) ScheduleEntry: Id, TeacherAssignmentId, TimeSlotId, DayOfWeek, AcademicYearId, RoomId?, CreatedAt
- Define índices únicos recomendados y constraints de conflicto:
  - Unique (AcademicYearId, DayOfWeek, TimeSlotId, TeacherId)
  - Unique (AcademicYearId, DayOfWeek, TimeSlotId, GroupId)
  (GroupId se obtiene por join desde TeacherAssignment -> SubjectAssignment -> GroupId)
- Define regla de compatibilidad Shift:
  - Si TimeSlot.ShiftId != null, entonces Group.ShiftId debe coincidir.

C) PLAN DE IMPLEMENTACIÓN EN FASES
Fase 1: Entidades + migración + DbContext
Fase 2: Interfaces Application (ITimeSlotService, IScheduleService)
Fase 3: Implementaciones Infrastructure
Fase 4: Controllers + Endpoints JSON
Fase 5: UI Razor:
  - Vista para administrar TimeSlots (por jornada)
  - Editor de horario por:
     * Docente + año
     * Grupo + año
  - Grid estilo “tabla horario” con drag/drop o selects (si ya hay libs)
Fase 6: Reglas y validaciones (conflictos) + pruebas

D) SALIDA ESPERADA
Entrega:
1) Lista de archivos nuevos y modificados con rutas.
2) Modelo final recomendado (clases y relaciones en texto).
3) Script de migración estimado (sin generarlo aún).
4) Decisión de endpoints mínimos necesarios.

RESTRICCIONES:
- No romper TeacherAssignment ni SubjectAssignment existentes.
- Mantener la arquitectura limpia.
- Mantener estilo visual del sistema: cards, DataTables, AJAX + SweetAlert.
- No usar AutoMapper.
- No introducir dependencias raras.
- No asumir que SubjectAssignment tiene AcademicYearId (no lo tiene).
- El horario siempre debe quedar ligado a AcademicYearId desde el día 1.
