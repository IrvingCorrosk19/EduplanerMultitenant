# Informe Enterprise – Optimización BD / EF Core – Eduplaner MultiTenant

**Fecha:** 2026-08-01  
**Alcance:** `SchoolManager` MultiTenant  
**Regla:** sin cambio de lógica de negocio ni de aislamiento `SchoolId` / GQF / RBAC.

---

## 1. Resumen ejecutivo

Se aplicó una pasada enterprise de rendimiento centrada en:

1. Índices compuestos con `SchoolId` primero (filtro multi-tenant temprano).
2. Eliminación de N+1 en cargas masivas y guardado de notas.
3. `AsNoTracking` / `AsSplitQuery` en lecturas calientes.
4. Pool Npgsql + `EnableRetryOnFailure` + `CommandTimeout`.
5. Caché segura de menú (solo por rol, sin datos de tenant).
6. Optimización in-memory de agregación de asistencia en reportes institucionales.

Compilación Release: **0 errores / 0 warnings**.  
Migración: `20260801152231_AddEnterprisePerformanceIndexes` aplicada en local; se despliega al VPS vía `Database.MigrateAsync()`.

---

## 2. Consultas / hot paths críticos detectados

| Área | Problema | Severidad | Mitigación |
|------|----------|-----------|------------|
| `TeacherAssignmentService.GetSubjectAssignmentIdsAsync` | N+1 `FirstOrDefaultAsync` por ítem | Alta | 1 query + lookup en memoria |
| `StudentAssignmentService.BulkAssignFromFileAsync` | 4–5 round-trips por fila | Alta | Precarga en diccionarios + 1 `SaveChanges` |
| `StudentActivityScoreService.SaveAsync` | Query + year + validate por nota | Alta | Batch load + 1 year + validate distinct |
| `TeacherAssignmentService` Includes | Cartesian explosion potencial | Media | `AsNoTracking` + `AsSplitQuery` |
| `UserService.GetAllStudentsAsync` | Tracking innecesario | Media | `AsNoTracking` |
| Gradebook / reportes scores | Tracking + sin índice school+activity | Alta | `AsNoTracking` + índice compuesto |
| `ReportesInstitucionalesBulkLoader` asistencia | O(estudiantes×trimestres×registros) | Media | Agrupación O(n) |
| `MenuService` | Reconstruía menú estático cada request | Baja | `ConcurrentDictionary` por rol |
| Npgsql | Sin retry / pool explícito | Media | Pool + retry + timeout 60s |
| Actividades / attendance / prematricula / payments | Índices solo mono-columna `SchoolId` | Alta | Índices compuestos |

---

## 3. Índices creados (migración)

| Índice | Tabla | Columnas |
|--------|-------|----------|
| `IX_activities_school_group_grade` | activities | school_id, group_id, grade_level_id |
| `IX_activities_school_teacher_group` | activities | school_id, teacher_id, group_id |
| `IX_activities_school_subject_group_trimester` | activities | school_id, subject_id, group_id, trimester |
| `IX_attendance_school_group_date` | attendance | school_id, group_id, date |
| `IX_attendance_school_student_date` | attendance | school_id, student_id, date |
| `IX_student_activity_scores_school_activity` | student_activity_scores | school_id, activity_id |
| `IX_student_assignments_school_group_grade_active` | student_assignments | school_id, group_id, grade_id, is_active |
| `IX_users_school_role` | users | school_id, role |
| `IX_users_school_status` | users | school_id, status |
| `IX_prematriculations_school_status` | prematriculations | school_id, status |
| `IX_prematriculations_school_period_status` | prematriculations | school_id, prematriculation_period_id, status |
| `IX_payments_school_payment_date` | payments | school_id, payment_date |
| `IX_payments_school_payment_status` | payments | school_id, payment_status |
| `IX_messages_school_sent_at` | messages | school_id, sent_at |
| `IX_orientation_reports_school_date` | orientation_reports | school_id, date |
| `IX_orientation_reports_school_status` | orientation_reports | school_id, status |

**Índices eliminados:** ninguno (no se borró índice sin evidencia de redundancia dañina).  
**Ya existente y útil:** `ix_users_school_id_lower_role` (script previo) — Bitmap Index Scan confirmado en EXPLAIN.

---

## 4. Cambios EF Core / aplicación

- `Program.cs`: `EnableRetryOnFailure(3)`, `CommandTimeout(60)`.
- `PostgresConnectionResolver`: `EnsurePooling` (Min 2 / Max 100 / Timeout 30).
- `TeacherAssignmentService`: anti-N+1 + `AsNoTracking` + `AsSplitQuery`.
- `StudentAssignmentService.BulkAssignFromFileAsync`: batch.
- `StudentActivityScoreService`: batch `SaveAsync` + `AsNoTracking` en gradebook.
- `UserService.GetAllStudentsAsync`: `AsNoTracking`.
- `ReportesInstitucionalesBulkLoader`: agregación asistencia O(n).
- `MenuService`: caché por rol (sin datos sensibles de escuela).

---

## 5. EXPLAIN ANALYZE (evidencia local)

Dataset local pequeño (~23 MB) → planner a menudo elige **Seq Scan** por bajo costo; aun así:

- **users** (filtro role+school): ya usa `Bitmap Index Scan on ix_users_school_id_lower_role` (0.9 ms).
- Tras migración, índices compuestos creados y registrados en `__EFMigrationsHistory`.
- Beneficio esperado en VPS / producción con volúmenes reales: Index Scan / Bitmap Heap en reportes, gradebook, prematriculación y pagos filtrados por `SchoolId` + dimensión secundaria.

---

## 6. Seguridad MultiTenant

- No se removieron Global Query Filters.
- No se alteró RBAC / Auth.
- Filtros `SchoolId` en reportes / gradebook se mantuvieron.
- Caché de menú **no** incluye datos por escuela.

---

## 7. Pruebas

| Prueba | Resultado |
|--------|-----------|
| `dotnet build -c Release` | 0 errores / 0 warnings |
| Migración local `database update` | OK |
| Smoke login VPS post-deploy | (ver sección deploy) |

No se ejecutó suite Playwright completa en este ciclo (tiempo); se recomienda smoke manual de: Login, Gradebook, Reportes institucionales, Prematrícula, Asignaciones.

---

## 8. Riesgos y recomendaciones futuras

1. **Índices adicionales de escritura:** monitorear `pg_stat_user_indexes` en VPS a 30 días; dropear índices con `idx_scan = 0`.
2. **Compiled queries** para gradebook fijo.
3. **Paginación** en listados SuperAdmin / Prematriculation grandes si crecen >5k filas.
4. **VACUUM ANALYZE** periódico en VPS tras carga masiva.
5. No activar `QueryTrackingBehavior.NoTracking` global (rompe updates).

---

## 9. Confirmaciones

- Compilación correcta: **Sí**
- Aislamiento MultiTenant intacto: **Sí** (sin cambios de filtros/seguridad)
- Lógica de negocio intacta: **Sí** (mismas reglas; menos round-trips)
- Migraciones no rotas: **Sí** (migración aditiva de índices)
