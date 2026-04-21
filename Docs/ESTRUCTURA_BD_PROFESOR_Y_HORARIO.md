# Estructura BD: Profesor y cómo identificar a qué horario pertenece

Documento de verificación: tablas y relaciones que permiten saber **en qué jornada/horario** está un profesor.

---

## 1. Identidad del profesor

| Tabla   | Descripción |
|---------|-------------|
| **users** | Usuarios del sistema. El profesor es un `User` con `Role` = Teacher (o el valor que use la app para docente). `users.id` = `teacher_id` en las tablas de asignación. |

No hay tabla `teachers` separada: el profesor es un usuario con rol de docente.

---

## 2. Tablas que vinculan al profesor con grupos y horarios

### 2.1. `teacher_assignments`

Asigna un **profesor** a una **asignatura–grupo** (materia en un grupo concreto).

| Columna               | Tipo | Descripción |
|-----------------------|------|-------------|
| id                    | uuid | PK          |
| **teacher_id**        | uuid | FK **users(id)** – el profesor |
| **subject_assignment_id** | uuid | FK **subject_assignments(id)** – materia en un grupo |
| created_at            | timestamptz | |

**Índice único:** `(teacher_id, subject_assignment_id)` – un profesor no puede estar dos veces en la misma materia-grupo.

- **Cadena:** `User (profesor)` → `TeacherAssignment` → `SubjectAssignment` → **Group** (y de ahí a **Shift**).

### 2.2. `subject_assignments`

Define una materia en un **grupo** (especialidad, área, materia, nivel, grupo).

| Columna     | Tipo | Descripción |
|-------------|------|-------------|
| id          | uuid | PK          |
| specialty_id| uuid | FK          |
| area_id     | uuid | FK          |
| subject_id  | uuid | FK          |
| grade_level_id | uuid | FK       |
| **group_id**| uuid | FK **groups(id)** – grupo donde se imparte |
| school_id   | uuid | FK (opcional) |
| status      | varchar |           |
| created_at  | timestamptz |        |

- **Cadena:** `SubjectAssignment` → **Group** → **Shift** (jornada del grupo).

### 2.3. `groups`

Cada grupo puede tener una **jornada** (Mañana/Tarde).

| Columna     | Tipo | Descripción |
|-------------|------|-------------|
| id          | uuid | PK          |
| school_id   | uuid | FK          |
| name        | varchar |           |
| **shift_id**| uuid | FK **shifts(id)** – jornada del grupo (opcional) |
| shift       | varchar | Legacy; preferir shift_id |

- **Cadena:** `Group` → **Shift** (nombre de jornada: Mañana, Tarde).

### 2.4. `shifts`

Catálogo de jornadas por escuela (Mañana, Tarde, etc.).

| Columna   | Tipo   | Descripción      |
|-----------|--------|------------------|
| id        | uuid   | PK               |
| school_id | uuid   | FK schools       |
| name      | varchar(50) | Ej. "Mañana", "Tarde" |

---

## 3. Horario concreto: bloques y días

### 3.1. `schedule_entries`

Une **asignación profesor–materia–grupo** con un **bloque horario** y un **día** (y año académico). Es la tabla que define “el profesor da esta materia en este grupo, este día, en este bloque”.

| Columna                | Tipo   | Descripción |
|------------------------|--------|-------------|
| id                     | uuid   | PK          |
| **teacher_assignment_id** | uuid | FK **teacher_assignments(id)** |
| **time_slot_id**       | uuid   | FK **time_slots(id)** – bloque (hora inicio/fin) |
| **day_of_week**        | smallint | 1 = Lunes … 7 = Domingo |
| **academic_year_id**   | uuid   | FK academic_years |
| created_at             | timestamptz |     |
| created_by             | uuid   | FK users (opcional) |

**Índice único:** `(teacher_assignment_id, academic_year_id, time_slot_id, day_of_week)` – evita duplicar la misma celda en el horario.

- **Cadena:** `User (profesor)` → `TeacherAssignment` → **ScheduleEntry** → **TimeSlot** (y opcionalmente TimeSlot → Shift).

### 3.2. `time_slots`

Bloques horarios de la escuela (ej. “Bloque 1”, 07:00–07:45). Pueden estar asociados a una jornada.

