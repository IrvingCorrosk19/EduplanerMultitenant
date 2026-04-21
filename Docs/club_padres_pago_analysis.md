# Análisis técnico: Módulo Club de Padres — Pagos (Carnet y Plataforma)

**Proyecto:** SchoolManager  
**Objetivo:** Estudio para implementar el rol "Administrador del Club de Padres de Familia" y el control de pagos (carnet + acceso a plataforma) como **módulo separado**, sin modificar lógica ni APIs existentes.

**Restricciones del análisis:** No implementar código, no crear migraciones, no cambiar el sistema actual. Solo documentar el estado actual y el diseño recomendado para una futura implementación.

---

## 1. Estado actual del sistema

### 1.1 Modelo de “estudiante” en el sistema

En SchoolManager existen **dos representaciones** de estudiante:

| Concepto | Ubicación | Uso principal |
|----------|-----------|----------------|
| **User (rol student/estudiante)** | `Models/User.cs`, tabla `users` | Login, carnets (`StudentIdCard`), prematrícula, pagos, asignaciones, notas, horarios. **Es la entidad principal** para flujos académicos y operativos. |
| **Student** | `Models/Student.cs`, tabla `students` | Entidad separada (Id, SchoolId, Name, BirthDate, Grade, GroupName, ParentId). Usada por `StudentController` / `StudentService` (listado CRUD). No se usa en carnets ni en prematrícula. |

**Conclusión para el módulo Club de Padres:**  
El “estudiante” a listar, filtrar y sobre el cual registrar pagos de carnet/plataforma debe ser el **User con rol `student` o `estudiante`** (y con `SchoolId` del colegio), ya que es el que se usa en carnets, tokens QR y lógica actual. La tabla `students` puede quedar fuera del alcance del nuevo módulo o usarse solo como referencia si se decide unificar criterios más adelante.

### 1.2 Campos existentes relacionados con carnets y acceso

**StudentIdCard** (`Models/StudentIdCard.cs`, tabla `student_id_cards`):

- `Id`, `StudentId` (FK a `users.id`), `CardNumber`, `IssuedAt`, `ExpiresAt`, `Status`.
- `Status`: solo valores **"active"** y **"revoked"** en el código actual.
- **No existen** estados tipo: Pendiente, Pagado, Impreso, Entregado. El carnet se “genera” (Generate) y luego se puede “imprimir” (PDF); no hay flujo explícito de pago → impresión → entrega.

**User** (`Models/User.cs`):

- `Status`: usado para **login** (ej. "active" / inactivo). No hay campo específico de “acceso a plataforma” (Pendiente/Activo) para bloquear notas, asignaciones o módulos académicos.

**Payment** (`Models/Payment.cs`):

- Vinculado a `PrematriculationId` (obligatorio) y opcionalmente a `StudentId` (FK a `users.id`).
- `PaymentStatus`: "Pendiente", "Confirmado".
- `PaymentConceptId`: concepto (ej. matrícula, mensualidad). No hay conceptos específicos hoy para “Carnet” o “Acceso plataforma” en el análisis estático.

No existe en el modelo actual:

- Estado de “pago de carnet” (Pendiente → Pagado → Impreso → Entregado).
- Estado de “acceso a plataforma” (Pendiente / Activo) a nivel de usuario/estudiante.

### 1.3 Roles y autorización actual

**Roles definidos** (`Enums/UserRole.cs`):  
Superadmin, Admin, Director, Teacher, Contable, Secretaria, Student, Estudiante.  
(No existe `ClubParentsAdmin` ni equivalente.)

**Autorización:**  
Se usa `[Authorize(Roles = "role1,role2")]` en controladores. Los roles se comparan en minúsculas en `MenuService` (`role.ToLower()`). No hay sistema de permisos granulares por recurso; el acceso se controla por rol.

**Carnet (StudentIdCard):**  
`[Authorize(Roles = "Admin,admin,SuperAdmin,superadmin,Director,director")]` — solo estos roles pueden generar e imprimir carnets.

**Pagos:**  
- Listado/administración: `admin`, `superadmin`, `contabilidad`, `contable`.  
- “Mis pagos” (estudiante/acudiente): `acudiente`, `parent`, `student`, `estudiante`.

**Menú** (`MenuService`):  
Se construye según el rol del usuario; no se incluye ningún ítem para “Club de Padres”.

### 1.4 Flujo actual de carnets

