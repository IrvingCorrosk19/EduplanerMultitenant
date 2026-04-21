# Auditoría técnica: Módulo de control de pagos Club de Padres

**Proyecto:** SchoolManager  
**Alcance:** Control de estados de carnet estudiantil, acceso a plataforma, y permisos de roles ClubParentsAdmin y QlServices  
**Fecha de análisis:** Código existente (sin modificaciones)

---

## 1. Resumen ejecutivo

El módulo Club de Padres está **parcialmente implementado** según el diseño funcional. La persistencia (modelo, tabla, migración, índices), la separación de responsabilidades por rol (ClubParentsAdmin solo Pendiente→Pagado / activar plataforma; QlServices solo Pagado→Impreso→Entregado), el menú para ClubParentsAdmin y la UI de listado con filtros están correctos. **La restricción de acceso del estudiante cuando `PlatformAccessStatus = Pendiente` no está aplicada:** existe el servicio `IPlatformAccessGuardService` pero **no se invoca en ningún controlador ni middleware**, por lo que los estudiantes con acceso pendiente pueden seguir entrando a notas, asignaciones y portal. La migración no añade los roles al esquema de BD (los roles se gestionan por enum y por el constraint `users_role_check` aplicado en arranque).

**Conclusión:** El sistema **no cumple por completo** el diseño del módulo mientras no se integre la validación de acceso a plataforma en las rutas del estudiante (notas, asignaciones, portal).

---

## 2. Elementos implementados correctamente

### 2.1 Modelo

| Requisito | Estado | Detalle |
|-----------|--------|---------|
| Modelo `StudentPaymentAccess` | ✅ | Definido en `Models/StudentPaymentAccess.cs`. |
| Propiedades: StudentId, SchoolId, CarnetStatus, PlatformAccessStatus, CreatedAt, UpdatedAt | ✅ | Incluye además CarnetStatusUpdatedAt, PlatformStatusUpdatedAt, CarnetUpdatedByUserId, PlatformUpdatedByUserId. |
| Relaciones con User y School | ✅ | Navegación a `Student`, `School`, `CarnetUpdatedByUser`, `PlatformUpdatedByUser`. |

**Archivo:** `Models/StudentPaymentAccess.cs`

---

### 2.2 Configuración en DbContext

| Requisito | Estado | Detalle |
|-----------|--------|---------|
| Tabla `student_payment_access` | ✅ | `entity.ToTable("student_payment_access")`. |
| Columnas en snake_case | ✅ | id, student_id, school_id, carnet_status, platform_access_status, etc. |
| PK | ✅ | `HasKey(e => e.Id)`, nombre `student_payment_access_pkey`. |
| FK a users (Student) y schools | ✅ | student_id → users, school_id → schools (Restrict). |
| Default CarnetStatus = "Pendiente" | ✅ | `HasDefaultValue("Pendiente")`. |
| Default PlatformAccessStatus = "Pendiente" | ✅ | `HasDefaultValue("Pendiente")`. |

**Archivo:** `Models/SchoolDbContext.cs` (aprox. líneas 2154–2233).

---

### 2.3 Índices

| Requisito | Estado | Detalle |
|-----------|--------|---------|
| Índice único (student_id, school_id) | ✅ | `IX_student_payment_access_student_id_school_id` con `.IsUnique()`. |
| Índice student_id | ✅ | `IX_student_payment_access_student_id`. |
| Índice school_id | ✅ | `IX_student_payment_access_school_id`. |
| Índice (carnet_status, school_id) | ✅ | `IX_student_payment_access_carnet_status_school_id`. |

**Archivo:** `Models/SchoolDbContext.cs`.

---

### 2.4 Migración

| Requisito | Estado | Detalle |
|-----------|--------|---------|
| Migración `AddStudentPaymentAccessAndClubRoles` | ✅ | `20260315084229_AddStudentPaymentAccessAndClubRoles.cs`. |
| Creación de tabla `student_payment_access` | ✅ | CreateTable con columnas, PK y FKs. |
| Índices definidos en migración | ✅ | IX_student_payment_access_* (student_id, school_id, carnet_status_school_id, etc.). |
| Adición de roles en BD en la migración | ⚠️ | La migración **no** altera `users` ni añade roles. Los roles ClubParentsAdmin y QlServices existen en el **enum** y en el constraint `users_role_check` aplicado por `EnsureUsersRoleCheck` al arranque, no en esta migración. |

