# Confirmación: corrección DeleteBehavior – Módulo Horarios

**Fecha:** 2026-02-12  
**Referencia:** REPORTE_AUDITORIA_DB_HORARIOS.md  
**Alcance:** SchoolDbContext – entidad ScheduleEntry. Sin cambios en base de datos.

---

## 1. Archivo y líneas modificadas

| Archivo | Sección | Líneas (aprox.) |
|---------|---------|------------------|
| `Models/SchoolDbContext.cs` | Configuración de `ScheduleEntry` en `OnModelCreating` | Índices: +1 línea (CreatedBy). Relaciones: 3 bloques HasOne/OnDelete (AcademicYear, TeacherAssignment, TimeSlot). |

---

## 2. Cambios realizados

### 2.1 Relaciones: Antes / Después

| Relación | Antes | Después | Columna en BD |
|----------|--------|---------|----------------|
| **ScheduleEntry → AcademicYear** | `OnDelete(DeleteBehavior.ClientSetNull)` | `OnDelete(DeleteBehavior.Restrict)` | `academic_year_id` NOT NULL |
| **ScheduleEntry → TeacherAssignment** | `OnDelete(DeleteBehavior.ClientSetNull)` | `OnDelete(DeleteBehavior.Restrict)` | `teacher_assignment_id` NOT NULL |
| **ScheduleEntry → TimeSlot** | `OnDelete(DeleteBehavior.ClientSetNull)` | `OnDelete(DeleteBehavior.Restrict)` | `time_slot_id` NOT NULL |
| **ScheduleEntry → CreatedByUser** | `OnDelete(DeleteBehavior.SetNull)` | **Sin cambio** | `created_by` NULL |

### 2.2 Índice añadido (bonus)

| Índice | Acción |
|--------|--------|
| `IX_schedule_entries_created_by` | Declarado en `OnModelCreating` con `entity.HasIndex(e => e.CreatedBy, "IX_schedule_entries_created_by")`. La migración ya creaba este índice en BD; el modelo queda alineado con el esquema. |

---

## 3. Riesgos mitigados

| Riesgo (auditoría) | Mitigación |
|--------------------|------------|
| **EF intentaba “set null” en columnas NOT NULL** | Con `Restrict`, EF ya no intenta poner la FK en null al borrar el principal. Si hay `ScheduleEntry` dependientes, el borrado del principal falla de forma coherente (igual que en BD con NO ACTION). |
| **Comportamiento engañoso / excepciones inesperadas** | El DbContext refleja el comportamiento real de la BD (NO ACTION / Restrict). Menos sorpresas al eliminar `AcademicYear`, `TeacherAssignment` o `TimeSlot`. |
| **Índice CreatedBy no declarado** | El modelo declara el índice existente en BD; consistencia documento–esquema y alineación con la migración. |

---

## 4. Lo que no se ha tocado

- **Base de datos:** Sin migración nueva. Las FKs en BD siguen con el mismo ON DELETE (NO ACTION para las tres FKs NOT NULL; SET NULL para `created_by`).
- **Otras entidades y relaciones:** Sin cambios.
- **Modelos (clases):** Sin cambios; solo configuración en `OnModelCreating`.

---

## 5. Comprobación rápida

- [x] Las tres FKs NOT NULL de `ScheduleEntry` usan `DeleteBehavior.Restrict`.
- [x] La FK nullable `CreatedBy` sigue con `DeleteBehavior.SetNull`.
- [x] Índice `IX_schedule_entries_created_by` declarado en DbContext.
- [x] No se ha generado ni aplicado ninguna migración en esta tarea.

---

*Corrección aplicada según criterio enterprise: DbContext alineado con la BD sin modificar el esquema.*