1. Usuario con rol Admin/Director/SuperAdmin accede a `/StudentIdCard/ui`.
2. Listado de estudiantes vía `GET StudentIdCard/api/list-json` (Users con rol student/estudiante, filtrados por `SchoolId`).
3. **Generate** (`GenerateAsync`): crea/revoca `StudentIdCard` y crea `StudentQrToken`. No hay validación de “pago de carnet” previa.
4. **Print**: genera PDF del carnet. No se registra un estado “Impreso” o “Entregado” en BD.

No hay integración con pagos ni alertas a terceros (QL Services).

### 1.5 Acceso a la plataforma (estudiante)

- El acceso a la aplicación se controla por **User.Status** (ej. "active") y por **School.IsActive** en el login (`AuthService`).
- No existe hoy un concepto de “acceso a plataforma pagado” que restrinja notas, asignaciones o módulos académicos según un estado Pendiente/Activo. Cualquier estudiante con cuenta activa puede acceder a sus pantallas según su rol.

---

## 2. Cambios mínimos necesarios (recomendación)

- **Base de datos:** Añadir solo las estructuras necesarias para estados de carnet, estados de acceso a plataforma y, si se desea trazabilidad, registros de pago del Club de Padres. Evitar tocar columnas críticas de `users`, `student_id_cards` y `payments` existentes en flujos actuales.
- **Roles:** Añadir el rol `ClubParentsAdmin` (o nombre acordado) y asignarlo solo a usuarios del Club de Padres; no dar este rol a usuarios que deban seguir usando Admin/Director/QL.
- **Autorización:** Nuevos controladores y acciones exclusivos para el Club de Padres; el resto del sistema sigue con sus `[Authorize(Roles = "...")]` actuales.
- **Menú:** Incluir ítems para el Club de Padres solo cuando el rol sea el nuevo rol; no exponer Carnet (generar/imprimir), ni módulos académicos.
- **Lógica de negocio:** No cambiar `StudentIdCardService` ni el flujo de Generate/Print actual; el módulo Club de Padres solo actualizará estados de “pago” y “acceso”; la transición Impreso/Entregado y la impresión física quedan del lado de QL Services (y, si se desea, de los mismos endpoints actuales de carnet con roles actuales).

---

## 3. Diseño recomendado de base de datos

### 3.1 Opción A: Tabla separada de “estado de pagos / acceso” (recomendada)

Objetivo: no alterar `users` ni `student_id_cards` para no impactar login, carnets ni prematrícula.

**Nueva tabla: `student_payment_access` (o nombre similar)**

| Columna | Tipo | Descripción |
|--------|------|-------------|
| Id | Guid (PK) | |
| StudentId | Guid (FK users.id) | Estudiante (User con rol student/estudiante) |
| SchoolId | Guid (FK schools.id) | Escuela (redundante pero útil para filtros) |
| CarnetStatus | string | `Pendiente` \| `Pagado` \| `Impreso` \| `Entregado` |
| PlatformAccessStatus | string | `Pendiente` \| `Activo` |
| CarnetStatusUpdatedAt | timestamp (nullable) | |
| PlatformStatusUpdatedAt | timestamp (nullable) | |
| UpdatedByUserId | Guid (nullable, FK users.id) | Quién hizo el último cambio (Club o QL) |
| CreatedAt / UpdatedAt | timestamp | |

- **Un registro por estudiante (por escuela)** cuando se necesite trackear estos estados. Si el estudiante no está en la tabla, se puede considerar CarnetStatus = Pendiente y PlatformAccessStatus = Pendiente por defecto en la lógica del módulo.
- Ventajas: módulo desacoplado, fácil de consultar, no toca tablas existentes. QL Services podría actualizar solo `CarnetStatus` a Impreso/Entregado desde un API restringido por rol.

### 3.2 Opción B: Columnas adicionales en User

Añadir en `users`:

- `CarnetPaymentStatus` (Pendiente / Pagado / Impreso / Entregado).
- `PlatformAccessStatus` (Pendiente / Activo).

**Riesgo:** Mezcla responsabilidades de “cuenta de usuario” con “estado de pago/entrega”. Cualquier cambio en `User` puede afectar login, menú y otros módulos. **No recomendado** si se busca un módulo separado y bajo impacto.

### 3.3 Registro de “quién marcó pago” (opcional)

Si se desea auditoría de “el Club de Padres marcó carnet como Pagado” o “activó plataforma”:

- Reutilizar `Payment` con un `PaymentConcept` específico (ej. “Carnet”, “Acceso plataforma”) y `StudentId` + `RegisteredBy` (usuario Club de Padres), **o**
- Crear tabla `club_parents_payment_log` (StudentId, TipoPago [Carnet|Plataforma], Acción, UserId, Fecha).

