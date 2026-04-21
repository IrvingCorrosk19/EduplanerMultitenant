# Análisis Técnico — ApplyAcademicYearChanges

## 1. Ubicación del Endpoint

| Campo | Valor (código) |
|--------|----------------|
| **Controlador** | `PrematriculationController` |
| **Archivo** | `Controllers/PrematriculationController.cs` (aprox. líneas 606–634) |
| **Ruta HTTP** | `POST /Prematriculation/ApplyAcademicYearChanges` |
| **Página GET (UI)** | `GET /Prematriculation/ApplyAcademicYearChanges` → acción `ApplyAcademicYearChangesPage`, vista `ApplyAcademicYearChanges.cshtml` |
| **Invocación real** | `await SchoolManager.Scripts.ApplyAcademicYearChanges.ApplyAsync(_context);` |
| **Servicio de dominio** | **No** se usa `IPrematriculationService` ni otro servicio de aplicación para esta acción; solo `SchoolDbContext` pasado al script estático. |

---

## 2. Entrada del Request

| Aspecto | Detalle |
|---------|---------|
| **Método** | `POST` |
| **Cuerpo** | Ninguno requerido; el método `ApplyAcademicYearChanges()` **no lee** `HttpContext`, query string ni `FromBody`. |
| **Parámetros** | Ninguno en la firma del método. |
| **Antifalsificación** | `[ValidateAntiForgeryToken]` en la acción POST. La vista envía el token vía cabecera `RequestVerificationToken` (coherente con `AddAntiforgery` en `Program.cs` que define `HeaderName = "RequestVerificationToken"`). |
| **Autorización** | `[Authorize(Roles = "admin,superadmin")]` — solo `admin` o `superadmin` (minúsculas en el atributo). |
| **Validaciones explícitas** | No hay validación de modelo (`[FromBody]` ausente). El único filtro previo relevante es autenticación + rol + antiforgery. |

---

## 3. Flujo Completo

### 3.1 Entrada (cliente)

1. Usuario abre `GET /Prematriculation/ApplyAcademicYearChanges` → render de `ApplyAcademicYearChanges.cshtml`.
2. Clic en “Aplicar Cambios” → confirmación SweetAlert → `$.ajax` **POST** a la URL generada por `Url.Action("ApplyAcademicYearChanges", "Prematriculation")` con cabecera `RequestVerificationToken`.

### 3.2 Controlador

1. Si antiforgery o autorización fallan → comportamiento estándar de ASP.NET Core (no detallado en el método; típicamente 400/401/403 según caso).
2. `try`: log informativo → `ApplyAcademicYearChanges.ApplyAsync(_context)`.
3. Éxito: `TempData["Success"]`, respuesta JSON `{ success: true, message: "..." }`.
4. Excepción: log de error, `TempData["Error"]`, JSON `{ success: false, message: ex.Message }`.

### 3.3 Script `ApplyAcademicYearChanges.ApplyAsync`

Ejecución **secuencial** de DDL condicional (crear tabla/columnas/índices/FK si no existen). No hay bucles sobre estudiantes ni prematrículas.

**Orden lógico en código:**

1. Tabla `academic_years` (si no existe), con FK a `schools(id) ON DELETE CASCADE`.
2. Índices en `academic_years`: `school_id`, `is_active`, `(school_id, is_active)`.
3. FKs opcionales en `academic_years`: `created_by`, `updated_by` → `users(id) ON DELETE SET NULL`.
4. Columna `trimester.academic_year_id` (uuid), índice, FK a `academic_years(id) ON DELETE SET NULL`.
5. Columna `student_assignments.academic_year_id` (uuid), índices (incl. `student_id, is_active` y `student_id, academic_year_id`), FK a `academic_years(id) ON DELETE SET NULL`.
6. Columna `student_activity_scores.academic_year_id` (uuid), índices, FK a `academic_years(id) ON DELETE SET NULL`.

### 3.4 Resultado

- Respuesta JSON al cliente; mensajes opcionales vía `TempData` (la petición AJAX no redirige, pero el controlador asigna `TempData` igualmente).

---

## 4. Entidades Afectadas (modelo de dominio vs. DDL real)

El script **no** actualiza entidades vía EF `SaveChanges` con entidades rastreadas. Opera con **SQL crudo** sobre el esquema.

