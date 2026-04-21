# VALIDACIÓN ARQUITECTÓNICA MULTITENANT — EDUPLANER
**Fecha:** 2026-04-20  
**Versión del sistema:** SchoolManager (ASP.NET Core 8 / PostgreSQL 18)  
**Modelo de tenancy:** Shared DB / Shared Schema — discriminador `school_id`  
**Auditor:** Análisis estático completo del código fuente post-implementación  

---

## 1. RESUMEN EJECUTIVO

La implementación multitenant de Eduplaner aplica una estrategia de defensa en profundidad en dos capas:

- **Capa 1 (Servicios):** Filtros explícitos `WHERE school_id = @currentSchoolId` en métodos GetAll/GetById/Delete de ~15 servicios.
- **Capa 2 (EF Core):** `HasQueryFilter` sobre 24 entidades que inyecta automáticamente el predicado de tenant en *toda* consulta LINQ que no use `IgnoreQueryFilters()`.

El sistema está mayoritariamente bien implementado y es apto para un entorno SaaS de múltiples instituciones. Sin embargo, se identificaron **2 riesgos de severidad media** y **5 observaciones de baja severidad** que deben atenderse antes de una puesta en producción con datos reales de múltiples clientes simultáneos.

**Veredicto global: APTO CON CORRECCIONES MENORES**

---

## 2. METODOLOGÍA

- Lectura directa de código fuente (no herramientas de análisis automatizado).
- Revisión de cada servicio listado en `Services/Implementations/`.
- Revisión de `SchoolDbContextTenantFilters.cs` (HasQueryFilter).
- Revisión del flujo completo de autenticación: `AuthService → AuthController → Claims → TenantProvider → DbContext`.
- Revisión del middleware Bearer HMAC (`ApiBearerTokenMiddleware`).
- Cruce entre entidades en DbContext vs entidades cubiertas por HasQueryFilter.

---

## 3. RESULTADOS POR ÁREA

### 3.1 C-01 — Claim `school_id` en Cookie de Autenticación
**Estado: ✅ CORRECTO**

`AuthService.LoginAsync` agrega el claim `school_id` con el `SchoolId` del usuario durante el inicio de sesión:
```csharp
new Claim("school_id", user.SchoolId?.ToString() ?? "")
```
El claim se persiste en la cookie cifrada durante 24 horas con `SlidingExpiration = true`.

Para el caso de usuarios con el mismo email en múltiples instituciones, el servicio detecta la colisión y solicita al usuario seleccionar la institución:
```csharp
if (!schoolId.HasValue && sameEmailCount > 1)
    return (false, "Existen varias cuentas con este correo...", null);
```

**Riesgo residual:** El claim `school_id` es `""` (cadena vacía) para el superadmin, no `null`. `TenantProvider` verifica correctamente `Guid.TryParse + schoolId != Guid.Empty`, por lo que `_tenantId` queda `null` para superadmin. Comportamiento correcto y esperado.

---

### 3.2 C-02 — `ICurrentUserService.GetCurrentSchoolIdAsync()`
**Estado: ✅ CORRECTO**

La implementación lee directamente del claim sin consulta a base de datos:
```csharp
public Task<Guid?> GetCurrentSchoolIdAsync()
{
    var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("school_id");
    if (claim != null && Guid.TryParse(claim.Value, out var schoolId) && schoolId != Guid.Empty)
        return Task.FromResult<Guid?>(schoolId);
    return Task.FromResult<Guid?>(null);
}
```
Cero round-trips a DB por request. Correcto.

---

### 3.3 C-03 — `AuditHelper.SetSchoolIdAsync()` en creación de entidades
**Estado: ✅ CORRECTO**

Usa reflexión para asignar `SchoolId` a cualquier entidad al momento de creación. Se llama en `StudentActivityScoreService`, `ActivityService`, y otros servicios que crean entidades.

---

### 3.4 C-04 — Filtrado explícito en capa de servicios
**Estado: ✅ CORRECTO (con nota)**

Servicios verificados con filtros explícitos de `school_id`:

| Servicio | GetAll | GetById | Delete | Notas |
|---|---|---|---|---|
| StudentService | ✅ | ✅ | ✅ | `GetByGroupAndGradeAsync` también filtra |
| GroupService | ✅ | ✅ | ✅ | `GetOrCreateAsync` filtra |
| SubjectService | ✅ | ✅ | ✅ | |
| GradeLevelService | ✅ | ✅ | ✅ | |
| AttendanceService | ✅ | ✅ | ✅ | |
| SpecialtyService | ✅ | ✅ | ✅ | |
| PaymentConceptService | ✅ | ✅ | ✅ | |
| PaymentService | ✅ | ✅ | ✅ | |
| DisciplineReportService | ✅ | ✅ | ✅ | |
| OrientationReportService | ✅ | ✅ | ✅ | |
| AcademicYearService | ✅ | ✅ | ✅ | |
| UserService | ✅ | ✅ | ✅ | |
| StudentActivityScoreService | ✅ | ✅ | ✅ | Filtro manual en todas las queries |

**Nota sobre `AreaService`:** Las áreas (materias agrupadas en áreas académicas) están diseñadas como catálogo global (`IsGlobal = true`). No tienen `school_id` por diseño intencional — es un catálogo compartido. **Esto es aceptable** pero debe documentarse formalmente como decisión arquitectónica para evitar que futuros desarrolladores agreguen filtros innecesarios.

---

### 3.5 C-05 — Multi-school same-email disambiguation
**Estado: ✅ CORRECTO**

El login detecta colisiones de email y solicita selección de institución al usuario antes de proceder. El API móvil acepta `SchoolId` opcional en `LoginApiRequest`. Correcto.

---

### 3.6 C-06 — Asignación de `SchoolId` al crear usuarios
**Estado: ✅ CORRECTO**

`UserController.CreateJson` asigna `SchoolId` desde el claim del usuario autenticado:
```csharp
var currentSchoolId = await _currentUserService.GetCurrentSchoolIdAsync();
var user = new User { ..., SchoolId = currentSchoolId, ... };
```

---

### 3.7 C-07 — `HasQueryFilter` Global en EF Core
**Estado: ✅ CORRECTO (con observación)**

**Entidades cubiertas (24):** User, Student, Group, GradeLevel, Subject, Specialty, Activity, ActivityType, Attendance, DisciplineReport, OrientationReport, Trimester, SubjectAssignment, TeacherWorkPlan, Payment, PaymentConcept, Prematriculation, PrematriculationPeriod, CounselorAssignment, AcademicYear, SecuritySetting, EmailConfiguration, Message, Shift.

**Evaluación del patrón `e => _tenantId == null || e.SchoolId == _tenantId`:**  
EF Core evalúa los query filters por instancia de DbContext en tiempo de consulta — no en tiempo de construcción del modelo. Dado que `DbContext` es `Scoped`, cada request obtiene una instancia nueva con `_tenantId` propio. El patrón es correcto y está documentado por Microsoft.

**Entidades con DbSet que NO están en HasQueryFilter:**