**Archivo:** `Migrations/20260315084229_AddStudentPaymentAccessAndClubRoles.cs`

---

### 2.5 Roles en enum

| Requisito | Estado | Detalle |
|-----------|--------|---------|
| ClubParentsAdmin en UserRole | ✅ | `Enums/UserRole.cs`. |
| QlServices en UserRole | ✅ | `Enums/UserRole.cs`. |
| Sistema los reconoce (constraint BD) | ✅ | `EnsureUsersRoleCheck` incluye 'clubparentsadmin', 'qlservices' en el CHECK de `users.role`. |

**Archivo:** `Enums/UserRole.cs`

---

### 2.6 Controladores y permisos por rol

| Requisito | Estado | Detalle |
|-----------|--------|---------|
| Controlador Club de Padres | ✅ | `ClubParentsController` con `[Authorize(Roles = "ClubParentsAdmin")]`, ruta base `/ClubParents`. |
| Endpoints ClubParentsAdmin | ✅ | GET Students (vista), GET Api/GradesAndGroups, GET Api/Students, GET Api/Students/{id}, POST Carnet/MarkPaid, POST Platform/Activate. |
| Controlador QL Services | ✅ | `QlServicesCarnetController` con `[Authorize(Roles = "QlServices,Admin")]`, ruta base `/QlServices`. |
| Endpoints QlServices | ✅ | GET Api/Carnet/PendingPrint, POST Carnet/MarkPrinted, POST Carnet/MarkDelivered. |
| ClubParentsAdmin no puede Impreso/Entregado | ✅ | Solo expone MarkPaid y Activate; no hay endpoints para Impreso/Entregado en ClubParents. |
| QlServices no registra pagos ni activa plataforma | ✅ | Solo expone PendingPrint, MarkPrinted, MarkDelivered. |

**Archivos:** `Controllers/ClubParentsController.cs`, `Controllers/QlServicesCarnetController.cs`

---

### 2.7 Validación de flujo de estados

| Requisito | Estado | Detalle |
|-----------|--------|---------|
| Carnet: solo Pendiente → Pagado (ClubParents) | ✅ | `ClubParentsPaymentService.MarkCarnetAsPaidAsync`: `if (access.CarnetStatus != CarnetPendiente) throw InvalidOperationException`. |
| Carnet: solo Pagado → Impreso (QlServices) | ✅ | `QlServicesCarnetService.MarkCarnetAsPrintedAsync`: `if (access.CarnetStatus != CarnetPagado) throw`. |
| Carnet: solo Impreso → Entregado (QlServices) | ✅ | `QlServicesCarnetService.MarkCarnetAsDeliveredAsync`: `if (access.CarnetStatus != CarnetImpreso) throw`. |
| No se permite saltar estados (ej. Pendiente→Impreso) | ✅ | Las transiciones están restringidas en código; no hay endpoint que permita pasar a Impreso/Entregado desde Pendiente. |
| Plataforma: solo Pendiente → Activo (ClubParents) | ✅ | `ClubParentsPaymentService.ActivatePlatformAsync`: `if (access.PlatformAccessStatus != PlatformPendiente) throw`. |

**Archivos:** `Services/Implementations/ClubParentsPaymentService.cs`, `Services/Implementations/QlServicesCarnetService.cs`

---

### 2.8 Seguridad (Authorize)

| Requisito | Estado | Detalle |
|-----------|--------|---------|
| ClubParentsController restringido a ClubParentsAdmin | ✅ | `[Authorize(Roles = "ClubParentsAdmin")]` a nivel de clase. |
| QlServicesCarnetController restringido a QlServices y Admin | ✅ | `[Authorize(Roles = "QlServices,Admin")]`. |
| Separación de permisos por rol | ✅ | ClubParentsAdmin no puede llamar a rutas QlServices; QlServices/Admin no necesitan registrar pagos desde este módulo (rutas distintas). |

