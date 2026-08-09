# Prueba funcional en navegador real (QA manual)

**Fecha:** 3 de mayo de 2026  
**Entorno:** `http://127.0.0.1:5173` (ASP.NET Core en ejecución local; equivalente a `localhost:5173`)  
**Herramienta:** navegador integrado (Chrome), interacción por clicks, formularios y navegación directa por URL.

---

## 1. Flujo ejecutado

### Login (admin)

1. Navegación a `/Auth/Login`.
2. Credenciales: `admin.canton@test.local` / `Test#2026`.
3. Institución: **Instituto Dr. Alfredo Canton** (`cc4e5e11-1be8-42de-8193-428f4484041c`).
4. **Resultado:** redirección a `/Home/Index` (Dashboard), usuario visible como administrador de pruebas Cantón. Sin mensaje de error en pantalla tras login correcto.

### Admin — Usuarios (`/User/Index`)

1. Listado de usuarios con DataTables (miles de filas en el entorno probado).
2. **Alta de usuario:** formulario con rol **Inspector**, datos de prueba tipo QA Browser / correo único `qa.browser.manual.*@test.local`, documento y contraseña `Test#2026`.
3. Tras enviar: **SweetAlert** de éxito (“¡Usuario Creado!”). En un intento de cierre del diálogo hubo **referencia obsoleta de elemento** en automatización; con **nuevo snapshot** y click en el botón de confirmación el flujo continuó (comportamiento típico de SPA/modal, no fallo del producto en sí).
4. **Búsqueda** en la tabla por fragmento del correo: filtrado operativo.
5. **Edición:** la UI principal usa **edición en contexto** (botón Editar en tabla); no se forzó en esta sesión una visita manual completa a `/User/Edit/{guid}` con guardado, salvo la prueba de **manipulación de URL** (ver sección 4).

### Navegación entre módulos (admin)

- Transición **Dashboard → User/Index** y retornos vía menú: sin pantallas de error de aplicación en esos saltos.

### Estudiante (`estudiante.canton@test.local`)

1. Login con misma escuela Cantón.
2. **`/StudentReport/Index`:** carga correcta (“Portal para Padres”), pestañas Calificaciones / Actividades / Asistencia / Disciplina, nombre **E2E Estudiante Canton**, grado mostrado como **Sin asignación** (coherente con datos E2E sin matrícula física completa).
3. Enlace lateral **“Mis Calificaciones”:** en el viewport de prueba el click falló por **scroll en sidebar anidado**; se validó el mismo destino navegando directamente a `/StudentReport/Index` (equivalente funcional).

### Manipulación de URL — estudiante

- **`/Student/Details/2e3ed445-d285-4d7d-b262-5e8fcd3c3cec`** (ID de referencia de otra escuela en fixtures E2E): **`/Auth/AccessDenied`** con título “Acceso Denegado” (403). **No hay fuga de datos** del otro tenant en UI.

### Carnet (estudiante)

- Navegación directa a **`/StudentIdCard/ui`** con sesión de estudiante: **`/Auth/AccessDenied`**. El controlador de carnet está restringido a **SuperAdmin**; el rol estudiante **no tiene flujo de “ver carnet”** en menú estándar de layout admin/estudiante probado. Esto es **hueco respecto al guion “ver carnet” como estudiante”**, salvo que exista otro endpoint o vista no enlazada en esta prueba.

### Profesor (`profesor.canton@test.local`)

1. Login Cantón.
2. **`/TeacherGradebook/Index`:** “Portal del Docente” con pestañas Registrar Notas, Promedios, Asistencias, Disciplina, Consejería; formulario de nueva actividad y botones **Agregar** / **Guardar Notas** visibles. **Sin error de página** al cargar.

### Secretaría (`secretaria.canton@test.local`)

1. **`/Student/Create`:** **403 Acceso Denegado**. En código, `StudentController.Create` responde **`Forbid()`** (no hay alta de alumno por esa ruta para ningún rol).
2. **`/Prematriculation/Create`:** **403** (ruta reservada a acudiente/parent/estudiante).
3. **`/StudentAssignment/Index`:** **200** — listado paginado, búsqueda, botones **Editar**; al pulsar Editar se despliega panel **“Asignación de …”** con bloques “Asignación actual” / “Nueva asignación”. **Flujo de secretaría operativo** para gestión de asignaciones (proxy del “trabajo con estudiantes” en este producto).

### Manipulación de URL — admin (cross-tenant)

- Con sesión **admin Cantón**, navegación a **`/User/Edit/b0b35595-cc47-4a3e-9233-1c57809daca5`** (usuario admin de **San Miguelito** en fixtures): **`/Auth/AccessDenied`** (403). Comportamiento correcto para **anti IDOR / multi-tenant**.

---

## 2. Problemas detectados

