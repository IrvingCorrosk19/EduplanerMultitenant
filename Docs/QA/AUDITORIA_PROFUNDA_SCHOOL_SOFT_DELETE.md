# Auditoría técnica profunda – School Soft Delete

**Fecha:** 2026-02-12  
**Alcance:** Análisis estático completo. Sin modificación de código. Sin refactorizaciones automáticas.  
**Enfoque:** QueryFilter, servicios, login/sesión, multi-tenant, integridad referencial, rendimiento y errores ocultos.

---

## 1. Resumen ejecutivo

La implementación de Soft Delete en School cumple el objetivo de no borrar físicamente la entidad y de bloquear acceso cuando la institución está inactiva. El QueryFilter está bien aplicado, los puntos de borrado físico han sido eliminados y el flujo de login/middleware es coherente. Se identifican **riesgos medios** por uso de `Include(School)` / `Include(SchoolNavigation)` en servicios que proyectan `School.Name` sin comprobar null cuando la escuela está inactiva (CounselorAssignmentService, EmailConfigurationService.GetAllAsync). El flujo tenant actual (por SchoolId del usuario activo) no dispara esos casos hoy, pero son **bugs latentes** si se exponen listados globales o se llama a GetAllAsync. No hay CASCADE que borre schedule_entries ni inconsistencia de integridad por el soft delete. El filtro no introduce subconsultas pesadas; un índice en `is_active` es opcional. **Veredicto:** implementación enterprise-safe con reservas documentadas y recomendaciones priorizadas para eliminar riesgos latentes.

---

## 2. Hallazgos críticos

### Ninguno que impida el uso en producción en el flujo actual

- No se encontraron `Remove(school)` ni borrados físicos ocultos.
- No hay rutas que permitan a un usuario operar bajo una School inactiva una vez aplicados login y middleware.
- La migración solo añade `is_active`; no modifica FKs ni cascadas.

**Nota:** Los hallazgos medios siguientes pueden convertirse en críticos si se habilita un listado “global” (p. ej. SuperAdmin) que use los servicios afectados sin filtrar por escuela activa.

---

## 3. Hallazgos medios

### 3.1 CounselorAssignmentService – NullReference latente con Include(School)

**Ubicación:** `CounselorAssignmentService.cs` – múltiples métodos que hacen `Include(ca => ca.School)` y proyectan `SchoolName = ca.School.Name`.

**Comportamiento:** Con el QueryFilter en School, al cargar `CounselorAssignment` con `Include(ca => ca.School)`, las filas cuya escuela está inactiva tendrán `ca.School == null`. En la proyección `SchoolName = ca.School.Name` se produciría **NullReferenceException** (o equivalente en la traducción a SQL según versión de EF).

**Métodos afectados:** `GetAllAsync()`, `GetByIdAsync()`, `GetBySchoolIdAsync()` (y otros que proyectan `ca.School.Name`), y el método de estadísticas que usa `a.School.Name` (aprox. línea 683).

**Mitigación actual:** El controlador usa solo `GetBySchoolIdAsync(currentUser.SchoolId.Value)`. Un usuario logueado tiene escuela activa (o no tendría sesión), por lo que en la práctica el flujo tenant no carga asignaciones de escuelas inactivas y no se dispara el fallo.

**Riesgo:** Si en el futuro se llama a `GetAllAsync()` (p. ej. un listado SuperAdmin de todas las asignaciones) o cualquier consulta que devuelva asignaciones de escuelas inactivas, la excepción aparecerá. **Recomendación:** En proyecciones, usar `SchoolName = ca.School != null ? ca.School.Name : "N/A"` (o equivalente) para ser resiliente al filtro.

---

### 3.2 EmailConfigurationService.GetAllAsync() – OrderBy(School.Name) con School filtrado

**Ubicación:** `EmailConfigurationService.cs` – `GetAllAsync()` hace `Include(ec => ec.School).OrderBy(ec => ec.School.Name)`.

