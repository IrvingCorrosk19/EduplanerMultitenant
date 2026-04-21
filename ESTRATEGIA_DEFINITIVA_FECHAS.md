# Estrategia definitiva de manejo de fechas — SchoolManager

**Clasificación:** Estándar corporativo  
**Stack:** ASP.NET Core 8, EF Core 9, Razor, PostgreSQL (timestamptz)  
**Objetivo:** Estandarización total para certificación en entornos críticos (banca/gobierno).

---

# FASE 1 — DIAGNÓSTICO ARQUITECTÓNICO

## 1.1 Verificación de persistencia y backend

| Requisito | Estado | Evidencia en código |
|-----------|--------|---------------------|
| Todas las fechas con hora se guardan en UTC | **Cumple** | `DateTimeInterceptor` convierte todo `DateTime`/`DateTime?` a UTC en `SavingChanges` antes de persistir. |
| Uso correcto de `timestamp with time zone` | **Cumple** | `SchoolDbContext.ConfigureDateTimeHandling` asigna `HasColumnType("timestamp with time zone")` a todas las propiedades DateTime. Migraciones y snapshot solo usan `timestamp with time zone` y `date`. |
| No existen conversiones dobles | **Cumple** | Servicios usan `DateTime.UtcNow` o asignan valores que el interceptor normaliza una sola vez; no se aplica `ToUniversalTime()` antes de guardar y luego el interceptor otra vez sobre el mismo valor. |
| No hay `DateTime.Now` en backend (.cs) | **Cumple** | Búsqueda en `*.cs` (excl. vistas): no hay `DateTime.Now`. Solo `DateTime.UtcNow` en servicios. |

**Excepción conocida:** En vistas (.cshtml) sí existe `DateTime.Now` para “Hoy es…”, “Generado el…”, `value="@DateTime.Now"` en inputs; se corrige con la política de presentación.

## 1.2 Puntos donde se muestran fechas en UTC sin convertir

Todos los siguientes muestran el valor UTC con `ToString(...)` sin conversión a zona de presentación:

| Archivo | Línea aprox. | Campo / Uso |
|---------|----------------|-------------|
| User/Details.cshtml | 53, 56 | LastLogin, CreatedAt |
| User/Index.cshtml | 600 | DateOfBirth (solo fecha, OK) |
| Student/Details.cshtml | 44 | CreatedAt |
| Subject/Details.cshtml | 44 | CreatedAt |
| School/Details.cshtml, Index.cshtml | 47, 85 | CreatedAt |
| SecuritySetting/Details.cshtml | 56 | CreatedAt |
| Activity/Details.cshtml, Index.cshtml | 39, 72 | DueDate |
| DisciplineReport/Details.cshtml, Index.cshtml | 26, 65 | Date, fechas listado |
| Attendance/Details.cshtml | 48 | CreatedAt |
| AuditLog/Details.cshtml, Index.cshtml | 29, 52-53 | Timestamp |
| Prematriculation/Details.cshtml, Index, MyPrematriculations, ByGroup | 103, 108, 114, 112, 137, 64 | CreatedAt, PaymentDate, MatriculationDate |
| PrematriculationPeriod/Index.cshtml | 62-63 | StartDate, EndDate |
| Payment/Details, Index, Register, Receipt, PayFromPortal, ByGroup, MyPayments, ReportResults | Varias | PaymentDate, ConfirmedAt, CreatedAt |
| Payment/PayWithCard.cshtml | 81 | **Excepción:** usa `ToLocalTime()` correctamente. |
| SuperAdmin: SystemStats, ListSchools, ListAdmins, ActivityLog | Varias | FechaUltimaActividad, CreatedAt, Timestamp |
| StudentReport/Index.cshtml | 423 | CreatedAt |

## 1.3 Uso de `ToString` sin conversión de zona

- Cualquier `@Model.CreatedAt.ToString("dd/MM/yyyy HH:mm")` (o similar) sobre propiedades que en BD son UTC se considera **prohibido** por la nueva política: debe usarse el helper de presentación que convierte UTC → zona de visualización antes de formatear.
- `DateOnly` / fechas solo día (BirthDate, Attendance.Date, filtros `type="date"`): no requieren conversión de zona; solo formato consistente.

## 1.4 Valores incorrectos enviados a `datetime-local`

