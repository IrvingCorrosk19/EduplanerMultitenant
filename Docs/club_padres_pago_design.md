# Diseño técnico detallado: Módulo Club de Padres — Pagos (Carnet y Plataforma)

**Proyecto:** SchoolManager  
**Documento:** Diseño técnico para implementación por fases  
**Basado en:** `Docs/club_padres_pago_analysis.md`  
**Restricción:** Solo documentación; no implementar código, migraciones, controladores ni servicios.

---

## 1. Resumen de arquitectura

### 1.1 Módulo propuesto

El módulo **Club de Padres — Pagos** agrega:

- Un **nuevo rol** (`ClubParentsAdmin`) para usuarios que solo registran pagos de carnet y de acceso a plataforma.
- **Estados de carnet** (Pendiente → Pagado → Impreso → Entregado) y **estados de plataforma** (Pendiente → Activo), almacenados en una tabla nueva sin tocar `users` ni `student_id_cards`.
- **Alertas** a QL Services cuando un carnet pasa a Pagado (carnets pendientes de impresión).
- **Bloqueo de acceso** a contenido académico (notas, asignaciones, módulos) cuando el acceso a plataforma está en Pendiente.

El módulo es **solo de registro de pagos y estados**; no imprime carnets ni modifica datos académicos.

### 1.2 Por qué va separado

- **Responsabilidad única:** El Club de Padres no debe acceder a carnets (imprimir), ni a notas, ni a administración de usuarios. Separar el módulo permite controlar permisos por rutas y menú sin tocar controladores existentes.
- **Bajo impacto:** No se modifican `StudentIdCardService`, `PaymentController` (flujos actuales), ni `User`. Solo se añaden tabla, rol, controladores/servicios nuevos y puntos de validación de acceso a plataforma.
- **Evolución independiente:** Cambios en prematrícula, pagos contables o carnets no obligan a tocar la lógica del Club de Padres.

### 1.3 Por qué usar User como estudiante operativo

En el sistema actual:

- **Carnets** (`StudentIdCard`) referencian `users.id` (StudentId).
- **Prematrícula y pagos** usan `User` (student/estudiante).
- **Asignaciones, notas, horarios** usan `User` + `StudentAssignment` (grado, grupo).

La entidad **Student** (tabla `students`) se usa en `StudentController`/`StudentService` para otro flujo y no interviene en carnets ni en prematrícula. Por tanto, el módulo Club de Padres debe tomar como “estudiante” al **User con rol `student` o `estudiante`** y a **StudentAssignment** (activa) para grado y grupo. Así se mantiene coherencia con el resto del sistema sin duplicar ni mezclar modelos.

---

## 2. Diseño de base de datos

### 2.1 Tabla `student_payment_access`

Una sola tabla nueva; no se alteran `users`, `student_id_cards` ni `payments` en su estructura actual.

| Columna | Tipo (sugerido) | Nullable | Descripción |
|--------|------------------|----------|-------------|
| `id` | `uuid` (Guid) | NO | PK. Valor por defecto: `uuid_generate_v4()` o equivalente. |
| `student_id` | `uuid` | NO | FK a `users.id`. Estudiante (User con rol student/estudiante). |
| `school_id` | `uuid` | NO | FK a `schools.id`. Escuela; redundante pero útil para filtros y seguridad por tenant. |
| `carnet_status` | `varchar(20)` | NO | Valores: ver sección 2.5. |
| `platform_access_status` | `varchar(20)` | NO | Valores: ver sección 2.6. |
| `carnet_status_updated_at` | `timestamp with time zone` | SÍ | Última actualización de estado de carnet. |
| `platform_status_updated_at` | `timestamp with time zone` | SÍ | Última actualización de estado de plataforma. |
| `carnet_updated_by_user_id` | `uuid` | SÍ | FK a `users.id`. Usuario que realizó el último cambio de estado de carnet (Club o QL). |
| `platform_updated_by_user_id` | `uuid` | SÍ | FK a `users.id`. Usuario que activó la plataforma (típicamente Club). |
| `created_at` | `timestamp with time zone` | NO | Alta del registro. Default: `CURRENT_TIMESTAMP`. |
| `updated_at` | `timestamp with time zone` | SÍ | Última modificación del registro. |

### 2.2 Clave primaria y unicidad

