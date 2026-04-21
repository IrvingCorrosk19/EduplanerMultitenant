# Auditoría de arquitectura — módulo `/Admin/UserPasswordManagement`

**Alcance:** evaluación del módulo como posible **centro de gestión de envío de correos masivos** en un SaaS educativo multi-escuela.  
**Enfoque:** ASP.NET Core MVC, multi-tenant por `SchoolId`, correo existente SMTP por escuela.  
**Código:** solo lectura (sin modificaciones).

---

## 1. Inventario de archivos del módulo

| Ruta | Rol |
|------|-----|
| `Controllers/Admin/UserPasswordManagementController.cs` | Controlador MVC |
| `Views/Admin/UserPasswordManagement/Index.cshtml` | Vista única |
| `Services/Interfaces/IUserPasswordManagementService.cs` | Contrato |
| `Services/Implementations/UserPasswordManagementService.cs` | Lógica de listado |
| `Dtos/UserListDto.cs` | Proyección de usuario en listado |
| `wwwroot/js/userPasswordManagement.js` | DataTables, filtros, selección múltiple |
| `Views/Shared/_AdminLayout.cshtml` | Enlace de navegación al módulo |
| `Program.cs` | Registro `IUserPasswordManagementService` |

**No existen:** repositorio dedicado, controlador API separado, vistas adicionales, tests automatizados específicos del módulo.

---

## 2. Análisis del controlador

**Clase:** `UserPasswordManagementController`  
**Ruta base:** `Admin/UserPasswordManagement`

### Autorización

```text
[Authorize(Roles = "SuperAdmin,superadmin,Admin,admin,Director,director")]
```

Acceso: **SuperAdmin**, **Admin**, **Director** (variantes de mayúsculas según claim).

### Dependencias inyectadas

| Dependencia | Uso |
|-------------|-----|
| `IUserPasswordManagementService` | Listados |
| `ILogger<UserPasswordManagementController>` | Registro de errores |

**No inyecta:** `IEmailService`, `IEmailConfigurationService`, `UserManager`, ni ningún servicio de notificaciones.

### Endpoints HTTP

| Método | Ruta | Acción |
|--------|------|--------|
| `GET` | `/Admin/UserPasswordManagement` o `.../Index` | `Index()` → vista |
| `GET` | `/Admin/UserPasswordManagement/ListJson` | JSON array de `UserListDto` |
| `GET` | `/Admin/UserPasswordManagement/FilterByRole?role=` | JSON filtrado por rol (servidor) |

**Observación:** la vista y el JS **solo consumen `ListJson`**. `FilterByRole` está implementado pero **no referenciado** desde `Index.cshtml` / `userPasswordManagement.js` (filtrado por rol es **100 % cliente** vía DataTables).

### Lógica principal

- Mostrar pantalla de administración de usuarios (nombre orientado a contraseñas).
- Exponer listado completo de usuarios en JSON para pintar tabla y filtrar en cliente.

---

## 3. Análisis de la vista y del front-end

### `Index.cshtml`

- **Título:** “User Password Management” (inglés).
- **Controles:** selector de rol (All, SuperAdmin, Admin, Teacher, Student), caja de búsqueda, botón “Limpiar filtros”.
- **Tabla DataTables:** columnas checkbox, Name, Email, Role, Status, Created At.
- **Scripts:** jQuery DataTables (CDN) + `userPasswordManagement.js`.

### `userPasswordManagement.js`

- Carga **una vez** todos los usuarios desde `ListJson`.
- **Filtro por rol:** búsqueda en columna Role (cliente).
- **Búsqueda global:** `#searchBox` → `dataTable.search()`.
- **Selección múltiple:** checkbox por fila + “select all”; API expuesta `getSelectedIds()` para **uso futuro** (comentarios: “Mass Password Delivery”).
- **Paginación:** 25 filas por página.

### Capacidades relevantes para correo masivo

