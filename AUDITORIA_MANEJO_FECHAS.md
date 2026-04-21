# Auditoría técnica: manejo de fechas

**Sistema:** SchoolManager  
**Stack:** ASP.NET Core 8, Entity Framework Core 9, Razor Views, PostgreSQL  
**Fecha de auditoría:** 2026-02-10  
**Alcance:** Base de datos, EF/modelos, controladores/servicios, vistas Razor, configuración global.

---

## Resumen ejecutivo

El sistema **ya persiste fechas en UTC** en PostgreSQL mediante `timestamp with time zone` y un interceptor que normaliza a UTC antes de guardar. No hay mezcla de `timestamp` sin zona ni columnas de fecha guardadas como string. Se detectan **inconsistencias en presentación, binding y cultura** que pueden causar desplazamiento horario en la UI, reportes y en formularios con `datetime-local`. La criticidad global se considera **Media**, con puntos **Altos** en vistas y en el atributo de conversión que no modifica parámetros simples.

---

# FASE 1 — BASE DE DATOS

## 1.1 Columnas tipo fecha identificadas

| Tipo en PostgreSQL | Uso en el sistema | Cantidad (aprox.) |
|--------------------|-------------------|-------------------|
| **timestamp with time zone** | Todas las columnas DateTime / DateTime? de entidades (CreatedAt, UpdatedAt, StartDate, EndDate, PaymentDate, DueDate, Date, Timestamp, etc.) | >60 columnas |
| **date** | Solo fechas sin hora: `Attendance.Date` (DateOnly), `Student.BirthDate` (DateOnly?) | 2 columnas |

**Hallazgos:**

- No existe en el modelo ni en migraciones ninguna columna `timestamp` (sin “with time zone”).
- No hay columnas de fecha almacenadas como `character varying`, `text` ni tipos string.
- Todas las fechas con hora usan **timestamptz** de forma consistente.

## 1.2 Verificaciones

| Verificación | Resultado |
|--------------|-----------|
| ¿Mezcla timestamp vs timestamptz? | **No.** Solo `timestamp with time zone` para DateTime. |
| ¿Fechas guardadas como string? | **No.** |
| ¿Las fechas incluyen zona horaria? | **Sí.** timestamptz almacena en UTC internamente y permite conversión por zona. |
| ¿Base configurada en UTC? | PostgreSQL no fuerza una “zona de la BD”; el valor almacenado en timestamptz es en UTC. **Correcto.** |

## 1.3 Conclusiones Fase 1

- **Almacenamiento:** El sistema guarda fechas con hora **en UTC** (EF escribe UTC gracias al interceptor).
- **Riesgo de desplazamiento en BD:** **Bajo.** Tipo único y sin strings.
- **Valores inconsistentes:** No detectables por esquema; posibles solo si hubo datos cargados fuera del interceptor antes de su implementación (recomendable script de verificación puntual si hay datos legacy).

---

# FASE 2 — ENTITY FRAMEWORK Y MODELOS

## 2.1 Entidades con tipos fecha

- **DateTime / DateTime?:** Presente en la mayoría de entidades (AcademicYear, Activity, Attendance, AuditLog, DisciplineReport, Message, Payment, Prematriculation, PrematriculationPeriod, User, Student, etc.). Uso coherente con `timestamp with time zone`.
- **DateOnly / DateOnly?:**  
  - `Attendance.Date` → `date`  
  - `Student.BirthDate` → `birth_date` tipo `date`  
  Sin zona horaria; correcto para “solo fecha”.

No se usa **DateTimeOffset** en modelos de dominio; solo en `AuthService` para `ExpiresUtc` de cookies (correcto).

## 2.2 Configuración EF

- **SchoolDbContext.ConfigureDateTimeHandling:** Todas las propiedades `DateTime`/`DateTime?` se mapean a `timestamp with time zone`. Coherente con la BD.
- **DateTimeInterceptor (ISaveChangesInterceptor):** Convierte a UTC todo `DateTime`/`DateTime?` con `Kind != Utc` antes de persistir (Local o Unspecified → UTC). **Correcto y crítico para la estrategia UTC.**

## 2.3 Uso de DateTime.Now / DateTime.UtcNow en código

