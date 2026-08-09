# Análisis multi-tenant y preparación para producción — Eduplaner (SchoolManager)

**Alcance:** auditoría estática del repositorio + inspección **solo lectura** de PostgreSQL local `eduplaner` (sin migraciones, sin cambios de datos ni de código).  
**Fecha de referencia:** 20 de abril de 2026.  
**Herramientas:** código C# / EF Core, `psql` contra `eduplaner`.

---

## 1. Resumen ejecutivo (nivel CTO)

Eduplaner implementa un **multi-tenancy lógico por columna `school_id` (o equivalente)** con refuerzo parcial vía **filtros globales de EF (`HasQueryFilter`)** y **filtros manuales en servicios**. Eso es un patrón habitual en MVPs, pero **no constituye aislamiento SaaS de grado enterprise**: el modelo permite **filas huérfanas de tenant**, **consultas que confían en GUID opacos** sin validación de pertenencia, **omitir `school_id` en rutas críticas de escritura**, y **desactivar el filtro de tenant cuando el claim `school_id` está ausente** (rol elevado o contexto sin HTTP).

La base local confirma señales duras: **`student_activity_scores.school_id` existe pero el 100 % de las filas muestreadas están en NULL** (11278/11278), lo que **rompe el contrato mental de “cada fila sabe su colegio”** para el módulo de notas y complica auditoría, reporting y defensa legal de PII. Existe además **deuda de esquema** (`subject_assignments` con columna citada como `"SchoolId"` en PostgreSQL frente al resto en `snake_case`), coherente con riesgo de errores en SQL ad-hoc y en herramientas de BI.

**Veredicto comercial:** como producto **multi-institución en una sola base compartida**, el sistema es **usable con supervisión fuerte**; como **SaaS multi-tenant vendible a escala** con garantías de aislamiento y cumplimiento, **no está cerrado**.

---

## 2. Estado actual del multi-tenancy

### 2.1 Mecanismo de tenant en runtime

- **`TenantProvider`** lee el claim `school_id` del usuario autenticado y expone `Guid? SchoolId`.
- **`SchoolDbContext` (parcial)** aplica `HasQueryFilter` a un **subconjunto** de entidades: cuando `_tenantId == null`, el comentario en código indica explícitamente que **los filtros se omiten y “todos los registros” son visibles**.

### 2.2 Tablas en `eduplaner` **sin** columna `school_id` (derivación solo por FK / joins)

Inspección `information_schema` (tablas base en `public` que **no** tienen columna `school_id`):

`activity_attachments`, `email_api_configurations`, `email_queues`, `prematriculation_histories`, `scan_logs`, `schedule_entries`, `student_assignments`, `student_id_cards`, `student_qr_tokens`, `subject_assignments` (usa `"SchoolId"` en PostgreSQL, no `school_id`), `teacher_assignments`, `teacher_work_plan_details`, `teacher_work_plan_review_logs`, `user_grades`, `user_groups`, `user_subjects`, además de metadatos (`__EFMigrationsHistory`) y `schools`.

**Interpretación:** muchas entidades operativas **dependen de la integridad referencial** hacia tablas que sí tienen tenant, pero **no son “first-class” en el modelo de aislamiento**. Un error de FK, un script mal escrito o un `IgnoreQueryFilters()` mal acotado **teletransporta datos entre colegios** sin que el motor lo impida.

### 2.3 Tablas con `school_id` pero **nullable** en BD (muestra representativa desde `eduplaner`)

Entre otras: `activities`, `activity_types`, `area`, `attendance`, `audit_logs`, `discipline_reports`, `email_jobs`, `grade_levels`, `groups`, `messages`, `orientation_reports`, `security_settings`, `specialties`, `student_activity_scores`, `students`, `subjects`, `teacher_work_plans`, `trimester`, `users`.

**Dato verificado en local:** `COUNT(*) FILTER (WHERE school_id IS NULL)` sobre `student_activity_scores` = **11278 / 11278**.