| Requisito | Estado actual |
|-----------|----------------|
| Seleccionar múltiples usuarios | **Sí** (checkboxes + `getSelectedIds()`) |
| Filtrar por rol | **Sí** (cliente) |
| Buscar texto | **Sí** (DataTables) |
| Filtrar por escuela | **No** en UI ni en DTO |
| Acción “Enviar correo” | **No** |
| Vista previa / editor HTML | **No** |

**Conclusión:** la UI está **parcialmente preparada** para un flujo masivo (selección + filtros básicos), pero **no hay backend** ni integración con correo.

---

## 4. Modelo de usuario y DTO

### Entidad `User` (campos relevantes)

- `Email`, `Name`, `LastName`, `Role`, `SchoolId` (nullable), `Status`, `CreatedAt`, etc.
- **No existen** en el modelo actual: `EmailVerified`, `EmailOptIn`, `NotificationPreferences`.

Para campañas masivas y cumplimiento (LOPD/GDPR-lite), convendría valorar campos o tablas de preferencias en el futuro.

### `UserListDto`

Incluye: `Id`, `FirstName`, `LastName`, `Email`, `Role`, `Status`, `CreatedAt`.  
**No incluye `SchoolId`**, por lo que en la tabla el administrador **no ve** a qué escuela pertenece cada usuario (relevante para envíos acotados por tenant).

---

## 5. Servicios de correo existentes

| Servicio | Tecnología | Uso actual |
|----------|------------|------------|
| `IEmailService` / `EmailService` | **SMTP** (`System.Net.Mail`) | Informes disciplina/orientación, prematrícula, adjuntos |
| `IEmailConfigurationService` | Persistencia **por escuela** (`EmailConfiguration`) | Credenciales SMTP activas por `SchoolId` |

**Características:**

- Envíos **transaccionales** por caso de uso (métodos específicos).
- `SendEmailWithAttachmentsAsync(toEmail, subject, body, attachments, emailConfig)` permite envío genérico **si ya se dispone de `EmailConfigurationDto`**.
- **No hay** cola, **no hay** envío masivo ni API tipo SendGrid en el código central revisado.
- El envío es **síncrono en el hilo de request** en los flujos actuales (riesgo de timeout en masivos).

---

## 6. Multi-tenancy (escuelas)

- `User.SchoolId` existe; muchos usuarios pertenecen a una escuela.
- `UserPasswordManagementService.GetAllUsersAsync()` usa **`.IgnoreQueryFilters()`** y **no filtra por escuela**: devuelve **todos los usuarios del sistema**.
- Un **Director** o **Admin** de una sola escuela, con el mismo endpoint, **vería usuarios de otras escuelas** en la tabla (riesgo de **fuga de datos** y de envío masivo cruzado si se implementara correo sobre esta lista sin restringir).

**Recomendación de negocio:** cualquier “correo masivo” debería, por defecto, limitarse a `SchoolId` del usuario autenticado (salvo SuperAdmin con selector explícito de escuela).

---

## 7. Seguridad y roles

Quién **puede entrar** al módulo hoy: SuperAdmin, Admin, Director.

| Rol | Acceso actual | Riesgo si se añade correo masivo sin cambios |
|-----|----------------|-----------------------------------------------|
| SuperAdmin | Lista global | Coherente con operación global |
| Admin / Director | Misma lista global | **Alto** — podrían ver/enviar a usuarios de otras escuelas |

Convendría restringir listado y envío por **ámbito de escuela** y auditar acciones (quién envió, a cuántos, asunto).

---

## 8. Viabilidad de ampliación hacia “correo masivo”

El módulo **puede** evolucionar como **punto de partida UX** (tabla, filtros, selección), pero hoy es solo **lectura de usuarios**. Haría falta:

