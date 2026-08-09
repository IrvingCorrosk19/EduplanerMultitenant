# Simulación completa del sistema Eduplaner (colegio real)

**Alcance:** guía de simulación end-to-end como **admin, secretaría, profesor y estudiante**, alineada con las pantallas y controladores reales del repositorio `SchoolManager`.  
**Fecha:** 4 de mayo de 2026.

---

## Estado de esta ejecución (honestidad operativa)

| Comprobación | Resultado |
|--------------|-----------|
| App en `http://127.0.0.1:5173` | Respondía en sesión de prueba previa; en esta sesión el login **SuperAdmin** (`superadmin@schoolmanager.com` / `Admin123!`) devolvió **Error de Autenticación** (credencial o usuario distinto al desplegado en esta BD). |
| Creación de 2 escuelas nuevas vía UI | **No completada** en esta sesión (bloqueada por autenticación SuperAdmin). |
| Validación SQL directa (`psql`) | **`psql` no está en PATH** en el agente. |
| Docker para `psql` | **Daemon Docker no disponible** (`dockerDesktopLinuxEngine` no accesible). |
| Criterio estricto del enunciado (“solo válido si todo el flujo funciona…”) | **No se puede marcar como cumplido** solo con esta corrida: faltan evidencias en vivo de fases 1 y 9 contra BD, y el alta masiva de estructura académica + actividades + notas en **dos** escuelas recién creadas. |

**Recomendación:** repetir la simulación con PostgreSQL accesible, credenciales SuperAdmin confirmadas para esa BD, y Playwright o checklist manual siguiendo las rutas de la tabla “Mapa de fases” más abajo.

---

## Mapa de fases → rutas y roles (producto real)

| Fase | Objetivo | Quién | Rutas / acciones principales |
|------|-----------|-------|------------------------------|
| 1 | Crear escuelas | **superadmin** | `GET/POST /SuperAdmin/CreateSchoolWithAdmin`, lista `GET /SuperAdmin/ListSchools`. Servicio: `SuperAdminService.CreateSchoolWithAdminAsync` (genera `School.Id` único, admin con `SchoolId`, año académico por defecto, bloques horarios). |
| 2 | Usuarios por escuela | **admin** (por colegio) | `GET /User/Index`, creación vía `POST /User/CreateJson`, edición `GET|POST /User/Edit/{id}`. **Solo rol `admin`** en `UserController`. |
| 3 | Estructura académica | **admin** / **director** / **secretaria** (según acción) | Grados: `GradeLevelController`; Grupos: `GroupController`; Materias: `SubjectController`; Asignación docente–materia–grupo: `TeacherAssignmentController`, `SubjectAssignmentController`. |
| 4 | Matrícula / asignación estudiantil | **admin** / **secretaria** | **`/Student/Create` está deshabilitado** (`StudentController` devuelve `Forbid()`). Flujo realista: usuarios rol `estudiante` creados por admin + **`/StudentAssignment/Index`** (asignación grado/grupo/materias) + cargas masivas si aplica. |
| 5 | Actividades, notas, asistencia | **teacher** | **`/TeacherGradebook/Index`**: pestañas Registrar notas, Asistencias, etc. Actividades administrativas: `ActivityController` (roles amplios incl. secretaría/admin). |
| 6 | Portal estudiante | **estudiante** | **`/StudentReport/Index`** (notas, asistencia en pestañas). Perfil: `StudentProfile`. Carnet masivo: **`/StudentIdCard/*`** restringido a **SuperAdmin** (estudiante típico no ve carnet por esa UI). |
| 7 | Multi-escuela | Todos los anteriores × 2 `school_id` | Login con selector de institución cuando el correo existe en varios colegios (`AuthController`). Filtros de tenant en servicios y pruebas `e2e/tests/ownership.spec.ts`. |
| 8 | Ruptura / IDOR | Cualquier rol | URLs con `Guid` de otra escuela → **`/Auth/AccessDenied`** (validado en sesión manual previa para `User/Edit` y `Student/Details`). |
| 9 | Validación BD | Operador | Consultas sobre `schools`, `users`, `student_assignments`, `activities`, `student_activity_scores`, `attendances` (nombres exactos según migraciones del modelo). |

---

## 1. Escuelas creadas

### Opción A — Greenfield (lo pedido en el enunciado)

1. Iniciar sesión como **SuperAdmin** (`/Auth/Login` sin institución obligatoria si el usuario es global).
2. **`/SuperAdmin/CreateSchoolWithAdmin`**: completar **Escuela A** + admin (email único, contraseñas coincidentes en `ConfirmPassword`).
3. Repetir para **Escuela B** (otro email de admin obligatorio).
4. Verificar en **`/SuperAdmin/ListSchools`** mensaje de éxito y filas nuevas.

**Validación `SchoolId`:** cada fila en `schools` tiene `id` UUID; `users.school_id` del admin creado debe coincidir con ese `id`; `schools.admin_id` apunta al usuario admin.

### Opción B — Stand-in con datos E2E ya sembrados (repositorio)

