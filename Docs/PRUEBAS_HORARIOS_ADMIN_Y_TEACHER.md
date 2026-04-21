# Pruebas: Horarios (Admin y Teacher)

**Fecha:** 2026-02-16  
**Estado compilación:** ✅ Build correcto (`dotnet build`)  
**Servidor:** `http://localhost:5172` (perfil `http` en launchSettings.json)

---

## 1. Comprobaciones automáticas realizadas

| Comprobación | Resultado |
|--------------|-----------|
| `dotnet build` | ✅ Build succeeded, 0 errores |
| `http://localhost:5172/Auth/Login` | ✅ 200, página de inicio de sesión |
| `http://localhost:5172/Schedule/ByTeacher` (sin sesión) | ✅ Redirige / muestra login |
| `http://localhost:5172/TimeSlot/Manage` (sin sesión) | ✅ Redirige / muestra login |

Sin sesión, las rutas protegidas llevan a la página de login (comportamiento correcto).

---

## 2. Pruebas como **Administrador** (o Director)

1. **Iniciar sesión** con un usuario **admin** o **director** de una escuela.
2. **Menú Horarios**
   - Debe aparecer el submenú con:
     - **Ver horario por docente**
     - **Ajustar bloques horarios**
3. **Ver horario por docente** (`/Schedule/ByTeacher`)
   - Seleccionar **Año académico** (ej. 2026).
   - Debe aparecer el desplegable **Docente**; elegir un docente.
   - Pulsar **Cargar horario**.
   - Debe mostrarse la tabla: columnas **Hora | Lunes … Viernes**, una fila por cada **TimeSlot** activo de la escuela (8, 10, 12, etc.).
   - Las celdas pueden ser solo lectura para Admin/Director (desplegables deshabilitados).
4. **Ajustar bloques horarios** (`/TimeSlot/Manage`)
   - Debe cargar la lista de bloques horarios de la escuela.
   - **Crear bloque:** botón "Nuevo bloque" → formulario (Nombre, Hora inicio, Hora fin, Orden) → guardar.
   - **Editar bloque:** en una fila, botón Editar → cambiar nombre/horas/orden/activo → guardar.
   - **Eliminar/Desactivar:** botón eliminar; si el bloque tiene horarios asignados, debe desactivarse; si no, eliminarse.

---

## 3. Pruebas como **Teacher** (docente)

1. **Iniciar sesión** con un usuario **teacher** o **docente** (ej. de la misma escuela que tenga asignaciones).
2. **Menú Horarios**
   - Debe aparecer solo **Horarios** (un enlace), sin submenú "Ajustar bloques horarios".
3. **Horario por Docente** (`/Schedule/ByTeacher`)
   - No debe aparecer el desplegable "Docente" (solo ve su propio horario).
   - Seleccionar **Año académico** y pulsar **Cargar horario**.
   - Debe mostrarse el mensaje: *"Puede modificar la asignación (materia/grupo) en cada celda usando el desplegable."*
   - Tabla con **Hora | Lunes … Viernes** y una fila por cada TimeSlot activo.
   - En cada celda: desplegable con opción "Vacío" y sus asignaciones (Materia - Grupo).
   - **Asignar:** elegir una materia/grupo en una celda → debe aparecer "Guardando…" y luego "Guardado"; la celda muestra la materia y un color pastel por materia.
   - **Quitar:** elegir "Vacío" en una celda con clase → confirmación "¿Eliminar?" → al confirmar, la celda queda vacía.
4. **TimeSlot/Manage (docente no debe acceder)**
   - Escribir en el navegador: `http://localhost:5172/TimeSlot/Manage`
   - Debe devolver **403 Forbidden** o redirigir a acceso denegado (solo Admin/Director).

---

## 4. Resumen de flujos

| Rol      | Horarios (menú)        | ByTeacher                    | TimeSlot/Manage   |
|----------|------------------------|-----------------------------|-------------------|
| Admin    | Submenú (Ver + Ajustar) | Ver cualquier docente        | ✅ Crear/Editar/Eliminar bloques |
| Director | Submenú (Ver + Ajustar) | Ver cualquier docente        | ✅ Crear/Editar/Eliminar bloques |
| Teacher  | Solo "Horarios"        | Solo su horario, editable    | ❌ 403 / no accesible |

---

## 5. Cómo ejecutar las pruebas

1. **Arrancar la aplicación** (si no está en marcha):
   ```bash
   cd c:\Proyectos\EduplanerIIC\SchoolManager
   dotnet run
   ```
2. Abrir `http://localhost:5172`, iniciar sesión como **admin** y seguir la sección 2.
3. Cerrar sesión (o usar otra ventana privada), iniciar sesión como **teacher** y seguir la sección 3.

Si alguna prueba falla, revisar: rol del usuario en BD (`users.role`), que la escuela tenga `academic_years` y `time_slots` (al arrancar la app se crean por defecto si no existen).