---

### 2.9 Menú (AdminLayout)

| Requisito | Estado | Detalle |
|-----------|--------|---------|
| Módulo visible en el layout de administración | ✅ | En `_AdminLayout.cshtml`, si `userRole == "clubparentsadmin"` se muestra ítem "Club de Padres" con enlace a `/ClubParents/Students`. |
| Estructura tipo “Administración > Control de Pagos > Carnets y Plataforma” | ⚠️ | No hay submenú “Control de Pagos”; hay un único ítem “Club de Padres” en el sidebar. Funcionalmente el acceso está cubierto. |

**Archivo:** `Views/Shared/_AdminLayout.cshtml` (aprox. líneas 152–159)

---

### 2.10 UI Club de Padres

| Requisito | Estado | Detalle |
|-----------|--------|---------|
| Lista de estudiantes | ✅ | Vista `Views/ClubParents/Students.cshtml` y API GET Api/Students. |
| Filtro por grado y grupo | ✅ | GET Api/GradesAndGroups y filtros en la vista con GET Api/Students?gradeId=&groupId=. |
| Marcar carnet pagado / Activar plataforma | ✅ | Botones que llaman a POST Carnet/MarkPaid y POST Platform/Activate. |
| Sin impresión de carnets ni Impreso/Entregado en esta UI | ✅ | La vista solo muestra estados y acciones Permitidas para ClubParentsAdmin. |

**Archivo:** `Views/ClubParents/Students.cshtml`

---

## 3. Elementos faltantes o no conformes

### 3.1 Restricción de acceso del estudiante (PlatformAccessStatus = Pendiente)

| Requisito | Estado | Detalle |
|-----------|--------|---------|
| Bloquear acceso cuando PlatformAccessStatus = Pendiente | ❌ | El servicio `IPlatformAccessGuardService` (método `ValidatePlatformAccessAsync`) existe y está registrado en DI, pero **no se utiliza** en ningún controlador ni en middleware. |
| Impedir acceso a notas, asignaciones, portal del estudiante | ❌ | `StudentReportController` (notas/calificaciones), `StudentProfileController`, `StudentScheduleController`, `StudentOrientationController` y otras rutas de estudiante **no** llaman a `ValidatePlatformAccessAsync` ni redirigen/deniegan cuando el acceso está pendiente. |
| Vista o ruta de “acceso pendiente” | ❌ | No existe una ruta dedicada (ej. `/Student/AccesoPendiente`) ni redirección cuando el estudiante no tiene acceso a plataforma. |

**Archivos donde falta la validación (ejemplos):**

- `Controllers/StudentReportController.cs` — No valida `PlatformAccessStatus` antes de devolver notas/calificaciones.
- `Controllers/StudentProfileController.cs` — No valida acceso a plataforma.
- `Controllers/StudentScheduleController.cs` — No valida acceso a plataforma.
- Cualquier otro controlador que sirva contenido académico o portal al rol student/estudiante.

**Archivo del servicio existente pero no integrado:** `Services/Implementations/PlatformAccessGuardService.cs`, `Services/Interfaces/IPlatformAccessGuardService.cs`.

---

### 3.2 Migración y roles en BD

| Requisito | Estado | Detalle |
|-----------|--------|---------|
| “Adición de roles ClubParentsAdmin y QlServices” en la migración | ⚠️ | La migración `AddStudentPaymentAccessAndClubRoles` **no** modifica la tabla `users` ni el constraint `users_role_check`. Los valores permitidos para `users.role` (incluidos clubparentsadmin y qlservices) se aplican en tiempo de ejecución mediante `EnsureUsersRoleCheck`, no por esta migración. Si se despliega en un entorno nuevo solo con esta migración, el constraint podría no incluir los nuevos roles hasta que la aplicación arranque y ejecute ese script. |

---

## 4. Riesgos funcionales detectados

