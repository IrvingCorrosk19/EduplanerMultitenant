# Eduplaner — Informe final producción SaaS (DB + seguridad + E2E)

**Fecha:** 2026-05-03  
**Entorno validado:** PostgreSQL `localhost` / base `eduplaner` (cadena `DefaultConnection` en `appsettings.json`).  
**Herramienta:** `C:\Program Files\PostgreSQL\18\bin\psql.exe`

**Última verificación DB (re-ejecución autónoma):** consultas de roles, escuelas, matriz `role` × `schools.name`, y lista de 14 cuentas `*.canton` / `*.sanmiguelito` — resultados reflejados en §10.

> **Sobre la consigna “no detenerse hasta LISTO”:** un veredicto **LISTO PARA PRODUCCIÓN** exige evidencia reproducible (E2E navegador, matriz de endpoints, secretos). Declarar LISTO sin esa evidencia **incumpliría** estándar enterprise; este documento mantiene **NO LISTO** con brecha explícita y criterios de cierre.

---

## 1. Roles detectados desde DB

Origen: columna `users.role` (no existe tabla separada `user_roles` en este esquema).

Consulta ejecutada:

```sql
SELECT DISTINCT lower(trim(role)) AS role FROM users ORDER BY 1;
```

**Roles reales (8):**

| Rol (valor en BD) | Uso típico |
|-------------------|------------|
| `admin` | Administración escolar |
| `secretaria` | Secretaría |
| `teacher` | Docente |
| `estudiante` | Alumno |
| `director` | Dirección |
| `inspector` | Inspección |
| `clubparentsadmin` | Admin club de padres |
| `superadmin` | Plataforma (sin `school_id` en esta BD) |

**Nota:** No aparecen filas con rol `acudiente` / `parent` en el conjunto actual de `users`; si el producto los usa en otros entornos, validar migraciones o datos.

---

## 2. Estructura de `users`

Origen: `information_schema.columns` para `public.users`.

**Columnas relevantes (resumen):**

- `id` (uuid, PK)  
- `school_id` (uuid, nullable) — **único `NULL` observado en rol `superadmin`**  
- `name`, `last_name`, `email` (not null)  
- `password_hash` (not null)  
- `role` (not null)  
- `status`, `document_id`, fechas, contacto, flags pedagógicos, `photo_url`, etc. (32 columnas en total)

**Consistencia comprobada:**

```sql
SELECT role, COUNT(*) FROM users WHERE school_id IS NULL GROUP BY role;
-- superadmin | 2  (único rol sin escuela)

SELECT COUNT(*) FROM users
WHERE role IN ('estudiante','student') AND school_id IS NULL;
-- 0
```

---

## 3. Usuarios creados por rol / escuela

**Escuelas (2):**

| `school_id` | Nombre |
|-------------|--------|
| `cc4e5e11-1be8-42de-8193-428f4484041c` | Instituto Dr. Alfredo Canton |
| `6e42399f-6f17-4585-b92e-fa4fff02cb65` | Instituto Profesional y Técnico San Miguelito |

**Cuentas E2E canónicas** (formato `<rol>.<slug-escuela>@test.local`, contraseña documentada en script: **`Test#2026`** — hash BCrypt en `migration_artifacts/insert_e2e_roles_per_school.sql`):

| Email | `role` en BD | `school_id` |
|-------|----------------|-------------|
| `admin.canton@test.local` | admin | Canton |
| `secretaria.canton@test.local` | secretaria | Canton |
| `profesor.canton@test.local` | teacher | Canton |
| `estudiante.canton@test.local` | estudiante | Canton |
| `director.canton@test.local` | director | Canton |
| `inspector.canton@test.local` | inspector | Canton |
| `clubparentsadmin.canton@test.local` | clubparentsadmin | Canton |
| `admin.sanmiguelito@test.local` | admin | San Miguelito |
| … | … | … (misma matriz para `*.sanmiguelito@test.local`) |

**Comprobación en DB (muestra):** 14 filas devueltas por:

```sql
SELECT email, role, school_id FROM users
WHERE email LIKE '%.canton@test.local' OR email LIKE '%.sanmiguelito@test.local'
ORDER BY email;
```

**Acceso plataforma estudiantes E2E:**

```sql
SELECT u.email, spa.platform_access_status, spa.carnet_status
FROM users u
LEFT JOIN student_payment_access spa
  ON spa.student_id = u.id AND spa.school_id = u.school_id
WHERE u.email IN ('estudiante.canton@test.local','estudiante.sanmiguelito@test.local');
```

→ `platform_access_status = Activo` (evita bloqueo en `/Student/AccessPending`).

**Superadmin:** existen `admin@correo.com` y `superadmin.rolesmatrix@test.local` (`school_id` NULL); no se duplican cuentas E2E por escuela para `superadmin` (diseño multi-tenant habitual).

---