| Entidad | ¿Tiene SchoolId? | Tipo | Riesgo | Mitigación actual |
|---|---|---|---|---|
| StudentActivityScore | ✅ Sí | `Guid?` | MEDIO | Filtro manual en servicio — correcto |
| TeacherAssignment | ❌ No | — | NINGUNO | Sin SchoolId: no aplica filtro de tenant |
| StudentIdCard | ❌ No | — | NINGUNO | Sin SchoolId: se relaciona vía Student (que sí filtra) |
| ScanLog | ❌ No | — | NINGUNO | Sin SchoolId: trazabilidad de auditoría, no datos sensibles |
| PrematriculationHistory | ❌ No | — | NINGUNO | Sin SchoolId: historial de FK a Prematriculation (filtrada) |
| SchoolIdCardSetting | ✅ Sí | `Guid` | BAJO | Sin HasQueryFilter; acceso solo por admin autenticado |
| TimeSlot | ✅ Sí | `Guid` | BAJO | Sin HasQueryFilter; acceso solo por admin autenticado |
| ScheduleEntry | ❌ No | — | NINGUNO | Sin SchoolId: se filtra a través de TimeSlot/Group |
| SchoolScheduleConfiguration | ✅ Sí | `Guid` | BAJO | Sin HasQueryFilter; acceso solo por admin autenticado |
| TeacherWorkPlanDetail | ❌ No | — | NINGUNO | Sin SchoolId: hija de TeacherWorkPlan (que sí filtra) |
| TeacherWorkPlanReviewLog | ❌ No | — | NINGUNO | Sin SchoolId: log de auditoría, no datos de negocio |
| EmailQueue | ❌ No | — | NINGUNO | Sin SchoolId: cola de envío interna, no expuesta vía API |
| EmailJob | ✅ Sí | `Guid?` | BAJO | Sin HasQueryFilter; proceso de fondo, sin acceso externo directo |
| EmailApiConfiguration | ❌ No | — | NINGUNO | Sin SchoolId: configuración global de API de email |
| StudentPaymentAccess | ✅ Sí | `Guid` | BAJO | Sin HasQueryFilter; módulo Club de Padres con acceso restringido |
| Area | ❌ No | — | NINGUNO | Catálogo global por diseño (`IsGlobal = true`); compartido entre instituciones |

**Entidades que requieren agregar `HasQueryFilter` (tienen SchoolId pero no están cubiertas):**
`StudentActivityScore`, `SchoolIdCardSetting`, `TimeSlot`, `SchoolScheduleConfiguration`, `EmailJob`, `StudentPaymentAccess` — 6 entidades.

**Recomendación:** Agregar `HasQueryFilter` a estas 6 entidades en `SchoolDbContextTenantFilters.cs`. Es defensivo, no tiene costo de rendimiento significativo y cierra la brecha de la segunda capa de defensa.

---

### 3.8 C-08 — Control de acceso a rutas administrativas
**Estado: ✅ CORRECTO**

`SchoolController` tiene `[Authorize(Roles = "superadmin")]`. Las rutas de administración de usuarios usan roles apropiados.

---

### 3.9 SEC-04 — HMAC-SHA256 Bearer Token (API Móvil)
**Estado: ✅ CORRECTO**

Formato del token: `base64(userId:schoolId:role:timestamp:HMAC-SHA256(payload, secret))`

Propiedades de seguridad verificadas:
- Validación criptográfica con `CryptographicOperations.FixedTimeEquals` (resistente a timing attacks).
- Expiración fija de 24 horas.
- No requiere consulta a base de datos para validar.
- La clave secreta se configura en `appsettings.json` bajo `ApiToken:SecretKey`.
- El token incluye `role` y `school_id` — el middleware reconstruye los claims correctamente.

---

## 4. RIESGOS IDENTIFICADOS

### RIESGO-01 — Bypass de tenant cuando `school_id` claim está ausente
**Severidad: MEDIA**  
**Categoría: Aislamiento de datos**

El predicado `_tenantId == null || e.SchoolId == _tenantId` hace que **cualquier usuario sin claim `school_id`** vea TODOS los registros de TODAS las instituciones, exactamente igual que un superadmin.

**Escenario de riesgo:** Un usuario con una cookie creada antes de que se agregara el claim `school_id` (cookies viejas pre-migración) tendría `_tenantId = null` y vería datos de todas las instituciones.

**Mitigación recomendada:**
```csharp
// En SchoolDbContextTenantFilters.cs — reemplazar el predicado por:
modelBuilder.Entity<User>().HasQueryFilter(e =>
    _tenantId == null && _isSuperAdmin ||   // superadmin explícito
    e.SchoolId == _tenantId);               // usuario normal con tenant

// Requiere agregar: private bool _isSuperAdmin; en el constructor
// _isSuperAdmin = tenantProvider.Role == "superadmin";
```

**Mitigación alternativa (más simple):** Forzar logout de todas las sesiones activas después del deploy que agrega el claim. Esto es factible si la app tiene un endpoint de invalidación de sesiones o si se cambia el `Data Protection` key.

