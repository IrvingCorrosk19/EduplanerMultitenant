# Implementación: School Soft Delete (IsActive)

**Fecha:** 2026-02-12  
**Política:** School NO se borra físicamente. Soft delete obligatorio con `IsActive = false`.

---

## 1. Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `Models/School.cs` | Añadida propiedad `IsActive` (bool, default true). |
| `Models/SchoolDbContext.cs` | Configuración `IsActive` (HasDefaultValue(true)), `HasQueryFilter(s => s.IsActive)` en School. |
| `ViewModels/SchoolListViewModel.cs` | Añadida propiedad `IsActive` para listado SuperAdmin. |
| `Services/Implementations/SuperAdminService.cs` | DeleteSchoolAsync reemplazado por soft delete; GetAllSchoolsAsync, GetSchoolByIdAsync, GetSchoolForEditAsync, GetSchoolForEditWithAdminAsync, DiagnoseSchoolAsync usan `IgnoreQueryFilters()` para ver todas las escuelas. |
| `Services/Implementations/SchoolService.cs` | GetByIdAsync usa `FirstOrDefaultAsync` (respeta filtro); DeleteAsync hace soft delete con `IgnoreQueryFilters()`. |
| `Services/Implementations/AuthService.cs` | Inyección de `SchoolDbContext`; tras validar usuario, si tiene SchoolId se comprueba School.IsActive y se rechaza login con mensaje "La institución se encuentra inactiva. Contacte al administrador." |
| `Middleware/SessionValidationMiddleware.cs` | Tras validar usuario activo, si tiene SchoolId se obtiene la escuela y si no está activa se cierra sesión y redirige a Login con `?schoolInactive=1`. |
| `Controllers/AuthController.cs` | Login GET acepta `schoolInactive` y asigna TempData["Error"] con el mensaje de institución inactiva. |
| `Controllers/SuperAdminController.cs` | Mensajes de éxito/error de DeleteSchool cambiados a "desactivada" / "desactivar". |
| `Views/SuperAdmin/ListSchools.cshtml` | Badge Activa/Desactivada; botón "Desactivar institución" y confirmación actualizada; botón deshabilitado si ya está desactivada. |
| `Migrations/20260217000736_AddSchoolIsActive.cs` | Migración que solo añade columna `is_active` a `schools` (boolean NOT NULL DEFAULT true). |

---

## 2. Código antes/después del método DeleteSchool (SuperAdminService)

### Antes

```csharp
public async Task<bool> DeleteSchoolAsync(Guid id)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    var school = await _context.Schools.Include(s => s.Users).FirstOrDefaultAsync(s => s.Id == id);
    if (school == null) return false;
    // Eliminar usuarios asociados (DeleteUserRelationsAsync, Remove(user))
    // Eliminar entidades: await DeleteSchoolEntitiesAsync(school);
    // Eliminar muchos a muchos: await DeleteManyToManyRelationsAsync(school);
    await _context.SaveChangesAsync();
    _context.Schools.Remove(school);
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
    return true;
}
```

### Después

```csharp
public async Task<bool> DeleteSchoolAsync(Guid id)
{
    var school = await _context.Schools
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(s => s.Id == id);

    if (school == null)
        return false;

    school.IsActive = false;
    _context.Schools.Update(school);
    await _context.SaveChangesAsync();
    return true;
}
```

- No se eliminan usuarios, `schedule_entries`, `time_slots` ni otras entidades.
- No hay transacción ni cascadas físicas.
- SuperAdmin sigue pudiendo listar y editar escuelas inactivas gracias a `IgnoreQueryFilters()`.

---

## 3. Migración

- **Nombre:** `20260217000736_AddSchoolIsActive.cs`
- **Contenido:** Solo añade en `schools` la columna `is_active boolean NOT NULL DEFAULT true`.
- No se modifican FKs de `schedule_entries` ni la tabla `school_schedule_configurations` en esta migración (ya existían en migraciones previas; la migración generada automáticamente se editó para dejar únicamente el `AddColumn`).

Para aplicar en BD:

```bash
dotnet ef database update --context SchoolDbContext
```

---

## 4. Confirmación: sin cambios no deseados

- **schedule_entries / time_slots:** No se tocaron modelo ni relaciones.
- **DeleteBehavior** del módulo horarios: Sin cambios (se mantiene Restrict en ScheduleEntry).
- **Otras entidades:** Solo se añadió `IsActive` y filtro global en School; el resto del modelo no se alteró.
- **Cascadas físicas:** No se elimina School ni se borran filas hijas por DELETE en BD; solo se actualiza `schools.is_active`.

---

## 5. Validación pre-borrado (desactivación)

- No se bloquea la desactivación si la escuela tiene users, teacher_assignments, schedule_entries o time_slots.
- Se permite desactivar siempre; el único efecto es `IsActive = false` y que la escuela deje de aparecer en consultas normales y que sus usuarios no puedan acceder (login y middleware).

---

## 6. QA rápido

| Prueba | Resultado esperado |
|--------|--------------------|
| Desactivar una School con horarios cargados | No se borran time_slots ni schedule_entries; no se rompen FKs. |
| Listados (Admin/Director, SchoolController, etc.) | Escuelas inactivas no aparecen (filtro global). |
| SuperAdmin ListSchools | Ve todas (activas e inactivas) con badge Activa/Desactivada. |
| Login con usuario de escuela inactiva | Mensaje: "La institución se encuentra inactiva. Contacte al administrador." |
| Usuario ya logueado cuya escuela se desactiva | En la siguiente petición el middleware cierra sesión y redirige a Login con el mensaje de institución inactiva. |

---

*Implementación enterprise: soft delete obligatorio, sin borrado físico ni cascadas.*
