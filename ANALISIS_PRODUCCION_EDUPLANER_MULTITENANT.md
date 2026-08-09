# Análisis de preparación para producción — EduPlaner multi-tenant

**Alcance:** ASP.NET Core MVC, PostgreSQL, aislamiento por escuela (`SchoolId`), seguridad web y arquitectura.  
**Metodología:** Revisión estática del código en el repositorio + consultas en vivo a PostgreSQL (`localhost`, base `eduplaner`, cadena `ConnectionStrings:DefaultConnection` de `appsettings.json`, cliente `C:\Program Files\PostgreSQL\18\bin\psql.exe`).  
**Fecha de referencia:** 2026-05-03.  
**Nota:** Este documento es un dictamen técnico; no sustituye pentest, revisión legal ni pruebas de carga con perfil de tráfico real.

---

## 1. Resumen ejecutivo

| Pregunta | Respuesta |
|----------|-----------|
| ¿Listo para producción enterprise SaaS? | **NO** (con matices: **PARCIAL** si se aceptan riesgos residualizados y compensaciones operativas explícitas). |
| Nivel de riesgo global | **ALTO** para datos sensibles multi-tenant hasta corregir huecos de modelo, consistencia de autorización y superficies públicas. |

**Motivos principales en una frase:** el aislamiento depende en gran medida de filtros globales EF y de disciplina en servicios; varias entidades operativas **no** tienen filtro global de tenant; la base de datos **no** refuerza por FK el `school_id` en tablas críticas como `discipline_reports`; hay endpoints anónimos y usos de `IgnoreQueryFilters()` que exigen revisión continua; la configuración por defecto expone secretos y políticas débiles típicas de entornos no endurecidos.

---

## 2. Hallazgos críticos

1. **`discipline_reports.school_id` sin FK a `schools`**  
   En PostgreSQL, `school_id` es `NOT NULL` e indexado, pero **no existe** restricción `FOREIGN KEY (school_id) REFERENCES schools(id)`. Solo hay FK hacia `users`, `groups`, `subjects`, `grade_levels`. Consecuencia: integridad referencial del tenant **no está garantizada por el motor**; datos huérfanos o `school_id` inválido pueden insertarse vía SQL, bug o migración.

2. **Filtro global de tenant + usuario anónimo = conjunto vacío sobre `User`**  
   El predicado en `SchoolDbContextTenantFilters` para `User` es: `(_tenantId == null && _isSuperAdmin) || e.SchoolId == _tenantId`.  
   Para petición **no autenticada**: `_tenantId` y `_isSuperAdmin` son falsos → queda **`e.SchoolId == null`**. En la base analizada, **todos** los estudiantes (`role` estudiante/student) tienen `school_id NOT NULL` (3206 con escuela, 0 sin).  
   Cualquier consulta anónima a `Users` que dependa del GQF (p. ej. flujos que usan `StudentRoleFilter` **sin** `IgnoreQueryFilters`) **no devolverá filas** para estudiantes reales, o quedará en estado funcional incoherente según el flujo. Esto es un **defecto de modelo de amenaza vs. implementación** (no se puede confiar en el GQF para endpoints públicos sin bypass explícito y auditado).

3. **SuperAdmin + GQF en `User`**  
   Si `_tenantId == null && _isSuperAdmin` → la primera rama es **verdadera** y el filtro de `User` **deja de acotar por escuela** (véase el propio comentario en código sobre “bypass solo para superadmin”). Cualquier endpoint autenticado como superadmin que combine IDs externos y `IgnoreQueryFilters` mal acotado amplifica **IDOR** y exfiltración; el riesgo es de **diseño** (confianza en rol + revisión humana permanente).

4. **Secretos y material criptográfico en configuración versionada / por defecto**  
   `appsettings.json` contiene contraseña de BD, claves de ejemplo para `QrSecurity` y `ApiToken`, y `AllowedHosts: "*"`. En producción real esto es **inaceptable** sin variables de entorno, rotación y política de secretos (aunque el usuario pueda sobrescribir en host, el **riesgo de fuga por repo o backup** permanece).