1. **Estudiantes con acceso “Pendiente” pueden usar el sistema con normalidad**  
   Sin invocar `ValidatePlatformAccessAsync` en rutas de estudiante (notas, horario, perfil, orientación, etc.), no se cumple la regla de negocio de bloquear el acceso hasta que ClubParentsAdmin active la plataforma.

2. **Dependencia del arranque para el constraint de roles**  
   Si en un entorno la aplicación no llega a ejecutar `EnsureUsersRoleCheck` (o falla), los roles clubparentsadmin y qlservices podrían no estar permitidos en la BD, provocando errores al asignar o usar esos roles.

3. **Ausencia de vista “Acceso pendiente”**  
   Aunque se añadiera la validación, sería recomendable una página clara que informe al estudiante que debe regularizar el acceso a la plataforma, en lugar de solo 403 o redirección genérica.

---

## 5. Recomendaciones técnicas

1. **Integrar `IPlatformAccessGuardService` en el flujo del estudiante**  
   Opciones (sin cambiar diseño de negocio):  
   - **Middleware:** Tras autenticación, para rutas bajo un prefijo (ej. `/StudentReport`, `/StudentProfile`, `/StudentSchedule`, `/StudentOrientation`, etc.), si el usuario es student/estudiante, llamar a `ValidatePlatformAccessAsync()`; si devuelve false, redirigir a una ruta fija (ej. `/Student/AccesoPendiente`) o devolver 403 en APIs.  
   - **Filtro de acción o base controller:** Un filtro o controlador base que, para roles student/estudiante, inyecte el guard y compruebe acceso antes de ejecutar la acción; si no hay acceso, Redirect o Forbid.  
   En ambos casos, excluir rutas que el estudiante sí debe poder usar (login, cambio de contraseña, posible “Mis pagos”, etc.).

2. **Crear la vista y ruta “Acceso pendiente”**  
   Por ejemplo una acción `StudentController.AccesoPendiente` (o nombre acordado) que muestre un mensaje claro y, si se desea, enlace a contacto o instrucciones.

3. **Documentar que los roles en BD dependen de `EnsureUsersRoleCheck`**  
   En el plan de despliegue o runbook, indicar que el constraint `users_role_check` se actualiza al arranque y que debe ejecutarse la aplicación al menos una vez tras desplegar la migración que crea `student_payment_access`.

4. **Mantener la separación actual de controladores y flujos**  
   No exponer a ClubParentsAdmin endpoints de impresión o de transición Impreso/Entregado; no dar a QlServices endpoints de registro de pago o activación de plataforma. La auditoría confirma que esto está respetado.

---

## 6. Archivos específicos donde se detectaron problemas o hallazgos

| Archivo | Hallazgo |
|---------|----------|
| `Controllers/StudentReportController.cs` | No valida `PlatformAccessStatus` antes de permitir acceso a notas/calificaciones. No se inyecta ni se usa `IPlatformAccessGuardService`. |
| `Controllers/StudentProfileController.cs` | No valida acceso a plataforma antes de servir el perfil del estudiante. |
| `Controllers/StudentScheduleController.cs` | No valida acceso a plataforma antes de servir horario. |
| `Controllers/StudentOrientationController.cs` | Revisar si debe restringirse cuando el estudiante no tiene acceso a plataforma. |
| `Program.cs` | `IPlatformAccessGuardService` está registrado en DI pero no hay middleware ni filtro que lo utilice para bloquear rutas de estudiante. |
| `Migrations/20260315084229_AddStudentPaymentAccessAndClubRoles.cs` | No incluye cambios en `users` ni en el constraint `users_role_check`; los roles se gestionan vía enum y `EnsureUsersRoleCheck` al arranque. |
| `Views/Shared/_AdminLayout.cshtml` | El ítem de menú es “Club de Padres” directo; no existe submenú “Control de Pagos” o “Carnets y Plataforma” como en el ejemplo del enunciado; funcionalmente el acceso al módulo está cubierto. |

---

**Fin del reporte.** No se ha modificado ningún archivo del proyecto; solo se ha analizado el código existente y contrastado con los requisitos indicados.