- **PK:** `id` (uuid).
- **Unicidad:** Un único registro por estudiante por escuela. Restricción única recomendada: `UNIQUE (student_id, school_id)` (o `UNIQUE (student_id)` si un estudiante solo pertenece a una escuela).

### 2.3 Claves foráneas

| FK | Referencia | Comportamiento recomendado |
|----|------------|----------------------------|
| `student_id` | `users(id)` | `ON DELETE RESTRICT` o `CASCADE` según política (evitar borrar User con registros aquí). |
| `school_id` | `schools(id)` | `ON DELETE RESTRICT`. |
| `carnet_updated_by_user_id` | `users(id)` | `ON DELETE SET NULL`. |
| `platform_updated_by_user_id` | `users(id)` | `ON DELETE SET NULL`. |

### 2.4 Índices recomendados

| Índice | Columnas | Uso |
|--------|----------|-----|
| PK | `id` | Por defecto. |
| `IX_student_payment_access_student_id` | `student_id` | Búsqueda por estudiante (validación de acceso, detalle). |
| `IX_student_payment_access_school_id` | `school_id` | Listados por escuela (Club de Padres, QL). |
| `IX_student_payment_access_carnet_school` | `carnet_status`, `school_id` | Listar “carnets pagados pendientes de impresión” (QL). |
| Unique | `(student_id, school_id)` | Garantizar un registro por estudiante por escuela. |

### 2.5 Valores permitidos: `carnet_status`

| Valor | Descripción |
|-------|--------------|
| `Pendiente` | Aún no pagado. Estado inicial o por defecto cuando no existe registro. |
| `Pagado` | Pago registrado por Club de Padres; pendiente de impresión/entrega. |
| `Impreso` | QL Services marcó como impreso. |
| `Entregado` | QL Services marcó como entregado al estudiante. |

Solo estos cuatro valores; validación en aplicación y opcionalmente `CHECK` en BD.

### 2.6 Valores permitidos: `platform_access_status`

| Valor | Descripción |
|-------|--------------|
| `Pendiente` | Acceso a plataforma no pagado; estudiante sin acceso a notas/asignaciones/módulos. |
| `Activo` | Acceso pagado; estudiante con acceso completo según su rol. |

Solo estos dos valores; validación en aplicación y opcionalmente `CHECK` en BD.

### 2.7 Valores por defecto al insertar

- `carnet_status`: `'Pendiente'`.
- `platform_access_status`: `'Pendiente'`.
- `created_at`: `CURRENT_TIMESTAMP`.
- Resto de campos de auditoría: según política (null o usuario actual).

### 2.8 Semántica “sin registro”

Si no existe fila en `student_payment_access` para un `(student_id, school_id)`:

- Tratar **carnet_status** como `Pendiente`.
- Tratar **platform_access_status** como `Pendiente`.

La lógica de aplicación debe contemplar “crear registro bajo demanda” (ej. al marcar Pagado o Activo) o crearlo en otro proceso (ej. al matricular). Esto se define en reglas de negocio y plan de implementación.

---

## 3. Reglas de negocio

### 3.1 Carnet: flujo y transiciones permitidas

Flujo completo: **Pendiente → Pagado → Impreso → Entregado**.

| Transición | Rol autorizado | Quién la ejecuta |
|------------|----------------|------------------|
| Pendiente → Pagado | ClubParentsAdmin | Club de Padres (registro de pago). |
| Pagado → Impreso | QL Services (y opcionalmente Admin) | QL Services. |
| Impreso → Entregado | QL Services (y opcionalmente Admin) | QL Services. |

**Restricciones:**

- **ClubParentsAdmin** solo puede hacer **Pendiente → Pagado**. No puede pasar a Impreso ni Entregado.
- **QL Services** (y los roles que se definan para ese módulo) solo pueden hacer **Pagado → Impreso** y **Impreso → Entregado**. No pueden volver a Pendiente ni a Pagado desde Impreso/Entregado (a menos que se defina explícitamente una política de corrección).
- No se permiten saltos (ej. Pendiente → Impreso). Solo la transición inmediata siguiente en el flujo.

**Efecto al marcar Pagado:** Disparar alerta/notificación a QL Services (carnets pendientes de impresión). No bloquear la acción si la alerta falla.

### 3.2 Plataforma: flujo y transiciones permitidas

Flujo: **Pendiente → Activo**.