5. **Ausencia de política global de autorización**  
   No hay `FallbackPolicy = RequireAuthenticatedUser` a nivel aplicación; cada controlador depende de `[Authorize]`. **AuthController** es correctamente anónimo para login; el riesgo es **olvidar** el atributo en un controlador nuevo → superficie **pública accidental** (clase de defecto recurrente en MVC grande).

---

## 3. Hallazgos importantes

1. **Entidades con datos de negocio sin Global Query Filter (GQF) de `SchoolId`**  
   Entre las tablas relevantes **sin** columna `school_id` o sin GQF en `SchoolDbContextTenantFilters`: `student_assignments`, `teacher_assignments`, `schedule_entries`, `activity_attachments`, `student_id_cards`, `student_qr_tokens`, `scan_logs`, `prematriculation_histories`, `email_queues`, `teacher_work_plan_details`, `teacher_work_plan_review_logs`, `email_api_configurations`, tablas puente `user_grades`, `user_groups`, `user_subjects`, etc.  
   El aislamiento depende de **cadenas de FK + consultas correctas**. Eso es habitual en diseños EF, pero **no equivale a RLS en PostgreSQL** ni a garantía ante consultas ad hoc o regresiones.

2. **`IdCardTemplateField` sin GQF explícito**  
   Los campos de plantilla se filtran indirectamente si siempre se accede vía `SchoolId`; cualquier `Set<IdCardTemplateField>()` sin `Where(x => x.SchoolId == …)` es vector de **fuga lógica**.

3. **Inconsistencia de roles en `[Authorize(Roles = "...")]`**  
   Mezcla de mayúsculas/minúsculas y variantes en español/inglés (`admin` vs `Admin`, `Director` vs `director`, `Teacher` vs `teacher`). ASP.NET Core hace comparación **case-sensitive** por defecto en roles. Resultado: **configuración frágil** (usuario creado con rol distinto al string del atributo → 403 o, peor, rutas duplicadas con distintos strings para “arreglarlo”).

4. **`IgnoreQueryFilters()` extendido**  
   Aparece en auth, superadmin, colas de email, carnet, gestión de contraseñas, etc. Cada uso es un **punto caliente**: un `Where` mal copiado puede exponer datos de otra escuela. No hay mecanismo automático (analizador, test) que lo impida.

5. **Índices en columnas FK**  
   La mayoría de FK críticas están cubiertas. Excepciones detectadas por cruce FK ↔ `pg_indexes` (columna FK sin índice dedicado obvio): p. ej. `prematriculations.grade_id`, `prematriculations.group_id`, `prematriculations.parent_id` (marcados `f` en el script de verificación), `subject_assignments.specialty_id`, `subjects."AreaId"`, varios `created_by`/`updated_by` (menos críticos para join masivo). Impacto: **JOIN/DELETE/UPDATE** y validaciones por integridad pueden degradar con volumen.

6. **Nulabilidad débil en columnas tenant-adjacentes**  
   Ejemplos: `activities.school_id` **YES** en catálogo de columnas; `attendance.school_id` **YES**; `orientation_reports.school_id` **YES**; `subject_assignments.school_id` **YES**. El GQF y la lógica de aplicación asumen valores coherentes; la BD **permite** filas ambiguas si algo bypasea validaciones.

7. **`teacher_work_plans.school_id` nullable**  
   Coherencia multi-tenant y reportes por escuela dependen de que siempre se rellene en aplicación; la BD no lo exige.

8. **Mezcla de convenciones de nombres**  
   Columnas EF/Pascal en algunas tablas (`ActivityTypeId`, `AreaId`, `UpdatedAt` en `users`) frente a `snake_case` predominante → fricción en SQL manual, migraciones y auditoría.

---

## 4. Hallazgos menores

- `AllowedHosts: "*"` — ampliar superficie de **Host header** attacks si no hay reverse proxy estricto.  
- `DisciplineReport/Details.cshtml` y otras vistas usan `@Html.Raw` con datos persistidos → riesgo **XSS almacenado** si el contenido no está sanitizado al guardar.  
- `CounselorAssignmentService`: patrón repetido de múltiples `.Include` en listados → posible **sobrecarga de grafo** y memoria en escuelas grandes.  
- Tabla `unification_audit_log` y similares: metadatos de migración de datos; no son tenant-scoped por diseño — correcto si solo las usa operaciones internas.