| Área | ¿Tocada por este endpoint? |
|------|----------------------------|
| **Prematriculation** (tabla/registros) | **No** en el script. El nombre del controlador es contexto de ruta/UI; la lógica no lee ni escribe prematrículas. |
| **User / Students** | **No** como filas de datos. Solo FKs hacia `users` en `academic_years` (`created_by`/`updated_by`) si se crean esas restricciones. |
| **AcademicYear** | **Sí a nivel de esquema**: creación de tabla `academic_years` si no existe (no inserta filas de año académico). |
| **Groups / Grades (grade_levels)** | **No**. |
| **UserGroups / UserGrades** | **No**. |
| **SubjectAssignments / TeacherAssignments** | **No**. |
| **StudentAssignments** | **Sí esquema**: columna `academic_year_id` + índices + FK (no modifica filas existentes en el script). |
| **Trimester** | **Sí esquema**: columna `academic_year_id` + índice + FK. |
| **Student activity scores** | **Sí esquema**: tabla `student_activity_scores` recibe columna `academic_year_id` + índices + FK. |

---

## 5. Operaciones en Base de Datos

| Tipo | Detalle |
|------|---------|
| **SELECT** | Consultas a `information_schema` / `pg_indexes` para existencia de tabla, columna, índice, constraint (helpers privados). |
| **CREATE TABLE** | `academic_years` (condicional). |
| **ALTER TABLE** | Añadir columnas `academic_year_id` en `trimester`, `student_assignments`, `student_activity_scores` (condicional). |
| **CREATE INDEX** | Varios `CREATE INDEX IF NOT EXISTS ...`. |
| **ALTER TABLE … ADD CONSTRAINT** | FKs nombradas en el script. |
| **DELETE / UPDATE de datos** | **Ninguno** en el script analizado. |
| **Transacciones** | **No** se envuelve `ApplyAsync` en `BeginTransaction` / `IDbContextTransaction` en el script ni en el controlador. Cada `ExecuteSqlRawAsync` es una operación independiente. |
| **Idempotencia** | Diseño “si ya existe, no repetir” para tabla, columnas, índices y FKs con nombres fijos. |

---

## 6. Reglas de Negocio

| Regla esperada en producto | En este endpoint |
|----------------------------|------------------|
| Promoción de estudiantes (ej. 1.° → 2.°) | **No implementada**. No hay lógica de promoción ni actualización de `student_assignments` por grado/grupo. |
| Cambio de grupo/grado | **No**. |
| Creación/actualización de prematrículas | **No**. |
| Año académico “activo” o selección | **No** se insertan ni actualizan filas en `academic_years`; solo se asegura la **estructura** de la tabla. |
| Datos incompletos | No aplica a filas: no hay procesamiento de negocio sobre registros. |

**Conclusión de negocio:** el endpoint es una **herramienta de homologación de esquema** (año académico en BD), no un caso de uso de “cambio de año” pedagógico/administrativo sobre alumnos.

---

## 7. Dependencias

| Dependencia | Uso |
|-------------|-----|
| `SchoolDbContext` | Pasado al script; `Database.ExecuteSqlRawAsync`, `SqlQueryRaw` para comprobaciones. |
| `Microsoft.EntityFrameworkCore` | Ejecución SQL y `SqlQueryRaw<int>` en helpers. |
| `Program.cs` — Antiforgery | Header `RequestVerificationToken` alineado con la vista AJAX. |
| **CLI** | `dotnet run -- --apply-academic-year` invoca el **mismo** `ApplyAcademicYearChanges.ApplyAsync` (línea ~414–418 de `Program.cs`), sin pasar por HTTP. |
| **Middleware** | Ninguno específico de este endpoint más allá del pipeline estándar (auth, routing). |

---

## 8. Manejo de Errores

| Capa | Comportamiento |
|------|----------------|
| **Script** | `try/catch` interno: escribe error en consola y **re-lanza** `throw`. |
| **Controlador** | `try/catch`: registra con `_logger.LogError`, asigna `TempData["Error"]`, devuelve JSON `success: false` con `ex.Message`. |
| **Validación de negocio** | No hay; fallos típicos serán **SQL** (permisos, bloqueos, conflictos de nombres de objetos, etc.). |

---

## 9. Riesgos Detectados

