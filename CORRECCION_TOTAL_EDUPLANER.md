# Corrección total — Eduplaner SchoolManager

**Fecha de documento:** 2026-05-03  
**Alcance:** Continuación de hardening multi-tenant, seguridad (IDOR/RBAC), compilación y cierre de brechas puntuales documentadas en auditorías y pruebas funcionales.

---

## 1. Resumen ejecutivo

### Estado inicial

- El análisis `ANALISIS_PRODUCCION_EDUPLANER.md` (v2) ya declaraba muchos ítems críticos/importantes como resueltos (GQF, `FindAsync`, CSRF, Data Protection, etc.).
- Persistían riesgos y trabajo de tipo **SaaS enterprise**: coherencia **PascalCase vs roles en BD**, **IDOR** en endpoints que reciben `studentId` o `id` sin validar propiedad, **listados** que exponen datos de pares en el mismo tenant, y **ruido de compilación** por archivos bajo `_pwtemp/`.
- Las guías `PRUEBAS_*.md` (roles multi-escuela, E2E, admin multi-escuela) definen el comportamiento esperado; no sustituyen una suite automatizada ejecutada en CI en esta sesión.

### Estado final (tras esta iteración)

| Área | Mejora aplicada |
|------|-----------------|
| Compilación | Exclusión de `_pwtemp\**\*.cs` en `SchoolManager.csproj` (evita CS8802 y ensamblados duplicados). |
| RBAC / MEN-07 | `AuthService`: emisión de **varias** reclamaciones `ClaimTypes.Role` (`BuildRoleClaims`) alineadas con `[Authorize(Roles = "...")]` en controladores. |
| IDOR — reportes | `StudentReportController`: `AuthorizeStudentReportTargetAsync` antes de `GetTrimesterData` y exportación PDF. |
| IDOR — perfil `Student` | `StudentController`: estudiante solo ve/edita/borra **su** `id`; índice sin listado de compañeros; creación denegada. |
| Build | Proyecto compila correctamente (verificación con salida alternativa `-o` cuando `SchoolManager.exe` está bloqueado por un proceso en ejecución). |

**Conclusión breve:** se reducen vectores concretos (roles, reportes por `studentId`, enumeración de estudiantes en `Student/Index`). El sistema **no** queda auditado de punta a punta contra todos los endpoints en esta sola entrega.

---

## 2. Problemas corregidos (detalle)

| ID / tema | Descripción | Mitigación |
|-----------|-------------|------------|
| COMP-01 | `_pwtemp/Program.cs` y otros `.cs` bajo `_pwtemp` participaban en la compilación del proyecto principal. | `<Compile Remove="_pwtemp\**\*.cs" />` en `SchoolManager.csproj`. |
| RBAC-01 | Roles en BD en minúsculas vs `[Authorize(Roles = "Director", "Teacher", ...)]` → 403 falsos o accesos incoherentes (MEN-07). | `AuthService.BuildRoleClaims`: emite rol crudo + variantes esperadas (Director, Teacher/Docente, Admin, SuperAdmin, etc.). |
| SEC-01 | `StudentReportController` aceptaba `studentId` en JSON/PDF sin comprobar relación con el actor (IDOR cross-tenant y parent/student). | `AuthorizeStudentReportTargetAsync`: superadmin libre; estudiante = mismo id; acudiente = self o vínculo en `Prematriculations`; staff = `SchoolId` del objetivo = escuela actual. |
| SEC-02 | `StudentController` autorizado a `estudiante`: `GetAllAsync()` listaba **todos** los estudiantes de la escuela; `Details/Edit/Delete` permitían otro `id` de la misma escuela. | Ownership estricto `id == currentUserId`; `Index` solo el registro propio (o vacío); `Create` → `Forbid`. |

---

## 3. Cambios en código

| Archivo | Qué se corrigió |
|---------|------------------|
| `SchoolManager.csproj` | Exclusión de compilación de `_pwtemp\**\*.cs`. |
| `Services/Implementations/AuthService.cs` | Claims de rol múltiples vía `BuildRoleClaims` en el login. |
| `Controllers/StudentReportController.cs` | Inyección de `SchoolDbContext`, gate `AuthorizeStudentReportTargetAsync` en acciones sensibles. |
| `Controllers/StudentController.cs` | Inyección de `ICurrentUserService`, restricción de `Index`/`Details`/`Edit`/`Delete` y denegación de `Create`. |

---

## 4. Cambios en base de datos

**En esta sesión no se ejecutaron scripts DDL/DML** contra PostgreSQL (entorno local no validado aquí con `psql`).

