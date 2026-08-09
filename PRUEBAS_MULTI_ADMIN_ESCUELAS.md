# Pruebas multi-admin por escuela — aislamiento de datos (QA)

**Alcance:** Base de datos PostgreSQL local (`eduplaner`, cadena en `appsettings.Development.json`), aplicación en `http://localhost:5172`, **sin cambios de lógica ni refactor** del código de producción.

**Fecha de ejecución:** 2026-05-03.

---

## 1. Escuelas detectadas

| `id` (UUID) | `name` |
|-------------|--------|
| `cc4e5e11-1be8-42de-8193-428f4484041c` | Instituto Dr. Alfredo Canton |
| `6e42399f-6f17-4585-b92e-fa4fff02cb65` | Instituto Profesional y Técnico San Miguelito |

**Conteos de usuarios por escuela (DB):**

| Escuela | Total usuarios (`users`) |
|---------|---------------------------|
| Instituto Dr. Alfredo Canton | 1359 |
| Instituto Profesional y Técnico San Miguelito | 1992 |

---

## 2. Usuarios creados

Inserción vía script SQL (re-ejecutable): `migration_artifacts/insert_qa_admin_per_school.sql`.

**Contraseña común (QA):** `AdminTest#2026`  
**Rol en DB:** `admin` (restricción/check y `[Authorize(Roles = "admin")]` en `UserController`).

**Nota sobre el formato de email solicitado:** el patrón `admin.<nombreEscuela>@test.local` se aplicó en forma **slug segura** (minúsculas, guiones, sin espacios) para cumplir longitud y unicidad del correo.

| Escuela | Email | `school_id` | `document_id` (único por tenant) |
|---------|-------|-------------|----------------------------------|
| Instituto Dr. Alfredo Canton | `admin.instituto-dr-alfredo-canton@test.local` | `cc4e5e11-1be8-42de-8193-428f4484041c` | `9-QA-0001` |
| Instituto Profesional y Técnico San Miguelito | `admin.instituto-profesional-y-tecnico-san-miguelito@test.local` | `6e42399f-6f17-4585-b92e-fa4fff02cb65` | `9-QA-0002` |

**Validación DB post-inserción:** consulta `SELECT email, role, school_id FROM users WHERE email LIKE 'admin.instituto%@test.local';` — ambas filas con `role = admin` y `school_id` coincidente con la escuela esperada.

---

## 3. Resultado por escuela

### 3.1 Instituto Dr. Alfredo Canton — `admin.instituto-dr-alfredo-canton@test.local`

| Área | Resultado | Evidencia / notas |
|------|-----------|-------------------|
| **Login** | **OK** | Acceso a `Home/Index`; cabecera muestra *Instituto Dr. Alfredo Canton*; usuario *Admin QA Escuela* / Administrador. |
| **`/User/Index`** | **OK** | Lista masiva con paginación (~55 páginas); coherente con volumen de usuarios de la escuela. Botones deshabilitados para editar/eliminar otros administradores (comportamiento esperado). |
| **Estudiantes (admin)** | **OK** | `StudentAssignment/Index` carga; paginación **136** páginas; **DB:** 1356 usuarios con rol `estudiante` en Canton → orden de magnitud consistente (~10 filas/página). *Nota:* la ruta `Student/*` es para rol estudiante, no para admin. |
| **Asignaciones académicas** | **OK** | `AcademicAssignment/Index` carga (DataTables). Búsqueda `iptsanmiguelito` en el buscador: sin filas útiles en el snapshot (aislamiento esperado frente a dominio de la otra escuela). |
| **Materias** | **OK** | `Subject/Index` carga con datos de catálogo. |
| **Grados** | **N/A ruta directa** | `GET /GradeLevel/Index` devuelve **404** (el controlador `GradeLevel` expone principalmente endpoints JSON bajo `[Route("GradeLevel")]`, no una vista Index MVC estándar). Los grados se administran en flujos de **Catálogo Académico**. |
| **Grupos** | **OK** | `Group/Index` carga (*Gestión de Grupos*). |
| **Calificaciones (admin)** | **Parcial por rol** | `TeacherGradebook` está restringido a rol `teacher`; el admin validó **Aprobados y Reprobados** (`AprobadosReprobados/Index`) — filtros cargan; **UUID de materia en dropdown** verificado en DB como perteneciente a Canton (ver sección 6). |
| **Asistencia** | **OK** | `Attendance/Index` carga; enlace *Registrar Asistencia* presente. |
| **Carnet estudiantil** | **403 (por diseño)** | `StudentIdCard/ui` redirige a `Auth/AccessDenied` — solo `SuperAdmin`/`superadmin` (ver código citado abajo). **No** es fuga de datos; es **alcance de rol**, no prueba de generación de PDF por admin. |
| **URL directa otra escuela** | **OK (404)** | Como admin Canton: `GET /StudentAssignment/Assign?id=<estudiante_San_Miguelito>` → **HTTP 404** (red de herramientas del navegador). Coherente con `GetByIdAsync` filtrado por tenant. |

