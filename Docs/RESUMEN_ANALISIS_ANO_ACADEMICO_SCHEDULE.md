# Resumen del análisis – Año académico en Schedule/ByTeacher

**Documento:** RESUMEN_ANALISIS_ANO_ACADEMICO_SCHEDULE  
**Fecha:** 2026-02-16  
**Alcance:** Causa por la que no aparecía el desplegable "Año académico" en `/Schedule/ByTeacher` y acciones realizadas.

---

## 1. Problema reportado

- En `http://localhost:5172/Schedule/ByTeacher` no se mostraba (o no cargaba) el desplegable **Año académico**.
- En logs del servidor no había errores 500; las consultas a `time_slots`, `academic_years` y teachers se ejecutaban correctamente.

---

## 2. Análisis realizado

### 2.1 Flujo de datos

- **Controlador:** `ScheduleController.ByTeacher` obtiene los años con `_academicYearService.GetAllBySchoolAsync(user.SchoolId.Value)` y los serializa en camelCase a `ViewBag.AcademicYearsJson`.
- **Vista:** `Views/Schedule/ByTeacher.cshtml` recibe ese JSON y debe rellenar el `<select id="academicYearSelect">` desde JavaScript.

### 2.2 Posibles causas consideradas

| Causa | Comprobación |
|--------|----------------|
| Tabla `academic_years` no existe | El servicio captura `PostgresException` 42P01 y devuelve lista vacía. |
| No hay filas para el `school_id` del usuario | Si `GetAllBySchoolAsync` devuelve 0 registros, el JSON es `[]` y el desplegable queda vacío. |
| JSON dañado al incrustarlo en el HTML | Atributos `data-*` con comillas/caracteres especiales pueden alterar el valor; `<script>` evita ese riesgo. |
| Error de script en el cliente | Parseo del JSON o referencias a elementos inexistentes pueden dejar la página o el desplegable sin rellenar. |

### 2.3 Verificación en base de datos

- No se pudo ejecutar `psql` desde el entorno (no estaba en PATH).
- Se añadieron comprobaciones desde la aplicación para no depender de herramientas externas.

---

## 3. Acciones realizadas

### 3.1 Logging en el controlador

- **Archivo:** `Controllers/ScheduleController.cs`
- **Cambio:** Inyección de `ILogger<ScheduleController>` y log al cargar ByTeacher:
  - `[Schedule/ByTeacher] SchoolId={SchoolId}, AcademicYearsCount={Count}. Si el desplegable no muestra años, verifique que existan registros en academic_years para esta escuela.`
- **Uso:** Si en consola aparece `AcademicYearsCount=0`, el origen del problema es que no hay años académicos en BD para esa escuela.

### 3.2 Paso de datos al cliente sin depender de atributos `data-*`

- **Archivo:** `Views/Schedule/ByTeacher.cshtml`
- **Cambio:** Los JSON (años académicos, bloques horarios, docentes, días) se envían en bloques `<script type="application/json" id="...">` y el script los lee con `getJsonFromScript(id, fallback)`.
- **Motivo:** Evitar que el encoding HTML en atributos rompa o vacíe el JSON.

### 3.3 Verificación en BD al arranque

- **Archivo:** `Scripts/VerifyAcademicYearsInDb.cs`
- **Comportamiento:**
  - Comprueba si existe la tabla `academic_years`.
  - Si no existe: log **Warning** indicando crear la tabla (p. ej. `dotnet run -- --apply-academic-year`).
  - Si existe y está vacía: log **Warning** indicando insertar al menos un año por escuela.
  - Si tiene datos: log **Information** con total y conteo por `school_id`.
- **Uso:** Se ejecuta en `Program.cs` dentro del mismo scope que los demás scripts de “ensure”, usando `ILogger<Program>`.

### 3.4 Script SQL de verificación manual

- **Archivo:** `Scripts/VerifyAcademicYears.sql`
- **Contenido:** Consultas para comprobar existencia de `academic_years`, total por escuela, listado de años y `school_id` de usuarios.
- **Uso:** Ejecutar en pgAdmin o cualquier cliente PostgreSQL cuando se quiera revisar la BD a mano.

---

## 4. Conclusión y siguientes pasos

- **Conclusión:** Lo más probable es que el desplegable esté vacío porque **no hay registros en `academic_years` para el `school_id` del usuario** (o la tabla no existe / está vacía). El resto de cambios mejoran diagnóstico y robustez del cliente.
- **Qué hacer:**
  1. Arrancar la app y revisar en consola el mensaje de `VerifyAcademicYearsInDb` y el de `Schedule/ByTeacher` (AcademicYearsCount).
  2. Si la tabla no existe: ejecutar `dotnet run -- --apply-academic-year` (o la migración correspondiente).
  3. Si la tabla existe pero está vacía (o no hay filas para tu escuela): crear al menos un año académico desde Prematrícula/Administración o insertar manualmente en `academic_years` para el `school_id` correcto.

---

## 5. Archivos tocados (resumen)

| Archivo | Cambio |
|--------|--------|
| `Controllers/ScheduleController.cs` | ILogger + log de SchoolId y AcademicYearsCount en ByTeacher. |
| `Views/Schedule/ByTeacher.cshtml` | JSON en `<script type="application/json">` y lectura con `getJsonFromScript`. |
| `Program.cs` | Llamada a `VerifyAcademicYearsInDb.RunAsync(db, logger)` en el scope de ensure. |
| `Scripts/VerifyAcademicYearsInDb.cs` | Nuevo: verificación de tabla y datos al arranque. |
| `Scripts/VerifyAcademicYears.sql` | Nuevo: consultas SQL para verificación manual. |