| Transición | Rol autorizado | Quién la ejecuta |
|------------|----------------|------------------|
| Pendiente → Activo | ClubParentsAdmin | Club de Padres (registro de pago de acceso). |

**Restricciones:**

- Solo **Pendiente → Activo**.
- No se define transición Activo → Pendiente en este diseño (si se requiere en el futuro, será cambio de requisitos).
- **ClubParentsAdmin** es el único rol que puede activar la plataforma en este módulo.

### 3.3 Resumen por rol

| Rol | Carnet | Plataforma |
|-----|--------|------------|
| ClubParentsAdmin | Solo Pendiente → Pagado | Solo Pendiente → Activo |
| QL Services | Solo Pagado → Impreso, Impreso → Entregado | — (no aplica) |
| Admin / Director | Según diseño: pueden tener acceso a rutas de carnet existentes (Generate/Print); si se les da acceso al módulo QL, también Pagado → Impreso → Entregado | No definido en este módulo (opcionalmente mismo que Club si se desea). |

---

## 4. Matriz de permisos

| Acción | ClubParentsAdmin | QL Services | Admin | Director | Student/Estudiante |
|--------|------------------|-------------|--------|-----------|---------------------|
| Ver lista de estudiantes (módulo Club/QL) | Sí (solo su escuela) | No (ve lista de carnets pagados) | Sí (según rutas que se expongan) | Según diseño | No |
| Filtrar estudiantes por grado/grupo | Sí | No aplica | Sí | Según diseño | No |
| Marcar carnet como Pagado | Sí | No | No (no en este módulo) | No | No |
| Activar plataforma (Pendiente → Activo) | Sí | No | No (no en este módulo) | No | No |
| Imprimir carnet (PDF / Generate) | No | No | Sí (como hoy) | Sí (como hoy) | No |
| Marcar carnet Impreso (Pagado → Impreso) | No | Sí | Sí (si se incluye en rol QL) | Según diseño | No |
| Marcar carnet Entregado (Impreso → Entregado) | No | Sí | Sí (si se incluye en rol QL) | Según diseño | No |
| Editar estudiante (User) | No | No | Sí (UserController) | Según diseño | No (solo su perfil si aplica) |
| Subir notas / gradebook | No | No | Sí | Según diseño | No |
| Ver notas / asignaciones / horario (propios) | No (no es estudiante) | No | No | No | Sí si PlatformAccessStatus = Activo; No si Pendiente |
| Acceder a menú Club de Padres | Sí | No | No (menú distinto) | No | No |
| Acceder a menú QL Services (carnets pendientes) | No | Sí | Según diseño | Según diseño | No |
| Cambiar contraseña / Mis pagos (estudiante) | No aplica | No aplica | No aplica | No aplica | Sí (independiente de PlatformAccessStatus si se desea) |

**Nota:** Admin/Director conservan sus permisos actuales en carnets (Generate/Print) y en el resto del sistema. La matriz solo añade lo que corresponde al nuevo módulo y al rol QL Services.

---

## 5. Diseño de servicios

### 5.1 Responsabilidades por interfaz

- **IClubParentsPaymentService:** Operaciones permitidas al Club de Padres: listar estudiantes, consultar estado, marcar carnet Pagado, activar plataforma. No toca impresión ni estados Impreso/Entregado.
- **IQlServicesCarnetService:** Operaciones de QL: listar carnets pagados pendientes de impresión, marcar Impreso, marcar Entregado. No modifica Pagado ni plataforma.
- **IPlatformAccessGuardService:** Consulta si un usuario (estudiante) tiene acceso a plataforma (PlatformAccessStatus = Activo). Usado por el mecanismo de bloqueo (middleware, filtro o controladores). No modifica datos.

### 5.2 IClubParentsPaymentService