- **DateTime.UtcNow:** Usado de forma consistente en servicios para CreatedAt, UpdatedAt, fechas de pago, etc. (UserService, PrematriculationService, PaymentService, AuthService, etc.). **Correcto.**
- **DateTime.Now:** No se encontró en el backend (.cs). Solo en vistas (ver Fase 4).
- **DateTime.Today:** No usado en backend.
- **SpecifyKind / ToUniversalTime:** Usado en interceptor, atributo de conversión, DateTimeHomologationService, DisciplineReportController, OrientationReportController, TrimesterService, etc., asumiendo “Unspecified/Local = hora local del usuario” antes de convertir a UTC. **Coherente con la estrategia.**

## 2.4 Riesgos en modelos/EF

| Riesgo | Nivel | Notas |
|--------|--------|--------|
| Pérdida de zona horaria en persistencia | **Bajo** | Interceptor normaliza a UTC. |
| Conversión implícita incorrecta C# ↔ DB | **Bajo** | Tipos alineados (DateTime ↔ timestamptz, DateOnly ↔ date). |
| Valores por defecto en entidades | **Bajo** | Algunos modelos usan `= DateTime.UtcNow` (ScanLog, StudentIdCard, EmailConfiguration, etc.); correcto. |

---

# FASE 3 — CONTROLADORES Y SERVICIOS

## 3.1 Recepción de fechas desde formularios

- **Formularios con `datetime-local`:**  
  - Activity (DueDate), PrematriculationPeriod (StartDate, EndDate), Payment (PaymentDate).  
  - El navegador envía cadena sin “Z” (ej. `yyyy-MM-ddTHH:mm`). El model binder de ASP.NET Core rellena `DateTime` con `Kind = Unspecified`.  
  - **DateTimeInterceptor** convierte a UTC en `SaveChanges`. **Correcto** si se asume que el usuario introduce hora local.
- **Formularios con `type="date"`:**  
  - User (DateOfBirth), Student (BirthDate), Attendance (Date), filtros en Payment/Reports, AcademicCatalog (trimestres).  
  - DateOnly/DateTime según modelo; para solo-fecha no hay problema de zona.
- **UserController:** Asigna `DateOfBirth = model.DateOfBirth?.ToUniversalTime()`. Si el binding trae Unspecified, tratarlo como Local antes de ToUniversalTime sería más seguro (p. ej. SpecifyKind(..., Local).ToUniversalTime()).
- **DisciplineReportController / OrientationReportController:** Construyen `Date` con `DateTime.SpecifyKind(DateTime.Parse($"{date} {hora}"), DateTimeKind.Local).ToUniversalTime()`. **Correcto** para entrada local.
- **AcademicCatalogController:** Recibe StartDate/EndDate como string, hace TryParse y `SpecifyKind(..., Unspecified).ToUniversalTime()`. Funcional pero ambiguo; preferible especificar que la entrada es “local” (p. ej. SpecifyKind Local) para evitar suposiciones del servidor.
- **PaymentController:** Si PaymentDate viene inválida, se reemplaza por `DateTime.UtcNow`. Correcto como fallback.

## 3.2 Devolución de fechas en APIs / JSON

- **AddControllers() + AddJsonOptions:** Se registran `DateTimeJsonConverter` y `NullableDateTimeJsonConverter` (definidos en `DateTimeMiddleware.cs`).
- **Escritura JSON:** `value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")` → **ISO 8601 con Z (UTC).** Correcto.
- **Lectura JSON:** TryParse del string y luego SpecifyKind(Local).ToUniversalTime(). Acepta ISO 8601; correcto para APIs que envían UTC.

## 3.3 Validación de cultura

- No hay configuración explícita de **CultureInfo** ni **RequestCulture** en `Program.cs`.
- **StudentReportService** usa `new CultureInfo("es-ES")` para nombres de mes (ToString("MMMM")). Punto aislado y correcto.
- El resto del sistema depende de la cultura del hilo/servidor (por defecto del SO o del host). **Riesgo:** en servidores en UTC o en otro locale, el parseo de fechas desde formularios o reportes puede variar.

## 3.4 Resumen Fase 3

- Persistencia y APIs JSON están alineadas con UTC e ISO 8601.
- Falta una cultura explícita para el flujo web (p. ej. es-PA o es-ES) y claridad en qué zona se asume en los pocos puntos que parsean strings manualmente.

---

# FASE 4 — RAZOR VIEWS

## 4.1 Inputs tipo date / datetime-local