### 2.4 Índices por `school_id`

Existen índices útiles en tablas núcleo (`users`, `students`, `groups`, `payments`, `trimester`+`school`, etc.). Las tablas **sin** `school_id` no pueden beneficiarse de partición lógica por tenant a nivel de índice; el coste crece con el número de colegios y el volumen de filas “puente”.

### 2.5 Filtros globales EF vs cobertura real

`SchoolDbContextTenantFilters.cs` aplica filtro a entidades como `User`, `Student`, `Group`, `Subject`, `Activity`, `Attendance`, `SubjectAssignment`, `Payment`, etc.

**Queda fuera del filtro global (ejemplos relevantes):** `StudentAssignment`, `TeacherAssignment`, `StudentActivityScore`, `ActivityAttachment`, `ScheduleEntry`, `StudentIdCard`, `StudentQrToken`, `ScanLog`, tablas `user_*`, `TeacherWorkPlanDetail`, etc. La defensa recae **100 % en código de servicio** — superficie enorme para fallos humanos.

### 2.6 Deriva modelo ↔ base (`area`)

En PostgreSQL, `area` tiene `school_id` (índices `IX_area_school_id`, `area_name_school_key`). El modelo C# expuesto en `Models/Area.cs` **no declara** `SchoolId`, mientras `SchoolDbContext` para `Area` **no mapea** esa columna en el fragmento revisado. Eso es **inconsistencia ORM / esquema**: datos tenant en BD **invisibles** para la capa de dominio actual.

---

## 3. Hallazgos críticos

Clasificación: **🔴 Crítico** (bloqueante o filtración directa), **🟠 Medio**, **🟢 Bajo**.

| ID | Severidad | Hallazgo |
|----|-----------|----------|
| C1 | 🔴 | **`HasQueryFilter` desactivado si `_tenantId` es null**: cualquier contexto sin claim `school_id` (p. ej. superadmin, workers, herramientas) opera sobre **todo el dataset** salvo que cada consulta sea blindada manualmente. |
| C2 | 🔴 | **`student_activity_scores` sin valor de tenant en producción de datos local:** 100 % `school_id` NULL. Las rutas de guardado masivo de notas (`SaveBulkFromNotasAsync`) **no invocan** `AuditHelper.SetSchoolIdAsync` al crear filas nuevas de `StudentActivityScore`, a diferencia de otros flujos (`SaveAsync`). |
| C3 | 🔴 | **Login por email global:** `UserService.GetByEmailAsync` filtra solo por email, sin `school_id`. Con filtro de tenant desactivado o con ambigüedad de usuarios, el **primer** usuario que coincida define el contexto de autenticación. En BD **no hay** restricción UNIQUE sobre `users.email` (solo PK sobre `id`). |
| C4 | 🔴 | **IDOR en libro de calificaciones:** `TeacherGradebookController` acepta `GetNotesDto` / cuerpos JSON con **`TeacherId`, `GroupId`, `StudentId` controlados por el cliente**. `GetNotasPorFiltroAsync` y flujos relacionados confían en esos GUID **sin amarrarlos** al docente autenticado ni a su `school_id`. |
| C5 | 🔴 | **`PaymentService.GetByPrematriculationAsync`:** filtra únicamente por `prematriculationId`; **no** restringe por escuela del caller. Si un `Guid` de otra institución se usa en un endpoint que llame a esto, hay **exfiltración lógica** de pagos asociados. |
| C6 | 🔴 | **`StudentService.GetByGroupAsync(string groupName)`:** filtra solo por `GroupName`, **sin** `SchoolId`. Depende enteramente del filtro global de `Student`; si el tenant es null o se usa `IgnoreQueryFilters`, es **fuga masiva**. (No hay referencias en controladores en el grep rápido, pero el método **permanece como API pública peligrosa**.) |
| C7 | 🔴 | **`StudentService.GetByGroupAndGradeAsync` / `GetBySubjectGroupAndGradeAsync`:** no aplican `SchoolId` explícito; dependen del filtro EF. **Colisión semántica:** dos colegios con grupo “1A” y mismo nombre de grado no colisionan por nombre (usan UUID), pero **cualquier bypass de filtro** expone estudiantes cruzados. |
| C8 | 🔴 | **`AttendanceService`:** métodos como `GetByStudentAsync`, `GetHistorialAsync`, `GetEstadisticasAsync` filtran por IDs de grupo/grado/estudiante **sin** `SchoolId` explícito; la contención es **por convención** y filtros globales parciales. |
| C9 | 🟠 | **`UserService.UpdateAsync`:** carga `Subjects` y `Groups` por lista de IDs **sin** verificar pertenencia al colegio del usuario editado más allá del filtro EF; con tenant null o manipulación de IDs, se pueden **asociar entidades cruzadas** al grafo del usuario. |
| C10 | 🟠 | **Uso extensivo de `IgnoreQueryFilters()`** en auth, superadmin, colas de email, carnet, etc. Cada uso es un **punto de revisión obligatoria**; un filtro `Where` mal puesto = **brecha**. |
| C11 | 🟠 | **`subject_assignments."SchoolId"`** (nombre de columna con comillas en PostgreSQL): riesgo operativo, reporting roto, migraciones frágiles; en local **0** NULL en 1142 filas, pero el diseño es **heterogéneo** respecto al resto del esquema. |
| C12 | 🟠 | **Cadena de conexión con credenciales en `SchoolDbContext.OnConfiguring`** (fallback local): anti-patrón para SaaS (secretos en binario, riesgo de fuga en repositorio / artefactos). |