1. **Backend:** endpoint `POST` (o cola) que reciba `userIds[]`, `subject`, `body`, opcionalmente `schoolId`.
2. **Resolución de configuración SMTP:** una escuela por lote o rechazo si los usuarios mezclan escuelas.
3. **Servicio de campaña** que no bloquee el request (cola + worker).
4. **Plantillas** y posible editor WYSIWYG o Markdown/HTML sanitizado.

La sección propuesta (“📧 Envío de correos”) encaja **visualmente** en la misma página como segunda tarjeta o pestaña.

---

## 9. Proveedor de correo (recomendación orientativa)

| Opción | Pros | Contras |
|--------|------|---------|
| **SMTP actual (por escuela)** | Ya integrado, cada colegio usa su buzón | Límites bajos, reputación por cliente, no escala para miles |
| **Amazon SES** | Muy barato, masivo, buena entregabilidad con buena config | Configuración AWS, verificación dominios |
| **SendGrid / Brevo (ex-Sendinblue)** | APIs simples, estadísticas | Coste según volumen |
| **Resend** | DX moderna, buena para productos SaaS | Menos madurez enterprise en algunos mercados |
| **Postmark / Mailgun** | Transaccional fuerte | Precio según uso |

Para **SaaS educativo** con muchas escuelas y **bajo coste a escala**, suele priorizarse **SES** (centralizado por la plataforma) o **un proveedor transaccional** con subcuentas por escuela. Mantener SMTP por escuela sigue siendo válido para **volúmenes bajos** y autonomía institucional.

---

## 10. Arquitectura recomendada para email masivo

```
[ UI: UserPasswordManagement + tarjeta "Enviar correo" ]
        │
        ▼
[ API / Controller: validar rol, school scope, IDs seleccionados ]
        │
        ▼
[ EmailCampaignService: crear campaña, persistir EmailCampaign + EmailCampaignRecipient ]
        │
        ▼
[ Cola: IMessageBus / Hangfire / BackgroundService + Channel ]
        │
        ▼
[ EmailDispatchWorker: lotes N/minuto, IEmailSender (SMTP o SES) ]
        │
        ▼
[ Registro: estado por destinatario, reintentos, logs ]
```

Entidades conceptuales: `EmailCampaign`, `EmailCampaignRecipient`, `EmailTemplate` (opcional).  
**No enviar** miles de correos en el mismo request HTTP.

---

## 11. Diseño de UI propuesto

- **Tarjeta** “📧 Envío de correos” debajo o en pestaña junto a la tabla actual.
- Campos: **Asunto**, **Cuerpo** (textarea o editor), **Solo usuarios seleccionados** / **Todos los filtrados**.
- **Vista previa** (modal) con un usuario de prueba o primer destinatario.
- **Indicador** de escuela objetivo y advertencia si hay mezcla de `SchoolId`.
- Botón **Encolar envío** (no “Enviar ya” sin cola).

---

## 12. Síntesis del informe

| # | Tema | Conclusión |
|---|------|------------|
| 1 | Arquitectura actual | MVC delgado: controller + servicio CRUD-listado + DataTables |
| 2 | Dependencias | Solo `SchoolDbContext` vía servicio; sin correo |
| 3 | Flujo de datos | `ListJson` → JSON completo → filtro cliente |
| 4 | Riesgos | Lista global sin `SchoolId` en DTO ni filtro por tenant para Admin/Director |
| 5 | Integración masiva | Factible ampliando servicios, cola y alcance por escuela |
| 6 | Proveedor | SES o API transaccional para escala; SMTP por escuela para volúmenes pequeños |
| 7 | UI | Tabla y checkboxes ya útiles; falta formulario de campaña y backend |

**Veredicto:** el módulo es un **buen ancla de UX** para selección y filtrado, pero **no es hoy un centro de correo masivo**. Para serlo requiere **governanza multi-tenant**, **cola de envíos** y **integración explícita** con `IEmailService` (o un nuevo `IBulkEmailService`) sin bloquear el request.

---

*Documento generado como auditoría de arquitectura — SchoolManager.*
