# AUDITORÍA DE SEGURIDAD MULTI-TENANT — MÓDULO CONVIVENCIA (DISCIPLINE REPORTS)
**Fecha:** 2026-04-29  
**Auditor:** Arquitecto de Software Senior + Auditor Técnico  
**Alcance:** DisciplineReportController, DisciplineReportService, TenantProvider, GlobalQueryFilters, EmailService  
**Base de datos auditada:** `eduplaner` (localhost:5432)

---

## 1. Resumen Ejecutivo

| Dimensión | Estado |
|---|---|
| Aislamiento multi-tenant (capa de datos) | ✅ Parcialmente implementado |
| Aislamiento multi-tenant (capa de controlador) | 🔥 **CRÍTICO — ROTO** |
| Global Query Filters | ✅ Presente para DisciplineReport |
| Defense-in-depth en servicio | ✅ Presente |
| Control de acceso por rol (RBAC) | 🔥 **CRÍTICO — AUSENTE** |
| Performance (índices) | ⚠️ Deficiente |
| Seguridad de archivos (path traversal) | ⚠️ Parcial |
| Consistencia de esquema | ⚠️ SchoolId nullable |

**Veredicto general: 🔴 RIESGOSO**

La capa de servicio tiene defensa multi-tenant razonable. Sin embargo, **el controlador expone 5 endpoints críticos sin ninguna validación de rol**, lo que permite que cualquier usuario autenticado (estudiante, padre, personal no autorizado) lea, edite y elimine reportes de disciplina de cualquier otro miembro de su escuela. Esto constituye una violación grave de IDOR y RBAC. No está listo para producción multi-tenant.

---

## 2. Hallazgos Críticos 🔥

---

### 🔥 C-01 — `Index()` sin restricción de rol: cualquier usuario ve todos los reportes de su escuela

**Archivo:** `Controllers/DisciplineReportController.cs:38-41`

```csharp
public async Task<IActionResult> Index()
{
    var reports = await _disciplineReportService.GetAllAsync();
    return View(reports);
}
```

**Problema:** No hay `[Authorize(Roles = "...")]`, no hay comprobación de rol en el cuerpo. Cualquier usuario autenticado — un estudiante, un acudiente, un usuario recién creado — puede navegar a `/DisciplineReport/Index` y obtener la lista completa de reportes disciplinarios de **todos los alumnos** de su escuela.

**Impacto:** Violación de privacidad FERPA/LOPD. Un estudiante puede ver el expediente de otro. Un padre puede ver datos de estudiantes ajenos.

**Ataque simulado:**
```
GET /DisciplineReport/Index  (con cookie de sesión de estudiante)
→ Respuesta: lista de todos los reportes de la escuela
```

**Corrección mínima:**
```csharp
[Authorize(Roles = "Director,Inspector,Docente,Teacher")]
public async Task<IActionResult> Index() { ... }
```

---

### 🔥 C-02 — `Details(Guid id)` sin restricción de rol: IDOR directo

**Archivo:** `Controllers/DisciplineReportController.cs:44-49`

```csharp
public async Task<IActionResult> Details(Guid id)
{
    var report = await _disciplineReportService.GetByIdAsync(id);
    if (report == null) return NotFound();
    return View(report);
}
```

**Problema:** Cualquier usuario autenticado puede consultar el detalle de cualquier reporte de su escuela cambiando el `id` en la URL. El servicio valida el `SchoolId` (evita cross-tenant), pero no impide que un alumno o padre vea reportes de otros alumnos dentro de la misma escuela.

**Impacto:** IDOR clásico. Acceso no autorizado a información disciplinaria sensible.

**Ataque simulado:**
```
GET /DisciplineReport/Details/3fa85f64-5717-4562-b3fc-2c963f66afa6
(usuario: estudiante Pedro → ve el expediente de estudiante Juan)
```

---

### 🔥 C-03 — `Edit()` GET + POST sin restricción de rol: cualquier usuario puede modificar reportes