| Archivo | Problema |
|---------|----------|
| **Views/Payment/Register.cshtml** | `value="... DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm")"` — El valor por defecto del input es UTC; el usuario espera hora local. Riesgo de registrar hora equivocada. |
| **Views/Activity/Create.cshtml, Edit.cshtml** | `asp-for="DueDate"` sin `value`: si el modelo trae UTC, el tag helper puede generar valor en UTC. Debe enviarse hora en zona de presentación para el input. |
| **Views/PrematriculationPeriod/Create, Edit** | `asp-for="StartDate"`, `asp-for="EndDate"`: mismo criterio; valor debe ser “local” para el usuario. |

Conclusión: el único caso explícito de “valor incorrecto” detectado es el valor por defecto en **Payment/Register.cshtml**. El resto depende del binding; el interceptor garantiza que lo que se persiste sea UTC.

---

# FASE 2 — ESTÁNDAR CORPORATIVO OFICIAL

## 2.1 Política de persistencia

| Regla | Decisión |
|-------|----------|
| ¿Siempre UTC? | **Sí.** Toda fecha con hora se persiste en UTC. Sin excepciones. |
| ¿Se permite `DateTimeOffset`? | **No** en el dominio ni en tablas. Solo `DateTime`/`DateTime?` (y `DateOnly`/`DateOnly?` donde corresponda). Razón: PostgreSQL timestamptz + interceptor ya garantizan UTC; `DateTimeOffset` añade complejidad sin beneficio en este stack. Uso permitido solo en integraciones (ej. cookies `ExpiresUtc`). |
| Tipo en BD | **Siempre** `timestamp with time zone` para DateTime; `date` para DateOnly. |
| Responsable | `DateTimeInterceptor` es el único punto que normaliza a UTC antes de `SaveChanges`. Los servicios pueden seguir usando `DateTime.UtcNow` o valores Unspecified/Local; el interceptor los convierte. |

## 2.2 Política de transporte (APIs / JSON)

| Regla | Decisión |
|-------|----------|
| Formato | **ISO 8601** obligatorio. |
| Zona en serialización | **Siempre con Z** (UTC). Formato: `yyyy-MM-ddTHH:mm:ss.fffZ`. |
| Validación en APIs | Aceptar solo cadenas que parseen a fecha válida; rechazar formatos ambiguos. En lectura JSON, interpretar sin Z como hora local del cliente y convertir a UTC internamente (comportamiento actual de los convertidores). |
| Responsable | `DateTimeJsonConverter` y `NullableDateTimeJsonConverter` en `AddJsonOptions`; no añadir otros convertidores de fecha que omitan Z. |

## 2.3 Política de presentación

| Regla | Decisión |
|-------|----------|
| Conversión a zona del usuario | **Obligatoria** para cualquier fecha con hora mostrada en UI. La zona por defecto es la configurada en aplicación (ej. `America/Panama`); en futuro multi-tenant puede ser por escuela/país. |
| Helper global para Razor | **Obligatorio.** Prohibido usar `Model.Fecha.ToString(...)` para propiedades DateTime que representen instante (CreatedAt, UpdatedAt, PaymentDate, DueDate, etc.). Debe usarse el helper inyectado (ej. `ITimeZoneService.ToLocalDisplayString(utc, format)`). |
| Prohibición | **Prohibido** en vistas: `@(Model.CreatedAt.ToString("dd/MM/yyyy HH:mm"))` y análogos para fechas con hora. Permitido solo para `DateOnly` o para fechas que ya estén en “hora de presentación” por un helper. |
| Valores por defecto en `datetime-local` | Deben ser “ahora” en la **zona de presentación**, no UTC. Ej.: `value="@TimeZoneService.GetNowForDisplayInput()"`. |
| “Hoy es” / “Generado el” | Deben usar la misma zona (servicio de presentación), no `DateTime.Now` del servidor. |

## 2.4 Cultura

| Regla | Decisión |
|-------|----------|
| Cultura oficial del sistema | **es-PA** (Español - Panamá). Definida explícitamente en `Program.cs` para el flujo web. |
| Evitar dependencia del servidor | Fijar `CultureInfo.DefaultThreadCurrentCulture` y `DefaultThreadCurrentUICulture` (y/o `RequestLocalizationOptions` con una cultura por defecto) para que formatos y parsing no dependan del locale del servidor. |
| Nombres de mes/día | Usar la cultura oficial en cualquier `ToString("MMMM", culture)` u equivalente. |

---

# FASE 3 — PROPUESTA DE IMPLEMENTACIÓN

## 3.1 Componentes a crear o reutilizar