La Opción A con una tabla `student_payment_access` puede complementarse con `Payment` para trazabilidad financiera sin cambiar el contrato actual de `Payment` (PrematriculationId podría seguir siendo obligatorio si el proceso de pago del Club está asociado a un periodo/prematrícula; si no, habría que valorar un concepto “sin prematrícula” o un flujo específico).

---

## 4. Diseño de roles y permisos

### 4.1 Nuevo rol

- **Nombre sugerido:** `ClubParentsAdmin` (o `ClubPadres` en BD/claims).
- **Dónde:** Añadir en `UserRole` (enum) y usar el mismo string en `User.Role` y en `[Authorize(Roles = "ClubParentsAdmin")]` (o variante en minúsculas si el menú usa ToLower).

### 4.2 Permisos del Club de Padres (resumen)

**Puede:**

- Ver lista de estudiantes (Users con rol student/estudiante del mismo SchoolId).
- Filtrar por grado y grupo (usando `StudentAssignment` activo: Grade, Group).
- Marcar **carnet** como **Pagado** (Pendiente → Pagado).
- Activar **plataforma** (Pendiente → Activo).

**No puede:**

- Imprimir carnets (no tener acceso a `StudentIdCard/ui/print`, ni a Generate si se decide que Generate siga siendo solo Admin/Director/QL).
- Cambiar CarnetStatus a Impreso o Entregado (solo QL Services u otros roles).
- Modificar información académica, subir notas, eliminar o editar estudiantes (no tener acceso a UserController, TeacherGradebook, StudentAssignment, etc.).

### 4.3 Cómo evitar que el rol acceda a otros módulos

- **Menú:** En `MenuService`, para el rol Club de Padres devolver solo ítems del módulo “Pagos Club de Padres” (lista estudiantes, filtros, acciones “Marcar carnet pagado” / “Activar plataforma”). No incluir Administración, Carnet Estudiantil (generar/imprimir), Catálogo, Docentes, etc.
- **Controladores:**  
  - Nuevos controladores con `[Authorize(Roles = "ClubParentsAdmin")]` para todas las acciones del módulo.  
  - No añadir `ClubParentsAdmin` a ningún `[Authorize(Roles = "...")]` de controladores existentes (StudentIdCard, User, TeacherGradebook, Payment actual, etc.). Así, el Club de Padres solo entra por sus propias rutas.
- **APIs:** Endpoints nuevos bajo rutas dedicadas (ej. `/ClubParents/...` o `/Api/ClubParents/...`). Los endpoints actuales de StudentIdCard, Payment (admin), User, etc., no incluyen el nuevo rol.

Con esto, el rol solo accede a lo que se le asigne explícitamente en menú y rutas.

---

## 5. Diseño de API / servicios

### 5.1 Controladores recomendados

- **ClubParentsController** (o **ClubParentsPaymentController**):  
  - Acciones solo para rol Club de Padres.  
  - Ejemplos:  
    - `GET` Listado de estudiantes (con filtros grado/grupo; datos desde Users + StudentAssignments + tabla de estados si se usa Opción A).  
    - `GET` Detalle estado de un estudiante (carnet + plataforma).  
    - `POST` Marcar carnet como Pagado (y disparar alerta QL).  
    - `POST` Activar plataforma (Pendiente → Activo).  
  - Sin acciones de impresión ni de cambio a Impreso/Entregado.

Si QL Services debe actualizar Impreso/Entregado desde el mismo sistema, se puede:

- Añadir un **QlServicesController** (o ampliar un área “Servicios”) con `[Authorize(Roles = "QlServices,Admin")]` y acciones que solo actualicen `CarnetStatus` a Impreso/Entregado, **o**
- Dejar que esa actualización se haga por integración externa (API, otro sistema), consumiendo un API de solo lectura o de actualización de estado que el backend exponga con roles restringidos.

### 5.2 Servicios recomendados

- **IClubParentsPaymentService** / **ClubParentsPaymentService**:  
  - Obtener estudiantes (con filtros grado/grupo).  
  - Obtener/actualizar estado de carnet (solo transiciones permitidas para Club: Pendiente → Pagado).  
  - Obtener/actualizar estado de acceso a plataforma (Pendiente → Activo).  
  - Al marcar carnet como Pagado: llamar a **IAlertService** o **INotificationService** para “carnets pendientes de impresión” (ver sección 7).  