| Vista | Campo | Tipo input | Observación |
|-------|--------|------------|-------------|
| User/Create, Edit | DateOfBirth | `type="date"` | Correcto (solo fecha). |
| Student/Create, Edit | BirthDate | `type="date"` | Correcto (DateOnly). |
| Attendance/Create, Edit | Date | `type="date"` | Correcto. |
| Activity/Create, Edit | DueDate | `type="datetime-local"` | Envía hora local; backend convierte con interceptor. |
| PrematriculationPeriod/Create, Edit | StartDate, EndDate | `type="datetime-local"` | Mismo caso. |
| Payment/Register | PaymentDate | `type="datetime-local"` | Ver riesgo siguiente. |
| Payment/Reports | startDate, endDate | `type="date"` | Correcto. |
| AcademicCatalog, TeacherGradebookDuplicate | Varios filtros/trimestres | `type="date"` | Correcto. |

## 4.2 Valor por defecto en Payment/Register.cshtml

```html
value="@(Model.PaymentDate != default(DateTime) && Model.PaymentDate.Year >= 2000 
    ? Model.PaymentDate.ToString("yyyy-MM-ddTHH:mm") 
    : DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm"))"
```

- Si no hay fecha válida se muestra **UTC** en el `datetime-local`. El usuario puede creer que es “hora local” y no cambiar la hora, guardando un valor desplazado.  
**Riesgo: Alto** en UX y posible error de hora registrada.

## 4.3 Uso de @Model.Fecha y ToString()

- Múltiples vistas muestran fechas con formatos fijos, por ejemplo:
  - `ToString("dd/MM/yyyy")`, `ToString("dd/MM/yyyy HH:mm")`, `ToString("dd/MM/yyyy HH:mm:ss")`.
- Los valores en BD están en UTC; se muestran **sin conversión a hora local** en la mayoría de vistas. Ejemplos: Activity/Details, DisciplineReport/Details, User/Details, Student/Details, Payment/Details, Prematriculation/Details, etc.
- **Riesgo:** El usuario en Panamá (UTC-5) verá la hora en UTC, no en su hora local. Ej.: “15:00” guardado (UTC) se muestra “15:00” cuando en Panamá serían “10:00”.

## 4.4 ToLocalTime() explícito

- **Payment/PayWithCard.cshtml:** `@selectedPrematriculation.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")` — **Correcto** para mostrar en hora del servidor (o del usuario si el servidor está en la misma zona). Es el patrón que debería replicarse donde se muestran fechas con hora.

## 4.5 DateTime.Now en vistas

Usado solo para **texto informativo** (no para persistir):

- User/Index, StudentAssignment/Index, AcademicCatalog/Index, TeacherGradebook/Index, TeacherGradebookDuplicate/Index: “Hoy es …” o similar.
- Payment/Receipt, Prematriculation/Certificate: “Generado el …”.

**Riesgo:** Si el servidor está en UTC, el usuario verá “hoy” y “generado el” en UTC. Si el servidor está en zona local (ej. América/Panama), verá hora local. **Inconsistencia según despliegue.** Nivel: **Medio** (solo presentación).

## 4.6 Formato y cultura del navegador

- Los inputs `date` y `datetime-local` envían siempre `yyyy-MM-dd` y `yyyy-MM-ddTHH:mm` (HTML5). No dependen de la cultura del navegador para el **valor enviado**.
- El **formato visual** del control sí puede variar por locale del navegador; no afecta al backend.
- Los `ToString("dd/MM/yyyy")` en servidor son fijos; no cambian con el navegador. Riesgo es **mostrar UTC como si fuera local**, no variación de formato por cultura.

---

# FASE 5 — CONFIGURACIÓN GLOBAL

## 5.1 Program.cs

- **Cultura:** No se configura `CultureInfo.DefaultThreadCurrentCulture`, `DefaultThreadCurrentUICulture` ni `RequestLocalizationOptions`. El sistema usa la cultura por defecto del proceso/servidor.
- **JSON:** AddControllers().AddJsonOptions con `DateTimeJsonConverter` y `NullableDateTimeJsonConverter`: serialización en **ISO 8601 con Z**. Correcto.
- **Filtro global:** DateTimeConversionAttribute convierte DateTime a UTC en **objetos complejos** (propiedades de ViewModels/DTOs); ver siguiente punto.

## 5.2 DateTimeConversionAttribute (ActionFilter)

