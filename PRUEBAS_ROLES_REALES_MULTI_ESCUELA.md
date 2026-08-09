# Pruebas — Roles reales en DB, usuarios por escuela y E2E multi-colegio

**Fecha:** 2026-05-03  
**Entorno:** PostgreSQL 18 (`C:\Program Files\PostgreSQL\18\bin\psql.exe`), base `eduplaner` según `appsettings.json` (`Host=localhost;Database=eduplaner;…`). Aplicación: `http://localhost:5172`.  
**Restricciones respetadas:** sin cambios de código ni refactors; solo datos (usuarios) y pruebas en navegador.

---

## 1. Roles detectados (desde base de datos)

Consulta ejecutada:

```sql
SELECT DISTINCT LOWER(TRIM(role)) AS role
FROM users
WHERE role IS NOT NULL AND TRIM(role) <> ''
ORDER BY 1;
```

**Resultado (8 roles en uso en `users.role`):**

| # | Rol (valor en columna `users.role`) |
|---|-------------------------------------|
| 1 | `admin` |
| 2 | `clubparentsadmin` |
| 3 | `director` |
| 4 | `estudiante` |
| 5 | `inspector` |
| 6 | `secretaria` |
| 7 | `superadmin` |
| 8 | `teacher` |

**Nota:** no existe tabla `roles` / `user_roles` en el esquema revisado; el rol vive en `users.role` (texto).

---

## 2. Escuelas usadas

```sql
SELECT id, name FROM schools ORDER BY name;
```

| Escuela | `school_id` |
|---------|-------------|
| Instituto Dr. Alfredo Canton | `cc4e5e11-1be8-42de-8193-428f4484041c` |
| Instituto Profesional y Técnico San Miguelito | `6e42399f-6f17-4585-b92e-fa4fff02cb65` |

---

## 3. Usuarios usados / creados (por rol y escuela)

**Convención de correo:** `<rol>.canton@test.local` / `<rol>.sanmiguelito@test.local` (slug por escuela). **Profesor** en BD = `teacher` (no existe columna “profesor”).  
**Contraseña:** `Test#2026` (BCrypt `$2a$11$Hkz2nUqK5jO6YvigG4j7SOo49ZB7aypDT9tnTiWKbDF7xpbjJ.C1e`).

**Matriz actual en DB** (consulta verificada en esta sesión):

| Email | `role` | Escuela (`school_id`) |
|-------|--------|------------------------|
| `admin.canton@test.local` | admin | Cantón |
| `admin.sanmiguelito@test.local` | admin | San Miguelito |
| `secretaria.canton@test.local` | secretaria | Cantón |
| `secretaria.sanmiguelito@test.local` | secretaria | San Miguelito |
| `profesor.canton@test.local` | teacher | Cantón |
| `profesor.sanmiguelito@test.local` | teacher | San Miguelito |
| `estudiante.canton@test.local` | estudiante | Cantón |
| `estudiante.sanmiguelito@test.local` | estudiante | San Miguelito |
| `director.canton@test.local` | director | Cantón |
| `director.sanmiguelito@test.local` | director | San Miguelito |
| `inspector.canton@test.local` | inspector | Cantón |
| `inspector.sanmiguelito@test.local` | inspector | San Miguelito |
| `clubparentsadmin.canton@test.local` | clubparentsadmin | Cantón |
| `clubparentsadmin.sanmiguelito@test.local` | clubparentsadmin | San Miguelito |
| `superadmin.rolesmatrix@test.local` | superadmin | `NULL` (global) |

**Alta en esta sesión:** `director`, `inspector`, `clubparentsadmin` (×2 escuelas) y `superadmin.rolesmatrix@test.local`. Los demás ya existían de corridas anteriores.  
**Corrección de datos:** el primer `INSERT` vía PowerShell truncó el prefijo `$2a` del hash BCrypt; se ejecutó `UPDATE` de `password_hash` para los 7 usuarios nuevos + superadmin de matriz.

