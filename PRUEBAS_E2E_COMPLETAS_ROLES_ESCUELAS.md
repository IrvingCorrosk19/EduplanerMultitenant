# Pruebas E2E — Roles, multi-escuela y aislamiento (SchoolManager)

**Fecha:** 2026-05-03  
**Entorno:** aplicación en `http://localhost:5172`, base `eduplaner` (PostgreSQL 18, conexión según `appsettings.json` → `Host=localhost;Database=eduplaner`).  
**Método:** navegación real (automatización tipo Chrome vía herramientas de navegador), login con formulario, clics y rutas directas; verificación complementaria en PostgreSQL.

**Alcance declarado:** se cubrieron en profundidad **login, menús, rutas clave y pruebas de fuga por URL** en ambas escuelas para **admin** y **profesor**; **secretaria** y **estudiante** con flujos representativos en Cantón y extrapolación documentada donde no se repitió cada clic (misma versión de código). No se automatizó la suite completa de todos los formularios de negocio (creación masiva de datos, carnets PDF, etc.).

---

## 1. Escuelas usadas

| Escuela | `school_id` (UUID) |
|--------|---------------------|
| **Escuela A — Instituto Dr. Alfredo Canton** | `cc4e5e11-1be8-42de-8193-428f4484041c` |
| **Escuela B — Instituto Profesional y Técnico San Miguelito** | `6e42399f-6f17-4585-b92e-fa4fff02cb65` |

---

## 2. Usuarios creados (por rol y escuela)

**Contraseña común:** `Test#2026` (hash BCrypt en script SQL).

**Nota sobre naming:** el pedido original sugería emails `admin.<escuela>@test.local`; en datos se usaron slugs **`canton`** y **`sanmiguelito`** para mantener correos válidos y únicos. En base de datos el rol docente es **`teacher`** (requerido por `[Authorize(Roles = "teacher")]` en `TeacherGradebookController`).

| Rol (app) | Escuela A (Cantón) | Escuela B (San Miguelito) |
|-----------|-------------------|---------------------------|
| Admin | `admin.canton@test.local` | `admin.sanmiguelito@test.local` |
| Secretaria | `secretaria.canton@test.local` | `secretaria.sanmiguelito@test.local` |
| Profesor (DB: `teacher`) | `profesor.canton@test.local` | `profesor.sanmiguelito@test.local` |
| Estudiante | `estudiante.canton@test.local` | `estudiante.sanmiguelito@test.local` |

**Script de creación / reproducción:** `migration_artifacts/insert_e2e_roles_per_school.sql` (incluye `INSERT` en `student_payment_access` para activar plataforma a los estudiantes E2E).

**Verificación SQL (muestra):** usuarios `@test.local` con `school_id` y `platform_access_status` para estudiantes E2E en `Activo`.

---

## 3. Resultado por rol

### Admin

- **Escuela A:** login con institución Cantón + `Test#2026` → dashboard con nombre de escuela Cantón; **`/User/Index`** (gestión de usuarios) accesible.
- **Escuela B:** login admin San Miguelito → **`/User/Index`**; cabecera **Instituto Profesional y Técnico San Miguelito**; búsqueda por `admin.canton@test.local` → **sin coincidencias en la página** (aislamiento de listado en UI).
- **URL directa (cross-tenant):** como admin Cantón, `StudentAssignment/Assign` con ID de estudiante de otra escuela → **404** (no exposición del formulario).

### Secretaria

- **Escuela A:** login OK; menú acotado (p. ej. Secretaría); **`/User/Index`** → **403 Acceso denegado** (coherente con control solo `admin` en `UserController`).
- **`/StudentAssignment/Index`:** accesible; contexto de escuela Cantón en UI.

### Profesor (`teacher`)

- **Escuela A:** login → **`/TeacherGradebook/Index`** “Portal del Docente”, escuela Cantón; pestañas Notas / Asistencias / Disciplina / Consejería visibles; **`/User/Index`** → **403** (no gestión de usuarios).
- **Escuela B:** tras cerrar sesión e inicio secuencial correcto (institución + correo + contraseña), **`/TeacherGradebook/Index`** con **San Miguelito** y usuario **E2E Profesor SanMiguelito Docente**.

### Estudiante

- Sin fila en `student_payment_access`: redirección a **`/Student/AccessPending`** (comportamiento de negocio / Club de Padres).
- Tras **`platform_access_status = Activo`** en `student_payment_access` para los E2E: **`/Home/Index`** dashboard estudiante; **`/StudentReport/Index`** “Reporte del Estudiante” con nombre propio; pestañas Calificaciones / Asistencia / Disciplina.
- **`/User/Index`:** no aplicable / esperado denegado para rol estudiante en rutas de administración (no se forzó en esta corrida tras cada cambio de rol).
- **Carnet (`StudentIdCard`):** el controlador está restringido a **SuperAdmin**; ruta probada → **404** en sesión estudiante (no es “carnet del alumno” en portal; es módulo administrativo).

---

## 4. Resultado por escuela (A vs B)

