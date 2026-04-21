# Pruebas funcionales – Módulo horarios (post-blindado Academic Year)

**Objetivo:** Validar el flujo completo del horario con datos reales.  
**Contexto:** Tras la implementación enterprise que garantiza una School con al menos un AcademicYear activo.  
**Fecha de ejecución:** _[completar]_  
**Ejecutante:** _[completar]_

---

## Metodología

Para cada escenario:
- **Pasos:** Acciones concretas a realizar.
- **Resultado esperado:** Comportamiento según diseño y código.
- **Resultado obtenido:** _[rellenar tras ejecutar]_.
- **Evidencias:** _[logs, capturas, requests/responses]_.
- **Estado:** OK / FALLO / NO EJECUTADO.

---

## Escenario 1: School nueva y AcademicYear automático

**Objetivo:** Verificar que al crear una School se crea automáticamente 1 AcademicYear y que no se crean duplicados si se llama dos veces EnsureDefault.

### Pasos

1. Como SuperAdmin, ir a la pantalla de creación de escuela con admin (p. ej. `/SuperAdmin/CreateSchoolWithAdmin`).
2. Completar formulario (nombre escuela, admin, etc.) y enviar.
3. En BD ejecutar:
   - `SELECT id, name, school_id FROM academic_years WHERE school_id = '<nuevo_school_id>';`
4. (Opcional) Desde código o un punto que llame `EnsureDefaultAcademicYearForSchoolAsync(schoolId)` de nuevo para la misma escuela; verificar en BD que no se inserta un segundo año con el mismo nombre/año.

### Resultado esperado

- La escuela se crea correctamente.
- Existe **exactamente 1** registro en `academic_years` para esa `school_id`.
- El año tiene: `name` = año actual (ej. "2026"), `start_date` 1 ene, `end_date` 31 dic, `is_active = true`.
- Si se invoca EnsureDefault una segunda vez para la misma escuela, no se inserta otro registro (GetAllBySchoolAsync devuelve 1, no se llama CreateAsync de nuevo).

### Resultado obtenido

_[Completar tras ejecutar]_

### Evidencias

- Log de consola: `[SuperAdminService] Escuela creada: ...` y (si aplica) creación de año.
- Query BD: número de filas en `academic_years` para el `school_id` creado.
- _[Adjuntar captura o salida de consulta si procede]_

### Estado

_[ ] OK  [ ] FALLO  [ ] NO EJECUTADO_

---

## Escenario 2: Teacher – Dropdown AcademicYear y TimeSlots en /Schedule/ByTeacher

**Objetivo:** Confirmar que, entrando como Teacher, en `/Schedule/ByTeacher` aparecen el dropdown de Año académico y los bloques (TimeSlots) en la tabla.

### Pasos

1. Iniciar sesión con un usuario con rol **Teacher** (o docente) asociado a una escuela que tenga al menos un AcademicYear y TimeSlots configurados.
2. Navegar a `/Schedule/ByTeacher`.
3. Comprobar en la vista: desplegable "Año académico" con al menos una opción; al seleccionar año y pulsar "Cargar horario", la tabla muestra columnas por bloque horario (TimeSlots) y filas por día.

### Resultado esperado

- La página carga sin error.
- El desplegable "Año académico" muestra al menos un año (el creado por defecto o los existentes).
- La tabla muestra cabeceras de columnas con los nombres de los TimeSlots de la escuela y filas Lunes–Viernes (o según configuración).
- No aparece el mensaje de alerta "No hay años académicos configurados para su escuela".

### Resultado obtenido

_[Completar]_

### Evidencias

- Captura de pantalla de `/Schedule/ByTeacher` con dropdown y tabla (o cabeceras de bloques).
- Log: `[Schedule/ByTeacher] SchoolId=..., AcademicYearsCount=...` con Count ≥ 1.

### Estado

_[ ] OK  [ ] FALLO  [ ] NO EJECUTADO_

---

## Escenario 3: Registrar ScheduleEntry válido

**Objetivo:** Una entrada de horario válida se guarda y se refleja en la tabla.

### Pasos

1. Como Teacher, en `/Schedule/ByTeacher` seleccionar Año académico y pulsar "Cargar horario".
2. En una celda (día + bloque) elegir una asignación docente (materia–grupo) del dropdown y dejar que se guarde por AJAX (o pulsar guardar si aplica).
3. Comprobar mensaje de éxito (p. ej. SweetAlert "Guardado").
4. Recargar la página, volver a cargar el horario y comprobar que la misma celda muestra la asignación guardada.
5. (Opcional) En BD: `SELECT * FROM schedule_entries WHERE academic_year_id = '...' AND day_of_week = X AND time_slot_id = '...';`