| Área | Severidad | Descripción |
|------|-----------|-------------|
| Consola (User/Index) | Media | **CSP `font-src`:** fuentes Bootstrap Icons desde **jsdelivr** bloqueadas; iconos pueden verse rotos o con fallback según navegador. |
| Consola (User/Index) | Baja | **`console.error`** por “elementos vacíos” (1) durante init de DataTables — posible fila/celda vacía o lógica de depuración demasiado ruidosa en producción. |
| Consola (User/Index) | Baja | Varios **`console.warn`** de depuración (“Intento 1: Verificando tabla…”, conteos de filas/columnas) — ruido en DevTools y ligero impacto de UX para quien abre consola. |
| Login | Baja | Al cambiar **Institución** en el combo, conviene **recomprobar correo y contraseña** (en pruebas a veces el foco/cambio de DOM hizo necesario rellenar contraseña otra vez). |
| Sidebar estudiante | Baja | **“Mis Calificaciones”** puede quedar fuera de vista en sidebar con scroll anidado; el usuario puede necesitar scroll manual. |
| Guion QA “carnet estudiante” | Media | No hay acceso con rol estudiante a `/StudentIdCard/ui` (403); carnet parece **flujo SuperAdmin**. |
| Guion QA “crear estudiante secretaría” | Media | **`/Student/Create` no existe como flujo habilitado** (Forbid). La creación de cuentas alumnado en este despliegue pasa por **admin** (`User` con rol estudiante) u otros flujos (prematrícula padre, importaciones, etc.). |
| Pie de página | Muy baja | En **403 Acceso Denegado** el copyright mostró **© 2025** mientras otras vistas muestran **2026** — inconsistencia cosmética. |

---

## 3. Errores UI

- **Iconos:** riesgo visual por **fuentes bloqueadas por CSP** (Bootstrap Icons desde CDN no permitido en `font-src`).
- **403:** mensaje claro (“¡Acceso Denegado!”) y enlace Volver — **UX aceptable**.
- Sin evidencia en esta sesión de botones “muertos” fuera del caso del sidebar (scroll).

---

## 4. Problemas de permisos

- **Correcto:** admin Cantón **no** edita usuario de otra escuela por URL (`/User/Edit/{id}` ajeno → 403).
- **Correcto:** estudiante Cantón **no** abre detalle de ID ajeno en `/Student/Details/{id}` → 403.
- **Correcto:** estudiante **no** entra a UI de carnet masivo `/StudentIdCard/ui` → 403 (rol SuperAdmin).
- **Esperado por diseño actual:** secretaría **no** tiene `/Student/Create` ni `/Prematriculation/Create`; sí **`/StudentAssignment/Index`**.

---

## 5. Evidencia (descripción)

- **Login admin:** Dashboard con nombre de usuario E2E Admin Cantón e institución Cantón.
- **User/Index:** tabla masiva, SweetAlert de creación exitosa, búsqueda por texto.
- **Consola:** mensajes CSP (debug), warnings de inicialización DataTables, un `console.error` por elementos vacíos, aviso del entorno de automatización de diálogos.
- **403:** URL literal con `ReturnUrl` apuntando al recurso solicitado; headings **403** y **¡Acceso Denegado!**.
- **StudentReport:** pestañas y datos de cabecera del portal estudiante.
- **TeacherGradebook:** pestañas del portal docente y sección de registro de calificaciones.
- **StudentAssignment (secretaría):** paginación numérica (ej. hasta página 136), fila con botón Editar y panel de asignación al expandir.

---

## Respuestas al criterio de cierre

1. **¿El sistema funciona correctamente como usuario real?**  
   **En gran parte sí** para login, dashboard, gestión de usuarios (alta + tabla), portal estudiante (calificaciones/reporte), portal docente, y módulo de asignación de secretaría. **No** se cumple el guion literal “estudiante ve carnet en UI” ni “secretaría crea estudiante en `/Student/Create`” sin ajustar expectativas al diseño real del producto.

2. **¿Hay errores visuales?**  
   **Posibles:** iconos por CSP; posible inconsistencia menor de año en footer de página 403.

3. **¿Hay accesos indebidos?**  
   **No detectados:** las URLs manipuladas a recursos de otro tenant devolvieron **403 Acceso Denegado**.

4. **¿Hay problemas de UX?**  
   **Sí, menores:** scroll en sidebar para alumnos, ruido en consola (warnings/errors de depuración), combo institución que obliga a repasar campos, y desalineación del guion de negocio (carnet / alta de estudiante) con las rutas y roles reales.

---

## Veredicto final

**No se emite “SISTEMA VALIDADO EN UX REAL” al 100 %** del checklist original, porque:

- el **carnet** no está expuesto al rol **estudiante** en la ruta probada;
- **“crear estudiante” como secretaría** no coincide con `/Student/Create` (siempre denegado por código);
- hay **hallazgos de consola/CSP** que un QA marcaría para backlog.

**Sí se valida en navegador real** lo crítico de **aislamiento por escuela (403 en IDOR)** y los **flujos principales** citados arriba con comportamiento estable en la sesión.

---

*Documento generado a partir de ejecución manual asistida en navegador; no sustituye regresión automatizada completa.*
