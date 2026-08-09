# Prueba completa — dos escuelas, roles y flujo producción (UI)

**Fecha:** 2026-05-03  
**Entorno:** `http://localhost:5173` (navegador tipo Chrome / automatización Cursor).  
**Regla:** operaciones vía **interfaz web**; sin `INSERT` manual en base de datos.

---

## 1. Escuelas creadas

| Escuela | Estado | Notas |
|---------|--------|--------|
| **Colegio Central** | Activa en BD y en combo de login | Creada en sesión previa (SuperAdmin → *Crear Escuela con Administrador*). Admin: `admin.central.qa@test.local`. |
| **Instituto Norte** | Activa en BD y en combo de login | Misma vía. Admin: `admin.norte.qa@test.local`. |

**Validación (Fase 1–2):** la raíz `/` carga login; combo **Institución** lista ambas escuelas con valores `school_id` distintos. Cabecera tras login de admin muestra **Colegio Central** cuando se elige esa institución.

---

## 2. Usuarios por rol

### Colegio Central (evidencia en esta corrida)

| Rol | Email | Creado vía UI | Contraseña de prueba |
|-----|--------|---------------|-------------------------|
| **admin** | `admin.central.qa@test.local` | Alta junto con la escuela (SuperAdmin) | `Test#2026` |
| **secretaria** | `secretaria.central.cfqa@test.local` | **Sí** — `Administración` → **Administrar Usuarios** (`/User/Index`) → formulario *Crear / Editar Usuario* → **Crear Usuario** | `Test#2026` |
| **teacher (Profesor 1 y 2)** | *(pendiente en esta sesión)* | Mismo formulario; rol **Docente** (`Teacher` en valor interno) | — |
| **estudiante** (≥5) | *(pendiente)* | Mismo formulario; rol **Estudiante**; puede exigir campos adicionales (inclusión, etc.) | — |

### Instituto Norte

| Rol | Estado |
|-----|--------|
| **admin** | `admin.norte.qa@test.local` existente (creación escuela). |
| **secretaria, 2 docentes, ≥5 estudiantes** | **No creados** en esta sesión (mismo procedimiento que en Central repetido con emails `*.norte.*`). |

**Verificación BD (muestra Central):** `secretaria.central.cfqa@test.local` tiene `role = secretaria` y `school_id` del Colegio Central.

**Nota sobre emails del enunciado** (`admin.central@test.local`, `profesor1.central@test.local`, …): son válidos como convención; en esta base ya existían admins `*.qa@test.local` para evitar colisiones. El flujo UI es el mismo cambiando correo y documento únicos.

**Ruta correcta de alta masiva:** **`/User/Index`** (JSON `POST /User/CreateJson`). La página **`/User/Create`** usa `serialize()` contra un endpoint `[FromBody]` y **no** es el flujo probado aquí (riesgo de desalineación cuerpo JSON vs formulario).

---

## 3. Configuración académica

**No ejecutada** en esta corrida en navegador.

**Rutas previstas (admin):**

- **Catálogo Académico** — `AcademicCatalog/Index`: especialidad / área / materia / grado / grupo (modelo jerárquico; requiere persistir filas y **Guardar** según la UI actual).
- **Catálogo de Asignaciones** — enlazar materia–grado–nivel para la escuela.
- **Asignar Docentes** — `TeacherAssignment/Index`: depende de IDs de especialidad, área, materia, grado y grupo ya existentes.

**Objetivo del plan (pendiente de ejecución completa):**

- Grados: **10°**, **11°**
- Grupos: **A**, **B**
- Materias: **Matemática**, **Ciencias**, **Español**
- Asignaciones: Profesor 1 → Matemática 10° A; Profesor 2 → Ciencias 10° A

---

## 4. Estudiantes creados

**No** en esta sesión. Depende de: usuarios rol **estudiante**, luego flujo de **secretaría** (alta / matrícula / asignación a grado–grupo según pantallas del proyecto, p. ej. `StudentAssignment`).

---

## 5. Actividades creadas

**No aplicable** (sin docentes asignados ni estudiantes matriculados en la corrida).

---

## 6. Notas registradas

**No aplicable.**

---

## 7. Notas editadas

**No aplicable.**

---

## 8. Asistencia registrada

**No aplicable.**

---

## 9. Validación por rol

### Admin

- **Login:** `admin.central.qa@test.local` + institución **Colegio Central** + `Test#2026` → **OK** (`/Home/Index`).
- **Gestión de usuarios:** `/User/Index` carga; **creación de secretaria** → SweetAlert *¡Usuario Creado!* → formulario reseteado tras confirmar → **OK**.

