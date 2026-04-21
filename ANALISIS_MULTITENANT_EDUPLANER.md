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