---

## 4. Hallazgos estructurales

- **Duplicidad / solapamiento conceptual:** coexisten `students` (tabla dedicada) y usuarios con rol estudiante en `users`, más `student_assignments` sin `school_id`. La trayectoria “verdadera” del tenant a menudo es **una cadena de joins**, no una columna en cada fila.
- **Ausencia de UNIQUE(email) por tenant** en BD: el producto **no puede asumir** email como identidad estable por colegio sin reglas adicionales.
- **Profesores compartidos entre colegios:** un mismo `users.id` con un solo `school_id` **no modela** docente multi-institución; sería duplicación de identidad o cambio de modelo.
- **M2M (`user_subjects`, `user_groups`, `teacher_assignments`, …):** sin `school_id` directo; la consistencia tenant es **indirecta**. Un junior asume “el FK al grupo basta”; en SaaS el fallo es **asignar un grupo UUID de otro tenant** si alguna capa omite el filtro.
- **`Area`:** divergencia ORM/BD dificulta políticas de catálogo global vs por escuela.

---

## 5. Hallazgos de performance

- Filtros globales EF **no sustituyen** índices compuestos alineados con consultas reales (`school_id` + rango de fechas + `group_id`, etc.). Varias consultas de negocio filtran por **solo** `group_id` / `student_id`.
- Tablas de alto crecimiento: `student_activity_scores`, `attendance`, `scan_logs`, `audit_logs`, `email_*`, `activities`. Con muchos colegios, **full scans** son probables si predominan consultas **sin** predicado selectivo alineado con índices.
- **`GetDistinctGradeGroupCombinationsAsync`** en `SubjectAssignmentService` hace `Distinct()` sobre **toda** la tabla accesible en el contexto actual; con tenant null es **operación O(N) global**.

---

## 6. Riesgos de seguridad

- **Aislamiento basado en claims (`school_id`)** sin enforcement en BD (RLS, vistas, particiones): el modelo de amenaza incluye **bugs de aplicación**, **tokens manipulados** (si en el futuro hubiera JWT débil), **IDOR por GUID**, y **cuentas con privilegios** que ven todo el parque.
- **Endpoints anónimos** (carnet / QR / emergencia) son inevitables en producto, pero amplían superficie: dependen de **tokens criptográficos**, rate limits y lógica en `ScanAsync`; cualquier regresión ahí es **PII en público**.
- **PII agregada:** estudiantes, acudientes, docentes, pagos, salud (`Allergies`, contactos de emergencia en flujos de carnet). Sin `school_id` materializado en cada hecho (`scores`), el **borrado / exportación / olvido** por institución es más caro y propenso a error.