### Secretaria

- Usuario creado; **login y permisos de navegación** no re-ejecutados en esta pasada tras el alta (pendiente: cerrar sesión admin → login secretaria → rutas típicas de estudiantes/asignaciones).

### Profesor (`teacher`)

- **No probado** en esta sesión (pendiente creación de 2 docentes y asignaciones académicas).

### Estudiante (`estudiante`)

- **No probado** (pendiente alta de ≥5 estudiantes y portal de notas / carnet).

---

## 10. Validación multi-escuela

- **Login:** el selector distingue **Colegio Central** e **Instituto Norte** (misma pantalla de acceso).
- **Datos independientes:** cada escuela tiene su propio `school_id`; el alta de secretaria quedó ligada al `school_id` de Central (comprobado en BD).
- **Repetición en Norte:** mismo patrón en sesión admin de **Instituto Norte** (no ejecutado aquí).

---

## 11. Validación de seguridad

En corrida previa del mismo entorno (documentada en `RESET_DB_PRUEBA_FUNCIONAL_COMPLETA.md`):

- Manipulación de URL **`/User/Edit/{id}`** con usuario de **otra** escuela → **`/Auth/AccessDenied` (403)**.
- **`/Student/Details/{id}`** sin permiso → **AccessDenied**.

**Recomendación:** repetir con **estudiante** logueado y GUID de compañero de otra escuela cuando existan datos.

---

## 12. Errores encontrados

1. **Alcance incompleto:** no se completaron fases 4–8 (académica → gradebook → asistencia) por volumen de UI y dependencias (catálogo jerárquico, asignaciones, matrícula).
2. **`/User/Create` vs `CreateJson`:** el formulario *Nuevo Usuario* (`Views/User/Create.cshtml`) publica con `serialize()`; el endpoint espera **JSON** en el flujo principal. **El flujo soportado y probado es `/User/Index`.**
3. **Consola:** en otras vistas del panel admin pueden aparecer `console.log` de depuración (ruido, no necesariamente fallo funcional).

---

## 13. Correcciones aplicadas

**Ninguna en código** en esta corrida (solo datos vía UI y documentación).

**Mejora sugerida (no implementada aquí):** alinear `Views/User/Create.cshtml` con el mismo `$.ajax` JSON que `User/Index`, o marcar la ruta como obsoleta y enlazar solo a `/User/Index`.

---

## 14. Evidencia funcional

| Evidencia | Detalle |
|-----------|---------|
| Login admin + tenant | Sesión en **Colegio Central**; menú **Administrar Usuarios**. |
| Alta secretaria UI | SweetAlert **¡Usuario Creado!**; fila persistida en BD con `role=secretaria` y `school_id` de Central. |
| E2E automatizado existente | `e2e/tests/admin-crud.spec.ts` documenta el mismo contrato `POST /User/CreateJson` con JSON. |

---

## Consola del navegador (Fase 10)

En la porción probada de **Gestión de usuarios**, no se reportó error JS bloqueante en el snapshot de consola de esta corrida; el código de la vista incluye **logs de depuración** en el flujo de creación (considerar limpieza en producción).

---

## Criterio final: ¿LISTO PARA PRODUCCIÓN?

| Criterio | Resultado |
|----------|------------|
| Flujo completo extremo a extremo | **No** — solo inicio + escuelas + admin + 1 usuario secretaría (Central) |
| CRUD y edición de datos en todos los módulos | **No verificado** |
| Todos los roles | **No** |
| Sin errores UI en lo probado | **Sí** en login y alta secretaria |
| Sin mezcla de datos multi-tenant | **Parcialmente** validado (diseño + muestra previa URL); falta repetir con datos académicos |
| Seguridad URL | **Muestras previas** OK (403) |

**Conclusión:** el sistema **no** puede declararse **listo para producción** con solo esta corrida. Sí queda **demostrado** el camino crítico: **admin por escuela → `/User/Index` → `CreateJson` → usuario con `school_id` correcto**.

**Siguiente paso recomendado (orden):** (1) completar usuarios en **ambas** escuelas desde `/User/Index`; (2) **Catálogo académico** y **asignaciones**; (3) estudiantes y matrícula con secretaría; (4) portal docente (actividades, notas, asistencia); (5) portal estudiante; (6) repetir pruebas de aislamiento y URL con datos reales.
