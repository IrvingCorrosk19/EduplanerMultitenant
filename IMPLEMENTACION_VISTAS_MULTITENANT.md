# Implementación vistas multi-tenant — Eduplaner / SchoolManager

Documento generado a partir de la línea de trabajo alineada con `ANALISIS_VISTAS_MULTITENANT_EDUPLANER.md`. Resume **cambios aplicados**, **superficies tocadas**, **riesgos residuales** y **cómo validar el aislamiento**.

---

## 1. Cambios realizados (resumen)

| Área | Qué se hizo |
|------|----------------|
| **Contexto visual de tenant** | Banner reutilizable con nombre, logo y mensaje de alcance; integración en layouts principales y bloque de institución en menú. |
| **Layouts** | `_AdminLayout`, `_MainLayout`, `_SuperAdminLayout`: nombre de colegio en cabecera/sidebar donde aplica; partial del banner antes del cuerpo. |
| **Login multi-escuela** | `AuthController` expone escuelas activas; `Login.cshtml` permite elegir institución cuando el usuario tiene varias asociaciones. |
| **Formularios / IDs en cliente** | Eliminación de `SchoolId` oculto o en payloads AJAX donde el tenant debe venir del servidor (`EmailConfiguration`, `CounselorAssignment`); creación de email fuerza escuela desde usuario actual. |
| **Consultas con `schoolId` en URL** | `DirectorWorkPlans`: la UI ya no envía `schoolId` en la query de listado/export (el servidor aplica filtro por tenant). |
| **Id Card Settings (SuperAdmin)** | Selección de escuela sin depender de bookmark con `?schoolId=` en redirects: `TempData` + acciones `select-school` / `clear-school`; vista con formulario POST seguro. |
| **Portal docente (gradebook)** | Subtítulo contextual con nombre de institución vía `ICurrentUserService` en `TeacherGradebook/Index` y `TeacherGradebookDuplicate/Index`. |
| **Email configuration UX** | Columna “Escuela” con nombre legible (`SchoolName` en DTO + servicio de mapeo). |

**Principio rector:** la UI refleja el tenant; **la autoridad del `school_id` y el filtrado real siguen en backend** (servicios, filtros globales de EF, claims).

---

## 2. Vistas y archivos modificados (lista operativa)

### Nuevos

- `Views/Shared/_TenantContextBanner.cshtml`

### Layouts y navegación

- `Views/Shared/_AdminLayout.cshtml`
- `Views/Shared/_MainLayout.cshtml`
- `Views/Shared/_SuperAdminLayout.cshtml`
- `Views/Shared/_Menu.cshtml`

### Autenticación

- `Views/Auth/Login.cshtml`
- `Controllers/AuthController.cs` (GET login async, `ViewBag.TenantSchools`)

### Módulos concretos

- `Views/EmailConfiguration/Index.cshtml`, `Create.cshtml`
- `Controllers/EmailConfigurationController.cs`
- `Dtos/EmailConfigurationDto.cs`
- `Services/Implementations/EmailConfigurationService.cs`
- `Views/DirectorWorkPlans/Index.cshtml`
- `Controllers/IdCardSettingsController.cs`
- `Views/IdCardSettings/Index.cshtml`
- `Controllers/CounselorAssignmentController.cs`
- `Views/CounselorAssignment/Index.cshtml`, `Create.cshtml`
- `Views/TeacherGradebook/Index.cshtml`
- `Views/TeacherGradebookDuplicate/Index.cshtml`

---

## 3. Problemas corregidos (mapeo a fases del análisis)

| Fase | Problema | Enfoque |
|------|-----------|---------|
| **1 — Contexto visual** | Usuario no veía claramente en qué colegio operaba | Banner + marca lateral/nav con nombre/logo cuando existen. |
| **2 — Filtrado en UI** | Riesgo de mezclar contexto al pasar `schoolId` desde JS | Director work plans deja de inyectar `schoolId` en URL; APIs deben seguir filtrando por usuario. |
| **3–4 — Seguridad / formularios** | Confiar en `SchoolId` oculto o en JSON del cliente | Quitar de vistas/AJAX donde no aporta; controladores asignan `SchoolId` desde `CurrentUser`. |
| **5 — Vistas críticas** | Gradebook sin refuerzo de contexto | Línea explícita de institución en portales docente (ambas vistas). |
| **6 — Navegación** | Menú sin ancla de contexto | Bloque “Institución” en `_Menu.cshtml`. |
| **7 — Componentes** | Parcial de tenant centralizado | Un solo partial para no duplicar lógica de logo/nombre. |
| **8 — UX** | SuperAdmin sin escuela “perdido” | Banner de advertencia en modo plataforma. |

---

## 4. Validación de aislamiento (qué comprobar manualmente)

1. **Usuario con una sola escuela:** tras login, banner y cabecera muestran el mismo colegio; listados (students, groups, etc.) solo muestran registros de esa escuela (verificar en BD o con segundo tenant).
2. **Usuario con varias escuelas:** selector en login; tras elegir, el contexto visual coincide con la sesión/claim efectiva.
3. **SuperAdmin:** banner amarillo; en Id Card Settings, elegir escuela → guardar → no debe “atascar” otro colegio por URL manipulada sin pasar por el flujo autorizado.
4. **Email configuration / Counselor assignment:** crear/editar sin campo `SchoolId` en cliente; intentar manipular request (Burp/DevTools): el servidor debe ignorar o sobreescribir con la escuela del usuario no superadmin.
5. **Docente:** en portales de calificaciones, debe leerse el nombre de **su** institución (no editable desde la vista).

**Nota:** la prueba definitiva de no fuga de datos es **backend + pruebas de integración**; la UI solo reduce error humano y superficie de manipulación.

---

## 5. Resultado final y trabajo residual consciente

**Resultado:** aplicación más alineada a SaaS multi-tenant en la capa de presentación: contexto visible, menos parámetros de tenant confiados al navegador en flujos ya auditados, login explícito cuando hay varias instituciones, y gradebook con refuerzo de contexto.

**Sin cambiar en esta iteración (consciente / riesgo controlado en servidor):**

- `Views/Shared/_Layout.cshtml` sigue siendo un layout genérico (Home/Privacy); el producto principal usa `_AdminLayout` / `_MainLayout`. Si algún flujo autenticado aún usa `_Layout`, conviene alinearlo o incluir el banner.
- Vistas de **edición** que conservan `asp-for="SchoolId"` oculto (`TimeSlot`, `SecuritySetting`, `PrematriculationPeriod`, `ScheduleConfiguration`, etc.): típico para edición de entidad ya cargada; **el controlador debe validar** que el registro pertenezca al tenant del usuario (revisión por endpoint, no solo vista).
- **SuperAdmin** (`StudentDirectory`, `ListSchools`, etc.): selectores globales de escuela son **intencionales** para administración de plataforma.
- **DataTables / scripts compartidos** (`_GroupsScripts`, etc.): el aislamiento depende de que los endpoints devuelvan solo datos del tenant; una pasada de auditoría API sigue siendo recomendable si no está cerrada.

---

## 6. Compilación

Última verificación: `dotnet build` en la solución **correcta** (0 errores).

---

*Referencia de análisis previo: `ANALISIS_VISTAS_MULTITENANT_EDUPLANER.md`.*