| Criterio | Escuela A (Cantón) | Escuela B (San Miguelito) |
|----------|-------------------|---------------------------|
| Contexto UI (nombre de escuela) | Correcto | Correcto |
| Admin usuarios | Lista y búsqueda en contexto local | Lista local; sin usuario admin de otra escuela en búsqueda |
| Profesor portal | OK | OK |
| Aislamiento URL asignación (muestra cruzada) | 404 en ID de otra escuela | Patrón equivalente esperado (misma implementación) |

---

## 5. Problemas críticos

1. **IDOR / fuga cross-tenant en API de reporte del estudiante**  
   Con sesión de **`estudiante.canton@test.local`**, `GET /StudentReport/GetTrimesterData?studentId=<UUID_estudiante_San_Miguelito>&trimester=1T` respondió **HTTP 200** (cuerpo JSON no capturado en herramienta, pero la respuesta no fue 403/404).  
   En código, el método **no compara** `studentId` con el usuario autenticado y delega en `GetReportByStudentIdAndTrimesterAsync(studentId, …)` sin comprobación de `SchoolId` del actor.

```111:117:c:\Proyectos\EduplanerMultitenant\SchoolManager\Controllers\StudentReportController.cs
    public async Task<IActionResult> GetTrimesterData(Guid studentId, string trimester)
    {
        try
        {
            _logger.LogInformation("=== INICIO GetTrimesterData - StudentId: {StudentId}, Trimester: {Trimester} ===", studentId, trimester);

            var report = await _reportService.GetReportByStudentIdAndTrimesterAsync(studentId, trimester);
```

   **Impacto:** cualquier usuario con rol autorizado en `StudentReportController` (incluye `estudiante`) podría consultar datos académicos de **otro estudiante u otra escuela** si conoce o adivina GUIDs. Mismo patrón de riesgo en **`ExportDisciplinePdf(Guid studentId, …)`** (parámetro arbitrario).

2. **Riesgo de producto confundido con fallo de seguridad:** carnet en `StudentIdCard` solo superadmin — correcto por diseño de roles, pero **no satisface** el flujo “estudiante ve carnet” sin otro canal (p. ej. vista propia o app padres).

---

## 6. Problemas importantes

1. **Login multi-paso sensible al orden:** seleccionar **institución** antes que correo/contraseña evita fallos; ejecución en paralelo de acciones de formulario puede dejar sesión anterior (se observó al intentar login docente estando aún como admin).
2. **`TeacherGradebookController` solo acepta rol literal `teacher`:** coherente con datos E2E; en UI el combo de creación de usuarios muestra “Docente (value: Teacher)” — hay que asegurar que claims en login coincidan (en BD está `teacher` y funcionó).
3. **Página 403 “Acceso denegado”** muestra pie **© 2025** mientras otras vistas muestran **2026** — inconsistencia cosmética.
4. **Estudiantes nuevos sin `student_payment_access`:** quedan bloqueados en AccessPending hasta que Club de Padres / datos activen plataforma — documentado; para E2E se añadió fila en SQL.

---

## 7. Evidencia (UI vs DB)

| Evidencia | UI | Base de datos |
|-----------|----|-----------------|
| Usuarios E2E | Login y menús por rol | `SELECT … FROM users JOIN schools … WHERE email LIKE '%@test.local'` — `school_id` alineado al colegio esperado |
| Estudiante plataforma | Antes AccessPending; después dashboard y `StudentReport` | `student_payment_access.platform_access_status = 'Activo'` para emails E2E estudiante |
| Integridad asignaciones | — | `student_assignments` vs `users.school_id`: **0** filas con `school_id` distinto (`COUNT` = 0) |
| IDOR | **200** en `GetTrimesterData` con `studentId` de otra escuela | Estudiante de prueba San Miguelito tiene datos (`student_activity_scores` > 0 para ese `student_id`) |

---

## 8. Riesgos en producción

- **Exfiltración de datos académicos y disciplina** vía endpoints JSON/PDF con `studentId` manipulable (CRÍTICO).
- **Enumeración de GUIDs** combinada con el punto anterior.
- Dependencia de **filtros globales EF** sin defensa en profundidad en controladores que aceptan IDs en query string.
- **Operadores humanos** confundiendo “Docente” vs claim `teacher` → tickets de acceso.

---

## 9. Veredicto final

| Pregunta | Veredicto |
|----------|-----------|
| **¿Sistema seguro (aislamiento multi-escuela + estudiante)?** | **NO SEGURO** — por el IDOR confirmado en comportamiento HTTP y respaldo en código en `StudentReportController` / servicio asociado. |
| **¿Listo para producción?** | **NO LISTO** — bloqueante de seguridad hasta corregir autorización (enlazar `studentId` al usuario actual o validar `SchoolId` + pertenencia del estudiante al tenant y al actor). |

**Fortalezas observadas:** listados de admin acotados por búsqueda local; **404** en asignación cruzada probada; **403** por rol en `User` para secretaria/profesor; portal docente y contexto de escuela coherentes.

**Recomendación técnica inmediata:** en todo endpoint que reciba `studentId` (o IDs de entidades hijas), validar: `CurrentUser` → `SchoolId` → fila `users` / `student_assignments` / política de “solo self” para estudiante, antes de llamar al servicio de reporte.

---

*Fin del informe.*
