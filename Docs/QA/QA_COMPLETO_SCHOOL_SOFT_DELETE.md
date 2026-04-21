# QA COMPLETO – SCHOOL SOFT DELETE

**Fecha:** 2026-02-12  
**Alcance:** Análisis estático (sin modificación de código). Validación de QueryFilter, servicios, login, middleware, multi-tenant, integridad referencial y rendimiento.

---

## 1. QueryFilter

### 1.1 Presencia del filtro en School

| Verificación | Resultado |
|--------------|-----------|
| `modelBuilder.Entity<School>().HasQueryFilter(s => s.IsActive)` | **OK** – Presente en `SchoolDbContext.cs` (línea 654). |
| Configuración de `IsActive` | **OK** – `entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active")` (líneas 661-663). |

### 1.2 Otras entidades con QueryFilter que dependan de School

| Verificación | Resultado |
|--------------|-----------|
| Otras entidades con `HasQueryFilter` en el proyecto | **Ninguna** – Solo `School` tiene `HasQueryFilter` en el DbContext. |
| Entidades que dependen de School sin manejar null | **OK** – No hay otros QueryFilters encadenados a School. Las relaciones (User → SchoolNavigation, AuditLog → School, etc.) son navegación normal; al incluir School con filtro activo, la escuela inactiva se resuelve como `null` y el código que se revisó maneja null (p. ej. `SchoolNavigation?.Name ?? "Sin escuela asignada"` en ListAdmins, `a.School != null ? a.School.Name : "N/A"` en GetActivityLogsAsync). |

**Conclusión 1:** QueryFilter correcto y único; no hay dependencias de otras entidades con filtro sobre School sin manejo de null.

---

## 2. Servicios

### 2.1 SchoolService

| Verificación | Resultado | Ubicación |
|--------------|-----------|-----------|
| GetByIdAsync respeta filtro | **OK** – Usa `_context.Schools.FirstOrDefaultAsync(s => s.Id == id)` (sin IgnoreQueryFilters). Solo devuelve escuela activa. | Líneas 21-22 |
| GetAllAsync | **OK** – `_context.Schools.ToListAsync()`; aplica filtro, solo activas. | Línea 19 |
| DeleteAsync | **OK** – Soft delete: `IgnoreQueryFilters()`, luego `IsActive = false` y `Update` + `SaveChanges`. No hay `Remove(school)`. | Líneas 54-62 |
| Remove(school) / borrado físico | **No existe** – Confirmado. | — |

### 2.2 SuperAdminService

| Verificación | Resultado | Ubicación |
|--------------|-----------|-----------|
| DeleteSchoolAsync usa IgnoreQueryFilters() | **OK** – Carga escuela con `IgnoreQueryFilters()` y realiza solo `IsActive = false` + `Update` + `SaveChanges`. | Líneas 281-292 |
| Remove(school) / borrado físico | **No existe** – Eliminado en la implementación de soft delete. | — |
| GetAllSchoolsAsync | **OK** – `_context.Schools.IgnoreQueryFilters()` para listar todas (activas e inactivas). | Línea 35 |
| GetSchoolByIdAsync, GetSchoolForEditAsync, GetSchoolForEditWithAdminAsync, DiagnoseSchoolAsync | **OK** – Usan `IgnoreQueryFilters()` para poder cargar escuelas inactivas. | Líneas 78, 99, 311, 495 |
| UpdateSchool (FindAsync) | **OK** – `FindAsync(model.SchoolId)` ignora el filtro por diseño; permite a SuperAdmin actualizar una escuela inactiva. | Línea 223 |
| TotalEscuelas (GetSystemStatsAsync) | **OK** – `_context.Schools.CountAsync()` con filtro; cuenta solo escuelas activas. | Línea 954 |
| GetActivityLogsAsync Include(a => a.School) | **OK** – Maneja null: `SchoolName = a.School != null ? a.School.Name : "N/A"`. | Líneas 998, 1012 |

### 2.3 AuthService

| Verificación | Resultado | Ubicación |
|--------------|-----------|-----------|
| Comprobación de School inactiva en login | **OK** – Tras validar usuario activo, si `user.SchoolId.HasValue` carga escuela con `IgnoreQueryFilters()` y rechaza si `!school.IsActive` con mensaje "La institución se encuentra inactiva. Contacte al administrador." | Líneas 65-73 |
| SuperAdmin (sin SchoolId) | **OK** – No se entra al `if (user.SchoolId.HasValue)`, por lo que no se bloquea. | — |

### 2.4 SessionValidationMiddleware