**Comportamiento:** Para configuraciones cuya escuela está inactiva, `ec.School` queda null por el QueryFilter. Dependiendo de cómo EF Core traduzca `OrderBy(ec => ec.School.Name)` (acceso a propiedad de navegación posiblemente null), puede generarse excepción en tiempo de ejecución o un orden donde las filas con School null se traten de forma especial (p. ej. NULL en SQL).

**Mitigación actual:** El controlador usa `GetBySchoolIdAsync(currentUser.SchoolId.Value)`, no `GetAllAsync()`. Solo se cargan configuraciones de la escuela del usuario (activa).

**Riesgo:** Si se expone un listado global de configuraciones de email (p. ej. SuperAdmin) usando `GetAllAsync()`, el fallo o el orden inesperado pueden aparecer. **Recomendación:** Si se mantiene GetAllAsync, ordenar por un campo que no dependa de la navegación (p. ej. `ec.SchoolId`) o usar null-conditional / coalesce en la proyección y evitar OrderBy sobre navegación filtrada.

---

### 3.3 GetSystemStatsAsync (SuperAdmin) – EscuelasStats solo activas

**Ubicación:** `SuperAdminService.cs` – `stats.EscuelasStats = await _context.Schools.Select(s => new EscuelaStatsDto { ... }).ToListAsync()` **sin** `IgnoreQueryFilters()`.

**Comportamiento:** Las estadísticas por escuela y el conteo de escuelas solo incluyen escuelas activas. Las inactivas no aparecen en el dashboard.

**Valoración:** No es un bug funcional; es coherente con “solo activas en listados”. Si el producto requiere que SuperAdmin vea también estadísticas de escuelas inactivas (p. ej. total histórico), habría que usar `_context.Schools.IgnoreQueryFilters()` en este bloque. Se documenta como hallazgo de diseño, no como defecto.

---

## 4. Hallazgos bajos

### 4.1 AprobadosReprobadosService – FindAsync(School)

**Ubicación:** `AprobadosReprobadosService.cs` – `var school = await _context.Schools.FindAsync(schoolId)`.

**Comportamiento:** `FindAsync` no aplica el QueryFilter, por lo que se puede cargar una escuela inactiva y generar el reporte.

**Riesgo:** Un usuario que aún tenga una pestaña/URL con un reporte de una escuela que luego fue desactivada podría seguir viendo el reporte si se invoca de nuevo. Bajo impacto; opcional validar `school.IsActive` y denegar o marcar “institución inactiva” si se desea ocultar reportes de inactivas.

---

### 4.2 CurrentUserService.GetCurrentUserSchoolAsync – FindAsync

**Ubicación:** `CurrentUserService.cs` – `return await _context.Schools.FindAsync(user.SchoolId.Value)`.

**Comportamiento:** Correcto y deseado. FindAsync ignora el filtro y permite al middleware y a otras capas comprobar `school.IsActive` aunque la escuela esté inactiva. Sin este comportamiento no se podría cerrar sesión cuando la escuela se desactiva después del login.

---

### 4.3 SuperAdminService.UpdateSchool – FindAsync(model.SchoolId)

**Ubicación:** `SuperAdminService.cs` – actualización de escuela por ID.

**Comportamiento:** Permite editar una escuela inactiva (p. ej. datos de contacto o futura reactivación). Intencional; no es un defecto.

---

### 4.4 Índice en schools.is_active

**Estado:** La migración solo añade la columna `is_active`; no se crea índice en `schools(is_active)`.

**Impacto:** Las consultas que filtran por `IsActive` (p. ej. el propio QueryFilter) pueden recorrer la tabla. Dado que `schools` suele tener pocas filas, el impacto es bajo. Para muchas escuelas (cientos/miles), un índice en `is_active` podría ayudar. **Recomendación:** Opcional; prioridad baja salvo que el volumen de escuelas crezca.