- Recorre parámetros de la acción y, en **tipos complejos**, modifica propiedades DateTime/DateTime? a UTC con SetValue. **Efectivo** para DTOs y modelos de formularios.
- Para **parámetros simples** (ej. un único DateTime), el comentario indica “No podemos modificar directamente el valor”; solo se escribe en consola. Por tanto, **los parámetros simples no se convierten** por este atributo. El interceptor sí normaliza al guardar, pero si ese valor se usa para comparaciones o lógica antes de persistir, podría quedar en Unspecified/Local. **Riesgo: Medio.**

## 5.3 Resumen configuración global

- Serialización JSON: **estandarizada a ISO 8601 UTC.**
- Cultura: **no fijada**; posible variación entre entornos.
- Conversión en binding: **parcial** (solo propiedades de objetos, no parámetros simples).

---

# DIAGNÓSTICO Y RIESGOS CONSOLIDADOS

## Diagnóstico actual

| Capa | Estado | Comentario |
|------|--------|------------|
| Base de datos | Correcto | Solo timestamptz y date; sin strings de fecha. |
| EF / Modelos | Correcto | Interceptor UTC; tipos alineados con BD. |
| Servicios | Correcto | Uso consistente de UtcNow y ToUniversalTime donde aplica. |
| Controladores | Aceptable | Mayoría correcta; algunos puntos mejorables (especificar Local en Parse). |
| APIs / JSON | Correcto | ISO 8601 y UTC en serialización. |
| Vistas – inputs | Aceptable | Payment/Register valor por defecto en UTC en datetime-local. |
| Vistas – visualización | Inconsistente | Casi todas muestran UTC sin ToLocalTime(); solo PayWithCard convierte. |
| Cultura global | No definida | Sin cultura fija; parseo y “hoy” dependen del servidor. |
| Atributo conversión | Incompleto | No modifica parámetros simples. |

## Riesgos detectados

| # | Riesgo | Criticidad | Ubicación |
|---|--------|------------|-----------|
| 1 | Fechas con hora mostradas en UTC en vez de hora local del usuario | **Alto** | Todas las vistas que muestran DateTime con ToString sin ToLocalTime (o sin zona conocida). |
| 2 | Valor por defecto UTC en datetime-local (Payment/Register) induce a error de hora | **Alto** | Views/Payment/Register.cshtml |
| 3 | DateTimeConversionAttribute no convierte parámetros simples a UTC | **Medio** | Attributes/DateTimeConversionAttribute.cs |
| 4 | Uso de DateTime.Now en vistas para “hoy” / “generado el” depende de zona del servidor | **Medio** | Varias vistas (User/Index, Receipt, Certificate, etc.). |
| 5 | Cultura no configurada: parseo y formatos pueden variar por entorno | **Medio** | Program.cs |
| 6 | UserController DateOfBirth: ToUniversalTime() sobre Unspecified sin SpecifyKind | **Bajo** | Controllers/UserController.cs |

## Nivel de criticidad global

- **Crítico:** Ninguno (la persistencia en UTC está bien).  
- **Alto:** Visualización en UTC en UI (1) y valor por defecto en Payment/Register (2).  
- **Medio:** Atributo de conversión (3), DateTime.Now en vistas (4), cultura (5).  
- **Bajo:** DateOfBirth en UserController (6).

**Nivel global: Medio**, con impacto alto en experiencia de usuario y en riesgo de confusión de horas si no se corrige presentación y valor por defecto.

---

# RECOMENDACIONES Y PLAN DE CORRECCIÓN

## 1. Mantener UTC como estándar global

- **No cambiar** la estrategia de guardar en UTC en BD y en dominio.
- Mantener **DateTimeInterceptor** y **timestamptz**.
- Estandarizar: **almacenar y transmitir en UTC; convertir a hora local solo en presentación.**

## 2. Plan de corrección estandarizado

### 2.1 Configuración global (cultura y zona)

- En `Program.cs`, fijar cultura y zona para el flujo web, por ejemplo:
  - `es-PA` (o la cultura objetivo del negocio) para formatos y mensajes.
  - Opcionalmente, zona horaria por defecto (ej. `America/Panama`) para “hoy” y “generado el” si no se implementa zona por usuario.
- Usar `RequestLocalizationOptions` con un solo idioma/cultura si no hay multi-idioma.

### 2.2 Presentación en vistas (prioridad alta)