| Componente | Descripción | Ubicación |
|------------|-------------|-----------|
| **ITimeZoneService** | Contrato: zona de presentación, conversión UTC → local, “ahora” para UI, formato para `datetime-local`. | `Services/Interfaces/ITimeZoneService.cs` |
| **TimeZoneService** | Implementación: lee `TimeZoneId` de configuración (default `America/Panama`), expone `ToLocal(DateTime utc)`, `ToLocalDisplayString(DateTime? utc, string format)`, `GetNowForDisplayInput()`. | `Services/Implementations/TimeZoneService.cs` |
| **Extensiones DateTime** | Opcional: `dateTime.ToLocalIn(TimeZoneInfo).ToString(format)` para uso en servicios que necesiten formatear en zona sin inyectar el servicio. | `Extensions/DateTimeDisplayExtensions.cs` (opcional) |
| **Middleware** | No obligatorio para el estándar mínimo. En multi-tenant futuro, un middleware podría establecer la zona por escuela en `HttpContext.Items` y el servicio leerla. | Fase posterior |
| **Configuración** | Sección `DateTime` en `appsettings.json`: `DisplayTimeZoneId: "America/Panama"`. | `appsettings.json` |

## 3.2 Refactor mínimo

| Área | Cambio |
|------|--------|
| **Program.cs** | 1) Cultura: `CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("es-PA")` (y UICulture). 2) Registro `ITimeZoneService` → `TimeZoneService`. 3) Sin cambios en JSON ni en interceptor. |
| **Vistas Razor** | 1) `_ViewImports.cshtml`: `@inject ITimeZoneService TimeZoneService`. 2) Sustituir todo `@Model.CreatedAt.ToString(...)` (y análogos para fechas con hora) por `@TimeZoneService.ToLocalDisplayString(Model.CreatedAt, "dd/MM/yyyy HH:mm")`. 3) Sustituir `DateTime.Now` en “Hoy es” / “Generado el” por `TimeZoneService.GetTodayString()` o equivalente. 4) Valor por defecto en Payment/Register: `TimeZoneService.GetNowForDisplayInput()`. 5) Inputs `datetime-local`: donde se fije `value`, usar formato en zona de presentación (desde controlador o desde servicio en vista). |
| **Controladores** | Donde se pase un `DateTime` a la vista para rellenar `datetime-local`, convertir con `ITimeZoneService.ToLocal(...)` y formatear `yyyy-MM-ddTHH:mm` antes de poner en ViewBag/ViewModel. Opcional: ViewModel con propiedad ya formateada. |
| **Servicios** | Sin cambio de lógica de persistencia. Opcional: usar `ITimeZoneService` en servicios que generen texto para emails o reportes con fechas, para que aparezcan en zona correcta. |

## 3.3 Estrategia escalable (multi-país / SaaS multi-tenant)

- **Fase actual:** Una sola zona por aplicación (`DisplayTimeZoneId` en appsettings).
- **Fase siguiente:** Tabla o configuración por tenant (escuela/país) con `TimeZoneId`. En cada request, middleware o filtro determina el tenant y pone en `HttpContext.Items["DisplayTimeZoneId"]` el id de zona. `TimeZoneService` lee de `IHttpContextAccessor` + fallback a configuración global.
- **APIs públicas:** Aceptar header opcional `X-Timezone-Id` (ej. `America/Panama`) para respuestas que incluyan fechas en “local”; por defecto seguir devolviendo UTC (Z) en JSON.

---

# FASE 4 — PLAN DE EJECUCIÓN

## 4.1 Orden recomendado de implementación

| Paso | Acción | Riesgo | Impacto en datos históricos |
|------|--------|--------|-----------------------------|
| 1 | Añadir sección `DateTime` en `appsettings.json` y crear `ITimeZoneService` + `TimeZoneService`. | Bajo | Ninguno. |
| 2 | Registrar `ITimeZoneService` y configurar cultura `es-PA` en `Program.cs`. | Bajo | Ninguno. Solo afecta formato y parsing en el hilo. |
| 3 | Añadir `@inject ITimeZoneService TimeZoneService` en `_ViewImports.cshtml`. | Bajo | Ninguno. |
| 4 | Corregir **Payment/Register.cshtml**: valor por defecto con `TimeZoneService.GetNowForDisplayInput()`. | Bajo | Ninguno. Solo valor inicial del input. |
| 5 | Corregir **DateTimeConversionAttribute**: convertir también parámetros simples (modificar `ActionArguments` por clave). | Medio | Ninguno. Evita que un parámetro simple DateTime llegue sin normalizar a UTC. |
| 6 | Sustituir en vistas todas las muestras de fechas con hora por `TimeZoneService.ToLocalDisplayString(...)` y “Hoy/Generado” por `TimeZoneService.GetTodayString()` / equivalente. | Medio | Ninguno. Solo presentación. |
| 7 | Ajustar controladores que pasan fechas a vistas para `datetime-local` (Activity, PrematriculationPeriod): enviar valor en zona de presentación. | Bajo | Ninguno. |