- No tocar `IStudentIdCardService` ni `IPaymentService` existentes en su lógica actual; el nuevo servicio solo trabaja sobre la nueva tabla (o campos) y, si se desea, registra en `Payment` como concepto “Carnet”/“Plataforma” sin cambiar flujos de prematrícula existentes.

### 5.3 Endpoints necesarios (resumen)

| Método | Ruta (ejemplo) | Descripción | Rol |
|--------|----------------|-------------|-----|
| GET | /ClubParents/Students o /Api/ClubParents/students | Lista estudiantes (filtros grado, grupo) | ClubParentsAdmin |
| GET | /ClubParents/Students/{id} o /Api/ClubParents/students/{id} | Estado carnet + plataforma de un estudiante | ClubParentsAdmin |
| POST | /ClubParents/Carnet/MarkPaid o similar | Marcar carnet Pagado; dispara alerta QL | ClubParentsAdmin |
| POST | /ClubParents/Platform/Activate o similar | Activar acceso a plataforma | ClubParentsAdmin |

Para QL (si se implementa en este backend):

| POST | /QlServices/Carnet/MarkPrinted o similar | CarnetStatus → Impreso | QlServices / Admin |
| POST | /QlServices/Carnet/MarkDelivered o similar | CarnetStatus → Entregado | QlServices / Admin |

Las rutas exactas y nombres de proyecto (MVC vs API) se pueden ajustar; lo importante es que todas estén protegidas por el rol correspondiente y que el Club de Padres no tenga rutas de impresión ni de Impreso/Entregado.

---

## 6. Flujo de negocio

### 6.1 Carnet (estados y responsabilidades)

- **Pendiente** → **Pagado**: Club de Padres (registro de pago). Al pasar a Pagado → disparar **alerta a QL Services** (carnets pendientes de impresión).
- **Pagado** → **Impreso**: QL Services (o proceso externo), no el Club de Padres.
- **Impreso** → **Entregado**: QL Services (o proceso externo), no el Club de Padres.

El flujo actual de “generar carnet” (Generate) y “imprimir PDF” puede quedar como está: solo Admin/Director/SuperAdmin. Opcionalmente, se puede exigir que el estado sea al menos “Pagado” antes de permitir Generate en la lógica futura, sin cambiar la firma del servicio actual (solo una validación previa en el controlador o en un decorador/orquestador).

### 6.2 Acceso a la plataforma

- **Pendiente**: el estudiante no debe acceder a notas, asignaciones, módulos académicos (solo, por ejemplo, pantalla de “Acceso pendiente de pago” o similar).
- **Activo**: acceso completo según su rol (student/estudiante).

Implementación sugerida: en cada controlador o pipeline que sirva datos académicos (notas, asignaciones, horario, etc.), además de comprobar rol y que el usuario sea el estudiante, comprobar que `PlatformAccessStatus == Activo` (leyendo desde la nueva tabla o desde el campo en User si se optara por Opción B). Si está Pendiente, devolver 403 o redirigir a una vista de “sin acceso”.

---

## 7. Alerta a QL Services cuando el carnet pase a Pagado

Opciones sin modificar APIs existentes de negocio:

1. **Cola / mensajería:** Al guardar CarnetStatus = Pagado, publicar un mensaje (ej. a una cola o topic) que un servicio de QL consuma. El backend solo publica; no necesita conocer la implementación de QL.
2. **Webhook / HTTP:** El backend llama a una URL configurada (ej. en `appsettings`: `QlServices:WebhookUrl`). Si falla, registrar en log y opcionalmente reintentar o guardar en tabla “pending_notifications” para un job que reintente.
3. **Tabla de “alertas”:** Insertar en una tabla `alerts` o `pending_print_jobs` (SchoolId, StudentId, Tipo = CarnetPagado, FechaHora, Procesado). Un proceso de QL (o un job en el mismo backend con rol/identity de QL) consulta o recibe notificaciones de esta tabla. El módulo Club de Padres solo escribe; no modifica lógica de carnets ni de pagos existentes.

La opción 2 o 3 suele ser la más simple para “avisar a QL” sin tocar su sistema; la 1 si ya hay infraestructura de mensajería.

---

## 8. Bloquear acceso a la plataforma cuando el estado sea Pendiente

