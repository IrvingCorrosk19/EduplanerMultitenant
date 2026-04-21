# Análisis de vistas Razor y UI — multi-tenant Eduplaner (SchoolManager)

**Tipo:** auditoría estática del código de presentación (`.cshtml`, layouts, parciales, scripts embebidos).  
**Alcance:** ~151 vistas Razor identificadas en el proyecto; revisión **profunda** en layouts, menú, login, dashboard, gradebook, flujos citados y muestra representativa de módulos críticos.  
**Regla:** sin cambios de código; sin propuesta de implementación.

---

## 1. Evaluación general de las vistas

La capa UI es **mayoritariamente “tenant-agnóstica” en el sentido visual**: muchas pantallas muestran listas y formularios que **asumen** que el servidor ya filtró por colegio. El **único refuerzo sistemático de contexto institucional** observado en layout es **`_AdminLayout`**: inyecta `ICurrentUserService`, muestra **logo y nombre de escuela** (`userSchool?.Name`) cuando existen datos de `School`.

Coexisten **varios shells**: `_AdminLayout`, `_MainLayout`, `_SuperAdminLayout`, `_LoginLayout`. **`_MainLayout`** no usa `CurrentUserService` para escuela: marca fija “School Manager”, logo genérico y menú vía **`_Menu`** sin nombre de institución en cabecera. Eso genera **inconsistencia de experiencia** según rol/pantalla: en SaaS multi-tenant, un usuario puede **no saber en qué colegio está** si cae en flujos que usan el layout “plano”.

El menú lateral dinámico (`Views/Shared/_Menu.cshtml`) depende de **`MenuService.GetMenuItemsForUserAsync(rol)`**: es **global por rol**, no por `school_id`. La contención de tenant queda **100 % en autorización y datos del controlador**, no en la vista.

**Conclusión general:** la UI funciona como **cliente de un backend multi-tenant**, no como producto SaaS que **comunique el contexto de forma uniforme** en todas las rutas.

---

## 2. Problemas críticos detectados

Clasificación pedida: **🔴 CRÍTICO** (fuga / confusión grave operativa o seguridad en superficie UI).

| ID | Severidad | Hallazgo |
|----|-----------|----------|
| V1 | 🔴 | **`Views/Auth/Login.cshtml`:** no se observa selector ni campo para `SchoolId` / institución, pese a que el modelo de login (`LoginViewModel`) y el backend soportan **login multi-escuela por correo duplicado**. En SaaS real, la UI **no acompaña** el contrato de seguridad/UX del servidor: el usuario no puede **declarar explícitamente** el colegio desde la pantalla analizada (riesgo de bloqueo o ambigüedad según datos). |
| V2 | 🔴 | **`Views/DirectorWorkPlans/Index.cshtml`:** uso de `ViewBag.SchoolId` en JavaScript y construcción de query `?schoolId=...` en peticiones. Cualquier **parámetro de tenant en URL** es vector de **tampering** (aunque el backend deba rechazar); la UI **expone** el patrón “el cliente sugiere el colegio”. |
| V3 | 🔴 | **`Views/IdCardSettings/Index.cshtml`:** redirección `window.location.href = '/id-card/settings?schoolId=' + encodeURIComponent(id)` — mismo patrón: **tenant en querystring** manipulable desde navegador/historial/referrer. |
| V4 | 🔴 | **`Views/TeacherGradebook/Index.cshtml`:** vista **muy grande** con numerosas llamadas `$.ajax` que envían **`teacherId`, `groupId`, `subjectId`, etc.** (p. ej. constantes desde `@Model.TeacherId`). La superficie de **IDOR por manipulación de cuerpo** es inherente al diseño “el navegador manda GUIDs”; el backend endurecido mitiga, pero la **UI sigue enseñando el modelo de confianza en el cliente**, típico de app single-tenant. (Duplicado análogo en `TeacherGradebookDuplicate/Index.cshtml`.) |
| V5 | 🟠→🔴 *contextual* | **`Views/CounselorAssignment/Index.cshtml`:** `<input type="hidden" id="schoolId" value="@ViewBag.SchoolId" />` y payloads AJAX con `SchoolId: $('#schoolId').val()`. El tenant **viaja en campo oculto**: falsificable si no hay validación estricta en **cada** endpoint. |

*Nota:* varios hallazgos “críticos” de UI son **coherentes con riesgos ya presentes en integración AJAX**; la vista no crea el agujero sola, pero **habilita y normaliza** el patrón inseguro desde perspectiva SaaS.

---

## 3. Problemas de UX multi-tenant