---

## 5. Riesgos futuros

| Riesgo | Condición | Mitigación sugerida |
|--------|-----------|----------------------|
| NullReference en proyecciones con School | Cualquier nuevo listado “global” (SuperAdmin u otro) que use CounselorAssignmentService.GetAllAsync, o similar con Include(School) y .School.Name sin null check. | Usar siempre `SchoolName = ca.School != null ? ca.School.Name : "N/A"` (o patrón equivalente) en DTOs que incluyan School. |
| OrderBy sobre navegación filtrada | Uso de GetAllAsync de EmailConfiguration u otros servicios que ordenen por School.Name. | Evitar OrderBy sobre navegación con QueryFilter; ordenar por FK o por campo del propio agregado. |
| Reactivación de School | Hoy no existe flujo de “reactivar” (IsActive = true). | Si se implementa, asegurar que solo SuperAdmin (o rol equivalente) pueda reactivar y que el flujo sea explícito y auditado. |
| Reportes y exports con escuela inactiva | Servicios que cargan School con FindAsync y no comprueban IsActive (p. ej. AprobadosReprobadosService). | Opcional: en reportes por schoolId, comprobar school.IsActive y denegar o marcar cuando la institución esté inactiva. |

---

## 6. Recomendaciones priorizadas

1. **Alta (evitar bugs latentes):** En **CounselorAssignmentService**, en todas las proyecciones que usen `ca.School.Name`, sustituir por `SchoolName = ca.School != null ? ca.School.Name : "N/A"` (o similar) para que el código sea seguro aunque se llame a GetAllAsync o se incluyan asignaciones de escuelas inactivas.
2. **Alta:** En **EmailConfigurationService.GetAllAsync()**, no ordenar por `ec.School.Name` cuando School tiene QueryFilter; ordenar por `ec.SchoolId`, por `ec.CreatedAt` o por otro campo que no dependa de la navegación filtrada; o usar IgnoreQueryFilters solo para esa consulta si se desea ordenar por nombre de escuela incluyendo inactivas.
3. **Media:** Revisar cualquier otro servicio que haga `Include(x => x.School)` o `Include(x => x.SchoolNavigation)` y use `x.School.Name` (o similar) en Select/OrderBy sin comprobar null (p. ej. PaymentService, PrematriculationService si hay listados globales).
4. **Baja:** Si el producto lo requiere, que **GetSystemStatsAsync** use `IgnoreQueryFilters()` para incluir escuelas inactivas en TotalEscuelas y EscuelasStats.
5. **Baja:** Valorar índice en `schools(is_active)` si el número de escuelas crece de forma relevante.

---

## 7. Validaciones realizadas (detalle)

### 7.1 QueryFilter

- **Presencia:** `modelBuilder.Entity<School>().HasQueryFilter(s => s.IsActive)` en SchoolDbContext (línea 654). Correcto.
- **Includes:** Al incluir School desde otra entidad (User, AuditLog, CounselorAssignment, EmailConfiguration, etc.), el filtro se aplica a la navegación: las escuelas inactivas aparecen como **null**. Quien proyecte o ordene por School debe asumir null.
- **SchoolNavigation:** User.SchoolNavigation es `School?`; ListAdmins usa `SchoolNavigation?.Name ?? "Sin escuela asignada"`. No se rompe la navegación; las vistas revisadas manejan null.
- **IgnoreQueryFilters:** Usado donde debe: SuperAdminService (listado/edición/desactivación de escuelas), AuthService (comprobar si la escuela del usuario está activa), SchoolService.DeleteAsync (desactivar por id). No se detecta uso de IgnoreQueryFilters en consultas tenant que deban respetar el filtro.

### 7.2 Servicios