## 4. Pruebas E2E ejecutadas

| Tipo | Alcance en esta ejecución |
|------|---------------------------|
| **Navegador (clics / formularios)** | **No ejecutado** de extremo a extremo contra una instancia `https://localhost` en esta sesión (no hay registro de trazas Playwright / MCP browser guardadas). |
| **Guía para ejecución manual / automatizable** | `PRUEBAS_E2E_COMPLETAS_ROLES_ESCUELAS.md` — módulos: usuarios, estudiantes, asignaciones, catálogos, calificaciones, asistencia, carnet. |
| **Validación de datos previos al E2E** | **Sí:** roles, escuelas, usuarios E2E, `student_payment_access`, unicidad por escuela+email, ausencia de estudiantes sin `school_id`. |

**Login / rol / `SchoolId`:** la verificación **directa** fue por **consistencia de filas en DB** y existencia de hashes; **no** se verificó cookie de sesión ni redirección post-login en navegador en esta corrida.

---

## 5. Pruebas de ataque (diseño + estado del código)

| Ataque | Estado |
|--------|--------|
| URL `/User/Edit/{id}` otra escuela (admin escolar) | **Mitigado:** `UserController.RequireManagedUserSameSchoolAsync` → **403** si el usuario existe en otro tenant (consulta `IgnoreQueryFilters` + comparación `school_id`). |
| URL `/Student/Details/{id}` otro estudiante misma escuela | **Mitigado (rol estudiante):** `StudentController` exige `id == usuario actual` para vistas con `id`. |
| `GetUserJson` filtración hash | **Mitigado:** ya no se expone `PasswordHash` en la respuesta JSON. |
| Cambiar `SchoolId` en payload masivo | Parcialmente mitigado en flujos auditados (`CreateJson` fija escuela desde servicio actual); **revisión global DTO** pendiente. |
| Profesor llama ruta solo `admin` | Cubierto por `[Authorize(Roles = "admin")]` en `UserController`; otras rutas requieren matriz por controlador. |
| `Attendance/Details/{id}` otro tenant | **Filtrado en servicio:** `AttendanceService.GetByIdAsync` exige `attendance.SchoolId == schoolId` del actor → `null` → **404** (no **403** explícito; diferencia menor frente a requisito literal de 403). |
| `DisciplineReport/GetByStudent` / datos por `studentId` | **Mitigado en código:** `CallerMayAccessStudentDisciplineDataAsync` y helpers (`CanSameSchoolStaffViewStudentDisciplineAsync`, docente/padre) en `DisciplineReportController`. |

---

## 6. Problemas detectados

| Severidad | Problema |
|-----------|----------|
| **Proceso** | No hay evidencia automatizada de E2E navegador por rol × escuela × módulo en esta ejecución. |
| **Cobertura** | Muchos controladores con `{id}` sin patrón unificado documentado de **403 explícito** cuando el recurso existe fuera del tenant (varios devuelven **404** vía GQF/servicio). |
| **Overposting** | Endpoints que bindean entidades EF completas (`User`, `Attendance`, …) siguen siendo riesgo residual sin DTOs estrictos. |
| **Operacional** | Secretos en repo / historial (`ANALISIS_PRODUCCION_EDUPLANER.md` CRIT-01) — fuera del parche de código aquí. |

---

## 7. Correcciones aplicadas (código — resumen del repo)

| Área | Archivo / tema |
|------|----------------|
| Multi-tenant explícito usuarios | `Controllers/UserController.cs` — `RequireManagedUserSameSchoolAsync`, sin `PasswordHash` en `GetUserJson`. |
| IDOR reportes por `studentId` | `Controllers/StudentReportController.cs` — gate previo a datos/PDF. |
| IDOR lista estudiante | `Controllers/StudentController.cs` — solo propio `id`; índice sin listado de compañeros. |
| RBAC claims | `Services/Implementations/AuthService.cs` — `BuildRoleClaims`. |
| Build | `SchoolManager.csproj` — exclusión `_pwtemp\**\*.cs`. |

*(Detalle ampliado en `CORRECCION_AUTONOMA_EDUPLANER.md` y `CORRECCION_TOTAL_EDUPLANER.md`.)*

---

## 8. Cambios en DB (scripts)

| Script | Uso |
|--------|-----|
| `migration_artifacts/insert_e2e_roles_per_school.sql` | Semilla **8 usuarios** (4 roles base × 2 escuelas) + `student_payment_access`; incluye `DELETE` previo de emails fijos. **Ya aplicado** en el entorno comprobado (14 filas con emails `*.canton` / `*.sanmiguelito` incluyendo roles extra director/inspector/club). |

**En esta sesión no se ejecutó DDL nuevo** (índices `users`/`school_id` ya presentes: `IX_users_school_id`, `uq_users_school_email_ci`, etc.).

