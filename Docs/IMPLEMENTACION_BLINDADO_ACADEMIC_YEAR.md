# Implementación enterprise – Blindado Academic Year

**Objetivo:** Garantizar que ninguna School exista sin AcademicYear activo.

---

## 1. Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `Services/Interfaces/IAcademicYearService.cs` | Nuevo método `EnsureDefaultAcademicYearForSchoolAsync(Guid schoolId)`. |
| `Services/Implementations/AcademicYearService.cs` | Implementación: si la escuela no tiene años, crea uno por defecto (año actual, 1 ene–31 dic, IsActive=true). |
| `Services/Implementations/SuperAdminService.cs` | Inyección de `IAcademicYearService`; tras crear escuela y admin, llama a `EnsureDefaultAcademicYearForSchoolAsync(school.Id)` dentro de la transacción; captura excepción y registra log sin romper el flujo. |
| `Services/Implementations/SchoolService.cs` | Inyección de `IAcademicYearService`; tras `CreateAsync(school)` llama a `EnsureDefaultAcademicYearForSchoolAsync(school.Id)`; captura excepción para no fallar la creación. |
| `Controllers/ScheduleController.cs` | Asigna `ViewBag.HasNoAcademicYears = (academicYears.Count == 0)`. |
| `Views/Schedule/ByTeacher.cshtml` | Si `hasNoAcademicYears`: no se renderizan filtros ni tabla; se muestra un único mensaje claro (alert warning). El script sale pronto si `data-has-no-academic-years="true"`. |

---

## 2. Código exacto (resumen)

### 2.1 IAcademicYearService

- Nuevo método:

```csharp
/// <summary>
/// Garantiza que la escuela tenga al menos un año académico activo.
/// Si no tiene ninguno, crea uno por defecto para el año actual (1 ene - 31 dic).
/// </summary>
Task EnsureDefaultAcademicYearForSchoolAsync(Guid schoolId);
```

### 2.2 AcademicYearService.EnsureDefaultAcademicYearForSchoolAsync

- Obtiene años de la escuela con `GetAllBySchoolAsync(schoolId)`.
- Si `existing.Count > 0`, finaliza.
- Si no, crea un `AcademicYear` con:
  - `Name = DateTime.UtcNow.Year.ToString()`
  - `StartDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc)`
  - `EndDate = new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc)`
  - `IsActive = true`
  - `SchoolId = schoolId`
- Llama a `CreateAsync(defaultYear)`.
- Captura `PostgresException` con `SqlState == "42P01"` (tabla inexistente) para no romper el flujo.

### 2.3 SuperAdminService (creación de escuela)

- Constructor: nuevo parámetro `IAcademicYearService academicYearService`, asignado a `_academicYearService`.
- Tras `SaveChangesAsync()` que actualiza `school.AdminId`, y antes de `CommitAsync()`:
  - `try { await _academicYearService.EnsureDefaultAcademicYearForSchoolAsync(school.Id); }`
  - `catch (Exception ex) { _logger.LogWarning(ex, "No se pudo crear año académico por defecto..."); }`

### 2.4 SchoolService.CreateAsync

- Constructor: nuevo parámetro `IAcademicYearService academicYearService`, asignado a `_academicYearService`.
- Tras `await _context.SaveChangesAsync();`:
  - `try { await _academicYearService.EnsureDefaultAcademicYearForSchoolAsync(school.Id); } catch { }`

### 2.5 ScheduleController.ByTeacher

- Antes de `return View();`: `ViewBag.HasNoAcademicYears = academicYears.Count == 0;`

### 2.6 ByTeacher.cshtml

- Variable: `var hasNoAcademicYears = ViewBag.HasNoAcademicYears ?? false;`
- En `schedule-page-data`: `data-has-no-academic-years="@(hasNoAcademicYears ? "true" : "false")"`.
- En `card-body`: si `hasNoAcademicYears`, solo un `<div class="alert alert-warning">` con el mensaje; si no, se mantiene el bloque de filtros, tabla y “Cargar horario”.
- En Scripts: al inicio del IIFE, si `el.getAttribute('data-has-no-academic-years') === 'true'` se hace `return` para no ejecutar listeners ni lógica de tabla.

---

## 3. Explicación breve

- **Flujo de creación de School:** Hay dos puntos de creación: (1) `SuperAdminService.CreateSchoolWithAdminAsync` (escuela + admin en una transacción) y (2) `SchoolService.CreateAsync` (uso desde `SchoolController.Create`). En ambos se llama a `EnsureDefaultAcademicYearForSchoolAsync` después de persistir la escuela.
- **Año por defecto:** Se crea un único año con nombre igual al año actual, del 1 de enero al 31 de diciembre (UTC), activo, asociado a la nueva escuela. No se usa AutoMapper; se instancia `AcademicYear` y se usa `CreateAsync` existente.
- **Schedule/ByTeacher:** Si no hay años académicos (`academicYears.Count == 0`), la vista no muestra filtros ni tabla ni botón “Cargar horario”; solo un mensaje de aviso indicando contactar al administrador. El script no se ejecuta para esos controles.
- **Robustez:** Si la tabla `academic_years` no existe (42P01), el servicio no lanza; en SuperAdmin se registra un warning y se hace commit de la escuela; en SchoolService se ignora la excepción. La arquitectura (Services/Interfaces/Implementations) y la lógica existente se mantienen.