| Método | Descripción | Entrada relevante | Salida relevante |
|--------|-------------|-------------------|------------------|
| `GetStudentsAsync` | Lista estudiantes (User student/estudiante) de la escuela del usuario actual, con filtros opcionales por grado y grupo. Incluye o une con `student_payment_access` para mostrar CarnetStatus y PlatformAccessStatus. | SchoolId (del usuario), GradeId (opcional), GroupId (opcional) | Lista de DTOs: StudentId, FullName, Grade, Group, CarnetStatus, PlatformAccessStatus |
| `GetStudentPaymentStatusAsync` | Devuelve el estado de carnet y plataforma de un estudiante. Si no hay registro, devuelve Pendiente/Pendiente. | StudentId, SchoolId | DTO: CarnetStatus, PlatformAccessStatus, timestamps de actualización |
| `MarkCarnetAsPaidAsync` | Transición Pendiente → Pagado. Crea registro en `student_payment_access` si no existe. Registra usuario que ejecuta. Dispara alerta a QL (según diseño de alertas). | StudentId, UserId (quien ejecuta) | Éxito o excepción si transición no permitida / estudiante no válido |
| `ActivatePlatformAsync` | Transición Pendiente → Activo en platform_access_status. Crea registro si no existe. Registra usuario que ejecuta. | StudentId, UserId (quien ejecuta) | Éxito o excepción si no permitido |

**Validaciones internas:** Comprobar que el usuario actual sea ClubParentsAdmin y pertenezca a la misma escuela que el estudiante; que la transición sea la permitida (Pendiente → Pagado o Pendiente → Activo).

### 5.3 IQlServicesCarnetService

| Método | Descripción | Entrada relevante | Salida relevante |
|--------|-------------|-------------------|------------------|
| `GetPendingPrintAsync` | Lista registros con carnet_status = Pagado (pendientes de impresión) para la escuela. | SchoolId (opcional si hay multi-tenant) | Lista de DTOs: StudentId, FullName, Grade, Group, CarnetStatus, UpdatedAt |
| `MarkCarnetAsPrintedAsync` | Transición Pagado → Impreso. Actualiza carnet_updated_by_user_id y carnet_status_updated_at. | StudentId, UserId (quien ejecuta) | Éxito o excepción |
| `MarkCarnetAsDeliveredAsync` | Transición Impreso → Entregado. Actualiza auditoría. | StudentId, UserId (quien ejecuta) | Éxito o excepción |

**Validaciones:** Rol QL (o Admin según diseño); transición válida (Pagado→Impreso, Impreso→Entregado); estudiante en la misma escuela si aplica.

### 5.4 IPlatformAccessGuardService

| Método | Descripción | Entrada | Salida |
|--------|-------------|---------|--------|
| `ValidatePlatformAccessAsync` | Indica si el usuario (por defecto el usuario actual) tiene acceso a plataforma (PlatformAccessStatus = Activo). Si el usuario no es student/estudiante, puede devolver true (no aplicar bloqueo). Si es estudiante y no hay registro o está Pendiente, devolver false. | UserId (opcional; si no se pasa, usar usuario autenticado) | bool: true = acceso permitido, false = bloquear |

Uso: desde middleware, filtro de acción o base controller que proteja rutas académicas del estudiante.

---

## 6. Diseño de controladores / endpoints

Rutas bajo el mismo proyecto MVC/API existente; prefijos claros para Club de Padres y QL Services.

### 6.1 Club de Padres (ClubParentsController)

Base: **`/ClubParents`**. Rol: **ClubParentsAdmin**.

| Verbo | Ruta | Descripción | Input | Output |
|-------|------|-------------|--------|--------|
| GET | `/ClubParents/Students` o `/ClubParents` | Pantalla listado de estudiantes con filtros grado/grupo. | Query: gradeId, groupId (opcionales) | Vista (HTML) con lista |
| GET | `/ClubParents/Api/Students` | Listado en JSON (para SPA o recargas parciales). | Query: gradeId, groupId | JSON: { data: [ { id, fullName, grade, group, carnetStatus, platformAccessStatus } ] } |
| GET | `/ClubParents/Students/{id}` | Detalle de un estudiante y sus estados. | id (Guid) en ruta | Vista o JSON con estado carnet y plataforma |
| GET | `/ClubParents/Api/Students/{id}` | Estado de pago/acceso en JSON. | id en ruta | JSON: { studentId, carnetStatus, platformAccessStatus, ... } |
| POST | `/ClubParents/Carnet/MarkPaid` | Marcar carnet como Pagado. | Body: { studentId } | 200 OK o 400 con mensaje (transición no permitida, estudiante no encontrado) |
| POST | `/ClubParents/Platform/Activate` | Activar plataforma. | Body: { studentId } | 200 OK o 400 con mensaje |

Todas las acciones del controlador: `[Authorize(Roles = "ClubParentsAdmin")]`. Obtener SchoolId del usuario actual (ICurrentUserService o claims); no permitir operar sobre estudiantes de otra escuela.