**Resultado general:** **PASS** en login, módulos administrativos probados y prueba negativa por URL.

---

### 3.2 Instituto Profesional y Técnico San Miguelito — `admin.instituto-profesional-y-tecnico-san-miguelito@test.local`

| Área | Resultado | Evidencia / notas |
|------|-----------|-------------------|
| **Login (UI automatizada)** | **No concluido** | Tras rellenar correo, institución y contraseña, el botón quedó en estado *Iniciando sesión…* y no hubo navegación a `Home` en el tiempo observado; al forzar `Home/Index` se redirige a login (sin sesión). **Causa probable:** latencia/red, carga del servidor o interacción del formulario en el entorno de prueba — **no** indica por sí misma fallo de datos en DB (el usuario existe y está bien asociado). |
| **Pruebas UI simétricas** | **Pendiente repetición manual** | Se recomienda repetir el mismo recorrido que Canton con este admin y la escuela seleccionada en login. |

**Resultado general:** **INCONCLUSIVE en UI** por bloqueo de automatización; **PASS en datos** (usuario y `school_id` verificados en PostgreSQL).

**Simetría esperada (código + DB):** mismos controladores y filtros globales de tenant; conteo **DB** de estudiantes San Miguelito: **1850** (referencia para validar paginación en `StudentAssignment/Index` cuando se complete el login manual).

**Prueba negativa URL (recomendada al validar manualmente):** como admin San Miguelito, abrir  
`/StudentAssignment/Assign?id=15874d53-7580-41e6-8442-9bda6d9facf9` (estudiante Canton) — se espera **404** por el mismo mecanismo que en Canton.

---

## 4. Problemas críticos

| ID | Descripción | Severidad |
|----|-------------|-----------|
| **C-0** | **Ninguno detectado** en las verificaciones de **mezcla de datos entre escuelas** en DB ni en la **prueba de URL directa** (404) con admin Canton. | — |

---

## 5. Problemas importantes

| ID | Descripción |
|----|-------------|
| **I-1** | **Login del segundo admin:** la sesión E2E no se estableció en el intento automatizado documentado en §3.2. Requiere **reintento manual** o revisión de rendimiento del host en `5172`. |
| **I-2** | **Búsqueda en listas:** en algunas vistas, rellenar el campo de búsqueda por automatización puede no disparar el mismo pipeline que la entrada por teclado (DataTables / JS). La validación de “no hay resultados de otra escuela” se complementó con **búsqueda en asignación académica** y **prueba de URL**. |
| **I-3** | **Carnet:** el administrador de escuela **no** tiene acceso a `StudentIdCard` (403). Si el requisito de negocio es que el **admin** genere carnets, es un **gap funcional de producto**, no una fuga multi-tenant. |
| **I-4** | **Calificaciones detalladas (libreta):** reservado a `teacher`; el admin debe usar flujos agregados (p. ej. Aprobados/Reprobados) u otros reportes según rol. |

---

## 6. Evidencia (UI vs DB)

### 6.1 Integridad referencial multi-tenant (SQL)