---

## 7. Evaluación de preparación para producción (SI / NO / PARCIAL)

**PARCIAL.**

- **Sí** para: despliegue operativo con **pocos** colegios, equipo técnico que controle roles, revisiones de código y monitoreo, y bajo riesgo regulatorio.
- **No** para: venta como **SaaS multi-tenant serio** (cientos/miles de clientes, SLAs estrictos, auditorías de terceros, separación legal de datos) **sin** endurecimiento de modelo, consultas y controles de acceso.

---

## 8. Conclusión brutalmente honesta

Eduplaner **no es un multi-tenant “incorrecto” en intención**: hay `school_id`, filtros EF, servicios que filtran por escuela y trabajo evidente en módulos sensibles (carnet, rate limit, fixes documentados en comentarios). Pero **la implementación es heterogénea y demasiado dependiente de la disciplina humana**. La base local muestra el síntoma más feo: **el sistema de calificaciones puede vivir sin tenant en la tabla de hechos**. Los endpoints de docente confían en **identificadores enviados por el cliente** para decisiones de confidencialidad. Eso no pasaría una auditoría de seguridad de un comprador enterprise ni la barra de un SaaS global serio.

**Inconsistencias que un junior no ve:** (1) filtros globales que **se apagan** con un claim ausente; (2) **omitir** `SetSchoolIdAsync` en un solo camino de escritura mientras otros lo usan; (3) **PostgreSQL `"SchoolId"`** vs `school_id`; (4) **`Area`** con columnas en BD no reflejadas en el modelo C#; (5) **métodos “helper”** (`GetByGroupAsync` por nombre) que violan el invariante tenant silenciosamente.

**Contraste intención vs realidad:** el código **dice** “defensa en profundidad” en comentarios del `DbContext`, pero la realidad es **defensa parcial**: entidades críticas sin filtro global, escrituras sin `school_id`, y consultas de agregación que **asumen** que el contexto de tenant siempre está bien configurado.

---

*Fin del informe — solo diagnóstico, sin recomendaciones de implementación.*

---

# Auditoría actualizada (29/04/2026) — Multitenancy Eduplaner (SchoolManager)

## 1. Resumen Ejecutivo (nivel CTO)

Eduplaner tiene un multi-tenancy **lógico** (no RLS) implementado con EF Core `HasQueryFilter` sobre un subconjunto de entidades y validaciones manuales en servicios/controladores. Eso reduce el riesgo en flujos “normales”, pero **no cierra el aislamiento matemáticamente** y no cumple el estándar de un SaaS multi-institución real.

El problema no es “intención”: es que el sistema permite y/o escribe estados donde el tenant queda indeterminado (`SchoolId = NULL`), y algunas validaciones/ownership dependen de datasets ya filtrados por QueryFilters, volviendo la verificación cross-tenant **ineficaz**.

**Veredicto**: no listo para producción SaaS multi-tenant con múltiples colegios simultáneos bajo auditoría seria.

## 2. Estado actual del multi-tenancy

### Tenant resolution (contrato real)

- `Infrastructure/TenantProvider.cs`: el tenant se deriva del claim `"school_id"`; `IsSuperAdmin` se deriva del rol `"superadmin"`.
- `Models/SchoolDbContext.cs`: `HasQueryFilter` para cada entidad filtra por `e.SchoolId == _tenantId`, salvo el caso especial `(_tenantId == null && _isSuperAdmin)`.

Implicación: si el claim `school_id` falta/no parsea, el sistema se reduce a filas con `SchoolId IS NULL` (para no-superadmin), no a “todas las escuelas”.