**Mitigación de corto plazo ya presente:** El middleware de autenticación Cookie rechazará cookies inválidas o expiradas. Las cookies tienen TTL de 24h, por lo que la ventana de exposición es de máximo 24h post-deploy.

---

### RIESGO-02 — Endpoints de mantenimiento `[AllowAnonymous]` en producción
**Severidad: MEDIA**  
**Categoría: Seguridad de acceso**

`AuthController` expone dos endpoints sin autenticación:

1. `GET /Auth/FixPasswords` — Itera todos los usuarios y re-hashea contraseñas. Aunque no devuelve contraseñas, confirma qué usuarios tienen contraseñas no hasheadas y modifica datos sin autorización.
2. `GET /api/auth/create-superadmin` — Crea el superadmin inicial. Ya está protegido con `AnyAsync(u => u.Role == "superadmin")` pero revela si existe un superadmin.

**Recomendación:** Remover `FixPasswords` del código de producción. Ya no es necesario si todas las contraseñas están hasheadas. `CreateSuperAdmin` debería convertirse en un script de inicialización offline (CLI arg `--create-superadmin`) en lugar de endpoint HTTP.

---

### RIESGO-03 — Sin mecanismo de revocación de tokens Bearer
**Severidad: BAJA**  
**Categoría: Seguridad de sesión**

Los tokens HMAC no se pueden invalidar antes de su expiración (24h). Si un token es comprometido, el atacante tiene hasta 24h de acceso.

**Recomendación para producción:** Implementar una blacklist en Redis/DB o reducir el TTL a 1-2h con refresh tokens. Para el caso de uso actual (app de escaneo de carnets en red escolar), el riesgo es aceptable.

---

### RIESGO-04 — Clave secreta con fallback hardcoded en código fuente
**Severidad: BAJA**  
**Categoría: Gestión de secretos**

Tanto `AuthController.BuildApiToken` como `ApiBearerTokenMiddleware` tienen el fallback:
```csharp
?? "EduPlaner-ApiToken-2024-HmacSecretKey-Min32Chars!!"
```
Si `ApiToken:SecretKey` no está configurado en el entorno de producción, el sistema usa esta clave conocida (está en el repositorio público).

**Recomendación:** Eliminar el fallback hardcoded. Si la clave no está configurada, lanzar `InvalidOperationException` en el arranque:
```csharp
_secretKey = configuration["ApiToken:SecretKey"]
    ?? throw new InvalidOperationException("ApiToken:SecretKey no configurado");
```

---

### RIESGO-05 — `GetCurrentUserSchoolAsync()` hace consulta a DB
**Severidad: BAJA**  
**Categoría: Rendimiento**

`CurrentUserService.GetCurrentUserSchoolAsync()` ejecuta dos queries a DB (primero el usuario, luego la escuela). Se llama en `StudentActivityScoreService.SaveBulkFromNotasAsync` (ruta crítica de guardado de notas).

En contraste, `GetCurrentSchoolIdAsync()` es O(1) desde claims. La versión lenta se usa donde se necesita el objeto `School` completo, lo cual es legítimo, pero debería cachearse por request si se llama múltiples veces.

---

## 5. ANÁLISIS DE ESCALABILIDAD

### 5.1 Patrón de tenant

El patrón Shared DB / Shared Schema con discriminador `school_id` es correcto para el volumen esperado (10-100 instituciones). Las ventajas son:
- Un solo deployment.
- Mantenimiento de schema centralizado.
- Costos de infraestructura bajos.

Las limitaciones aparecen a >500 instituciones con alta concurrencia, momento en el que se debería considerar Shared DB / Separate Schema o Separate DB.

### 5.2 Índices de tenant

La aplicación tiene índices explícitos en `school_id` para las tablas principales (verificado en `SchoolDbContext.cs`):
- `IX_activities_school_id`
- Otros índices en tablas de alta consulta.

Esto es correcto. Sin índices en `school_id`, las queries con HasQueryFilter harían full-table scans en producción.

### 5.3 Concurrencia de EF Core DbContext

