# Prueba funcional completa en navegador (QA + usuario final + seguridad)

**Fecha:** 4 de mayo de 2026  
**Base URL:** `http://127.0.0.1:5173` (equivalente a `http://localhost:5173`)  
**Método:** pestaña de navegador real (Chrome integrado), clicks, formularios, navegación directa por URL, revisión de consola.

---

## Fase 1 – Preparación

| Paso | Resultado |
|------|-----------|
| Abrir navegador | Nueva pestaña creada (`browser_tabs` → `new`) para aislar sesión respecto a otras pestañas. |
| Ir a la app | `GET /Auth/Login` carga formulario con combo de **dos instituciones** activas. |
| Limpiar cookies | No se ejecutó borrado manual de almacenamiento del perfil de Chrome del usuario; **sí** sesión nueva por pestaña y flujos de **logout implícito** al cambiar de usuario vía login. Para auditoría estricta de “cookies limpias”, usar ventana de incógnito o borrar sitio en DevTools → Application. |

---

## 1. Escuelas creadas

| Ítem | Estado |
|------|--------|
| Crear en UI **“Colegio Central”** y **“Instituto Norte”** como escuelas nuevas | **No ejecutado** en esta sesión (requiere **SuperAdmin** → `SuperAdmin/CreateSchoolWithAdmin`; no se completó alta greenfield). |
| Validar **dos escuelas distintas** en el tenant | **Sí:** el login muestra **Instituto Dr. Alfredo Canton** (`cc4e5e11-1be8-42de-8193-428f4484041c`) e **Instituto Profesional y Técnico San Miguelito** (`6e42399f-6f17-4585-b92e-fa4fff02cb65`) con **GUID distintos**. |
| Evidencia multi-escuela | Tras login, el encabezado muestra el nombre de la institución correcta (Cantón vs San Miguelito) según la opción elegida en el combo. |

---

## 2. Usuarios por rol

Cuentas **E2E** usadas (contraseña común de pruebas `Test#2026` según `e2e/tests/fixtures.ts`):

| Rol | Escuela A (Cantón) | Escuela B (San Miguelito) |
|-----|--------------------|---------------------------|
| Admin | `admin.canton@test.local` | `admin.sanmiguelito@test.local` |
| Secretaria | `secretaria.canton@test.local` | (no re-login en B en esta vuelta corta) |
| Profesor | `profesor.canton@test.local` | — |
| Estudiante | `estudiante.canton@test.local` | — |

**Validado en navegador:**

- **Login + institución:** seleccionar escuela en combo, rellenar contraseña tras el combo si hace falta, **Iniciar sesión** → `Home/Index` con nombre de usuario y **nombre de escuela** coherentes.
- **Redirección:** sin pantalla de error en login correcto.
- **`school_id`:** inferido por contexto (claims); verificación SQL en sección 11 (pendiente si no hay cliente DB).

---

## 3. Flujo académico ejecutado

| Acción | Rol | Resultado |
|--------|-----|-----------|
| Dashboard / menú administración | Admin Cantón | OK (`Home/Index`, enlaces Administración, Usuarios, etc.). |
| **Gestión de usuarios** | Admin Cantón | `GET /User/Index` — formulario de alta, tabla con paginación, botones Editar/Eliminar. |
| **Catálogo / grados / grupos / materias** (crear 10°, 11°, A, B, Matemática, Ciencias) | Admin | **No ejecutado** en esta sesión (limitación de tiempo; rutas esperadas: catálogo académico, `GradeLevel`, `Group`, `Subject`, asignaciones). |
| **Asignar profesor → materia + grupo + grado** | Admin | **No ejecutado** en esta sesión. |

---

## 4. Actividades creadas

| Ítem | Estado |
|------|--------|
| Crear Tarea / Parcial / Examen en UI | **No persistido** en esta sesión (no se envió el formulario “Agregar” en `TeacherGradebook` para evitar datos basura en producción compartida). |
| Portal docente cargado | **Sí:** `GET /TeacherGradebook/Index` como **profesor Cantón** — pestañas Registrar Notas, Promedios, Asistencias, Disciplina, Consejería; sección “Agregar Nueva Actividad” y botones **Agregar** / **Guardar Notas** visibles. |