### Cobertura incompleta (puntos sin `SchoolId` directo)

Existen entidades críticas que **no tienen** columna `SchoolId` en el modelo:

- `Models/StudentAssignment.cs` (sin `SchoolId`)
- `Models/TeacherAssignment.cs` (sin `SchoolId`)
- `Models/ScheduleEntry.cs` (sin `SchoolId`)

Estas tablas sólo quedan “tenant-scoped” si cada endpoint hace scoping correcto en el punto de entrada (lo cual en un SaaS exige garantías consistentes y automatizadas).

### Semántica peligrosa de NULL

El modelo C# usa `Guid? SchoolId` en tablas sensibles:

- `Models/Attendance.cs` (`Guid? SchoolId`)
- `Models/StudentActivityScore.cs` (`Guid? SchoolId`)

Cuando se insertan filas sin poblar `SchoolId`, el aislamiento depende del estado del claim/tenant context y genera un “null tenant pool”.

**Lectura clave (modelo C# ↔ aislamiento):** el sistema trata `SchoolId = NULL` como un estado “especial”, pero el aislamiento real depende de QueryFilters por tenant. En particular:
- Tablas de hechos sensibles con `SchoolId` nullable: `Models/Attendance.cs`, `Models/StudentActivityScore.cs`.
- Tablas con `SchoolId` no-null (ejemplo): `Models/Payment.cs`.

Esta asimetría hace que cualquier flujo de escritura que olvide setear `SchoolId` cree un pool con comportamiento impredecible entre entornos (request, worker, anon).

## 3. Hallazgos críticos

### 🔴 1) `Attendance` bulk insert sin `SchoolId` (crea pool NULL)

- `Services/Implementations/AttendanceService.cs` → `SaveAttendancesAsync(...)` crea `Attendance` sin setear `SchoolId` ni llamar a `AuditHelper.SetSchoolIdAsync`.
- Con `Attendance.SchoolId` nullable, el insert genera `SchoolId = NULL`.

Impacto: corrupción de datos multi-tenant, degradación de trazabilidad y potencial exposición cuando el tenant context es `null` (anon/worker).

### 🔴 2) Validación de ownership en notas es ineficaz (QueryFilter ya filtra)

- `Services/Implementations/StudentActivityScoreService.cs` → `SaveBulkFromNotasAsync(...)` intenta detectar outsiders con:
  - `_context.Users.CountAsync(u => studentIds.Contains(u.Id) && u.SchoolId != currentUserSchool.Id)`
- Pero `_context.Users` tiene QueryFilter por tenant (`Models/SchoolDbContext.cs`).

Impacto: IDs de estudiantes cross-tenant pueden pasar la validación (porque los outsiders ni aparecen en el dataset filtrado) y el sistema puede escribir hechos (notas) con `SchoolId` del docente sobre IDs de estudiantes ajenos.

Escenario (2 colegios en paralelo): un docente de `School A` envía en el payload notas para `StudentId` perteneciente a `School B`. Si el backend no puede detectar el mismatch de pertenencia (porque el dataset de validación ya está filtrado por QueryFilter), el sistema termina creando/actualizando registros de notas con el tenant del docente (de `School A`).

### 🔴 3) Asignaciones cross-tenant: `StudentAssignmentController.GuardarAsignacion` no valida pertenencia

- `Controllers/StudentAssignmentController.cs` → `GuardarAsignacion(...)` delega a `StudentAssignmentService.AssignStudentAsync(request.UserId, ...)` con IDs enviados por el cliente.
- `Services/Implementations/StudentAssignmentService.cs` → `AssignStudentAsync(...)` no valida que `student.SchoolId` coincida con el tenant del caller.
- `StudentAssignment` no tiene `SchoolId`, por lo que QueryFilters no protegen esa tabla.

Impacto: un operador de colegio A puede asociar al grafo de asignaciones estudiantes de colegio B mediante payload.

### 🔴 4) Lecturas cross-tenant por `studentId` sin scoping en endpoint

- `Controllers/StudentAssignmentController.cs` → `GetGradeGroupByStudent(studentId)` no valida que `studentId` pertenezca al colegio del caller.

Impacto: enumeración inter-tenant (aunque el contenido sea “grado/grupo”, sigue siendo canal de información).

### 🔴 5) Endpoint anónimo depende de QueryFilters de tenant

- `Controllers/StudentIdCardController.cs` → `PublicEmergencyInfo` es `AllowAnonymous` y consulta `_context.Users` sin `IgnoreQueryFilters`.
- `Helpers/StudentRoleFilter.cs` sólo filtra por rol, no desactiva QueryFilters.

Impacto: funcionalidad inconsistente y riesgo de exposición si existen usuarios en pool `SchoolId = NULL`.

### 🔴 6) Sin RLS / “cinturón final” en BD

El aislamiento depende 100% de aplicación/EF. Un fallo de endpoint o un bypass futuro (`IgnoreQueryFilters()` mal usado) convierte el problema en “escala de dataset”.

## 4. Hallazgos estructurales

1. **Multi-tenancy por joins**: tablas sin `SchoolId` (StudentAssignment/TeacherAssignment/ScheduleEntry) elevan la carga de “cada endpoint bien escrito” a un nivel que no es aceptable como garantía de producto SaaS.
2. **Semántica de NULL inconsistente**: el código usa patrones tipo `x.SchoolId == null || x.SchoolId == schoolId`, pero QueryFilters excluyen NULL para tenants normales; esto rompe el contrato mental de “global por NULL”.
3. **Dependencia de claim vs DB**: el tenant context está ligado al claim `school_id`, mientras varios servicios además consultan `user.SchoolId` en BD. Esa dualidad puede desalinear QueryFilters, escrituras y validaciones.
4. **Ownership parcial**: hay defensas (por ejemplo, TeacherId vs docente autenticado en notas), pero no existe un mecanismo uniforme que amarre ownership para TODO el grafo crítico en todos los paths.

## 5. Hallazgos de performance

1. **Patrones de N+1**: `TeacherGradebookController.GetCounselorGroupAverages` hace loops y llama repetidamente a `GetNotasPorFiltroAsync` durante la construcción de promedios.
2. **Materialización/pivots en memoria**: libros de calificaciones materializan resultados y agrupan en C#.
3. **Crecimiento multi-tenant amplifica el costo**: si existen filas en pool NULL y/o joins sin scoping consistente, el “dataset efectivo” por request crece y agrava planes caros.

## 6. Riesgos de seguridad

1. **Riesgo de fuga/interferencia por pool NULL** (asistencias/notas con `SchoolId` nullable y escritura sin setear).
2. **Ownership verificado contra datasets ya filtrados** (validación de outsiders en notas).
3. **Cross-tenant write** en asignaciones (StudentAssignments) donde no hay `SchoolId` en el hecho.
4. **Anon endpoints** con QueryFilters activos (comportamiento depende del estado de datos en pool NULL).
5. **No hay enforcement en BD** (RLS ausente).

## 7. Evaluación de preparación para producción (SI / NO / PARCIAL)

**NO**.

## 8. Conclusión brutalmente honesta

Eduplaner no está listo para venderse como SaaS multi-tenant real. El sistema implementa “multi-tenancy lógico” con EF QueryFilters, pero:
- hay escrituras sin poblar el tenant en tablas sensibles,
- existen validaciones cross-tenant que no pueden detectar outsiders correctamente cuando dependen de QueryFilters,
- y hay endpoints que aceptan IDs sin ownership scoping consistente en tablas sin `SchoolId`.

En auditoría real, esto se traduce en que el aislamiento no es una propiedad del sistema, sino una propiedad del código “perfecto”. Eso no pasa.

---

*Fin del informe — solo diagnóstico, sin recomendaciones de implementación.*