- **Dónde:** En el middleware o en los controladores que sirven contenido académico al estudiante (StudentController, StudentScheduleController, TeacherGradebook cuando el usuario es estudiante, StudentReportController, etc.), o en un **middleware** que después del login compruebe el rol y el estado de acceso.
- **Lógica:** Si el usuario tiene rol student/estudiante, consultar `PlatformAccessStatus` (desde la nueva tabla o desde User). Si es Pendiente, no ejecutar la acción y devolver 403 o redirigir a una vista “Acceso pendiente de pago” (y permitir, por ejemplo, Cambiar contraseña o “Mis pagos” si se desea).
- **Login:** No bloquear el login; el estudiante puede entrar pero sin acceso a notas/asignaciones/módulos hasta que el estado sea Activo. Así se evita tocar la lógica actual de login (User.Status, School.IsActive).

---

## 9. Riesgos de implementación

| Riesgo | Mitigación |
|--------|------------|
| Confusión Student (tabla) vs User (estudiante) | Usar siempre User (student/estudiante) + StudentAssignment para listados y filtros del Club de Padres. Documentar en el código y en este documento. |
| Que el nuevo rol reciba menú o rutas de Admin | No añadir ClubParentsAdmin a ningún `[Authorize]` existente; menú solo con ítems explícitos para Club de Padres. |
| Modificar Payment o Prematriculation y romper flujos | Mantener Payment y Prematriculation como están; usar nueva tabla de estados y, si se desea, crear pagos con conceptos nuevos sin cambiar contratos de servicios actuales. |
| Que “activar plataforma” o “carnet pagado” se use en lógica antigua | Introducir estados solo en el nuevo módulo y en los puntos de comprobación explícitos (bloqueo de plataforma; opcionalmente validación previa a Generate). No reutilizar campos existentes de User/StudentIdCard para estos flujos sin diseño explícito. |
| Alerta QL no entregada | Log + reintentos o cola; no bloquear la acción “Marcar pagado” del Club de Padres si falla la notificación. |

---

## 10. Recomendación final de arquitectura

- **Base de datos:** Nueva tabla `student_payment_access` (o equivalente) con estados de carnet y de acceso a plataforma, sin añadir columnas a `users` ni a `student_id_cards`. Opcional: registrar en `Payment` con conceptos “Carnet”/“Acceso plataforma” para trazabilidad.
- **Rol:** Nuevo rol `ClubParentsAdmin`; menú y controladores exclusivos; ningún acceso a carnets (imprimir/generar), ni a módulos académicos ni a administración de usuarios.
- **API:** Controlador y servicio propios para el Club de Padres; endpoints para listar estudiantes (filtros grado/grupo), marcar carnet pagado y activar plataforma. Endpoints separados (o integración externa) para que QL actualice Impreso/Entregado.
- **Alertas:** Al pasar carnet a Pagado, notificar a QL por webhook, cola o tabla de alertas, sin acoplar el resto del sistema.
- **Acceso estudiante:** Comprobar `PlatformAccessStatus` en las rutas académicas del estudiante; si es Pendiente, denegar acceso a notas/asignaciones/módulos y mostrar mensaje o redirección clara.

Con esto, la funcionalidad queda como **módulo separado**, reutilizando solo el modelo de identidad (User, SchoolId, StudentAssignment) y sin modificar APIs ni lógica existentes de carnets, pagos de prematrícula o roles actuales.

---

## Resumen del análisis

- **Estudiante operativo:** User con rol student/estudiante; listados y filtros vía StudentAssignment (grado, grupo).
- **Carnet actual:** StudentIdCard con Status active/revoked; sin estados Pagado/Impreso/Entregado; flujo Generate/Print sin pago previo.
- **Plataforma actual:** Sin estado “acceso pagado”; solo User.Status y School.IsActive en login.
- **Recomendación:** Nueva tabla de estados (carnet + plataforma), nuevo rol ClubParentsAdmin, nuevos controlador y servicio, menú restringido, alerta a QL al marcar Pagado, y comprobación de acceso a plataforma en rutas académicas del estudiante.

## Posibles impactos en el sistema

- **Bajo impacto si se respetan las restricciones:** No se modifican modelos existentes ni contratos de servicios; solo se añaden tablas, rol, rutas y lógica nueva.
- **Puntos de atención:** (1) Definir bien en qué momento se crea el registro en `student_payment_access` (al matricular, al primer acceso del Club, etc.). (2) Si más adelante se exige “carnet Pagado” para Generate, será una validación adicional en un solo lugar. (3) El bloqueo de acceso a plataforma requiere tocar (o centralizar) los puntos de entrada a notas/asignaciones/módulos del estudiante, lo cual es un impacto controlado y acotado a “lectura de estado + 403 o redirección”.