Si no se crean escuelas nuevas, usar como **Escuela A** y **Escuela B** los UUID fijos de pruebas:

| Etiqueta | Nombre (catálogo típico) | `school_id` (GUID) |
|----------|--------------------------|---------------------|
| Escuela A | Instituto Dr. Alfredo Canton | `cc4e5e11-1be8-42de-8193-428f4484041c` |
| Escuela B | Instituto Profesional y Técnico San Miguelito | `6e42399f-6f17-4585-b92e-fa4fff02cb65` |

Script de referencia: `migration_artifacts/insert_e2e_roles_per_school.sql`.

**En esta sesión:** no se registraron altas nuevas de escuela en BD por falta de SuperAdmin autenticado.

---

## 2. Usuarios creados

### Por escuela (plantilla operativa)

Para **cada** `school_id`:

| Rol | Email sugerido (único) | Notas |
|-----|------------------------|--------|
| admin | `admin.escuelaX@...` | Creado junto con la escuela (fase 1) o vía SuperAdmin según política. |
| secretaria | vía **`/User/Index`** solo si el actor es **admin** | `UserController` está `[Authorize(Roles = "admin")]`. |
| teacher | idem | Rol `teacher` / docente según convención del formulario. |
| estudiante | idem | Rol `estudiante`; acceso plataforma puede requerir fila en `student_payment_access` (ver script E2E). |

**Login:** `POST` login con `SchoolId` en el combo cuando hay homónimos.

**Validación:** en BD, `users.school_id` del usuario debe ser el GUID de su colegio; **superadmin** debe tener `school_id` NULL.

**E2E existente (no creado en esta sesión):** `admin.canton@test.local`, `secretaria.canton@test.local`, `profesor.canton@test.local`, `estudiante.canton@test.local` (Cantón) y análogos San Miguelito; contraseña de pruebas `Test#2026` en `e2e/tests/fixtures.ts`.

---

## 3. Estructura académica

Orden sugerido (evita FKs huérfanas):

1. **Año académico** — al crear escuela el servicio intenta `EnsureDefaultAcademicYearForSchoolAsync`; confirmar en UI catálogo académico si la escuela lo usa en pantallas de asignación.
2. **Grados** — `GradeLevel` (UI según menú admin/director).
3. **Grupos** — ligados a grado (`Group`).
4. **Materias** — `Subject` con `school_id`.
5. **Asignación docente** — `TeacherAssignment` / `SubjectAssignment` para ligar **profesor → materia + grado + grupo** (y trimestre si aplica en el flujo de negocio).

**Validación “sin duplicados”:** índices únicos multi-tenant (revisar migraciones `FixUniqueIndexesMultiTenant` y similares); en UI, intentar crear el mismo código duplicado y esperar error de validación o 400.

**En esta sesión:** no se ejecutó la secuencia completa en dos colegios nuevos; el código y los E2E asumen Cantón/San Miguelito ya poblados.

---

## 4. Actividades creadas

Como **profesor** en **`/TeacherGradebook/Index`** (pestaña **Registrar Notas**):

- Formulario “Agregar Nueva Actividad”: nombre, tipo (tarea / examen / apreciación según combo), fecha, archivo opcional.
- Tras **Agregar**, la actividad debe listarse y generar columnas/celdas en el libro.

**Referencia automatizada:** `e2e/tests/teacher-gradebook-flow.spec.ts` (depende de que el docente E2E tenga `#selGroup` con opciones).

**En esta sesión:** no se crearon actividades nuevas con evidencia persistida en BD.

---

## 5. Notas registradas

- En el mismo portal docente, celdas `.gradebook-score-cell`: editar, blur, verificar valor (ej. `8.5` en E2E).
- **Promedios:** calculados en cliente/servidor según implementación del módulo; validar con 2–3 notas y regla de redondeo esperada por negocio.

**En esta sesión:** no se verificaron promedios numéricos contra una hoja de cálculo de referencia.

---

## 6. Asistencia registrada

- Pestaña **Asistencias** en `TeacherGradebook/Index`: seleccionar fecha, materia/grupo, marcar ausencias/tardanzas, guardar.
- Validar **lista coherente** con estudiantes del grupo y **sin duplicados** por (fecha, estudiante, materia) según restricciones de BD.

**En esta sesión:** no se persistió asistencia de prueba verificada en BD.

---

## 7. Validación estudiante

- **`/StudentReport/Index`:** pestañas Calificaciones, Actividades pendientes, Asistencia, Disciplina.
- **Solo su información:** comparar `student-id` oculto en la vista con el usuario logueado; intentar `Student/Details/{otro}` → debe ser **403** (probado en `PRUEBA_BROWSER_REAL.md` / `ownership.spec.ts`).
- **Carnet:** ruta típica **`/StudentIdCard/ui`** es **SuperAdmin**; el estudiante verá **403** si fuerza la URL — documentar como limitación de UX si el negocio exige carnet en portal del alumno.

---

## 8. Problemas detectados (relevantes para la simulación)