**Archivo:** `Controllers/DisciplineReportController.cs:389-405`

```csharp
public async Task<IActionResult> Edit(Guid id) { ... }

[HttpPost]
public async Task<IActionResult> Edit(DisciplineReport report)
{
    if (ModelState.IsValid)
    {
        await _disciplineReportService.UpdateAsync(report);
        return RedirectToAction(nameof(Index));
    }
    return View(report);
}
```

**Problema:** No hay verificación de rol. Cualquier usuario autenticado puede editar cualquier reporte de su escuela. El servicio tiene defensa (valida `existing.SchoolId != schoolId.Value`), pero la puerta de entrada en el controlador está abierta.

**Agravante:** El endpoint `Edit(DisciplineReport report)` acepta model binding completo del objeto `DisciplineReport`. Un atacante puede intentar inyectar campos adicionales (mass assignment) como `SchoolId`, `TeacherId`, etc. aunque el servicio los ignore parcialmente.

**Impacto:** Manipulación de datos disciplinarios. Un estudiante puede alterar su propio expediente.

---

### 🔥 C-04 — `Delete()` GET + POST sin restricción de rol: cualquier usuario puede borrar reportes

**Archivo:** `Controllers/DisciplineReportController.cs:407-418`

```csharp
public async Task<IActionResult> Delete(Guid id) { ... }

[HttpPost, ActionName("Delete")]
public async Task<IActionResult> DeleteConfirmed(Guid id)
{
    await _disciplineReportService.DeleteAsync(id);
    return RedirectToAction(nameof(Index));
}
```

**Problema:** Sin restricción de rol. El servicio sí valida `SchoolId`, pero no hay ninguna verificación de que el usuario tenga permisos para borrar.

**Impacto:** Un estudiante puede eliminar su propio expediente disciplinario o el de cualquier compañero de escuela.

---

### 🔥 C-05 — `GetFiltered()` sin restricción de rol: cualquier usuario extrae reportes filtrados

**Archivo:** `Controllers/DisciplineReportController.cs:467-502`

```csharp
[HttpGet]
public async Task<IActionResult> GetFiltered(DateTime? fechaInicio, DateTime? fechaFin, Guid? gradoId, ...)
{
    var reports = await _disciplineReportService.GetFilteredAsync(...);
    return Json(result);
}
```

**Problema:** No hay restricción de rol. Si un estudiante conoce un `gradoId` válido (que es fácil de obtener de otras APIs), puede obtener la lista completa de reportes disciplinarios de ese grado, incluyendo nombre, documentoId, descripción y archivos adjuntos de todos sus compañeros.

**Agravante:** La respuesta incluye `documentId` (cédula/pasaporte), descripción libre y `documents` (rutas de archivos adjuntos).

**Ataque simulado:**
```
GET /DisciplineReport/GetFiltered?gradoId=<uuid_conocido>
(usuario: estudiante → recibe lista completa del grado con cédulas y descripciones)
```

---

### 🔥 C-06 — `ExportToExcel` sin restricción de rol: extracción masiva de datos

**Archivo:** `Controllers/DisciplineReportController.cs:504-511`

```csharp
[HttpGet]
public async Task<IActionResult> ExportToExcel(DateTime? fechaInicio, DateTime? fechaFin, Guid? gradoId)
{
    var reports = await _disciplineReportService.GetFilteredAsync(fechaInicio, fechaFin, gradoId);
    // Genera CSV con todos los reportes
}
```

**Problema:** Sin restricción de rol. Cualquier usuario puede descargar un CSV completo con datos de convivencia de toda la escuela. Aunque el método se llama `ExportToExcel`, genera un archivo CSV real.

**Agravante secundario:** El nombre del método es engañoso (`ExportToExcel` pero devuelve `text/csv`), lo que dificulta auditorías futuras.

---

### 🔥 C-07 — Índice de `school_id` ausente en `discipline_reports`: degradación crítica de performance

**Archivo:** `Models/SchoolDbContext.cs:427-501`