## 4.2 Nivel de riesgo por cambio

| Cambio | Riesgo | Mitigación |
|--------|--------|------------|
| Nuevo servicio y cultura | Bajo | Pruebas de humo; reversión trivial. |
| Cambio en vistas (ToString → helper) | Medio | Revisión vista a vista; pruebas de regresión por pantalla. |
| Cambio en DateTimeConversionAttribute | Medio | Pruebas de formularios con fechas (Activity, Payment, PrematriculationPeriod). |
| Valor por defecto Register | Bajo | Verificar en navegador que el valor mostrado coincide con hora local esperada. |

## 4.3 Archivos que deben modificarse

| Archivo | Modificación |
|---------|--------------|
| `appsettings.json` | Añadir sección `DateTime: { "DisplayTimeZoneId": "America/Panama" }`. |
| `Program.cs` | Cultura es-PA; registro `AddScoped<ITimeZoneService, TimeZoneService>()`. |
| `Services/Interfaces/ITimeZoneService.cs` | **Nuevo.** |
| `Services/Implementations/TimeZoneService.cs` | **Nuevo.** |
| `Views/_ViewImports.cshtml` | `@inject SchoolManager.Services.Interfaces.ITimeZoneService TimeZoneService`. |
| `Views/Payment/Register.cshtml` | Valor por defecto con `TimeZoneService.GetNowForDisplayInput()`. |
| `Views/User/Details.cshtml` | LastLogin, CreatedAt con `TimeZoneService.ToLocalDisplayString`. |
| `Views/User/Index.cshtml` | Reemplazar `DateTime.Now` “Hoy es” por servicio. |
| `Attributes/DateTimeConversionAttribute.cs` | Convertir parámetros simples y reasignar en `ActionArguments`. |
| Resto de vistas listadas en 1.2 | Sustituir `ToString` de fechas con hora por `TimeZoneService.ToLocalDisplayString`; “Hoy/Generado” por método del servicio. |

Lista completa de vistas a tocar (para sustitución sistemática):

- User: Details, Index (DateTime.Now).
- Student: Details.
- Subject: Details.
- School: Details, Index.
- SecuritySetting: Details.
- Activity: Details, Index.
- DisciplineReport: Details, Index.
- Attendance: Details.
- AuditLog: Details, Index.
- Prematriculation: Details, Index, MyPrematriculations, ByGroup, Certificate.
- PrematriculationPeriod: Index.
- Payment: Details, Index, Register, Receipt, PayWithCard, PayFromPortal, ByGroup, MyPayments, ReportResults.
- SuperAdmin: SystemStats, ListSchools, ListAdmins, ActivityLog, SystemSettings.
- StudentReport: Index.
- StudentAssignment: Index (DateTime.Now).
- SubjectAssignment: Index (DateTime.Now).
- TeacherGradebook: Index (DateTime.Now + value fecha).
- TeacherGradebookDuplicate: Index (DateTime.Now + value fecha).
- Shared: _SuperAdminLayout, _MainLayout, _AdminLayout (solo `DateTime.Now.Year` puede quedar o pasarse por servicio “año actual” si se desea homogeneidad).

---

# IMPLEMENTACIÓN INCLUIDA

Se han implementado en el código:

1. **ITimeZoneService** e **implementación TimeZoneService** con configuración `DateTime:DisplayTimeZoneId`.
2. **Program.cs:** cultura `es-PA` y registro de `ITimeZoneService`.
3. **appsettings.json:** sección `DateTime`.
4. **_ViewImports.cshtml:** inyección de `TimeZoneService`.
5. **Payment/Register.cshtml:** valor por defecto con `TimeZoneService.GetNowForDisplayInput()`.
6. **DateTimeConversionAttribute:** conversión de parámetros simples a UTC y reasignación en `ActionArguments`.
7. **Una vista de ejemplo (User/Details.cshtml):** uso de `TimeZoneService.ToLocalDisplayString` para fechas con hora.

**Estado:** Plan de vistas ejecutado (todas las vistas listadas actualizadas con `TimeZoneService`). Excepciones dejadas a propósito: `DateTime.Now.Year` en layouts y Login (año igual en todas las zonas); campos `DateOnly` (BirthDate, Attendance.Date) sin conversión de zona; `AcademicCatalog`: año escolar con `DateTime.Now.Year` sin cambio.