- Definir un helper o tag helper que, dado un `DateTime` (UTC), lo convierta a hora local del usuario (o del servidor si no hay perfil de zona) y lo formatee (ej. “dd/MM/yyyy HH:mm”).
- Reemplazar en todas las vistas los `@Model.Fecha.ToString("...")` por ese helper cuando la propiedad sea “fecha con hora” (CreatedAt, UpdatedAt, PaymentDate, DueDate, etc.).
- Mantener `DateOnly` y fechas “solo día” sin conversión de zona (solo formato).

### 2.3 Payment/Register valor por defecto (prioridad alta)

- No usar `DateTime.UtcNow` para el valor por defecto del `datetime-local`.
- Opciones:
  - Dejar el campo sin `value` (navegador suele mostrar hora local), o
  - Calcular “ahora” en la zona del usuario/servidor y formatear en `yyyy-MM-ddTHH:mm` para ese momento en **hora local** (p. ej. con TimeZoneInfo o NodaTime según stack).

### 2.4 DateTimeConversionAttribute (prioridad media)

- Mejorar el atributo para que también convierta **parámetros simples** DateTime/DateTime? a UTC (por reflexión sobre los argumentos por nombre y reasignación si el parámetro es by-ref, o documentar que los parámetros simples deben normalizarse en el propio action con un helper “ToUtc”).

### 2.5 “Hoy” y “Generado el” en vistas (prioridad media)

- Sustituir `DateTime.Now` por una función que devuelva “ahora” en la zona deseada (ej. servidor o usuario), o por `DateTime.UtcNow` y luego convertir con la misma lógica que el resto de fechas mostradas. Así “hoy” y “generado el” serán coherentes con el resto de la UI.

### 2.6 Controladores (prioridad baja)

- UserController: al asignar `DateOfBirth`, usar `SpecifyKind(..., Local).ToUniversalTime()` (o zona explícita) si el binding trae Unspecified.
- AcademicCatalogController: al parsear StartDate/EndDate, usar SpecifyKind(Local) si la intención es “fecha local del usuario”.

### 2.7 Estandarización en servicios

- Donde se reciban fechas “de usuario” (formularios, APIs) sin zona, asumir de forma explícita “hora local” (o zona configurada) y convertir una sola vez a UTC antes de asignar a entidades. Reutilizar un helper (ej. `IDateTimeHomologationService` o similar) para no duplicar lógica.

---

# ESTRATEGIA DE MIGRACIÓN A UTC (DATOS HISTÓRICOS)

El sistema **ya persiste en UTC**. No se requiere migración de datos existentes por cambio de tipo ni de zona en BD.

Si en el pasado hubo datos insertados **sin** el interceptor (p. ej. scripts SQL o otra app), se puede:

1. **Identificar filas sospechosas:** Por ejemplo, fechas con hora fuera de rango esperado para la zona (ej. CreatedAt a las 03:00 en Panamá cuando la actividad es diurna).
2. **Script de corrección (solo si se confirma que están en local):**  
   - Leer valor actual (interpretado como local).  
   - Convertir local → UTC (con TimeZoneInfo de la zona del colegio).  
   - Actualizar la columna.  
   Hacer backup y ejecutar en ventana de mantenimiento.
3. **No reescribir** columnas que ya estén en UTC; el interceptor desde su introducción ya ha estado guardando en UTC.

---

# ANEXO — ARCHIVOS RELEVANTES

| Componente | Archivo(s) |
|------------|------------|
| Interceptor UTC | Models/DateTimeInterceptor.cs |
| Configuración EF fechas | Models/SchoolDbContext.cs (ConfigureDateTimeHandling) |
| Convertidores JSON | Middleware/DateTimeMiddleware.cs (DateTimeJsonConverter, NullableDateTimeJsonConverter) |
| Filtro conversión | Attributes/DateTimeConversionAttribute.cs |
| Modelo snapshot BD | Migrations/SchoolDbContextModelSnapshot.cs |
| Cultura / JSON | Program.cs |
| Vistas con datetime-local / valor UTC | Views/Payment/Register.cshtml |
| Vista con ToLocalTime | Views/Payment/PayWithCard.cshtml |
| Servicio homologación fechas | Services/Implementations/DateTimeHomologationService.cs, GlobalDateTimeService.cs |

---

*Fin del reporte de auditoría.*