---

## 5. Problemas de multi-tenant

### 5.1 Riesgos de fuga de datos (lógica / aplicación)

- **Dependencia del GQF** con regla especial para superadmin y “fail-closed” cuando falta `school_id` en claims (usuarios no superadmin sin claim → `SchoolId == null` → vacío). Bien intencionado, pero **rompe** o complica endpoints públicos y background mal diseñados.  
- **Entidades sin GQF** (listado en §3): cualquier consulta que filtre solo por `Id` o por entidad hija puede **cruzar escuela** si existen datos inconsistentes (aunque en el snapshot local `student_assignments` vs `groups`/`users` no mostró cruces en conteos de verificación).  
- **`FileController`** autenticado: `GetSchoolLogo` / avatares por parámetro URL — riesgo de **IDOR** si las URLs son enumerables y el backend no valida pertenencia al tenant del usuario (mitigación típica: token firmado o comprobación de `SchoolId` del recurso vs claim).

### 5.2 Tablas sin aislamiento directo por `school_id`

Tablas `public` **sin** columna `school_id` / `SchoolId` (extraído de `information_schema`):  
`__EFMigrationsHistory`, `activity_attachments`, `data_protection_keys`, `email_api_configurations`, `email_queues`, `prematriculation_histories`, `scan_logs`, `schedule_entries`, `schools`, `student_assignments`, `student_id_cards`, `student_qr_tokens`, `teacher_assignments`, `teacher_work_plan_details`, `teacher_work_plan_review_logs`, `unification_audit_log`, `user_grades`, `user_groups`, `user_subjects`.  

Interpretación: muchas son **dependientes** de entidades con tenant; la exigencia de producción es **documentar el grafo de confianza** y, donde el negocio lo exija, añadir `school_id` redundante + constraint/trigger o **Row Level Security (RLS)** en PostgreSQL.

### 5.3 Integridad referencial cruzada

- Verificación de muestra en BD local: **0** filas en `student_assignments` con `users.school_id ≠ groups.school_id`; **0** en `discipline_reports` con desajuste `school_id` vs estudiante o grupo; **0** `discipline_reports` huérfanos de `schools`.  
  Eso **no** elimina el riesgo futuro sin FK en `discipline_reports.school_id`.

---

## 6. Problemas de performance

- **Includes profundos** en servicios (p. ej. asignaciones de orientación con muchas navegaciones) → riesgo de consultas anchas y transferencia de datos innecesaria.  
- **Índices:** en general la BD está razonablemente indexada para patrones multi-tenant (`school_id`, compuestos en informes disciplinarios, etc.). Los huecos en FK secundarias (§3.5) pueden aparecer con crecimiento.  
- **Sin evidencia en este informe** de pruebas de carga, `EXPLAIN ANALYZE` en endpoints calientes ni métricas APM — gap típico pre-producción.

---

## 7. Problemas de seguridad

| Área | Evaluación breve |
|------|------------------|
| Autenticación | Cookies configuradas; rate limiting para login y API pública de escaneo — **bien**. |
| Autorización | Roles inconsistentes entre atributos y posibles valores en BD — **riesgo operativo y de 403/elevación mal diagnosticada**. |
| Tenant | GQF + servicios; sin RLS en PostgreSQL — **no defensa en profundidad a nivel BD**. |
| Datos sensibles en reposo | Contraseñas con BCrypt en flujos revisados; SMTP y secretos en tablas/config — revisar cifrado en tránsito y rotación. |
| XSS | Uso de `Html.Raw` con JSON o contenido de informes — **riesgo si el origen no es confiable**. |
| Configuración | Secretos en `appsettings.json` y `OnConfiguring` con cadena por defecto en `SchoolDbContext` — **riesgo de filtración y despliegue erróneo**. |
| Superficie anónima | `PublicEmergencyInfo` con token firmado + rate limit — dirección correcta; interacción con GQF sobre `User` debe ser **explícitamente modelada** (ver §2). |