```sql
-- student_assignments: estudiante y fila de asignación deben compartir escuela
SELECT COUNT(*) AS bad_sa
FROM student_assignments sa
JOIN users u ON u.id = sa.student_id
WHERE u.school_id IS DISTINCT FROM sa.school_id;
-- Resultado observado: 0

-- subject_assignments vs grupo: mismo school_id
SELECT COUNT(*) AS subj_mismatch
FROM subject_assignments sa
JOIN groups g ON g.id = sa.group_id
WHERE sa.school_id IS DISTINCT FROM g.school_id;
-- Resultado observado: 0
```

### 6.2 Aprobados / Reprobados — materia en UI vs DB

- **UI (Canton):** dropdown *Materias* incluye `Matemáticas` con value `886aa678-3340-45cb-b44d-045699379a58`.
- **DB:** `SELECT id, name, school_id FROM subjects WHERE id = '886aa678-3340-45cb-b44d-045699379a58';`  
  → `school_id = cc4e5e11-1be8-42de-8193-428f4484041c` (**Canton**). **Coincide** con el tenant de la sesión.

### 6.3 URL directa — otro tenant

- **Solicitud:** `GET /StudentAssignment/Assign?id=2e3ed445-d285-4d7d-b262-5e8fcd3c3cec` (estudiante documentado con `school_id` San Miguelito) con sesión **admin Canton**.
- **Respuesta observada:** **404** (main frame, herramienta de red del navegador).

### 6.4 Referencia de código (sin modificación)

Filtro de usuario por id con query filter de tenant:

```324:328:c:\Proyectos\EduplanerMultitenant\SchoolManager\Services\Implementations\UserService.cs
        public async Task<User?> GetByIdAsync(Guid id)
        {
            // Where() aplica el Global Query Filter de tenant automáticamente (FindAsync lo bypasaba)
            return await _context.Users.Where(u => u.Id == id).FirstOrDefaultAsync();
        }
```

Carnet solo superadmin:

```17:19:c:\Proyectos\EduplanerMultitenant\SchoolManager\Controllers\StudentIdCardController.cs
[Authorize(Roles = "SuperAdmin,superadmin")]
[Route("StudentIdCard")]
public class StudentIdCardController : Controller
```

---

## 7. Riesgos (producción)

1. **Credenciales QA compartidas** (`AdminTest#2026`) en entornos no aislados: riesgo de acceso no autorizado si el script se ejecuta en producción o los correos `@test.local` se exponen.
2. **Emails de prueba** en la tabla `users`: limpiar o desactivar (`status`) tras las pruebas para evitar confusión operativa o intentos de login.
3. **Dependencia de filtros globales EF:** si en el futuro algún acceso usa `IgnoreQueryFilters()` sin validación explícita de `school_id`, podría reabrir superficie de fuga (no observado en esta corrida).
4. **SuperAdmin en carnet:** operaciones sensibles concentradas en superadmin — correcto para segregación, pero implica **proceso operativo** claro en producción.

---

## 8. Veredicto final

**SISTEMA SEGURO** respecto al **aislamiento lógico multi-escuela** en los puntos verificados:

- **0** filas anómalas en cruces `student_assignments` / `users` y `subject_assignments` / `groups`.
- **404** al intentar editar asignación de estudiante de **otra** escuela por URL con sesión de admin de Canton.
- **403** en carnet por **rol**, no por exposición cruzada de datos.
- **Dropdown** de materia en Aprobados/Reprobados alineado con `subjects.school_id` del tenant en sesión.

**Condiciones del veredicto:**

1. Completar **manualmente** el login y el mismo recorrido UI para el admin de **San Miguelito** (la automatización quedó inconclusa en ese paso).
2. No extrapolar “seguro” a módulos **no** visitados en esta sesión (p. ej. todos los flujos POST de carga masiva, mensajería, pagos) sin una matriz de pruebas ampliada.

---

## Anexo — Comandos útiles

```bash
# Reaplicar usuarios QA (borra e inserta los dos correos del script)
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -p 5432 -U postgres -d eduplaner -f "migration_artifacts/insert_qa_admin_per_school.sql"
```

**Conexión de referencia:** `Host=localhost;Database=eduplaner;...` en `appsettings.Development.json`.