### 6.2 QL Services (QlServicesCarnetController o similar)

Base: **`/QlServices`**. Rol: **QlServices** (y opcionalmente Admin si se desea).

| Verbo | Ruta | Descripción | Input | Output |
|-------|------|-------------|--------|--------|
| GET | `/QlServices/Carnet/PendingPrint` | Pantalla de carnets pagados pendientes de impresión. | — | Vista (HTML) con lista |
| GET | `/QlServices/Api/Carnet/PendingPrint` | Lista en JSON. | Query: schoolId (opcional) | JSON: { data: [ { studentId, fullName, grade, group, carnetStatus, paidAt } ] } |
| POST | `/QlServices/Carnet/MarkPrinted` | Marcar carnet como Impreso. | Body: { studentId } | 200 OK o 400 |
| POST | `/QlServices/Carnet/MarkDelivered` | Marcar carnet como Entregado. | Body: { studentId } | 200 OK o 400 |

Todas las acciones: `[Authorize(Roles = "QlServices,Admin")]` (o el rol que se defina para QL). Filtrar por escuela del usuario si aplica.

### 6.3 Resumen de rutas

| Ruta | Verbo | Rol | Uso |
|------|--------|-----|-----|
| /ClubParents/Students | GET | ClubParentsAdmin | UI listado |
| /ClubParents/Api/Students | GET | ClubParentsAdmin | API listado |
| /ClubParents/Students/{id} | GET | ClubParentsAdmin | UI detalle |
| /ClubParents/Api/Students/{id} | GET | ClubParentsAdmin | API estado |
| /ClubParents/Carnet/MarkPaid | POST | ClubParentsAdmin | Marcar Pagado |
| /ClubParents/Platform/Activate | POST | ClubParentsAdmin | Activar plataforma |
| /QlServices/Carnet/PendingPrint | GET | QlServices, Admin | UI pendientes impresión |
| /QlServices/Api/Carnet/PendingPrint | GET | QlServices, Admin | API pendientes |
| /QlServices/Carnet/MarkPrinted | POST | QlServices, Admin | Marcar Impreso |
| /QlServices/Carnet/MarkDelivered | POST | QlServices, Admin | Marcar Entregado |

---

## 7. Diseño de UI

### 7.1 Pantalla Club de Padres (listado)

- **Título:** “Pagos — Club de Padres” o similar.
- **Listado:** Tabla de estudiantes (User student/estudiante de la escuela), con columnas:
  - Nombre completo
  - Grado (desde StudentAssignment activa)
  - Grupo (desde StudentAssignment activa)
  - **Carnet:** valor de CarnetStatus (Pendiente | Pagado | Impreso | Entregado). Mostrar con badge/color (ej. Pendiente = amarillo, Pagado = naranja, Impreso = azul, Entregado = verde).
  - **Plataforma:** valor de PlatformAccessStatus (Pendiente | Activo). Badge (ej. Pendiente = gris, Activo = verde).
  - **Acciones (botones):**
    - “Marcar carnet pagado”: visible solo si CarnetStatus = Pendiente; al hacer clic llama a POST `/ClubParents/Carnet/MarkPaid` con studentId.
    - “Activar plataforma”: visible solo si PlatformAccessStatus = Pendiente; al hacer clic llama a POST `/ClubParents/Platform/Activate` con studentId.
- **Filtros (arriba de la tabla):**
  - Desplegable Grado (opcional): valores desde GradeLevel de la escuela.
  - Desplegable Grupo (opcional): valores desde Group de la escuela.
  - Botón “Filtrar” / filtrado en tiempo real según diseño.
- **Restricciones UI:** No mostrar botones de imprimir carnet, ni “Marcar impreso”/“Marcar entregado”. No enlaces a edición de estudiante, notas ni administración.

### 7.2 Pantalla Club de Padres (detalle estudiante)

- Opcional: al hacer clic en una fila o “Ver detalle” se abre vista con mismo estado (carnet y plataforma) y mismos botones permitidos (Marcar pagado / Activar plataforma) según estado actual.

### 7.3 Pantalla QL Services (carnets pendientes de impresión)

