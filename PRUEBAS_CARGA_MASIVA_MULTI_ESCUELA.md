# Validación de carga masiva por colegio (multi-tenant)

**Fecha:** 2026-05-03  
**Alcance:** Tres flujos expuestos como Excel→JSON en UI: **catálogo académico**, **asignación académica (docentes)**, **asignación de estudiantes**. Sin cambios de código de negocio.

## Automatización

Script: `qa_bulk_upload/Invoke-BulkUploadValidation.ps1`  
Ejecutado contra: `http://localhost:5172`  
Resultado JSON: `qa_bulk_upload/last_bulk_run.json`

### Sufijo de esta corrida

`b352b6b920` (embebido en nombres `QA_BULK_*` y correos `@test.local`).

### Respuestas HTTP (resumen)

| Escuela | SaveCatalog | SaveAssignmentsFromExcel | SaveAssignments (estudiantes) |
|---------|-------------|---------------------------|-------------------------------|
| Instituto Dr. Alfredo Canton | `success: true`, 1 creado por tipo de catálogo | 1 asignación insertada, 1 profesor creado | `insertadas: 1`, `estudiantesCreados: 1` |
| IPT San Miguelito | idem | idem | idem |

## Verificación SQL (school_id)

### Estudiantes y docentes creados

```sql
SELECT email, role, school_id FROM users
WHERE email LIKE '%b352b6b920@test.local'
ORDER BY email;
```

**Esperado:** 4 filas — dos `estudiante` y dos `teacher`, cada `school_id` coincidente con su escuela (Canton vs San Miguelito).  
**Observado:** Cumplido.

### Asignaciones de estudiantes

```sql
SELECT u.email, sa.school_id AS sa_school, u.school_id AS user_school
FROM student_assignments sa
JOIN users u ON u.id = sa.student_id
WHERE u.email LIKE '%b352b6b920%';
```

**Esperado:** `sa_school` = `user_school` para cada fila.  
**Observado:** Cumplido.

### Materias y asignaciones académicas (docentes / subject_assignments)

```sql
SELECT name, school_id FROM subjects
WHERE name ILIKE '%b352b6b920%'
ORDER BY school_id, name;
```

**Esperado:** Cuatro materias (`MAT`/`MAT2` por escuela), cada una con el `school_id` correcto.  
**Observado:** Cumplido (nombres normalizados a mayúsculas en servicio).

```sql
SELECT sa.id, sa.school_id, sub.name AS subject
FROM subject_assignments sa
JOIN subjects sub ON sub.id = sa.subject_id
WHERE sub.name ILIKE '%b352b6b920%';
```

**Esperado:** Dos filas (una por escuela), `subject_assignments.school_id` alineado con la materia.  
**Observado:** Cumplido.

### Especialidades creadas por catálogo

```sql
SELECT name, school_id FROM specialties
WHERE name ILIKE '%B352B6B920%'
ORDER BY school_id;
```

**Observado:** Cuatro filas (ESP + ESP2 por escuela), `school_id` correcto por escuela.

## Riesgos / hallazgos (no bloqueantes de esta prueba)

1. **`GetByEmailAsync`** en carga de estudiantes no filtra por escuela; si existiera el mismo email en dos colegios, la fila del Excel podría asociarse al usuario equivocado. Esta corrida usó **emails únicos globalmente** (`qa.bulk.*@test.local`), por lo que no se disparó el caso.
2. **Limpieza:** los registros `QA_BULK_*` y usuarios `qa.bulk.*` permanecen en la base de pruebas; eliminar con cuidado si se requiere DB limpia (solo entornos no productivos).

## Veredicto (carga masiva)

**Por escuela, los tres endpoints probados respetan el tenant:** catálogo, asignación docente y asignación estudiantil crean o enlazan datos con el `school_id` del administrador autenticado; cruces DB muestran coherencia entre `users`, `subjects`, `subject_assignments` y `student_assignments`.

## Cómo repetir

1. Levantar la aplicación.
2. `.\qa_bulk_upload\Invoke-BulkUploadValidation.ps1 -BaseUrl "http://localhost:5172"`.
3. Sustituir el sufijo en las consultas SQL por el de `last_bulk_run.json` (campo `CatalogMarker` contiene el patrón `QA_BULK_<TAG>_MAT_<suffix>`).