La configuración de la entidad `DisciplineReport` define índices para `GradeLevelId`, `GroupId`, `StudentId`, `SubjectId` y `TeacherId`, pero **NO define un índice para `school_id`**:

```csharp
entity.HasIndex(e => e.GradeLevelId, "IX_discipline_reports_grade_level_id");
entity.HasIndex(e => e.GroupId, "IX_discipline_reports_group_id");
entity.HasIndex(e => e.StudentId, "IX_discipline_reports_student_id");
// ❌ NO HAY: entity.HasIndex(e => e.SchoolId, "IX_discipline_reports_school_id");
```

**Impacto:** En producción multi-tenant con miles de registros, **CADA consulta** que filtra por `school_id` (que son todas las consultas del módulo) realizará un full sequential scan. Esto es la columna más consultada y no tiene índice.

**Estimación:** Con 10,000 registros y 100 escuelas, cada query tardará ~100x más de lo necesario.

---

## 3. Hallazgos Importantes ⚠️

---

### ⚠️ I-01 — `FindAsync` bypassa el Global Query Filter en toda la capa de servicio

**Archivos:**
- `Services/Implementations/DisciplineReportService.cs:39, 60, 82, 276`

```csharp
var report = await _context.DisciplineReports.FindAsync(id.Value);  // Bypasa GQF
```

**Problema:** En EF Core, `FindAsync` verifica primero el caché del contexto y luego va a la DB **sin aplicar global query filters**. Esto significa que si un record está en caché de otra request (en contexto scoped esto no ocurre, pero en contexto singleton sí), o simplemente que el GQF no protege esta ruta.

**Mitigación actual:** Los servicios aplican verificación explícita posterior (`if (report.SchoolId != schoolId.Value) return null`), lo que constituye defensa en profundidad válida.

**Riesgo residual:** Si alguien agrega un nuevo método de servicio usando `FindAsync` sin la verificación explícita, hay fuga inmediata. El patrón es frágil.

**Recomendación:** Reemplazar `FindAsync` con:
```csharp
await _context.DisciplineReports
    .Where(r => r.Id == id && r.SchoolId == schoolId.Value)
    .FirstOrDefaultAsync();
```

---

### ⚠️ I-02 — `SchoolId` nullable en el modelo permite registros huérfanos

**Archivo:** `Models/DisciplineReport.cs:9`

```csharp
public Guid? SchoolId { get; set; }
```

**Problema:** El modelo permite `null`. Si por cualquier bug o condición de carrera `SchoolId` no se asigna, el registro queda huérfano. El Global Query Filter ante `_tenantId = null` solo deja pasar si `_isSuperAdmin = true`, pero si un superadmin hace una query, verá estos registros sin escuela junto a los de su scope.

**Agravante:** Hay 1 usuario sin `school_id` en cada base según el análisis previo. El mismo patrón puede reproducirse en DisciplineReports.

---

### ⚠️ I-03 — Path traversal en upload de archivos (nombre de archivo no sanitizado)

**Archivo:** `Controllers/DisciplineReportController.cs:94`

```csharp
var fileName = $"{Guid.NewGuid()}_{file.FileName}";
var filePath = Path.Combine(uploadsPath, fileName);
```