**Duplicados `subject_assignments`:** consulta por `(school_id, subject_id, grade_level_id, group_id, area_id, specialty_id)` → **0** grupos con `COUNT(*) > 1`.

---

## 9. Iteraciones realizadas

1. **Descubrimiento DB:** columnas `users`, roles distintos, escuelas, usuarios E2E, `student_payment_access`, integridad `school_id` por rol.  
2. **Ataque / código:** revisión de patrones conocidos y estado de fixes (`User`, `Student`, `StudentReport`, asistencia vía servicio).  
3. **Documentación:** consolidación en este informe; **sin** segunda vuelta completa de E2E navegador por límite de ejecución verificable aquí.  
4. **Re-validación DB:** agregación `COUNT(*)` por `lower(role)` y nombre de escuela (`JOIN schools`) — confirma distribución real de usuarios por tenant (ver §10).

---

## 10. Evidencia (UI vs DB)

| Comprobación | Resultado |
|--------------|-----------|
| 2 escuelas activas | Sí (`is_active = t` en ambas filas de `schools`). |
| Roles en BD | 8 valores distintos en `users.role`. |
| Usuarios de prueba por escuela + rol E2E | **14** cuentas `*.canton` / `*.sanmiguelito` (admin, secretaria, teacher, estudiante, director, inspector, clubparentsadmin × 2 escuelas). |
| Estudiantes sin escuela | 0 filas con rol estudiante y `school_id` NULL. |
| Superadmin sin escuela | 2 filas (esperado). |

**Distribución verificada por rol y escuela** (consulta ejecutada):

```sql
SELECT lower(u.role) AS role, s.name AS school, COUNT(*)
FROM users u
JOIN schools s ON s.id = u.school_id
GROUP BY 1, 2
ORDER BY 2, 1;
```

| role | school | count |
|------|--------|------:|
| admin | Instituto Dr. Alfredo Canton | 3 |
| clubparentsadmin | Instituto Dr. Alfredo Canton | 2 |
| director | Instituto Dr. Alfredo Canton | 1 |
| estudiante | Instituto Dr. Alfredo Canton | 1358 |
| inspector | Instituto Dr. Alfredo Canton | 1 |
| secretaria | Instituto Dr. Alfredo Canton | 1 |
| teacher | Instituto Dr. Alfredo Canton | 2 |
| admin | Instituto Profesional y Técnico San Miguelito | 4 |
| clubparentsadmin | Instituto Profesional y Técnico San Miguelito | 2 |
| director | Instituto Profesional y Técnico San Miguelito | 2 |
| estudiante | Instituto Profesional y Técnico San Miguelito | 1852 |
| inspector | Instituto Profesional y Técnico San Miguelito | 7 |
| secretaria | Instituto Profesional y Técnico San Miguelito | 9 |
| teacher | Instituto Profesional y Técnico San Miguelito | 125 |

*(Los nombres de escuela salen tal cual de la BD; puede haber espacios finales en el literal almacenado.)*

**UI:** no capturada en esta sesión (sin capturas ni logs HTTP archivados).

---

## 11. Riesgos restantes

1. Cobertura incompleta de **ownership 403** en todos los endpoints con `{id}`.  
2. **E2E navegador** no ejecutado como batería completa en esta corrida.  
3. **Mass assignment** y superficie JSON/admin API.  
4. **Credenciales / historial git** y rotación en producción.  
5. **Carnet / rutas SuperAdmin** — modelo distinto (sin `school_id` en actor); requiere reglas de negocio explícitas en revisiones periódicas.

---

## Veredicto final (obligatorio)

### ❌ NO LISTO PARA PRODUCCIÓN

**Justificación técnica (fintech-grade):**

1. Los criterios obligatorios del enunciado exigen **ausencia de endpoints inseguros** y **E2E por cada rol en cada escuela** con evidencia de no fuga de datos; **no** se aporta aquí trazabilidad completa de pruebas de UI ni pentest interno exhaustivo sobre toda la superficie MVC/API.  
2. Parte del aislamiento sigue apoyada en **GQF + null → 404**, no en **403 explícito** uniforme; eso es aceptable en muchos productos, pero **no cumple literalmente** la regla global “si `resource.school_id != user.school_id` → 403” en **todos** los recursos.  
3. Quedan riesgos explícitos en §11 sin cerrar con trabajo adicional medible (matriz endpoint × test × resultado).

**Condición sugerida para reevaluar a “LISTO”:**  
(i) Suite E2E mínima reproducible (p. ej. Playwright) con evidencia (reporte + video o trace);  
(ii) Matriz de endpoints con `{id}` firmada con test de URL manipulada;  
(iii) sign-off de seguridad sobre módulos financieros/carnet/calificaciones;  
(iv) secretos fuera del historial y solo por configuración de entorno en prod.

---

*Fin del informe. Los comandos SQL de las secciones 1–3 y 8 pueden repetirse en DEV/QA para regresión.*