| Severidad | Hallazgo |
|-----------|----------|
| 🟠 | **Asistencia, pagos, prematrículas** (`Attendance/Index`, `Payment/Index`, `Prematriculation/MyPrematriculations`, etc.): encabezados genéricos (“Gestión de…”), **sin línea fija de “Institución: …”** en la propia vista. El usuario depende del layout admin para contexto. |
| 🟠 | **`Home/Index.cshtml`:** para **superadmin** existe selector GET `schoolId` (“Ver escuela”). Es el patrón correcto de **exploración cross-tenant**, pero la UX mezcla **modo plataforma** y **modo escuela** en la misma pantalla; riesgo humano de creer que los demás módulos siguen “scopados” a la escuela elegida si el servidor no persiste ese contexto de forma visible en toda la app. |
| 🟠 | **`Views/EmailConfiguration/Index.cshtml`:** columna “Escuela” muestra **`@config.SchoolId` (GUID)** en lugar del nombre legible del colegio — **fallo de UX multi-tenant** y exposición innecesaria de identificadores internos. |
| 🟠 | **`Views/SuperAdmin/StudentDirectory.cshtml`:** selector de escuela explícito — **adecuado para superadmin**; no aplica a usuario de un solo colegio, pero confirma que **solo en superadmin** la UI modela el tenant de forma explícita. |
| 🟢 | Títulos de página repetidos (“SchoolManager”) sin variante por cliente white-label en muchas rutas. |

---

## 4. Riesgos de seguridad en frontend

| Severidad | Hallazgo |
|-----------|----------|
| 🟠 | **IDs en URLs** (`Attendance/Details`, `Edit` con `asp-route-id`, pagos, etc.): patrón estándar MVC; el riesgo no es la vista sino **confianza exclusiva en el id** sin mensaje de contexto. Un atacante con sesión válida puede **probar enumeración** de GUIDs en endpoints débiles (responsabilidad backend; la UI **no desincentiva** el intento). |
| 🟠 | **JSON / AJAX sin metadatos de tenant** en muchos scripts (ej. `_GroupsScripts.cshtml`: `POST /Group/Create` con cuerpo `{ name, description, shift }` — **sin** `school_id` visible). Correcto si el servidor infiere escuela; **incorrecto** si algún endpoint aceptara `school_id` del cliente sin validar. |
| 🟠 | **Datos sensibles en tablas:** listados de pagos (recibos, montos), prematrículas, usuarios en directorios — visibles para roles autorizados; en HTML es inevitable, pero en **entorno multi-tenant** aumenta el impacto de cualquier **confusión de sesión** o **XSS** (no auditado XSS en este informe). |
| 🟢 | `LoginViewModel` incluye `SchoolId` en modelo; **la vista de login revisada no enlaza** ese campo a inputs visibles — reduce superficie de envío malicioso de `school_id` desde login, pero choca con el requisito de multi-cuenta por email. |

---

## 5. Inconsistencias entre backend y UI

| Hallazgo |
|----------|
| Backend: **login con resolución por email + `schoolId` opcional** y mensaje de ambigüedad. Frontend login: **no se observa** mecanismo de selección de escuela en `Login.cshtml` — **desalineación funcional** entre capas. |
| Backend: validaciones recientes en gradebook / notas. Frontend: **sigue enviando** el mismo volumen de identificadores en AJAX — la UI **no refleja** el endurecimiento (no hay indicación de “solo tu contexto” ni minimización de IDs). |
| **`_MainLayout`:** formulario de logout apunta a `asp-controller="Account"` mientras `_AdminLayout` usa `Auth` — posible **inconsistencia de flujo** según layout (no es multi-tenant en sí, pero sí **calidad de producto** y confianza del usuario en la sesión). |

---

## 6. Preparación para SaaS real (SI / NO / PARCIAL)

**PARCIAL.**

- **Sí** en parte del producto: `_AdminLayout` + dashboard con bloque de escuela dan **contexto razonable** para usuarios con `School` asignada.
- **No** como experiencia **homogénea** en todas las vistas y roles: layouts alternativos, ausencia de branding/contexto en varias pantallas, login sin selección de institución alineada al backend, y patrones **tenant en querystring / hidden** sin una capa UI que **niegue** el modelo “cliente decide colegio”.

---

## 7. Conclusión brutalmente honesta

Las vistas de Eduplaner están **diseñadas principalmente como front-end de un solo colegio** con mejoras puntuales para admin y superadmin. Para **SaaS multi-tenant serio**, la UI **no demuestra** de forma consistente que “estás siempre en el colegio X”: muchas pantallas son **intercambiables entre instituciones** si cambias los datos subyacentes, y varias **facilitan** el anti-patrón de **tenant en parámetros manipulables** (querystring, hidden, AJAX con GUIDs).

Un auditor o un comprador enterprise miraría esto y diría: **el backend puede estar cerrando brechas, pero la presentación sigue entrenando al usuario y al desarrollador en malos hábitos SaaS** (IDs por todas partes, poco contexto institucional, superadmin mezclado con flujo escolar en la misma UX).

---

## Bonus (junior vs auditor)

- **Junior:** “Si el usuario es admin, ya ve solo su escuela.” **Auditor:** “¿Y si el layout es `_MainLayout` o una vista parcial hace POST sin contexto visible?”
- **Junior:** “El `school_id` en hidden está bien porque viene del servidor.” **Auditor:** “Cualquier campo oculto es **input del atacante**; la UI no debe ser la línea de defensa.”
- **Backend corregido, UI que ‘rompe’:** login multi-tenant **sin** UI de escuela; gradebook **sigue** siendo un cliente rico de IDs aunque el servidor rechace suplantación.

---

*Fin del informe — solo diagnóstico.*