- **SchoolService:** GetByIdAsync usa `FirstOrDefaultAsync` (respeta filtro). DeleteAsync usa IgnoreQueryFilters + IsActive = false; no hay Remove(school).
- **SuperAdminService:** DeleteSchoolAsync solo pone IsActive = false y Update; no hay Remove(school) ni borrado en cascada de usuarios/entidades. Métodos que deben ver todas las escuelas usan IgnoreQueryFilters.
- **AuthService:** Comprueba School.IsActive con IgnoreQueryFilters antes de firmar la sesión; mensaje claro cuando la institución está inactiva.
- **SessionValidationMiddleware:** Obtiene la escuela del usuario (FindAsync, sin filtro) y, si existe y no está activa, cierra sesión y redirige a Login con schoolInactive=1.

No se encontraron Remove(school), Delete(school) ni borrados físicos ocultos.

### 7.3 Login y sesión – Edge cases

- Usuario activo con School activa: login OK; sesión válida.
- Usuario activo con School inactiva: AuthService rechaza con mensaje de institución inactiva; no se firma sesión.
- Usuario ya logueado y luego School desactivada: en la siguiente request el middleware obtiene la escuela (FindAsync), ve !school.IsActive, cierra sesión y redirige a Login?schoolInactive=1. Correcto.
- SuperAdmin (sin SchoolId): no entra en el bloque de comprobación de escuela; login OK.
- Usuario con SchoolId null (raro): no se comprueba escuela; login OK. Aceptable.

No se identificaron edge cases adicionales que rompan el flujo de soft delete o de bloqueo de acceso.

### 7.4 Multi-tenant

- Ningún usuario con sesión válida puede tener School inactiva: el middleware los desloguea en la siguiente petición.
- Include(u => u.School) / SchoolNavigation: con escuela inactiva la navegación es null; la vista ListAdmins maneja null. GetByIdWithRelationsAsync incluye SchoolNavigation; las vistas que lo usen deben seguir manejando null (ejemplo positivo: ListAdmins).
- NullReference potenciales: limitados a CounselorAssignmentService y EmailConfigurationService.GetAllAsync en el escenario de listados globales o de escuelas inactivas, tal como se ha descrito en hallazgos medios.

### 7.5 Integridad referencial

- No existe ON DELETE CASCADE que elimine schedule_entries. Las FKs de schedule_entries usan Restrict (o equivalente).
- time_slots.school_id tiene CASCADE a schools; al no borrarse nunca School, no se dispara ese CASCADE.
- Soft Delete no borra filas; schedule_entries, time_slots, teacher_assignments y users siguen referenciando el mismo school_id. No se generan huérfanos ni violaciones de FK por la desactivación.

### 7.6 Performance

- El QueryFilter se traduce en un predicado `WHERE is_active = true` (o equivalente) en las consultas que tocan School. No se observan subconsultas adicionales innecesarias.
- Índice en is_active: no existe; recomendación opcional y de prioridad baja salvo crecimiento notable del número de escuelas.

---

## 8. Veredicto final

**School Soft Delete Enterprise Safe: YES**, con las siguientes reservas:

- La implementación cumple los requisitos de no borrado físico, bloqueo de login y de sesión para instituciones inactivas, y uso correcto del QueryFilter e IgnoreQueryFilters en los flujos actuales.
- Existen **bugs latentes** en CounselorAssignmentService y en EmailConfigurationService.GetAllAsync cuando se incluyen escuelas inactivas (Include + proyección/OrderBy sobre School sin null check). El uso actual (tenant por SchoolId activa) no los dispara.
- Se recomienda aplicar las correcciones de prioridad alta en esos servicios para que cualquier uso futuro (p. ej. listados globales SuperAdmin) sea seguro sin cambios adicionales.

Con las recomendaciones priorizadas aplicadas, el diseño puede considerarse plenamente enterprise-safe también ante futuras extensiones (listados globales, reportes, reactivación).

---

*Auditoría realizada por análisis estático de código. No se modificó código. No se ejecutaron pruebas en runtime.*