Ítems ya contemplados en el repositorio / análisis previo (referencia `ANALISIS_PRODUCCION_EDUPLANER.md`):

- Migraciones multi-tenant (índices únicos compuestos con `SchoolId`, etc.).
- Limpieza o deduplicación de datos: **pendiente de ejecutar en DEV** según necesidad y backups.

**Recomendación:** en DEV, tras backup, revisar duplicados funcionales (ej. `subject_assignments`) con consultas agregadas y corregir con scripts idempotentes documentados en el propio ticket.

---

## 5. Validaciones realizadas

| Tipo | Resultado |
|------|-----------|
| Compilación | `dotnet build -o _verify_build_out2` → **Correcto** (0 errores). Nota: `dotnet build` por defecto puede fallar con MSB3027 si `bin\Debug\net8.0\SchoolManager.exe` está en uso; cerrar el proceso o usar `-o` en carpeta distinta. |
| E2E manual (Admin A / B, Secretaria, Profesor, Estudiante) | **No ejecutado en esta sesión** como batería completa; las listas de casos siguen en `PRUEBAS_E2E_COMPLETAS_ROLES_ESCUELAS.md`, `PRUEBAS_ROLES_REALES_MULTI_ESCUELA.md`, `PRUEBAS_FUNCIONALES_ADMIN_MULTI_ESCUELA.md`. |
| PostgreSQL (`C:\Program Files\PostgreSQL\18\bin`) | **No ejecutado** en esta sesión; usar cadena de `appsettings` / variables de entorno para comprobar `school_id`, FKs y duplicados. |
| Pentest interno exhaustivo | **Parcial** (solo superficies tocadas arriba). |

---

## 6. Problemas pendientes y riesgos

1. **Historial git con secretos (CRIT-01):** sigue siendo acción manual (BFG / `git filter-repo` + rotación en Render).
2. **IDOR en otros controladores:** rutas con `Guid id` (p. ej. APIs JSON en `UserController`, otros módulos) requieren revisión sistemática **servicio + controlador** (no solo GQF).
3. **`UserController`:** `[Authorize(Roles = "admin")]` — confirmar si `Secretaria` / `Director` deben gestionar usuarios; si sí, ampliar roles y mantener comprobación de escuela.
4. **Vínculo acudiente–estudiante:** el gate de reportes usa `Prematriculations`; si hay otra fuente de verdad, extender la comprobación.
5. **`GetCurrentUserRoleAsync`:** si solo lee el **primer** claim de rol, revisar callers tras multi-claim (comportamiento mayormente cubierto por `AuthorizeStudentReportTargetAsync` con lectura de `User.FindAll(ClaimTypes.Role)` donde aplica).
6. **GQF en asignaciones (backlog análisis):** `StudentAssignment` / `TeacherAssignment` sin `SchoolId` directo en entidad — dependencia de JOINs; validar filtros en consultas críticas.
7. **Mass assignment / DTOs:** revisión global de POST que bindean entidades completas (`Student`, `User`, etc.).
8. **Performance:** `AsNoTracking`, proyecciones y N+1 en listados grandes — auditoría aparte.
9. **Student sin fila en tabla `students`:** con el nuevo `Index`, el estudiante verá lista vacía hasta exista fila con `Id` = usuario; alinear datos o crear fila al provisionar usuario estudiante.

---

## 7. Veredicto final

**Veredicto: NO LISTO para declarar “producción fintech-grade cerrada” solo con esta entrega**, por las razones siguientes:

- No se completó la **Fase 1** (reproducción + queries en PostgreSQL) ni la **Fase 6** (E2E multi-rol en dos escuelas) en esta sesión.
- El alcance de seguridad multi-tenant correcto en SaaS exige **auditoría exhaustiva de endpoints**, no solo los archivos modificados.
- Quedan riesgos operativos externos al código (**secretos en historial git**, políticas de despliegue).

**Sí se avanza de forma tangible** en: compilación estable, alineación RBAC por claims, cierre de IDOR en reportes por `studentId`, y reducción de fuga de listado PII en `StudentController` para el rol estudiante.

**Próximo paso recomendado:** ejecutar las pruebas de `PRUEBAS_E2E_COMPLETAS_ROLES_ESCUELAS.md` y `PRUEBAS_ROLES_REALES_MULTI_ESCUELA.md` contra un entorno DEV con dos escuelas; correlacionar fallos con rutas y añadir gates de **ownership + `SchoolId`** en servicios.

---

*Documento generado como entregable solicitado. Ajustar fechas y resultados de E2E/DB cuando se ejecuten en su entorno.*