| Riesgo | Descripción |
|--------|-------------|
| **Transacción ausente** | Si falla a mitad del script, parte del DDL puede haberse aplicado; recuperación manual o re-ejecución (idempotente en teoría) sin rollback automático global. |
| **Concurrencia** | Dos administradores ejecutando el POST simultáneamente: condiciones de carrera en creación de objetos con el mismo nombre; PostgreSQL puede serializar o uno falla según el caso. |
| **Expectativa funcional errónea** | Nombre “ApplyAcademicYearChanges” y ruta bajo `Prematriculation` pueden sugerir lógica de prematrícula o promoción; **el código no la contiene** — riesgo de uso operativo incorrecto. |
| **Seguridad** | Endpoint potente (DDL) restringido a `admin`/`superadmin`; sigue siendo riesgo elevado si credenciales se comprometen. Comentario en controlador: *"Endpoint temporal"* / *"TODO: Remover"* en otro método cercano (`ApplyDatabaseChanges`) indica deuda técnica de limpieza. |
| **Nombre de tabla `trimester`** | Asume tabla `trimester` en minúsculas como en el script; si en algún entorno el nombre difiere, el `ALTER` fallaría. |
| **Comprobación de índices** | `IndexExistsAsync` usa `pg_indexes` con `indexname` en minúsculas; los `CREATE INDEX IF NOT EXISTS` usan nombres en mayúsculas en el SQL — en PostgreSQL los nombres no entrecomillados se pliegan a minúsculas, coherente con la búsqueda, pero cualquier desviación histórica en BD es riesgo de duplicados lógicos o índices no detectados. |
| **Sin correlación con migraciones EF** | El script puede coexistir con migraciones que ya crearon objetos; idempotencia ayuda, pero **desalineación** entre modelo EF y BD real es posible si no se gobierna en despliegue. |

---

## 10. Ejemplo Práctico (pedagógico)

**Escenario solicitado:** “Estudiante pasa de 1.er grado a 2.° grado.”

**Qué hace realmente este endpoint:** **Nada** sobre ese estudiante. No lee `student_assignments`, no actualiza `grade_id`/`group_id`, no inserta prematrícula.

**Qué podría quedar en BD después de ejecutarlo (solo esquema):**

- Existencia de `academic_years` y columnas `academic_year_id` en tablas enlazadas, permitiendo en **otros** procesos futuros o manuales vincular asignaciones y notas a un año.

Para modelar “pasa a 2.°” haría falta **otro** flujo (servicio/controlador) no presente en `ApplyAcademicYearChanges.ApplyAsync`.

---

## 11. Conclusión Técnica

- **Qué hace:** aplica de forma **idempotente** (según el código) cambios de **esquema** relacionados con **años académicos**: tabla `academic_years` y columnas `academic_year_id` en `trimester`, `student_assignments` y `student_activity_scores`, más índices y foreign keys.
- **Qué no hace:** no ejecuta reglas de prematrícula, no promociona estudiantes, no modifica grupos/grados ni tablas de prematrícula.
- **Confianza del análisis:** basada **solo** en los archivos listados abajo; cualquier comportamiento no leído (p. ej. filtros globales de EF que no aplican a `ExecuteSqlRaw`) queda fuera de alcance.

---

## Archivos analizados

- `Controllers/PrematriculationController.cs` (acciones `ApplyAcademicYearChanges`, `ApplyAcademicYearChangesPage` y contexto de rutas)
- `Scripts/ApplyAcademicYearChanges.cs` (implementación completa)
- `Views/Prematriculation/ApplyAcademicYearChanges.cshtml` (POST AJAX y antiforgery)
- `Program.cs` (referencia CLI `--apply-academic-year` y antiforgery header; fragmento de arranque con argumentos)

## Métodos clave

- `PrematriculationController.ApplyAcademicYearChanges` (POST)
- `PrematriculationController.ApplyAcademicYearChangesPage` (GET)
- `ApplyAcademicYearChanges.ApplyAsync`
- Helpers privados: `CreateTableIfNotExists`, `ExecuteIfColumnNotExists`, `ExecuteIfIndexNotExists`, `ExecuteIfForeignKeyNotExists`, `ColumnExistsAsync`, `TableExistsAsync`, `IndexExistsAsync`, `ForeignKeyExistsAsync`

## Nivel de confianza del análisis

**Alto** para el flujo HTTP → controlador → script y para el alcance exacto del DDL. **Medio** en detalles de runtime de PostgreSQL (bloqueos, orden exacto de errores) sin ejecutar el script contra una BD.
