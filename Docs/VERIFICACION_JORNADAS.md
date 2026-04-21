# Verificación: ¿El sistema maneja jornadas?

**Fecha:** 2026-02-16

---

## Resumen

**Sí**, el sistema maneja jornadas, pero en **dos sentidos distintos** que hoy **no están unidos** entre sí.

---

## 1. Jornadas como catálogo (entidad `Shift`)

| Qué | Dónde |
|-----|--------|
| **Entidad** | `Models/Shift.cs` – Id, SchoolId, Name, Description, IsActive, DisplayOrder |
| **Tabla** | `shifts` |
| **Servicio** | `ShiftService` – GetAllAsync, GetOrCreateAsync, CreateAsync, etc. |

**Uso actual:**

- **Grupos (`Group`):** tienen `ShiftId` y texto `Shift`. Un grupo puede estar en la jornada “Mañana” o “Tarde”.
- **Asignaciones de estudiante (`StudentAssignment`):** tienen `ShiftId`. Al guardar asignaciones se usa la jornada (ej. “Mañana”) y se asocia al estudiante/grupo.
- **Controlador** | `StudentAssignmentController` – obtiene jornadas, muestra “Jornada: {nombre}” y asigna `ShiftId` al guardar.
- **Carnés** | `StudentIdCardService` – muestra la jornada de la asignación activa del estudiante.

**Conclusión:** Las jornadas como **catálogo (Mañana/Tarde)** sí se manejan: grupos, estudiantes y asignaciones pueden tener una jornada (`Shift`).

---

## 2. Jornadas como configuración horaria (mañana/tarde de bloques)

| Qué | Dónde |
|-----|--------|
| **Entidad** | `SchoolScheduleConfiguration` – MorningStartTime, MorningBlockCount, AfternoonStartTime, AfternoonBlockCount, etc. |
| **Vista** | Horarios → **Configuración de jornada** – define inicio y cantidad de bloques de mañana y opcionalmente de tarde. |
| **Efecto** | Al guardar se **generan** los `TimeSlot` (bloques) de mañana y, si se configuró, de tarde. |

**Conclusión:** La **configuración de la jornada escolar** (qué bloques hay en la mañana y en la tarde) también se maneja y genera los bloques automáticamente.

---

## 3. Punto no conectado: `TimeSlot.ShiftId`

- La tabla **`time_slots`** tiene columna **`shift_id`** (FK opcional a `shifts`).
- En el modelo: `TimeSlot.ShiftId` y `TimeSlot.Shift`.
- **Nadie asigna este valor** en el flujo actual:
  - `ScheduleConfigurationService` al generar bloques **no** setea `ShiftId`.
  - `EnsureDefaultTimeSlots` **no** setea `ShiftId`.
  - Las vistas/controladores de TimeSlot (crear/editar) **no** muestran ni guardan jornada (Shift).

Por tanto: los bloques generados por “Configuración de jornada” **no** quedan asociados a una jornada del catálogo (Mañana/Tarde). La integridad referencial existe en BD, pero el uso de `TimeSlot.ShiftId` está sin usar.

---

## 4. Tabla resumen

| Ámbito | ¿Se maneja? | Notas |
|--------|-------------|--------|
| Catálogo de jornadas (Shift) | Sí | Grupos, StudentAssignment, ShiftService |
| Asignar jornada a estudiantes/grupos | Sí | StudentAssignmentController, GetOrCreateAsync |
| Configurar bloques mañana/tarde | Sí | SchoolScheduleConfiguration + generación de TimeSlots |
| Asignar jornada (Shift) a cada bloque (TimeSlot) | No | ShiftId existe en TimeSlot pero no se asigna |
| Filtrar horario por jornada en ByTeacher | No | La vista muestra todos los bloques activos sin filtrar por Shift |

---

## 5. Recomendación (opcional)

Si se quiere que “jornada” signifique lo mismo en catálogo y en horarios:

1. En **Configuración de jornada**: al generar bloques de mañana, asignar a cada `TimeSlot` el `ShiftId` de la jornada “Mañana” (buscada o creada por escuela). Igual para los bloques de tarde y la jornada “Tarde”.
2. En **TimeSlot** (crear/editar): permitir opcionalmente elegir jornada (Shift) para bloques creados a mano.
3. En **Horario por docente**: opcionalmente filtrar por jornada (solo bloques de Mañana o solo de Tarde) si la escuela trabaja por turnos separados.

Con esto el sistema seguiría manejando jornadas como ya lo hace, y además unificaría el uso de `Shift` con los bloques horarios generados y la vista de horarios.