- **Título:** “Carnets pagados — Pendientes de impresión” o similar.
- **Listado:** Solo registros con CarnetStatus = Pagado. Columnas:
  - Nombre completo
  - Grado, Grupo
  - Fecha/hora de pago (carnet_status_updated_at cuando pasó a Pagado)
  - **Acciones:**
    - “Marcar impreso”: llama a POST `/QlServices/Carnet/MarkPrinted` con studentId.
    - (Opcional) Enlace a generación/impresión de carnet si se integra con la ruta actual de StudentIdCard para Admin/Director.)
- **Segunda vista o pestaña (opcional):** Carnets con estado Impreso (pendientes de entrega). Columna “Marcar entregado” con POST `/QlServices/Carnet/MarkDelivered`.

### 7.4 Menú

- **ClubParentsAdmin:** Un ítem de menú, ej. “Pagos Club de Padres” o “Club de Padres”, que lleve a `/ClubParents/Students`. No mostrar ítems de Carnet (generar/imprimir), Administración, Notas, etc.
- **QL Services:** Un ítem “Carnets pendientes impresión” (o similar) que lleve a `/QlServices/Carnet/PendingPrint`. Solo para rol QlServices (y Admin si se decide).

---

## 8. Alerta a QL Services

### 8.1 Requisito

Cuando **CarnetStatus** pase a **Pagado**, el sistema debe notificar a QL Services para que sepan que hay carnets pendientes de impresión. La acción “Marcar pagado” no debe fallar si la notificación falla.

### 8.2 Opciones comparadas

| Opción | Descripción | Pros | Contras |
|--------|-------------|------|---------|
| **Webhook HTTP** | Al guardar Pagado, el backend hace una petición HTTP POST a una URL configurada (ej. en appsettings) con payload (schoolId, studentId, timestamp). | Simple, sin infraestructura extra. QL puede exponer un endpoint y registrar la URL. | Acoplamiento a disponibilidad de QL; reintentos y timeouts a implementar. |
| **Tabla `pending_print_jobs`** | Insertar en una tabla (school_id, student_id, created_at, processed_at, etc.). Un proceso (job en el backend o sistema de QL) consulta o recibe notificaciones de esta tabla. | Desacoplado; el backend solo escribe; QL puede hacer polling o consumir por API. Persistencia garantizada. | Requiere tabla y posible job o API de consulta. |
| **Cola (RabbitMQ, Azure Service Bus, etc.)** | Publicar mensaje en una cola/topic que QL consuma. | Desacoplado, escalable, reintentos típicos de la cola. | Requiere infraestructura de mensajería que puede no existir hoy. |

### 8.3 Recomendación final

- **Primera implementación (bajo impacto):** **Tabla `pending_print_jobs`** (o nombre similar).
  - Columnas sugeridas: `id`, `school_id`, `student_id`, `created_at`, `processed_at` (nullable), `notes` (opcional).
  - Al ejecutar **MarkCarnetAsPaidAsync**, además de actualizar `student_payment_access`, insertar una fila en `pending_print_jobs` con `processed_at = null`.
  - QL Services puede:
    - Consumir un endpoint **GET /QlServices/Api/PendingPrintJobs** (o incluir estos ítems en la misma lista de “carnets pagados”) y al “marcar impreso” actualizar `processed_at` en `pending_print_jobs` para ese student_id, o
    - Usar la misma lista de carnets Pagado que ya se sirve desde `student_payment_access` (GetPendingPrintAsync), sin tabla extra, y la “alerta” es simplemente que aparecen en esa lista.
  - Ventaja: no depende de que un servicio externo esté levantado; la información queda en BD y QL trabaja sobre ella.
- **Alternativa si QL tiene endpoint público:** **Webhook** a URL configurada (ej. `QlServices:WebhookUrl`). En segundo plano (fire-and-forget o job en cola en memoria) para no bloquear la respuesta. Reintentos limitados y log en caso de error.

Se recomienda implementar primero la **tabla + API de listado** (o reutilizar el listado de carnets Pagado) y, si más adelante QL exige notificación en tiempo real, añadir webhook o cola sin cambiar la lógica de “Marcar pagado”.

---

## 9. Bloqueo de plataforma

### 9.1 Requisito

Cuando **PlatformAccessStatus = Pendiente**, el estudiante no debe acceder a:

- Notas
- Asignaciones
- Módulos académicos (horario, reportes, etc.)

Sí puede (según diseño): login, cambiar contraseña, “Mis pagos” u otras pantallas no académicas que se definan. No se bloquea el login.

### 9.2 Dónde validar