1. **Alta de estudiante “tipo CRM” en `/Student/Create`:** no existe para ningún rol; la matrícula real pasa por **usuarios estudiante + asignaciones**.
2. **Secretaría no gestiona usuarios** en `UserController` (solo **admin**); la secretaría trabaja **asignaciones**, catálogos autorizados, etc.
3. **SuperAdmin** necesario para **nuevas escuelas**; sin esa cuenta no hay fase 1 greenfield.
4. **Carnet estudiantil** vía `StudentIdCard` no alineado con rol estudiante (diseño actual).
5. **Consola / CSP** en páginas pesadas (`User/Index`): fuentes Bootstrap Icons bloqueadas; logs de depuración — ver `PRUEBA_BROWSER_REAL.md`.

---

## 9. Correcciones necesarias (producto / QA)

| Prioridad | Acción |
|-----------|--------|
| Alta | Documentar en manual de operador que **`Student/Create` está cerrado** y cuál es el flujo oficial de alta de alumno. |
| Alta | Asegurar entorno de simulación: **SuperAdmin** conocido, PostgreSQL accesible, Cloudinary opcional para fotos (el arranque loguea críticos si faltan credenciales). |
| Media | Si el negocio pide carnet al estudiante: exponer flujo autorizado (rol y pago) o página de solo lectura distinta de SuperAdmin. |
| Media | Reducir ruido en consola en vistas con DataTables en producción. |
| Baja | Alinear pie de página (años 2025 vs 2026) en todas las vistas de error. |

---

## 10. Validación multi-escuela

### Comportamiento esperado

- Listas desplegables de grado/grupo/materia **filtradas** por `school_id` del usuario.
- Manipular IDs en URL de otra escuela → **403** / AccessDenied (comportamiento observado para edición de usuario y detalle de estudiante ajeno).

### Datos de prueba cruzada (fixtures)

- Admin otra escuela: `b0b35595-cc47-4a3e-9233-1c57809daca5` (San Miguelito).
- Estudiante otra escuela (user id fijo en script): `2e3ed445-d285-4d7d-b262-5e8fcd3c3cec`.

### En esta sesión

- **No** se repitió todo el ciclo académico en las dos escuelas con evidencia nueva.
- Aislamiento **sí** está cubierto por pruebas de ownership en `e2e/tests/ownership.spec.ts` y por prueba manual documentada en `PRUEBA_BROWSER_REAL.md`.

---

## Fase 9 — Consultas SQL sugeridas (cuando `psql` o cliente esté disponible)

Conexión de desarrollo típica: `appsettings.Development.json` → `Host=localhost;Database=eduplaner;...`

```sql
-- Escuelas recientes y admins
SELECT s.id, s.name, s.admin_id, u.email AS admin_email
FROM schools s
LEFT JOIN users u ON u.id = s.admin_id
ORDER BY s.created_at DESC
LIMIT 10;

-- Usuarios por escuela (conteo por rol)
SELECT school_id, lower(trim(role)) AS role, COUNT(*)
FROM users
WHERE school_id IS NOT NULL
GROUP BY school_id, lower(trim(role))
ORDER BY school_id, role;

-- Asignaciones estudiantiles (muestra)
SELECT sa.school_id, COUNT(*) AS assignments
FROM student_assignments sa
GROUP BY sa.school_id;

-- Actividades recientes por escuela (ajustar nombre de tabla si el modelo difiere)
SELECT a.school_id, COUNT(*) AS activities
FROM activities a
GROUP BY a.school_id;
```

(Ajustar nombres de tablas/columnas según el `SchoolDbContext` y migraciones vigentes si alguna consulta falla.)

---

## Automatización repetible

Desde la raíz del repo, con app y BD levantadas:

```bash
cd e2e
npm ci
npx playwright test
```

Tests relevantes: `login.spec.ts`, `admin-crud.spec.ts`, `teacher-gradebook-flow.spec.ts`, `secretary-assignment.spec.ts`, `student-portal.spec.ts`, `ownership.spec.ts`, `roles-smoke.spec.ts`.

---

## Criterio final del enunciado

| Requisito | Estado respecto a esta sesión |
|-----------|--------------------------------|
| Todo el flujo funciona | **No demostrado de punta a punta** (bloqueos SuperAdmin + sin SQL). |
| Sin errores | **No verificado** en todas las fases. |
| Sin mezcla de datos | **Parcialmente** (diseño + E2E + pruebas manuales de 403). |
| Notas y promedios correctos | **No auditado** numéricamente. |
| Roles correctos | **Parcialmente** (rutas y `[Authorize]` revisados; flujos completos no ejecutados en dos escuelas nuevas). |

**Conclusión:** el documento define **cómo** ejecutar la simulación completa en producción o preproducción con evidencia UI+BD. La **validación integral** del criterio estricto queda **pendiente** hasta completar fases 1, 5–6 y 9 en un entorno con credenciales y base de datos operativos.

---

*Generado como entregable de simulación; complementar con capturas y resultados de SQL tras ejecutar el ciclo en su instancia.*
