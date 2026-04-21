# Estructura de la base de datos (relación con jornadas)

Documento generado a partir del modelo EF Core y migraciones. Para obtener el DDL completo de tu BD ejecuta:

```powershell
cd c:\Proyectos\EduplanerIIC\SchoolManager
.\Scripts\ExportDbSchema.ps1
```

(Requiere `pg_dump` en el PATH y conexión configurada en appsettings o variable `DefaultConnection`.)

---

## Tablas que manejan jornadas

### 1. `shifts` (catálogo de jornadas)

| Columna        | Tipo      | Nulo | Descripción                    |
|----------------|-----------|------|--------------------------------|
| id             | uuid      | PK   | gen_random_uuid()              |
| school_id      | uuid      | FK   | schools(id) ON DELETE CASCADE  |
| name           | varchar(50)| NOT NULL | Ej. Mañana, Tarde           |
| description    | text      | sí   |                                |
| is_active      | boolean   | default true |                 |
| display_order  | integer   | default 0 |                  |
| created_at     | timestamptz |    |                                |
| updated_at     | timestamptz |    |                                |
| created_by     | uuid      | FK users |                      |
| updated_by     | uuid      | FK users |                      |

**Índices:** `IX_shifts_school_id` (school_id).

---

### 2. `groups`

| Columna        | Tipo      | Relación con jornadas |
|----------------|-----------|------------------------|
| id             | uuid      | PK                     |
| school_id      | uuid      | FK schools             |
| name           | varchar   |                        |
| grade          | integer   |                        |
| **shift_id**   | **uuid**  | **FK shifts(id)** – jornada del grupo |
| shift          | varchar   | Texto legacy (por compatibilidad)     |
| ...            |           |                        |

Cada grupo puede pertenecer a una jornada (Mañana/Tarde) vía `shift_id`.

---

### 3. `student_assignments`

| Columna        | Tipo      | Relación con jornadas |
|----------------|-----------|------------------------|
| id             | uuid      | PK                     |
| student_id     | uuid      | FK users               |
| grade_id       | uuid      | FK grade_levels        |
| group_id       | uuid      | FK groups              |
| **shift_id**   | **uuid**  | **FK shifts(id)** – jornada del estudiante en esa asignación |
| ...            |           |                        |

La asignación estudiante–grado–grupo puede tener una jornada explícita (`shift_id`).

---

### 4. `time_slots` (bloques horarios)

| Columna        | Tipo      | Relación con jornadas |
|----------------|-----------|------------------------|
| id             | uuid      | PK                     |
| school_id      | uuid      | FK schools             |
| **shift_id**   | **uuid**  | **FK shifts(id) ON DELETE SET NULL** – jornada del bloque (opcional) |
| name           | varchar(50) |                      |
| start_time     | time      |                        |
| end_time       | time      |                        |
| display_order  | integer   |                        |
| is_active      | boolean   |                        |
| created_at     | timestamptz |                      |

**Índices:** `IX_time_slots_school_id`, `IX_time_slots_shift_id`.

El bloque puede asociarse a una jornada (`shift_id`). En el código actual la generación automática de bloques **no** asigna este valor.

---

### 5. `school_schedule_configurations` (configuración mañana/tarde)

| Columna                          | Tipo      | Descripción                    |
|----------------------------------|-----------|--------------------------------|
| id                               | uuid      | PK                             |
| school_id                        | uuid      | FK schools, UNIQUE (una por escuela) |
| morning_start_time               | time      | Inicio jornada mañana          |
| morning_block_duration_minutes   | integer   | Minutos por bloque mañana      |
| morning_block_count              | integer   | N.º de bloques mañana          |
| afternoon_start_time             | time      | NULL si no hay tarde           |
| afternoon_block_duration_minutes | integer   | NULL                           |
| afternoon_block_count            | integer   | NULL                           |
| created_at                       | timestamptz |                             |
| updated_at                       | timestamptz |                             |

Define la “jornada” en sentido de **horario** (qué bloques se generan para mañana y tarde). No guarda FK a `shifts`; al guardar se generan filas en `time_slots`.

---

### 6. `schedule_entries` (horario asignado)

| Columna              | Tipo   | Descripción          |
|----------------------|--------|----------------------|
| id                   | uuid   | PK                   |
| teacher_assignment_id| uuid   | FK teacher_assignments |
| time_slot_id         | uuid   | FK time_slots        |
| day_of_week          | smallint | 1–7 (Lun–Dom)     |
| academic_year_id     | uuid   | FK academic_years    |
| created_by           | uuid   | FK users             |

No tiene columna de jornada; la jornada del bloque viene de `time_slots.shift_id` (si se rellenara).

---

## Resumen: ¿manejamos jornadas?

| Concepto                         | ¿Se maneja en BD? | Tablas / columnas                          |
|----------------------------------|-------------------|--------------------------------------------|
| Catálogo de jornadas (Mañana/Tarde) | **Sí**         | `shifts`                                   |
| Jornada del grupo                | **Sí**            | `groups.shift_id` (y `shift` legacy)       |
| Jornada del estudiante (asignación) | **Sí**        | `student_assignments.shift_id`             |
| Jornada del bloque horario       | **Sí en BD**      | `time_slots.shift_id` (opcional; en código no se asigna al generar) |
| Configuración mañana/tarde (horarios) | **Sí**      | `school_schedule_configurations`           |

Conclusión: **sí, la base de datos maneja jornadas**: tabla `shifts`, y relaciones en `groups`, `student_assignments` y `time_slots`. La configuración de bloques mañana/tarde está en `school_schedule_configurations`. Lo que no está implementado en la aplicación es **asignar** `time_slots.shift_id` al generar bloques desde la configuración de jornada.