Puntos de entrada que sirven contenido académico al estudiante (según análisis y código actual):

- **StudentController** (lista, detalle, etc. si aplica a estudiante viendo sus datos).
- **StudentScheduleController** (horario del estudiante).
- **StudentReportController** (reportes del estudiante).
- Acceso a notas (p. ej. vistas o APIs que consuma el estudiante para sus calificaciones).
- Cualquier otra ruta que se identifique como “solo estudiante y contenido académico”.

En todos ellos, además de comprobar que el usuario sea student/estudiante, se debe comprobar que **PlatformAccessStatus = Activo** (consultando `student_payment_access` o el servicio de guarda). Si es Pendiente, no ejecutar la acción y devolver 403 o redirigir a una vista “Acceso pendiente de pago”.

### 9.3 Opciones de implementación

| Opción | Descripción | Pros | Contras |
|--------|-------------|------|---------|
| **Middleware** | Después de autenticación, si el usuario tiene rol student/estudiante, llamar a IPlatformAccessGuardService. Si no tiene acceso, redirigir a una ruta fija (ej. /Student/AccesoPendiente) o devolver 403 para APIs. | Centralizado; una sola vez por request. | Hay que excluir rutas que el estudiante sí puede usar (login, cambio contraseña, Mis pagos, etc.) mediante lista de rutas excluidas o convención. |
| **Filtro de acción / atributo** | Atributo [RequirePlatformAccess] en controladores o acciones que sirven contenido académico al estudiante. Dentro del filtro se llama al guard y si falla se devuelve 403 o Redirect. | Explícito por acción; no afecta otras rutas. | Hay que colocar el atributo en cada controlador/acción relevante; riesgo de olvido. |
| **Validación en cada controlador** | Al inicio de cada acción que sirve contenido académico al estudiante, llamar a IPlatformAccessGuardService y si es false devolver 403 o Redirect. | Máximo control por acción. | Código repetido y riesgo de olvidar en nuevas acciones. |

### 9.4 Recomendación final

- **Enfoque híbrido:**
  - **Middleware** que aplique solo a rutas bajo un prefijo (ej. `/Student`, `/StudentSchedule`, `/StudentReport`) o a un conjunto de rutas definidas en configuración. Si el usuario es student/estudiante y ValidatePlatformAccessAsync devuelve false, redirigir a una vista dedicada **/Student/AccesoPendiente** (o similar) con mensaje claro. Para peticiones que acepten JSON (API), devolver 403 con mensaje.
  - **Lista de rutas excluidas** en el middleware: login, logout, cambio de contraseña, “Mis pagos”, y la propia ruta de “Acceso pendiente”. Así no se bloquea lo que el estudiante sí puede usar.
- **Alternativa aceptable:** **Filtro de acción** `[RequirePlatformAccess]` aplicado a una base controller que usen todos los controladores que sirven contenido académico al estudiante, para no depender de una lista de rutas en middleware y mantener la intención explícita en el código.

La vista **AccesoPendiente** debe indicar que el acceso a notas, asignaciones y módulos académicos está pendiente de pago y, si aplica, enlazar a “Mis pagos” o información de contacto.

---

## 10. Riesgos y controles

| Riesgo | Tipo | Mitigación |
|--------|------|------------|
| Confusión Student (tabla) vs User (estudiante) | Técnico | Usar siempre User (student/estudiante) + StudentAssignment en el módulo. Documentar en diseño y en código (comentarios en servicios/controladores). |
| ClubParentsAdmin accede a rutas de Admin o carnets | Funcional / Seguridad | No añadir ClubParentsAdmin a ningún [Authorize] existente. Menú solo con ítems Club de Padres. Revisión de rutas antes de desplegar. |
| Transiciones de estado incorrectas (ej. saltos o rol equivocado) | Funcional | Validar en servicio: solo transiciones permitidas por rol; rechazar con mensaje claro. Opcional: CHECK en BD para valores de status. |
| Alerta QL no entregada o webhook caído | Operativo | No bloquear “Marcar pagado” por fallo de notificación. Usar tabla pending_print_jobs como fuente de verdad; QL puede consultar. Reintentos y log si se usa webhook. |
| Bloqueo de plataforma aplicado a rutas equivocadas | Funcional | Lista explícita de rutas a proteger y a excluir (middleware) o uso de atributo en controladores; pruebas de regresión para login, cambio contraseña, Mis pagos. |
| Creación masiva de registros en student_payment_access | Técnico | Definir política: crear solo al marcar Pagado o Activo, o en proceso batch controlado (ej. al matricular). Índice por (student_id, school_id) y unicidad. |
| Rol QL Services no existente en BD | Implementación | Añadir valor en UserRole y documentar; crear al menos un usuario de prueba con rol QlServices para validar acceso. |