| Verificación | Resultado | Ubicación |
|--------------|-----------|-----------|
| Comprobación de escuela inactiva en cada request | **OK** – Si el usuario tiene `SchoolId`, obtiene la escuela vía `GetCurrentUserSchoolAsync()` y si `school != null && !school.IsActive` cierra sesión y redirige a `/Auth/Login?schoolInactive=1`. | Líneas 44-52 |
| GetCurrentUserSchoolAsync (CurrentUserService) | **OK** – Usa `FindAsync(user.SchoolId.Value)`; FindAsync no aplica query filter, por lo que devuelve la escuela aunque esté inactiva y el middleware puede evaluar `IsActive`. | CurrentUserService.cs línea 54 |

**Conclusión 2:** Servicios y middleware alineados con soft delete: sin Remove(school), sin borrados físicos, uso correcto de IgnoreQueryFilters donde debe verse toda la escuela, y GetByIdAsync (SchoolService) respeta el filtro.

---

## 3. Login (simulación analítica)

| Escenario | Comportamiento esperado | Verificación en código |
|-----------|-------------------------|-------------------------|
| Usuario con School activa | Login OK; se firma la sesión. | AuthService solo rechaza si `school != null && !school.IsActive`. Si la escuela está activa, no se entra a ese rechazo. |
| Usuario con School inactiva | Login rechazado; mensaje "La institución se encuentra inactiva. Contacte al administrador." | AuthService carga escuela con IgnoreQueryFilters, comprueba `!school.IsActive` y devuelve `(false, mensaje, null)`. |
| Usuario sin SchoolId (p. ej. SuperAdmin) | Login OK. | No se ejecuta el bloque `if (user.SchoolId.HasValue)`. |

**Conclusión 3:** Lógica de login coherente con la política de institución inactiva (rechazo solo cuando hay SchoolId y la escuela está inactiva).

---

## 4. Middleware (simulación analítica)