**Problema:** `file.FileName` puede contener `../` en algunos clientes o navegadores no estándar. `Path.Combine` en Windows ignora el primer segmento si el segundo comienza con `\` o `/`. Aunque el prefijo `Guid.NewGuid()_` reduce el riesgo, no elimina la vulnerabilidad completamente.

**Comparación:** En `TryDeleteDisciplineUploadedFiles` SÍ se usa `Path.GetFileName(name)` para sanitizar ✅. Inconsistencia de patrón.

**Corrección:**
```csharp
var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
```

---

### ⚠️ I-04 — `GetVisibleDisciplineInfo`: rol "director" sin validar que el estudiante pertenece a su escuela

**Archivo:** `Controllers/DisciplineReportController.cs:615-617`

```csharp
var canView = role switch
{
    "director" => true,  // ← Sin verificación de escuela
    ...
};
```

**Problema:** El director recibe `canView = true` sin verificar que `studentId` pertenezca a su escuela. La defensa existe en el servicio (que filtra por `SchoolId`), pero si alguien proporciona un `studentId` de otra escuela, el servicio devolverá lista vacía — lo cual no genera error sino silencio. La defensa correcta es rechazar explícitamente en el controlador.

**Impacto:** Bajo (datos vacíos al fallar), pero la ausencia de rechazo explícito es arquitectónicamente incorrecta y dificulta auditorías.

---

### ⚠️ I-05 — `SendEmailToStudent` sin restricción de rol

**Archivo:** `Controllers/DisciplineReportController.cs:514`

Cualquier usuario autenticado de la misma escuela puede disparar el envío de un correo disciplinario a cualquier estudiante de esa escuela, siempre que conozca el `studentId` y un `disciplineReportId` válido.

**Impacto:** Un estudiante malintencionado podría acosar a otro enviándole repetidamente correos de "reporte disciplinario".

---

### ⚠️ I-06 — `GetByCounselor` accesible para cualquier usuario autenticado

**Archivo:** `Controllers/DisciplineReportController.cs:566`

No hay validación de rol. Si un usuario no es consejero, el servicio devolverá lista vacía (safe-fail), pero el endpoint está abierto. Un atacante que obtiene el rol de consejero de otra forma puede abusar de este endpoint.

---

### ⚠️ I-07 — Inconsistencia de fuente de `SchoolId`: JWT claim vs. DB

**Archivos:**
- `Infrastructure/TenantProvider.cs` → lee claim JWT `school_id` → alimenta Global Query Filter
- `Services/Implementations/CurrentUserService.cs:30-35` → lee claim JWT `school_id`
- `Controllers/DisciplineReportController.cs:333-343` → usa `currentUser.SchoolId` (del DB)

**Problema:** En `GetOwnedDisciplineReportForTeacherAsync`, la validación de SchoolId usa el objeto `User` cargado desde DB (SchoolId real). En los servicios, se usa el claim JWT. Si se cambia la escuela de un usuario en la DB sin revocar el JWT, ambas capas discrepan durante el tiempo de vida del token.

**Impacto:** Ventana de inconsistencia proporcional al tiempo de vida del JWT (potencialmente horas).

---

### ⚠️ I-08 — Validación de `Subject`/`Group`/`GradeLevel` acepta entidades globales (SchoolId null)

**Archivo:** `Controllers/DisciplineReportController.cs:148-168`

```csharp
var subjectOk = await _context.Subjects.AsNoTracking()
    .AnyAsync(s => s.Id == subjectGuid && (!s.SchoolId.HasValue || s.SchoolId == schoolScopeId));
