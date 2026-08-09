# Correcciones de producción — EduPlaner multi-tenant

**Fecha:** 2026-05-03  
**Base:** `ANALISIS_PRODUCCION_EDUPLANER_MULTITENANT.md`  
**Alcance:** Código ASP.NET Core MVC, PostgreSQL (localhost `eduplaner`), migración EF aplicada con éxito.

---

## Cambios realizados (resumen)

| Área | Qué se hizo |
|------|-------------|
| **Base de datos** | FK `discipline_reports.school_id → schools`, columnas `school_id` en `student_assignments` y `teacher_assignments` con backfill, índices compuestos y en FKs faltantes. |
| **EF / modelo** | `StudentAssignment` y `TeacherAssignment` con `SchoolId` + navegación `School`; `School` con colecciones; GQF en `StudentAssignment`, `TeacherAssignment`, `IdCardTemplateField`; FK `DisciplineReport` → `School` con `Restrict` y nombre explícito. |
| **Servicios** | Resolución y persistencia de `SchoolId` en inserciones/actualizaciones (`StudentAssignmentService`, `TeacherAssignmentService`, `AcademicAssignmentService`, `PrematriculationService`); `SubjectAssignment` creado en `GetOrCreateSubjectAssignment` con `SchoolId` desde el grupo. |
| **Controladores** | Carnet: `IgnoreQueryFilters` donde el GQF anónimo rompía emergencias públicas y vista previa; `HomeController` `Privacy`/`Error` con `[AllowAnonymous]`; `AuthController.Logout` con `[Authorize]`. |
| **Seguridad global** | Política MVC por defecto: usuario autenticado (`AuthorizeFilter`); rutas públicas siguen con `[AllowAnonymous]`. |
| **Vistas** | `DisciplineReport/Details.cshtml`: documentos sin `JSON.parse` sobre `Html.Raw` de string crudo; construcción de enlaces con jQuery para mitigar XSS. |

---

## Cambios en base de datos

Migración aplicada: **`20260503193023_ProductionTenantAssignmentsAndDisciplineFk`**.

1. **`discipline_reports`**  
   - Eliminación condicional de la FK sombra `FK_discipline_reports_schools_school_id` si existía.  
   - Creación condicional de **`discipline_reports_school_id_fkey`** → `schools(id)` **ON DELETE RESTRICT**.

2. **`student_assignments`**  
   - Columna **`school_id`** (backfill desde `users.school_id`, luego `groups.school_id`).  
   - **NOT NULL** tras validación (si quedara `NULL`, la migración **falla** con mensaje explícito — no se borran filas).  
   - FK **`student_assignments_school_id_fkey`** → `schools(id)` **RESTRICT**.

3. **`teacher_assignments`**  
   - Columna **`school_id`** (backfill desde `subject_assignments.school_id`, luego `groups` vía `subject_assignments`).  
   - **NOT NULL** con la misma política de error si no es resoluble.  
   - FK **`teacher_assignments_school_id_fkey`** → `schools(id)` **RESTRICT**.

---

## Índices creados

| Índice | Tabla | Columnas |
|--------|-------|----------|
| `IX_student_assignments_school_id` | `student_assignments` | `school_id` |
| `IX_student_assignments_school_student_active` | `student_assignments` | `(school_id, student_id, is_active)` |
| `IX_teacher_assignments_school_id` | `teacher_assignments` | `school_id` |
| `IX_teacher_assignments_school_teacher` | `teacher_assignments` | `(school_id, teacher_id)` |
| `IX_prematriculations_grade_id` | `prematriculations` | `grade_id` |
| `IX_prematriculations_group_id` | `prematriculations` | `group_id` |
| `IX_prematriculations_parent_id` | `prematriculations` | `parent_id` |
| `IX_subject_assignments_specialty_id` | `subject_assignments` | `specialty_id` |
| `IX_subjects_area_id` | `subjects` | `"AreaId"` |
| `IX_email_jobs_created_by_user_id` | `email_jobs` | `created_by_user_id` |
| `IX_teacher_work_plan_review_logs_performed_by_user_id` | `teacher_work_plan_review_logs` | `performed_by_user_id` |

*(Los `CREATE INDEX IF NOT EXISTS` evitan fallo si el índice ya existía con otro nombre en entornos previos.)*

---

## Problemas corregidos

1. **Integridad:** `discipline_reports.school_id` ahora referenciado por el motor PostgreSQL.  
2. **Multi-tenant en tablas operativas:** `student_assignments` y `teacher_assignments` con `school_id` materializado + **GQF** alineado al resto del modelo.  
3. **Endpoint público de emergencia (carnet):** consultas bajo usuario anónimo ya no quedan vacías por el predicado `User.SchoolId == null` del GQF.  
4. **Superficie MVC sin auth por defecto:** filtro global + `AllowAnonymous` solo donde corresponde.  
5. **XSS en detalle de disciplina:** eliminación del patrón `JSON.parse('@Html.Raw(Model.Documents)')`.  
6. **`SubjectAssignment` huérfano de escuela:** al crear por `GetOrCreateSubjectAssignment`, se asigna `SchoolId` desde el grupo.

---

## Validación multi-tenant

- **GQFs nuevos:** `StudentAssignment`, `TeacherAssignment`, `IdCardTemplateField` siguen el mismo patrón que el resto (`superadmin` + bypass explícito, o `SchoolId == tenant`).  
- **Backfill:** en la BD local la migración completó sin excepción (todos los `school_id` resolubles).  
- **Comprobación SQL post-migración:** existen las FK esperadas en `discipline_reports`, incluida **`discipline_reports_school_id_fkey`**.

Consulta útil para auditoría rápida por escuela:

```sql
SELECT school_id, COUNT(*) FROM student_assignments GROUP BY 1 ORDER BY 2 DESC;
SELECT school_id, COUNT(*) FROM teacher_assignments GROUP BY 1 ORDER BY 2 DESC;
```

---

## Resultado de pruebas

| Prueba | Resultado |
|--------|-----------|
| `dotnet build` | **OK** (tras liberar `SchoolManager.exe` bloqueado por proceso en ejecución). |
| `dotnet ef database update` | **OK** — migración `20260503193023_ProductionTenantAssignmentsAndDisciplineFk` aplicada contra `localhost` / `eduplaner`. |
| Suite automatizada (xUnit/NUnit) en repo | **No** hay proyecto de tests de integración MVC en el repositorio; no se ejecutó E2E automatizado. |

**Recomendación:** prueba manual mínima — login admin, CRUD usuarios/grupos/asignaciones, enlace público de emergencia con token válido, y comprobación de que dos escuelas no ven datos cruzados en listados filtrados por claim.

---

## Estado final: LISTO / NO LISTO

**Veredicto:** **LISTO PARA ENTORNO LOCAL Y DESPLIEGUE CON BUENAS PRÁCTICAS DE SECRETS** — con **reservas enterprise** habituales:

- No se implementó **RLS en PostgreSQL** (defensa en profundidad a nivel motor).  
- `appsettings.json` sigue pudiendo contener secretos de desarrollo; en **producción real** deben ir a **variables de entorno** / vault y rotarse.  
- No se sustituyó el árbol de **roles case-sensitive** por completo (sería refactor amplio de `[Authorize(Roles=...)]` y datos).  
- **Pentest / carga** no forman parte de este entregable.

Si la organización exige sello “enterprise SaaS” estricto, el estado es **PARCIAL** hasta RLS o equivalente, hardening de secretos y pruebas de seguridad/carga.

---

*Fin del informe de correcciones.*