| Columna     | Tipo | Descripción |
|-------------|------|-------------|
| id          | uuid | PK          |
| school_id   | uuid | FK          |
| **shift_id**| uuid | FK **shifts(id)** – jornada del bloque (opcional) |
| name        | varchar(50) |     |
| start_time  | time |             |
| end_time    | time |             |
| display_order | int |           |
| is_active   | boolean |          |

---

## 4. Cómo identificar “a qué horario pertenece” el profesor

Hay **dos formas** de interpretar “horario”:

### A) Por JORNADA (Mañana / Tarde)

**Pregunta:** ¿En qué jornada(s) imparte el profesor?

**Relación en BD:**

```
users (profesor)
  → teacher_assignments (teacher_id)
    → subject_assignments (subject_assignment_id)
      → groups (group_id)
        → shifts (shift_id)  →  shifts.name = "Mañana" | "Tarde" | ...
```

- **Tablas:** `users` → `teacher_assignments` → `subject_assignments` → `groups` → `shifts`.
- **Consulta lógica:** Para un `teacher_id`, obtener los `groups.shift_id` distintos vía `teacher_assignments` y `subject_assignments`, y luego los `shifts.name`.
- En la app esto es lo que se usa para mostrar “Usted imparte en jornada(s): Mañana, Tarde” en **Horario por Docente**.

### B) Por BLOQUES Y DÍAS (horario semanal concreto)

**Pregunta:** ¿Qué bloque y qué día tiene asignado el profesor?

**Relación en BD:**

```
users (profesor)
  → teacher_assignments (teacher_id)
    → schedule_entries (teacher_assignment_id)
      → time_slots (time_slot_id)  →  name, start_time, end_time, shift_id (jornada del bloque)
      + day_of_week (1–7)
      + academic_year_id
```

- **Tablas:** `users` → `teacher_assignments` → `schedule_entries` → `time_slots` (y opcionalmente `time_slots.shift_id` → `shifts`).
- **Consulta lógica:** Para un `teacher_id` y un `academic_year_id`, listar `schedule_entries` con su `time_slot` y `day_of_week`. La “jornada” de cada celda puede leerse de `time_slots.shift_id` → `shifts.name` si está definida.

---

## 5. Resumen de tablas “todo lo relacionado con el profesor” y horario

| Tabla                  | Rol respecto al profesor / horario |
|------------------------|-------------------------------------|
| **users**              | Identidad del profesor (`teacher_id` = users.id). |
| **teacher_assignments**| Vincula profesor ↔ materia–grupo (subject_assignment). |
| **subject_assignments**| Materia en un **grupo** (group_id). |
| **groups**             | Grupo con **jornada** (shift_id → shifts). |
| **shifts**             | Catálogo de jornadas (Mañana, Tarde). |
| **schedule_entries**   | Asigna una **teacher_assignment** a un **time_slot** + **day_of_week** + **academic_year**. |
| **time_slots**         | Bloques horarios (hora inicio/fin); opcionalmente shift_id. |
| **academic_years**     | Año escolar al que pertenece cada schedule_entry. |

---

## 6. Diagrama de relaciones (profesor → horario)

```
                    users (Teacher)
                         |
                         | teacher_id
                         v
                teacher_assignments
                    /         \
                   /           \
    subject_assignment_id    (mismo teacher_assignment)
                   |                    |
                   v                    v
           subject_assignments    schedule_entries
                   |                    |
              group_id             time_slot_id, day_of_week,
                   |                academic_year_id
                   v                    |
                groups                  v
                   |               time_slots
              shift_id                  |
                   |               shift_id (opcional)
                   v                    |
                shifts <----------------+
              (Mañana, Tarde)
```

- **Jornada del profesor:** por los **grupos** que imparte → `groups.shift_id` → `shifts`.
- **Horario concreto (bloques y días):** por **schedule_entries** → `time_slots` (+ `time_slots.shift_id` si se quiere la jornada de cada bloque).

Para más detalle de las tablas de jornadas (`shifts`, `groups`, `time_slots`, `school_schedule_configurations`), ver **ESTRUCTURA_BD_JORNADAS.md**.