| Escenario | Comportamiento esperado | Verificación en código |
|-----------|-------------------------|-------------------------|
| Usuario logueado, School activa | Request continúa con `_next(context)`. | Solo se hace sign out + redirect si `school != null && !school.IsActive`. |
| Usuario logueado, School desactivada manualmente en BD | En la siguiente request: cierre de sesión y redirección a Login con `?schoolInactive=1`. | GetCurrentUserSchoolAsync devuelve la escuela (FindAsync ignora filtro). Si `!school.IsActive`, se ejecuta SignOutAsync y Redirect con schoolInactive=1. |
| Ruta /Auth/* | No se valida escuela; se deja pasar. | Primera condición del middleware hace `return` sin comprobar usuario/escuela. |

**Conclusión 4:** El middleware garantiza que, tras desactivar la escuela, la siguiente petición autenticada cierre sesión y redirija a login con el parámetro de institución inactiva.

---

## 5. Multi-tenant

| Verificación | Resultado | Detalle |
|--------------|-----------|---------|
| Listados normales no muestran Schools inactivas | **OK** | SchoolService.GetAllAsync y cualquier consulta a `_context.Schools` sin IgnoreQueryFilters aplican el filtro; solo se listan activas. |
| SuperAdmin usa IgnoreQueryFilters correctamente | **OK** | GetAllSchoolsAsync, GetSchoolByIdAsync, GetSchoolForEditAsync, GetSchoolForEditWithAdminAsync, DiagnoseSchoolAsync y DeleteSchoolAsync usan IgnoreQueryFilters donde debe listar/editar/desactivar cualquier escuela. |
| Include(u => u.SchoolNavigation) / Include(a => a.School) | **OK** | Con query filter en School, al incluir School desde User o AuditLog, una escuela inactiva se resuelve como null. ListAdmins usa `SchoolNavigation?.Name ?? "Sin escuela asignada"`. GetActivityLogsAsync usa `a.School != null ? a.School.Name : "N/A"`. No se rompe el Include; se maneja null. |
| Program.cs (arranque): schools para EnsureDefault* | **OK** | `db.Schools.Select(s => s.Id).ToListAsync()` aplica filtro; solo escuelas activas reciben año académico por defecto y bloques horarios por defecto. Inactivas no se procesan (comportamiento esperado). |

**Conclusión 5:** Multi-tenant correcto: listados normales solo ven activas, SuperAdmin ve todas donde debe, e Include a School con filtro se maneja con null en las vistas/repositorios revisados.

---

## 6. Integridad referencial

| Verificación | Resultado | Detalle |
|--------------|-----------|---------|
| ON DELETE CASCADE que elimine schedule_entries | **No existe** | schedule_entries tiene FKs con DeleteBehavior.Restrict (academic_year, teacher_assignment, time_slot). No hay CASCADE desde otra tabla que borre schedule_entries. |
| time_slots.school_id | **CASCADE** | Si se borrara School (no ocurre con soft delete), time_slots se borrarían en cascada. Con soft delete no se ejecuta DELETE sobre School, por tanto no se dispara CASCADE. |
| Huérfanos | **No se generan** | No hay eliminación física de School; no se eliminan users, time_slots ni schedule_entries por esta funcionalidad. Los datos siguen referenciando a la misma School (con IsActive = false). |

**Conclusión 6:** No se introducen cascadas que borren schedule_entries; no hay borrado físico de School ni generación de huérfanos por la política de soft delete.

---

## 7. Performance

| Verificación | Resultado | Detalle |
|--------------|-----------|---------|
| QueryFilter y subconsultas | **Aceptable** | HasQueryFilter(s => s.IsActive) se traduce en un predicado `WHERE is_active = true` (o equivalente) en las consultas que tocan School. No implica por sí mismo subconsultas adicionales; es un predicado más en el árbol de consulta. |
| Consultas que filtran por School | **Normal** | Por ejemplo, `_context.Schools.ToListAsync()` → `SELECT * FROM schools WHERE is_active = true`. Include desde otras entidades añade el predicado al join/subconsulta de School. Comportamiento estándar de EF Core con query filters. |
| IgnoreQueryFilters en SuperAdmin | **Correcto** | Se usa solo donde el rol SuperAdmin debe ver o actuar sobre todas las escuelas; no afecta a las consultas tenant normales. |

**Conclusión 7:** El query filter no introduce subconsultas innecesarias; es un predicado estándar. Uso de IgnoreQueryFilters acotado y adecuado.

---

## 8. Hallazgos adicionales (no bloqueantes)

| # | Hallazgo | Severidad | Nota |
|---|----------|-----------|------|
| 1 | AprobadosReprobadosService carga School con `FindAsync(schoolId)` (ignora filtro). | Baja | Si la escuela está inactiva, el reporte podría ejecutarse igual porque FindAsync devuelve la escuela. Opcional: añadir comprobación de IsActive y denegar o filtrar si se desea ocultar reportes de escuelas inactivas. |
| 2 | TotalEscuelas en GetSystemStatsAsync cuenta solo escuelas activas. | Información | Coherente con “solo activas en listados”; si se quisiera “total de escuelas (activas + inactivas)” para el dashboard SuperAdmin, habría que usar IgnoreQueryFilters().CountAsync(). No es error. |
| 3 | UpdateSchool (SuperAdmin) usa FindAsync; permite editar escuela inactiva. | Información | Deseable para poder corregir datos o eventualmente “reactivar” en el futuro; no es un defecto. |

---

## 9. Riesgos

| Riesgo | Nivel | Mitigación actual |
|--------|--------|--------------------|
| Vistas que usen Admin.SchoolNavigation sin null check | Bajo | ListAdmins ya usa `SchoolNavigation?.Name ?? "Sin escuela asignada"`. Otras vistas que consuman GetAllAdminsAsync / GetUserByIdAsync y muestren la escuela deben seguir manejando null. |
| Reporte AprobadosReprobados para escuela inactiva | Bajo | FindAsync devuelve la escuela; el reporte se puede generar. Si en negocio se decide no permitir reportes de escuelas inactivas, añadir comprobación explícita de IsActive. |
| EF Core warnings “required end of relationship” con query filter | Conocido | Documentado en implementación; no afecta al soft delete ni a la integridad. Opcional: definir filtros coherentes en entidades relacionadas o marcar navegación como opcional donde corresponda. |

---

## 10. Confirmación final

| Criterio | Estado |
|----------|--------|
| QueryFilter presente y único en School; sin dependencias problemáticas | OK |
| GetByIdAsync (SchoolService) respeta filtro | OK |
| DeleteSchoolAsync / DeleteAsync usan solo soft delete con IgnoreQueryFilters donde corresponde | OK |
| No existe Remove(school) ni borrado físico de School | OK |
| Login rechaza usuario de escuela inactiva con mensaje adecuado | OK |
| Middleware cierra sesión y redirige cuando la escuela está inactiva | OK |
| Listados normales no muestran inactivas; SuperAdmin usa IgnoreQueryFilters correctamente | OK |
| Include(School) / Include(SchoolNavigation) con null manejado en los usos revisados | OK |
| No hay CASCADE que borre schedule_entries; no se generan huérfanos | OK |
| Query filter no introduce subconsultas innecesarias | OK |

---

## School Soft Delete Enterprise Safe: **YES**

La implementación cumple la política de soft delete: no hay borrado físico de School, no hay cascadas que eliminen schedule_entries, el filtro global está bien acotado, los servicios y el middleware protegen el acceso cuando la institución está inactiva, y los listados multi-tenant y el uso de Include con School son coherentes con el diseño. Los hallazgos son menores o informativos y no invalidan el uso en entorno enterprise.

---

*QA realizado por análisis estático de código. No se modificó código. No se ejecutaron pruebas automatizadas en runtime.*