---

## 8. Recomendaciones prioritarias

1. Añadir **`FOREIGN KEY (school_id) REFERENCES schools(id)`** en `discipline_reports` (y revisar otras tablas con `school_id` sin FK).  
2. Rediseñar endpoints **anónimos** que consultan `User`: usar **`IgnoreQueryFilters()` + filtros explícitos** (p. ej. por `studentId` resuelto desde token HMAC y comprobación de rol estudiante) **documentados** y cubiertos por tests.  
3. Introducir **política de autorización por defecto** requiriendo autenticación salvo convención explícita (`[AllowAnonymous]`).  
4. Normalizar **roles** (una sola convención de strings, seed/migración de datos, tests de autorización).  
5. Inventariar entidades **sin GQF** y, para las de alto riesgo, o bien **GQF + `school_id` redundante** o **RLS** en PostgreSQL por `current_setting('app.school_id')`.  
6. Eliminar secretos del repo; usar **User Secrets / variables de entorno**; rotar claves expuestas en historial.  
7. Pasar **pentest** ligero (IDOR entre escuelas, XSS en informes, subida de archivos) y **prueba de carga** en módulos de asistencia, horarios y listados masivos.

---

## 9. Veredicto final

**Arquitectura (Fase 5, integrada):** el proyecto es **ASP.NET Core MVC clásico** (`Controllers`, `Services/Implementations`, repositorios parciales, `Models` como persistencia EF). **No** cumple Clean Architecture estricta (dominio acoplado a EF, sin capa de aplicación como única frontera). La escalabilidad es la de un **monolito modular** razonable; `DataProtectionKeyDbContext` y scripts de arranque en `Program.cs` ayudan en contenedores, pero la mezcla de migraciones EF con SQL idempotente en startup exige **gobernanza** para no divergir entornos.

**No se recomienda declarar el sistema “listo para producción enterprise” en el estado auditado** sin abordar al menos los hallazgos críticos (integridad de `school_id`, modelo de autorización y tenant en endpoints públicos, secretos fuera del código) y sin un plan formal de hardening (RLS o equivalente, pruebas de seguridad y performance).

Con las mitigaciones prioritarias aplicadas, gobierno de datos y operación disciplinada (revisiones de `IgnoreQueryFilters`, roles unificados, monitoreo), el producto puede evolucionar a un estado **PARCIALmente aceptable** para producción controlada (pocas escuelas, SLA claro, equipo que entiende los riesgos residuales).

---

## Anexo A — Inventario de tablas (`public`)

`__EFMigrationsHistory`, `academic_years`, `activities`, `activity_attachments`, `activity_types`, `area`, `attendance`, `audit_logs`, `counselor_assignments`, `data_protection_keys`, `discipline_reports`, `email_api_configurations`, `email_configurations`, `email_jobs`, `email_queues`, `grade_levels`, `groups`, `id_card_template_fields`, `messages`, `orientation_reports`, `payment_concepts`, `payments`, `prematriculation_histories`, `prematriculation_periods`, `prematriculations`, `scan_logs`, `schedule_entries`, `school_id_card_settings`, `school_schedule_configurations`, `schools`, `security_settings`, `shifts`, `specialties`, `student_activity_scores`, `student_assignments`, `student_id_cards`, `student_payment_access`, `student_qr_tokens`, `students`, `subject_assignments`, `subjects`, `teacher_assignments`, `teacher_work_plan_details`, `teacher_work_plan_review_logs`, `teacher_work_plans`, `time_slots`, `trimester`, `unification_audit_log`, `user_grades`, `user_groups`, `user_subjects`, `users`.

---

## Anexo B — Controladores revisados (lista)

Los 51 archivos bajo `Controllers/**/*.cs` incluyen `AuthController` (sin `[Authorize]` a nivel clase, esperado), el resto con atributos de autorización en su mayoría; la ausencia de política global mantiene el riesgo de omisión futura.

---

*Fin del informe.*