**Integridad (muestra):** `student_assignments` con `student_id` → `users` donde `users.school_id` difiere de `student_assignments.school_id`: **0** filas.

---

## 4. Resultado por rol (acceso, permisos, módulos)

Resumen **E2E en navegador** (muestras representativas) + coherencia con atributos `[Authorize]` conocidos del proyecto (solo lectura previa, sin modificar).

| Rol | Login `Test#2026` | Rutas / comportamiento observado |
|-----|-------------------|-----------------------------------|
| **admin** | Sí (sesiones previas + matriz) | `User/Index` OK; menú administración; búsqueda en usuarios sin cruzar colegio (ej. admin Cantón no aparece en búsqueda admin San Miguelito). |
| **secretaria** | Sí (matriz) | `StudentAssignment/Index` típico; `User/Index` → **403** (solo `admin`). |
| **teacher** | Sí | `TeacherGradebook/Index` “Portal del Docente”; `User/Index` → **403**. |
| **estudiante** | Sí (con `student_payment_access` en Activo si aplica) | Dashboard / `StudentReport/Index`; sin acceso a gestión de usuarios. |
| **director** | Sí (`director.canton` + institución Cantón) | **`/Director/Index`** “Portal del Director”, cabecera **Instituto Dr. Alfredo Canton**; **`/User/Index`** → **403**; **`/DisciplineReport/Index`** → **403**. |
| **inspector** | Sí (`inspector.canton` + Cantón) | Dashboard “QA Inspector”; **`/DisciplineReport/Index`** → **403**. |
| **clubparentsadmin** | Sí (`clubparentsadmin.canton@test.local` + Cantón) | **`/ClubParents/Students`** “Club de Padres — Estudiantes”, cabecera **Instituto Dr. Alfredo Canton**, usuario **QA Club Padres Administrador Club de Padres**; **`/User/Index`** → **403**. |
| **superadmin** | Parcial | Usuario en DB con hash corregido; intento de login en esta corrida **no reemplazó** la sesión activa (seguía inspector) → **`/SuperAdmin/Index`** no verificado como superadmin en esta pasada. Recomendación operativa: cerrar sesión explícita y login secuencial (institución vacía + correo + contraseña). |

### Hallazgo de permisos (bloqueante funcional)

En login, el claim de rol se emite con el valor **exacto** de `users.role` (`AuthService`: `ClaimTypes.Role` = `user.Role`). Varios controladores usan `[Authorize(Roles = "Director,Inspector,…")]` con **mayúscula inicial**, mientras la BD usa **`director`**, **`inspector`**, **`teacher`** en minúsculas.  
**Efecto comprobado en UI:** usuario **`director`** autenticado accede a **`/Director/Index`** (`[Authorize(Roles = "director")]`) pero recibe **403** en **`/DisciplineReport/Index`** (`Director`/`Inspector` con mayúscula). Igual para **`inspector`** en la misma ruta.

Esto es **inconsistencia rol BD vs cadenas en `[Authorize]`**, no un “aislamiento multi-tenant” fallido por sí mismo.

---

## 5. Resultado por escuela (comparativa)

| Tema | Cantón | San Miguelito |
|------|--------|-----------------|
| Contexto de escuela en cabecera tras login | Cantón en pruebas con institución seleccionada | San Miguelito en pruebas previas (admin / teacher) |
| Cobertura E2E en esta sesión | Director + Inspector + matriz previa admin/secretaria/teacher/estudiante | Menos toques en esta sesión; usuarios de matriz existentes en DB |
| Aislamiento listados admin | Búsqueda sin usuario de otro colegio (muestra previa) | Análoga expectativa |

**URL cruzada:** en corridas previas del mismo entorno, asignación con ID de otra escuela respondió **404** (buen signo para ese flujo concreto).

---

## 6. Problemas críticos