```

**Problema:** La condición `!s.SchoolId.HasValue` permite usar materias/grupos/grados que no tienen escuela asignada. Si existe un registro huérfano (sin `SchoolId`) en cualquiera de estas tablas, puede ser referenciado por cualquier escuela. Esto rompe el aislamiento a nivel de metadatos.

---

## 4. Hallazgos Menores ℹ️

---

### ℹ️ M-01 — `SendEscalationMessageToDirector`: funcionalidad incompleta / dead code

**Archivo:** `Controllers/DisciplineReportController.cs:889`

```csharp
// Aquí deberías usar el servicio de mensajería para enviar el mensaje
// await _messagingService.SendMessageAsync(message);
```

El mensaje se construye pero **nunca se envía**. La escalación al director es una característica crítica del módulo de convivencia que está comentada. El `Message` se crea en memoria pero no se persiste ni envía.

---

### ℹ️ M-02 — `ExportToExcel` genera CSV, no Excel

**Archivo:** `Controllers/DisciplineReportController.cs:504-511`

El método se llama `ExportToExcel` y devuelve `text/csv`. Nombre engañoso que dificulta el mantenimiento y puede generar confusión en auditorías futuras.

---

### ℹ️ M-03 — `UpdateStatus` excluye al rol "Inspector"

**Archivo:** `Controllers/DisciplineReportController.cs:796-801`

```csharp
var canUpdate = currentUser.Role?.ToLower() switch
{
    "director" => true,
    "teacher" => request.Status?.ToLower() == "escalado",
    _ => false
};
```

El rol `inspector` (reconocido en `GetVisibleDisciplineInfo`) no puede actualizar estados. Si es intencional, debe documentarse. Si es un olvido, es un bug de negocio.

---

### ℹ️ M-04 — `UpdateStatus` sin validación de SchoolId a nivel de controlador

**Archivo:** `Controllers/DisciplineReportController.cs:784-838`

El controlador verifica rol pero **no verifica que el `reportId` pertenezca a la escuela del usuario antes del rol check**. El servicio lo valida correctamente, pero la secuencia de checks en el controlador es: rol → servicio (con validación de tenant). Un atacante puede sondear si reportes de otras escuelas existen mediante timing attacks (la respuesta tarda diferente si el registro existe pero pertenece a otra escuela vs. no existe).

---

### ℹ️ M-05 — `GetCurrentUserAsync` usa `FindAsync` con caché de contexto

**Archivo:** `Services/Implementations/CurrentUserService.cs:43`

```csharp
return await _context.Users.FindAsync(userId.Value);
```

En requests con múltiples operaciones sobre el mismo usuario en la misma request, EF Core puede devolver el objeto cacheado del contexto en lugar de ir a DB. Si se modifica el usuario durante la request (improbable pero posible), la información puede ser stale.

---

### ℹ️ M-06 — Sin auditoría de acceso a datos sensibles

No existe logging de accesos a reportes de disciplina. En un sistema multi-tenant educativo con datos sensibles, cada lectura de reporte debería registrarse en `AuditLogs` con usuario, timestamp y registro accedido. Actualmente solo se loguean errores.

---

## 5. Recomendaciones Técnicas

### Prioridad 1 — Acción inmediata (antes de cualquier deploy)

**R-01: Agregar `[Authorize(Roles)]` a los 5 endpoints críticos**

```csharp
// Controllers/DisciplineReportController.cs

[Authorize(Roles = "Director,Inspector,Docente,Teacher")]
public async Task<IActionResult> Index() { ... }

[Authorize(Roles = "Director,Inspector,Docente,Teacher")]
public async Task<IActionResult> Details(Guid id) { ... }

[Authorize(Roles = "Director,Inspector")]
public async Task<IActionResult> Edit(Guid id) { ... }

[HttpPost]
[Authorize(Roles = "Director,Inspector")]
public async Task<IActionResult> Edit(DisciplineReport report) { ... }

[Authorize(Roles = "Director,Inspector")]
public async Task<IActionResult> Delete(Guid id) { ... }

[HttpPost, ActionName("Delete")]
[Authorize(Roles = "Director,Inspector")]
public async Task<IActionResult> DeleteConfirmed(Guid id) { ... }

[HttpGet]
[Authorize(Roles = "Director,Inspector,Docente,Teacher")]
public async Task<IActionResult> GetFiltered(...) { ... }

[HttpGet]
[Authorize(Roles = "Director,Inspector")]
public async Task<IActionResult> ExportToExcel(...) { ... }
```

**R-02: Agregar índice de `school_id` en `discipline_reports`**

```csharp
// Models/SchoolDbContext.cs — dentro del bloque DisciplineReport
entity.HasIndex(e => e.SchoolId, "IX_discipline_reports_school_id");
// Índice compuesto para la query más frecuente:
entity.HasIndex(e => new { e.SchoolId, e.Date }, "IX_discipline_reports_school_date");
```

Más la migración correspondiente:
```sql
CREATE INDEX CONCURRENTLY IF NOT EXISTS IX_discipline_reports_school_id 
ON discipline_reports(school_id);
CREATE INDEX CONCURRENTLY IF NOT EXISTS IX_discipline_reports_school_date 
ON discipline_reports(school_id, date DESC);
```

### Prioridad 2 — Hardening (antes de producción)

**R-03: Reemplazar `FindAsync` con queries explícitas con filtro de SchoolId**

```csharp
// Patrón seguro para todos los métodos del servicio:
var report = await _context.DisciplineReports
    .Where(r => r.Id == id && r.SchoolId == schoolId.Value)
    .FirstOrDefaultAsync();