### Resultado esperado

- La respuesta del endpoint `Schedule/SaveEntry` (POST) devuelve `success: true`, `message: "Entrada guardada."` y `data` con la entrada (id, dayOfWeek, timeSlotId, etc.).
- La tabla en la vista muestra la materia–grupo en la celda correspondiente.
- Tras recargar y volver a cargar, el dato persiste.
- En BD existe un registro en `schedule_entries` con los mismos `academic_year_id`, `day_of_week`, `time_slot_id`, `teacher_assignment_id`.

### Resultado obtenido

_[Completar]_

### Evidencias

- Request: `POST /Schedule/SaveEntry` con body (teacherAssignmentId, timeSlotId, academicYearId, dayOfWeek).
- Response: JSON con `success: true` y objeto en `data`.
- Captura de la tabla con la celda rellenada y/o query a `schedule_entries`.

### Estado

_[ ] OK  [ ] FALLO  [ ] NO EJECUTADO_

---

## Escenario 4: Conflicto docente

**Objetivo:** Mismo AcademicYear + mismo DayOfWeek + mismo TimeSlot con otra TeacherAssignment del **mismo** Teacher debe bloquearse.

### Pasos

1. Como Teacher, tener ya una entrada guardada para un año A, día D y bloque T (p. ej. Lunes, bloque 1).
2. En otra celda del **mismo** día D y **mismo** bloque T intentar asignar **otra** materia/grupo del mismo docente (otra TeacherAssignment del mismo Teacher).
3. Observar la respuesta del servidor y el mensaje mostrado en la UI.

### Resultado esperado

- El servidor no crea una segunda entrada.
- `Schedule/SaveEntry` responde con `success: false` y un mensaje que indique conflicto de horario del docente (ej. "Conflicto de horario: el docente ya tiene una clase asignada en el mismo día y bloque para este año académico.").
- En BD no aparece un segundo registro con el mismo (academic_year_id, day_of_week, time_slot_id) y mismo teacher_id (vía teacher_assignment_id).

### Resultado obtenido

_[Completar]_

### Evidencias

- Request POST a SaveEntry con el segundo teacherAssignmentId (mismo docente, mismo día y bloque).
- Response JSON: `success: false`, `message` con texto de conflicto docente.
- _[Captura de SweetAlert o mensaje en pantalla]_

### Estado

_[ ] OK  [ ] FALLO  [ ] NO EJECUTADO_

---

## Escenario 5: Conflicto grupo

**Objetivo:** Mismo AcademicYear + mismo DayOfWeek + mismo TimeSlot con TeacherAssignment que apunte al **mismo GroupId** (otra materia, mismo grupo) debe bloquearse.

### Pasos

1. Tener una entrada ya guardada para año A, día D, bloque T para un **grupo G** (p. ej. Matemáticas – 5°A).
2. Con el mismo u otro docente, intentar asignar otra materia para el **mismo grupo G** en el mismo día D y bloque T (ej. Ciencias – 5°A en Lunes bloque 1).
3. Observar respuesta y mensaje en UI.

### Resultado esperado

- El servidor no crea la entrada.
- `Schedule/SaveEntry` responde `success: false` con mensaje de conflicto de grupo (ej. "Conflicto de horario: el grupo ya tiene una clase asignada en el mismo día y bloque para este año académico.").
- En BD no hay dos entradas con mismo (academic_year_id, day_of_week, time_slot_id) y mismo group_id (vía teacher_assignment → subject_assignment → group_id).

### Resultado obtenido

_[Completar]_

### Evidencias

- Request POST SaveEntry con teacherAssignmentId cuya asignación sea del mismo grupo en mismo día y bloque.
- Response: `success: false`, mensaje de conflicto grupo.

### Estado

_[ ] OK  [ ] FALLO  [ ] NO EJECUTADO_

---

## Escenario 6: Seguridad Teacher

**Objetivo:** Teacher no puede guardar entradas con TeacherAssignment de otro docente ni consultar horario de otro teacherId.

### Pasos

**6.1 Guardar con TeacherAssignment de otro docente**

1. Iniciar sesión como Teacher (usuario T1).
2. Obtener el ID de una TeacherAssignment que pertenezca a **otro** docente (T2) – p. ej. desde BD o desde una respuesta previa de Admin.
3. Enviar POST a `Schedule/SaveEntry` con ese `teacherAssignmentId` y timeSlotId, academicYearId, dayOfWeek válidos.
4. Comprobar respuesta.

