# Confirmación: listo para ejecutar pruebas del módulo Horarios

**Revisado:** Estado de BD y aplicación para las pruebas de `Docs/PRUEBAS_FUNCIONALES_HORARIOS_POST_BLINDADO.md`.

---

## 1. Estado de la base de datos (revisado)

| Dato | Estado |
|------|--------|
| **Escuela** | 1 escuela: Instituto Profesional y Técnico San Miguelito |
| **Años académicos** | ✅ 1 año académico para esa escuela (insertado con `EnsureAcademicYearPerSchool.sql`) |
| **Tabla `time_slots`** | Se crea al **arrancar la app** (EnsureScheduleTables). Si estaba vacía, ahora la app crea **1 bloque por defecto** por escuela al iniciar. |
| **TeacherAssignments** | ✅ 432 asignaciones de docentes en la BD (hay docentes con materia/grupo para elegir en el dropdown). |

---

## 2. Cambio realizado para que puedas probar

- En **Program.cs** se añadió lógica al arranque: después de asegurar años académicos, para cada escuela que **no tenga** bloques horarios activos se crea uno por defecto (**"Bloque 1"**, 07:00–08:00, activo).
- Así, al abrir **Horario por Docente** y pulsar **Cargar horario**, la tabla tendrá al menos una columna (bloque) y no aparecerá “No hay bloques horarios configurados”.

---

## 3. Cómo ejecutar las pruebas

1. **Arrancar la aplicación** (p. ej. `dotnet run`).
   - Se crean las tablas `time_slots` y `schedule_entries` si no existen.
   - Se asegura 1 año académico por escuela si faltaba.
   - Se asegura 1 bloque horario por escuela si no tenía ninguno.

2. **Escenario 1 (School + AcademicYear):** Como SuperAdmin, crear una escuela con admin y comprobar en BD que tiene 1 registro en `academic_years`.

3. **Escenario 2 (Teacher ve dropdowns):** Iniciar sesión como **Teacher** (docente de la escuela), ir a **Horarios** → **Horario por Docente** (`/Schedule/ByTeacher`). Comprobar que aparece el desplegable **Año académico** (con al menos “2026” o el año actual) y que al pulsar **Cargar horario** la tabla muestra al menos la columna “Bloque 1” y filas Lunes–Viernes.

4. **Escenario 3 (Guardar entrada):** En una celda elegir una asignación (Materia - Grupo) del dropdown; debe guardar por AJAX, mostrar “Guardado” y al recargar y volver a cargar el horario debe seguir la asignación.

5. **Escenarios 4 y 5 (Conflictos):** Probar mismo docente mismo día/bloque (debe bloquear) y mismo grupo mismo día/bloque (debe bloquear); en ambos debe mostrarse SweetAlert de error.

6. **Escenario 6 (Seguridad Teacher):** Como Teacher, intentar guardar con asignación de otro docente o consultar otro `teacherId`; debe responder con error / no autorizado.

7. **Escenario 7 (Admin/Director):** Como Admin o Director, abrir Horario por Docente; debe verse el desplegable **Docente** y poder cargar el horario de cualquier docente (solo lectura).

---

## 4. Resumen

- **Sí puedes hacer tus pruebas:** con la BD actual (1 escuela, 1 año académico, 432 teacher assignments) y tras **arrancar la app una vez** (para crear tablas y bloques por defecto), los 7 escenarios de `PRUEBAS_FUNCIONALES_HORARIOS_POST_BLINDADO.md` son ejecutables.
- **Requisito:** Tener al menos un usuario **Teacher** (o docente) y otro **Admin** o **Director** de la misma escuela para cubrir todos los escenarios.
- Documento de detalle de cada escenario: **`Docs/PRUEBAS_FUNCIONALES_HORARIOS_POST_BLINDADO.md`**.