```

**R-04: Sanitizar nombre de archivo en uploads**

```csharp
var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
```

**R-05: Validar explícitamente el estudiante en `GetVisibleDisciplineInfo` para directores**

```csharp
"director" => await CanSameSchoolStaffViewStudentDisciplineAsync(currentUser, studentId),
```

**R-06: Completar `SendEscalationMessageToDirector`**

Descomentar y registrar correctamente el mensaje en la base de datos usando el servicio de mensajería.

**R-07: Agregar restricción NOT NULL a `school_id` en `discipline_reports`**

```sql
ALTER TABLE discipline_reports 
    ALTER COLUMN school_id SET NOT NULL;
```

Y actualizar el modelo:
```csharp
public Guid SchoolId { get; set; }  // No nullable
```

### Prioridad 3 — Mejoras de arquitectura

**R-08: Centralizar validación de tenant en un filtro de acción (ActionFilter)**

En lugar de depender de que cada developer recuerde agregar la validación, crear un `[TenantScopedAction]` attribute que valide automáticamente.

**R-09: Implementar auditoría de acceso**

```csharp
// En cada acceso a reporte sensible:
await _auditService.LogAccessAsync(currentUserId, "DisciplineReport", reportId, "READ");
```

**R-10: Estandarizar fuente de SchoolId**

Decidir entre JWT claim vs. DB lookup y ser consistente en toda la capa. Considerar validar el claim JWT contra la DB en el middleware de autenticación para detectar cambios de escuela.

---

## 6. Evaluación Final

| Criterio | Estado | Detalle |
|---|---|---|
| Aislamiento multi-tenant en servicio | ✅ PRESENTE | Todas las queries del servicio filtran por SchoolId |
| Global Query Filter en DisciplineReport | ✅ PRESENTE | Configurado en SchoolDbContextTenantFilters |
| Control de acceso por rol (RBAC) | 🔥 **AUSENTE** | 6 endpoints sin restricción de rol |
| Protección contra IDOR | 🔥 **PARCIAL** | Tenant aislado, pero acceso cross-user dentro del tenant |
| Validación de entidades relacionadas | ✅ PRESENTE | Student/Teacher/Subject validados en creación |
| Defense-in-depth en servicio | ✅ PRESENTE | Validación explícita incluso con FindAsync |
| Índice de SchoolId en discipline_reports | 🔥 **AUSENTE** | Full table scan en producción |
| Sanitización de uploads | ⚠️ PARCIAL | Falta `Path.GetFileName` en upload |
| Auditoría de accesos | ❌ AUSENTE | No hay logging de lecturas |
| Funcionalidad de escalación | ❌ INCOMPLETA | Código comentado |

### ¿Está listo para producción multi-tenant?

## ❌ NO

**Motivo principal:** Los endpoints `Index`, `Details`, `Edit`, `Delete`, `GetFiltered` y `ExportToExcel` del controlador no tienen ninguna restricción de rol. Cualquier usuario autenticado de la escuela puede leer, modificar y eliminar reportes disciplinarios de cualquier otro alumno. Esto es una violación de IDOR y RBAC inaceptable para datos educativos sensibles.

**Plazo estimado de corrección de críticos:** 2-4 horas de desarrollo + pruebas de regresión.
**Plazo para producción segura completa:** 1-2 días aplicando todas las recomendaciones de Prioridad 1 y 2.

---

*Auditoría realizada sobre código fuente en C:\Proyectos\EduplanerMultitenant\SchoolManager — rama main — commit 6360f17*