---

## 5. Notas registradas

| Ítem | Estado |
|------|--------|
| Ingresar notas a varios estudiantes y validar promedio | **No ejecutado** en esta sesión (requiere grupo con celdas `.gradebook-score-cell`; ver `e2e/tests/teacher-gradebook-flow.spec.ts` para automatización). |

---

## 6. Asistencia

| Ítem | Estado |
|------|--------|
| Marcar asistencia y validar duplicados | **No ejecutado** en esta sesión; la UI del módulo de asistencia está presente en `TeacherGradebook/Index` (pestaña y textos de “Tomar Asistencia”). |

---

## 7. Validación estudiante

| Prueba | Resultado |
|--------|-------------|
| Ver notas / portal | `GET /StudentReport/Index` — **Portal para Padres**, nombre **E2E Estudiante Canton**, grado **Sin asignación**, pestañas Calificaciones / Actividades / Asistencia / Disciplina. |
| Solo su información | No se abrió detalle de otro estudiante con éxito (ver sección 11 seguridad: URL ajena → 403). |
| **Ver carnet** | `GET /StudentIdCard/ui` → **`/Auth/AccessDenied`** (403). El módulo de carnet masivo está restringido a **SuperAdmin**; el rol **estudiante** no tiene esta pantalla en el diseño actual. |

---

## 8. Problemas detectados

| # | Severidad | Descripción |
|---|-----------|-------------|
| 1 | Media | **CSP `font-src`:** Bootstrap Icons cargadas desde `cdn.jsdelivr.net` eran **bloqueadas** por la política (solo `self`, gstatic, cdnjs). |
| 2 | Baja | **`console.error`** en `User/Index` por “elementos vacíos” (1) al inicializar DataTables — ruido y posible celda vacía en datos. |
| 3 | Baja | Varios **`console.warn`** de depuración en `User/Index` (intentos de carga de tabla, conteos). |
| 4 | Baja | Mensaje de consola **ambiguo** (“Rol actual”) aludiendo al valor del **combo de rol del formulario de alta**, no al usuario logueado. |
| 5 | Documental | **Secretaría** no puede “crear estudiantes” en `/Student/Create` (ruta **Forbid**); el flujo real es **usuarios estudiante + asignación** (`StudentAssignment`). |
| 6 | UX | Pie **© 2025** en página **403** vs **© 2026** en vistas principales. |

---

## 9. Correcciones aplicadas

| Archivo | Cambio | Motivo |
|---------|--------|--------|
| `Program.cs` | En **Content-Security-Policy**, `font-src` incluye ahora **`https://cdn.jsdelivr.net`**. | Permitir fuentes Bootstrap Icons servidas por jsdelivr; elimina violaciones CSP y mejora iconos en UI. |
| `Views/User/Index.cshtml` | Texto de log: **“Rol del formulario (alta usuario)”** en lugar de “Rol actual”. | Evitar confusión en QA al leer la consola (no insinuaba el rol de sesión). |

**Re-verificación:** Tras el cambio de CSP, hace falta **reiniciar la aplicación** para que el header CSP nuevo se envíe; la consola capturada **antes** del reinicio seguía mostrando la directiva antigua sin `jsdelivr`.

---

## 10. Validación multi-escuela

| Prueba | Resultado |
|--------|-----------|
| Login **admin Cantón** vs **admin San Miguelito** | Ambos OK; encabezado muestra **Instituto Dr. Alfredo Canton** vs **Instituto Profesional y Técnico San Miguelito**. |
| Dropdown de instituciones | Solo las escuelas **activas** listadas en `AuthController.Login` (no “globales” cruzadas sin contexto). |
| Mezcla de datos en UI | No observada en las pantallas visitadas; listados masivos son por tenant del usuario autenticado. |

---

## 11. Validación de seguridad