`DbContext` es `Scoped` (una instancia por request HTTP). No hay riesgo de compartir el tenant entre requests concurrentes. Correcto.

---

## 6. ANÁLISIS DE INTEGRIDAD DE BASE DE DATOS

### 6.1 Restricciones a nivel DB

La integridad multitenant depende principalmente de restricciones a nivel aplicación (EF Core filters + service filters). **No existen restricciones a nivel DB (CHECK constraints, Row-Level Security de PostgreSQL) que impidan que datos de una institución sean visibles desde otra a través de queries directas SQL.**

Esto es normal para el patrón elegido, pero significa que:
- Un DBA con acceso directo a la DB puede cruzar datos entre instituciones.
- Una inyección SQL exitosa (si existiera) no estaría contenida por el modelo de tenant.

**Recomendación avanzada (post-MVP):** Evaluar PostgreSQL Row-Level Security (RLS) como tercera capa de aislamiento para datos especialmente sensibles (calificaciones, pagos, datos personales).

### 6.2 Cascade deletes

Las relaciones FK con `OnDelete(DeleteBehavior.Cascade)` en la entidad `School` garantizan que al eliminar una institución se eliminen todos sus datos hijos. Correcto para el modelo multitenant.

---

## 7. ANÁLISIS DE CONTROLADORES

Los controladores siguen el patrón correcto: delegan el filtrado de datos a los servicios. La excepción positiva es `TeacherGradebookController` y similares, que también realizan validaciones de propiedad antes de operar.

**Observación:** No todos los controladores tienen atributos `[Authorize]` explícitos. Verificar que `app.UseAuthorization()` con la política de autorización global cubra todos los endpoints no marcados con `[AllowAnonymous]`. El orden en `Program.cs` es correcto: `UseAuthentication → ApiBearerTokenMiddleware → UseAuthorization`.

---

## 8. CHECKLIST DE PRODUCCIÓN

| Item | Estado | Prioridad |
|---|---|---|
| Claims school_id en cookie | ✅ | — |
| HasQueryFilter en entidades core | ✅ | — |
| Filtros explícitos en servicios principales | ✅ | — |
| HMAC Bearer token para móvil | ✅ | — |
| Rate limiting en API de escaneo | ✅ | — |
| Índices en school_id | ✅ | — |
| Invalidar sesiones pre-migración (riesgo null tenant) | ⚠️ | ALTA |
| Remover/proteger endpoint FixPasswords | ⚠️ | ALTA |
| Eliminar fallback hardcoded de ApiToken:SecretKey | ⚠️ | MEDIA |
| Agregar HasQueryFilter a 6 entidades: StudentActivityScore, SchoolIdCardSetting, TimeSlot, SchoolScheduleConfiguration, EmailJob, StudentPaymentAccess | ⚠️ | MEDIA |
| Documentar Area como catálogo global | 📝 | BAJA |
| Evaluar PostgreSQL RLS para datos sensibles | 📝 | BAJA (post-MVP) |
| Mecanismo de revocación de tokens Bearer | 📝 | BAJA (post-MVP) |

---

## 9. CONCLUSIÓN

La implementación multitenant de Eduplaner es **sólida en su núcleo**. La estrategia de doble capa (service filters + EF Core HasQueryFilter) proporciona defensa en profundidad y está correctamente conectada al ciclo de vida del HttpContext. El token Bearer HMAC es criptográficamente seguro y elimina la dependencia de DB en la validación de API móvil.

Los dos puntos críticos a resolver antes de producción son:
1. **Garantizar que no hayan sesiones activas sin claim `school_id`** (ventana de 24h post-deploy).
2. **Remover el endpoint `FixPasswords` de producción** (es código de migración que ya cumplió su función).

El resto son mejoras de hardening que pueden implementarse de forma incremental.

**Veredicto final: LISTO PARA PRODUCCIÓN con los ítems de prioridad ALTA resueltos.**

---

*Generado mediante análisis estático del código fuente. Cubre los commits `fd3c3c9`, `fa4f8a2`, `71a3905` en `main`.*