1. **Autorización por rol frágil por mayúsculas/minúsculas**  
   Usuarios reales con `role` en minúsculas (como en DB) **pierden** módulos donde `[Authorize]` lista `Director` / `Inspector` / `Teacher` en PascalCase, mientras otros endpoints usan minúsculas (`director`, `teacher`). Comportamiento **inconsistente** y difícil de explicar al usuario final (ve 403 “sin permiso” aunque “sí es director/inspector”).

2. **Exposición de datos entre estudiantes / tenants (IDOR)**  
   En la batería anterior del mismo proyecto se documentó acceso con sesión estudiante a `StudentReport/GetTrimesterData` con `studentId` ajeno devolviendo **HTTP 200** (ver `PRUEBAS_E2E_COMPLETAS_ROLES_ESCUELAS.md`). No se repitió el curl con cookie en esta sesión, pero el riesgo sigue siendo **crítico de seguridad** hasta auditoría/corrección en backend.

---

## 7. Problemas importantes

1. **Scripts SQL desde PowerShell:** los `$` de BCrypt se interpretan; riesgo de **hash inválido** si no se escapan (`$2a$…`). Corregido con `UPDATE` explícito en esta sesión.
2. **Login E2E:** orden **institución → correo → contraseña** y evitar paralelismo en automatización; si no, la sesión anterior persiste.
3. **Pie de página 403:** copyright **2025** vs otras vistas **2026** — menor, pero rompe percepción de calidad.
4. **Superadmin:** un solo usuario global en muchos despliegues; crear cuentas QA adicionales debe gobernarse por política (aquí solo laboratorio local).

---

## 8. Evidencia (UI vs DB)

| Evidencia | UI (navegador) | DB |
|-----------|----------------|-----|
| Roles distintos en producción de datos | Menú y título cambian por rol (ej. Portal Director, Portal Docente) | `DISTINCT role` en `users` → 8 valores |
| Director en Cantón | “Portal del Director”, enlace escuela Cantón | `director.canton@test.local` → `school_id` Cantón |
| Inspector en Cantón | “QA Inspector Inspector”, escuela Cantón | `inspector.canton@test.local` → `school_id` Cantón |
| Permiso Disciplina listado | **403** para director e inspector en `/DisciplineReport/Index` | `users.role` = `director` / `inspector` (minúsculas) |
| Club de Padres | Vista `ClubParents/Students` OK; sin acceso a `User/Index` | `clubparentsadmin` + `school_id` Cantón en fila de usuario |
| Matriz de prueba | Login con `Test#2026` tras corregir hash | 15 filas en consulta matriz `@test.local` + superadmin matrix |

---

## 9. Riesgos en producción

- **Roles en BD no alineados con cadenas `[Authorize]`** → funcionalidades “apagadas” para directores/inspectores reales o, si se normalizaran claims sin revisar todo, posible **elevación accidental** en endpoints mal alineados.
- **IDOR en reportes estudiantiles** si no se valida `studentId` contra actor y tenant (ver informe E2E previo).
- **Gestión de hashes** en migraciones manuales / scripts ops.

---

## 10. Veredicto final

| Pregunta | Veredicto |
|----------|-----------|
| **¿Sistema seguro (SaaS multi-escuela)?** | **NO SEGURO** — por el riesgo **crítico** de IDOR en reporte del estudiante documentado previamente y no corregido en esta actividad (sin cambiar código). |
| **¿Listo para producción?** | **NO LISTO** — además del punto de seguridad, la **inconsistencia de mayúsculas en roles** rompe permisos en módulos clave (ej. disciplina para director/inspector con roles tal como están en DB). |

**Fortalezas:** modelo de datos con `school_id` en usuarios; unicidad `(school_id, lower(email))`; conteo de desalineación `student_assignments` vs `users.school_id` en **0** en el momento de la consulta.

---

*Fin del informe.*