---

## 11. Plan de implementación por fases

### Fase 1: Base de datos y rol

- Crear migración para tabla `student_payment_access` (estructura, PK, FK, índices, valores por defecto, CHECK si se usan).
- Añadir rol **ClubParentsAdmin** y **QlServices** en `UserRole` (enum) y donde se listen roles (ej. creación de usuarios).
- No asignar aún el rol a usuarios en producción hasta tener el módulo listo.
- Opcional: crear tabla `pending_print_jobs` si se adopta ese diseño de alerta.

**Entregable:** BD actualizada; roles disponibles en código y en listas de roles.

### Fase 2: Servicios y endpoints

- Implementar **IClubParentsPaymentService** / **ClubParentsPaymentService** (GetStudentsAsync, GetStudentPaymentStatusAsync, MarkCarnetAsPaidAsync, ActivatePlatformAsync).
- Implementar **IQlServicesCarnetService** / **QlServicesCarnetService** (GetPendingPrintAsync, MarkCarnetAsPrintedAsync, MarkCarnetAsDeliveredAsync).
- Implementar **IPlatformAccessGuardService** / **PlatformAccessGuardService** (ValidatePlatformAccessAsync).
- Implementar **ClubParentsController** y **QlServicesCarnetController** (o nombre acordado) con las rutas descritas en la sección 6; solo lógica de llamada a servicios y autorización por rol.
- Registrar servicios en el contenedor de DI.
- En **MarkCarnetAsPaidAsync**, implementar escritura en `pending_print_jobs` (o webhook) según diseño de alertas.

**Entregable:** APIs funcionando; pruebas unitarias/integración de servicios y de transiciones de estado.

### Fase 3: UI Club de Padres

- Añadir ítem de menú “Pagos Club de Padres” (o similar) para rol ClubParentsAdmin en **MenuService**.
- Crear vistas: listado de estudiantes con filtros grado/grupo, columnas Carnet y Plataforma, botones “Marcar carnet pagado” y “Activar plataforma”.
- Conectar vistas con endpoints (formularios o AJAX según stack).
- Probar con usuario ClubParentsAdmin; verificar que no ve menú de Admin, Carnet (imprimir), etc.

**Entregable:** Pantalla Club de Padres operativa y restringida al rol.

### Fase 4: UI QL Services

- Añadir ítem de menú “Carnets pendientes impresión” (o similar) para rol QlServices (y Admin si se define).
- Crear vista listado de carnets con estado Pagado (y opcionalmente Impreso para “Marcar entregado”).
- Botones “Marcar impreso” y “Marcar entregado” conectados a los POST correspondientes.
- Probar con usuario QlServices/Admin.

**Entregable:** Pantalla QL operativa.

### Fase 5: Bloqueo de plataforma

- Implementar middleware (o filtro) que use **IPlatformAccessGuardService** para usuarios con rol student/estudiante en las rutas definidas.
- Crear vista **AccesoPendiente** y configurar redirección o 403.
- Excluir login, cambio contraseña, Mis pagos y ruta de AccesoPendiente.
- Probar: estudiante con PlatformAccessStatus = Pendiente no accede a notas/asignaciones/horario; sí a lo excluido; con Activo accede a todo lo permitido por su rol.

**Entregable:** Restricción de acceso académico según estado de plataforma.

### Fase 6: Alertas y validación final

- Completar integración de alerta (consulta de pending_print_jobs por QL o webhook con reintentos y log).
- Revisión de permisos: matriz de permisos, pruebas con cada rol.
- Pruebas de regresión: flujos actuales de carnets (Generate/Print), pagos, login, estudiantes.
- Documentación de operación: cómo dar de alta usuarios ClubParentsAdmin y QlServices, valores de configuración (webhook si aplica).

**Entregable:** Módulo listo para producción; documentación actualizada.

---

**Fin del documento de diseño.**  
Este documento queda listo para ser usado como referencia en la implementación por fases sin modificar código existente hasta que cada fase se ejecute.