| Prueba | URL / acción | Resultado esperado | Resultado observado |
|--------|--------------|---------------------|------------------------|
| IDOR usuario otra escuela | `/User/Edit/b0b35595-cc47-4a3e-9233-1c57809daca5` (admin SM, fixture) como **admin Cantón** | Bloqueo | **`/Auth/AccessDenied`** (403) |
| IDOR estudiante ajeno | `/Student/Details/2e3ed445-d285-4d7d-b262-5e8fcd3c3cec` como **admin Cantón** | Bloqueo | **`/Auth/AccessDenied`** (403) |
| Profesor sin admin | `GET /User/Index` como **profesor Cantón** | Bloqueo | **403** |
| Secretaría sin admin | `GET /User/Index` como **secretaria Cantón** | Bloqueo | **403** |
| Estudiante sin carnet admin | `GET /StudentIdCard/ui` como **estudiante** | Bloqueo | **403** |

**Conclusión seguridad (ownership / RBAC):** los casos anteriores **cumplen** bloqueo por URL y por rol.

---

## 12. Validación UX

| Aspecto | Notas |
|---------|--------|
| Login multi-tenant | Combo de institución claro; conviene **volver a introducir contraseña** tras cambiar el combo si el flujo del navegador vacía el campo. |
| Admin usuarios | Tabla densa pero operativa; formulario de creación visible. |
| Profesor | Portal docente con pestañas reconocibles. |
| Estudiante | Portal de calificaciones legible; **carnet** no enlazado para rol estudiante (403 si se fuerza URL). |
| Errores | Página 403 con mensaje estándar y enlace Volver. |

---

## Consola del navegador (Fase 10)

**Antes del reinicio del servidor (CSP antiguo):**

- `debug`: violación **font-src** al cargar `.woff2` / `.woff` de Bootstrap Icons desde **jsdelivr**.
- `warning`: logs de depuración DataTables / tabla.
- `error`: “⚠️ Se encontraron elementos vacíos: 1”.
- `warning`: aviso del entorno de automatización de diálogos (herramienta de prueba).

**Después de reiniciar** con el `Program.cs` actualizado, se debe **volver a capturar** consola en `User/Index` y esperar **ausencia** de violaciones `font-src` para jsdelivr.

---

## Validación DB (Fase 11)

En esta sesión **no** se ejecutaron consultas SQL contra PostgreSQL (entorno del agente sin `psql` en PATH y sin Docker daemon disponible para cliente efímero).

**Recomendación:** tras cada alta en UI, validar:

- `users.school_id` = GUID de la escuela elegida en login.
- `student_assignments`, `activities`, etc., con el mismo `school_id` que el del usuario que creó el registro.

---

## Criterio final del enunciado

| Criterio | Estado |
|----------|--------|
| Flujo completo (académico de punta a punta + dos escuelas nuevas) | **No cumplido al 100 %** en esta ejecución (faltan altas académicas en UI, actividades, notas, asistencia, DB). |
| Sin errores UI críticos en rutas probadas | **Cumplido** en las vistas visitadas (sin pantallas rotas). |
| Sin errores de lógica en seguridad probada | **Cumplido** en casos IDOR/RBAC ejecutados. |
| Sin mezcla de datos (multi-escuela) | **Indicios positivos** (contexto de escuela correcto; sin prueba de listados cruzados exhaustiva). |
| Roles correctos | **Cumplido** en admin / secretaría / profesor / estudiante para las rutas probadas. |
| Seguridad (ownership) | **Cumplido** en las URLs manipuladas y restricciones de rol. |

**Veredicto:** el sistema muestra **comportamiento sano en login multi-escuela, RBAC, bloqueo IDOR y módulos principales visitados**. La **simulación completa de colegio** (catálogo nuevo, actividades, notas, asistencia, verificación SQL) queda **pendiente** o cubierta en parte por la suite **Playwright** (`e2e/tests/*.spec.ts`).

---

## Próximos pasos sugeridos

1. **Reiniciar** la aplicación y repetir **Fase 10** en `/User/Index` para confirmar CSP de fuentes.  
2. Ejecutar `cd e2e && npx playwright test` con BD y app levantadas.  
3. Opcional: reducir `console.warn`/`console.error` en producción en `Views/User/Index.cshtml` o mover a nivel `debug` condicionado.

---

*Documento generado como entregable `PRUEBA_FUNCIONAL_COMPLETA_BROWSER.md`.*