**6.2 Consultar teacherId distinto**

1. Siguir como Teacher (T1).
2. Abrir o hacer GET a `/Schedule/ListJsonByTeacher?teacherId=<id_del_otro_docente>&academicYearId=<id_año>`.
3. Comprobar respuesta.

### Resultado esperado

- **6.1:** `SaveEntry` responde `success: false` con mensaje de no autorizado (ej. "Solo puede asignar horarios a sus propias materias. La asignación docente no le pertenece."). No se crea ninguna entrada para T2.
- **6.2:** `ListJsonByTeacher` responde `success: false` con mensaje tipo "Solo puede consultar su propio horario." (cuando teacherId ≠ currentUserId y rol es Teacher). No se devuelven datos del otro docente.

### Resultado obtenido

_[Completar 6.1 y 6.2]_

### Evidencias

- Request/response 6.1: POST SaveEntry con teacherAssignmentId de otro docente.
- Request/response 6.2: GET ListJsonByTeacher con teacherId distinto al del usuario logueado.

### Estado

_[ ] OK  [ ] FALLO  [ ] NO EJECUTADO_

---

## Escenario 7: Admin/Director puede ver horarios de todos

**Objetivo:** Usuario Admin o Director puede ver horarios de cualquier docente (dropdown docente + selección de año y carga de tabla).

### Pasos

1. Iniciar sesión como **Admin** o **Director** de una escuela.
2. Ir a `/Schedule/ByTeacher`.
3. Comprobar que aparece el desplegable de **Docente** (además del de Año académico).
4. Seleccionar un docente distinto al propio usuario y un año académico; pulsar "Cargar horario".
5. Comprobar que se muestra la tabla con las entradas de ese docente (o vacía si no tiene).
6. (Opcional) Cambiar a otro docente y comprobar que la tabla se actualiza.

### Resultado esperado

- La vista muestra el filtro de Docente con lista de docentes de la escuela.
- Al elegir docente y año y cargar, la petición a `ListJsonByTeacher?teacherId=<id_seleccionado>&academicYearId=...` devuelve `success: true` y `data` con las entradas de ese docente (o array vacío).
- La tabla refleja correctamente los datos del docente seleccionado.

### Resultado obtenido

_[Completar]_

### Evidencias

- Captura con dropdown Docente visible y tabla cargada para un docente.
- Request GET ListJsonByTeacher con teacherId de otro usuario; response 200 con JSON success: true.

### Estado

_[ ] OK  [ ] FALLO  [ ] NO EJECUTADO_

---

## Resumen de resultados

| Escenario | Descripción breve                    | Estado        |
|----------|--------------------------------------|---------------|
| 1        | School nueva → 1 AcademicYear, no duplicados | _[ ] OK / FALLO / N/E_ |
| 2        | Teacher ve dropdown año y TimeSlots  | _[ ] OK / FALLO / N/E_ |
| 3        | ScheduleEntry válido guarda y persiste | _[ ] OK / FALLO / N/E_ |
| 4        | Conflicto docente bloqueado          | _[ ] OK / FALLO / N/E_ |
| 5        | Conflicto grupo bloqueado            | _[ ] OK / FALLO / N/E_ |
| 6        | Seguridad Teacher (guardar/consultar) | _[ ] OK / FALLO / N/E_ |
| 7        | Admin/Director ve horarios de todos  | _[ ] OK / FALLO / N/E_ |

---

## Hallazgos y recomendaciones

### Hallazgos

_[Anotar aquí desviaciones respecto al resultado esperado, errores de UI, mensajes confusos, comportamientos inesperados en BD o en logs.]_

- Ejemplo: "En escenario X el mensaje mostrado fue Y en lugar del mensaje esperado Z."
- Ejemplo: "Al recargar la página, la celda no mostraba la asignación hasta pulsar dos veces Cargar horario."

### Recomendaciones

_[Recomendaciones de mejora (UX, mensajes, logs, pruebas automatizadas, etc.). No incluye cambios de código en este documento.]_

- Ejemplo: "Incluir en logs el teacherId y academicYearId en SaveEntry para trazabilidad."
- Ejemplo: "Añadir prueba de integración para conflicto docente y grupo."
- Ejemplo: "Considerar deshabilitar en UI las celdas ya ocupadas por el mismo docente para evitar intentos de conflicto."

---

**Fin del documento.** Ejecutar las pruebas, rellenar "Resultado obtenido", "Evidencias" y "Hallazgos/Recomendaciones" según corresponda.
