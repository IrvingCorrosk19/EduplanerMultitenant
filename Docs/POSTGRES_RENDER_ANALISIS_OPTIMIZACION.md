# Análisis completo de optimización — PostgreSQL (Render)

**Ámbito:** base de datos `schoolmanagement_*` alojada en Render (región Oregon), consultada mediante cliente `psql` desde `C:\Program Files\PostgreSQL\18\bin`.  
**Fecha de referencia de métricas:** abril 2026 (valores de `pg_stat_*` acumulados desde el último reset de estadísticas o desde el arranque del servidor; son **orientativos** y crecen con el tráfico).

**Nota de seguridad:** este documento **no** incluye cadenas de conexión ni contraseñas. Las credenciales deben permanecer en variables de entorno, secretos de Render o `User Secrets`, no en `appsettings.json` versionado.

---

## 1. Objetivo y metodología

### 1.1 Objetivo

Identificar cuellos de botella de lectura/escritura, uso ineficiente de índices, tablas sobrecargadas por `seq_scan`, y oportunidades de optimización a nivel aplicación (caché, consultas) y base de datos (índices, mantenimiento).

### 1.2 Fuentes de datos (PostgreSQL)

| Fuente | Uso |
|--------|-----|
| `pg_database_size(current_database())` | Tamaño total de la BD. |
| `pg_stat_user_tables` | `seq_scan`, `idx_scan`, `n_live_tup`, `n_dead_tup`, `last_autovacuum`, etc. |
| `pg_stat_user_indexes` | `idx_scan`, tamaño de índice, detección de índices sin uso. |
| `pg_total_relation_size` / `pg_relation_size` | Tamaño por tabla (heap + índices TOAST). |
| `EXPLAIN (ANALYZE, BUFFERS)` | Planes concretos para consultas críticas (ej. `StudentAssignment`). |
| `pg_extension` | Comprobar si `pg_stat_statements` está disponible. |

### 1.3 Limitaciones

- Las estadísticas **no distinguen** qué aplicación o endpoint disparó cada acceso; reflejan **todo** el tráfico contra la BD.
- En tablas **muy pequeñas**, el planificador suele preferir **seq scan** aun existan índices; un `seq_scan` alto no implica siempre “error de diseño”.
- Sin **`pg_stat_statements`**, no hay ranking automático de “queries más lentas en ms”; las conclusiones se basan en **patrones de tabla/índice** y en pruebas puntuales con `EXPLAIN`.

---

## 2. Panorama general de la instancia

| Métrica | Valor observado (referencia) |
|---------|------------------------------|
| Tamaño aproximado de la BD | ~42 MB |
| Motor | PostgreSQL 17.x (Debian, hosting Render) |
| `pg_stat_statements` | **No instalado** en el momento del análisis |

La base es de **tamaño modesto**; muchos problemas percibidos en la aplicación no vienen del volumen en disco sino de **número de round-trips**, **planes de consulta**, **consultas repetidas** y **capa cliente** (navegador, DataTables, etc.).

---

## 3. Tablas con mayor `seq_scan` acumulado

Orden aproximado por `seq_scan` descendente (tablas `public` con más lecturas secuenciales registradas):

| Tabla | Filas vivas (`n_live_tup` ref.) | `seq_scan` (ref.) | `idx_scan` (ref.) | Interpretación breve |
|-------|----------------------------------|-------------------|-------------------|----------------------|
| **groups** | ~27 | ~506 000 | ~38 000 | Catálogo minúsculo leído masivamente; casi siempre full scan barato pero **repetido en exceso** desde la app. |
| **grade_levels** | ~6 | ~490 000 | ~15 000 | Igual que `groups`: patrón de **reconsulta** constante. |
| **student_id_cards** | ~77 | ~144 000 | ~2 300 | Muchas lecturas secuenciales; conviene revisar **consultas** e **índices** alineados con filtros reales. |
| **shifts** | ~2 | ~112 000 | ~2 400 | Catálogo mínimo; mismo patrón que grupos/grados. |
| **orientation_reports** | 0 | ~25 000 | 0 | Tabla vacía pero **muy consultada**; revisar código que la incluye siempre. |
| **attendance** | ~1 337 | ~25 000 | ~334 | **~99%** del tráfico de escaneo es secuencial; falta alinear índices con filtros (p. ej. `school_id` + fecha). |
| **schools** | 0 | ~25 000 | ~1 400 | Similar: consultas frecuentes con pocos o ningún resultado. |
| **users** | ~1 969 | ~18 700 | ~276 000 | Mezcla de PK y otros índices; proporcionalmente **más indexado** que catálogos pequeños. |
| **activities** | ~62 | ~14 200 | ~4 000 | Revisar patrones de listado/filtro. |
| **counselor_assignments** | ~13 | ~13 800 | 0 | Tabla diminuta; seq scan es barato; si crece, revisar filtros por `school_id` / `user_id`. |
| **student_activity_scores** | ~532 | ~12 700 | ~4 600 | Alto ratio secuencial; revisar consultas de reportes o listados. |
| **discipline_reports** | 0 | ~12 700 | ~6 | Tabla vacía, muchas lecturas. |
| **subjects** | ~71 | ~10 700 | ~31 000 | Catálogo; parte secuencial por consultas sin filtro por clave. |
| **student_assignments** | ~1 842 | ~6 100 | ~1 160 000 | **Muy indexado** en operaciones típicas; coherente con optimizaciones recientes (JOIN + índices parciales). |

**Porcentaje aproximado secuencial** (útil para priorizar):  
`100 * seq_scan / NULLIF(seq_scan + idx_scan, 0)`

Ejemplos del análisis:

- **attendance:** ~98,7% secuencial.
- **student_id_cards:** ~98,5% secuencial.
- **groups / shifts:** ~93–98% secuencial (esperable en tablas de decenas de filas, pero el **volumen de llamadas** es el problema de agregado).
- **student_assignments:** ~0,5% secuencial (muy bueno).

---

## 4. Tablas más grandes en disco (referencia)

Orden por `pg_total_relation_size` (incluye índices asociados al heap principal):

| Tabla | Tamaño total (ref.) | Filas (ref.) | Comentario |
|-------|---------------------|--------------|------------|
| **student_activity_scores** | ~24 MB | ~532 | Desproporción tamaño/filas: posible **TOAST** (texto/json grande), índices pesados o histórico; merece revisión de modelo y retención. |
| **activities** | ~1 MB | ~62 | Similar sospecha de payload grande o muchos índices. |
| **users** | ~1 MB | ~1 969 | Normal para entidad central. |
| **student_assignments** | ~1 MB | ~1 842 | Coherente con índices múltiples. |

*(Los valores exactos en MB pueden variar levemente entre mediciones.)*

---

## 5. Esquema puntual: tablas problemáticas

### 5.1 `attendance`

- Columnas relevantes: `student_id`, `teacher_id`, `group_id`, `grade_id`, `date`, `school_id`, etc.
- Índices observados: `IX_attendance_*` sobre `grade_id`, `group_id`, `student_id`, `teacher_id`.
- **No** aparece índice dedicado sobre **`school_id`**, aunque existe FK `fk_attendance_school`.

**Riesgo:** listados o informes por **escuela + rango de fechas** tienden a **seq scan** sobre toda la tabla.

**Recomendación:** tras revisar las consultas reales en la aplicación, añadir un índice compuesto acorde, por ejemplo:

- `(school_id, date DESC)` si el listado principal es por escuela y fecha, o  
- `(school_id, student_id, date)` si el caso dominante es historial por alumno dentro de la escuela.

Usar `CREATE INDEX CONCURRENTLY` en producción y validar con `EXPLAIN ANALYZE`.

### 5.2 `counselor_assignments`

- Pocas filas; índices en `school_id`, `user_id`, `grade_id`, `group_id` y uniques compuestos.
- Estadísticas mostraron **solo seq scan** a nivel agregado: para 13 filas el costo es irrelevante; optimizar solo tiene sentido si la tabla **crece** o si hay joins costosos.

### 5.3 `student_id_cards`

- Alto `seq_scan` con pocas filas sugiere que las consultas **no están usando** un índice útil o que el patrón es `SELECT` amplio sin predicado selectivo.
- Revisar en código: búsquedas por `student_id`, `card_number`, `school_id`, etc., y crear índices que **coincidan** con el orden de columnas del `WHERE`.

---

## 6. Índices sin uso (`idx_scan = 0`)

Fragmento representativo (excluyendo claves primarias; nombres según catálogo):

| Tabla | Índice | Comentario |
|-------|--------|------------|
| **student_assignments** | `ix_student_assignments_academic_year_id` | Candidato a revisar; puede ser redundante si otros índices cubren las mismas consultas. |
| **users** | `IX_users_cellphone_primary` | Sin lecturas por estadística; útil solo si se implementan búsquedas por celular. |
| **users** | `users_email_key`, `users_document_id_key` | **UNIQUE**: mantienen integridad aunque `idx_scan` sea 0 (login puede usar otro camino o estadísticas aún no reflejan todo). **No eliminar** sin análisis funcional. |
| **groups** | `IX_groups_school_id`, `ix_groups_shift_id` | 0 uso coherente con `GetAllAsync()` sin `WHERE` en muchos flujos. |
| **shifts** | `ix_shifts_name`, `ix_shifts_is_active` | Similar. |
| **activities** | Varios con 0 uso | Validar con planes reales antes de `DROP`. |
| **email_queues** / **email_jobs** | Varios con 0 uso | Posible código batch aún no ejercitado o workers distintos. |

**Criterio prudente:**

- **No borrar** índices **UNIQUE** que garanticen reglas de negocio.
- **Evaluar `DROP`** solo en índices **no únicos**, tras confirmar en staging con `EXPLAIN` y carga de prueba.

---

## 7. Mantenimiento: vacío y `dead tuples`

Ejemplo de filas muertas observadas:

| Tabla | `n_dead_tup` (ref.) | `last_autovacuum` (ref.) |
|-------|---------------------|---------------------------|
| **users** | ~82 | Reciente (autovacuum activo). |
| **groups** | ~29 | Anterior; seguir monitorizando. |
| **student_id_cards** | ~31 | Reciente. |
| **attendance** | 0 | Autovacuum reciente. |

PostgreSQL gestiona esto con **autovacuum**; en picos de actualización masiva puede valorarse un `VACUUM (ANALYZE)` manual en ventana de bajo uso.

---

## 8. Extensiones recomendadas

### 8.1 `pg_stat_statements`

**Estado:** no instalada en el momento del análisis.

**Beneficio:** identificar las consultas con mayor tiempo total, mayor `mean_time`, mayor `shared_blks_read`, etc.

**Acción sugerida:** habilitar en Render según documentación del proveedor (parámetro `shared_preload_libraries` y `CREATE EXTENSION pg_stat_statements;`), con precaución en entornos compartidos.

---

## 9. Optimizaciones ya aplicadas en el proyecto (contexto)

Estas acciones ya forman parte del repositorio o de la BD gestionada; se documentan para no duplicar esfuerzos.

### 9.1 Aplicación — `StudentAssignment/Index`

- Eliminación del patrón **N+1** (cientos/miles de llamadas a `GetAssignmentsByStudentIdAsync` por carga).
- Carga masiva mediante **`GetActiveAssignmentsForCurrentSchoolAsync()`** con **JOIN** por `school_id` y roles, alineada con planes eficientes en PostgreSQL.
- Uso de **diccionarios** en memoria para resolver grado, grupo y jornada sin `FirstOrDefault` repetido en bucles grandes.
- Ajustes menores en **DataTables** (`autoWidth: false`, `orderClasses: false`) para reducir trabajo en el cliente con muchas filas.

### 9.2 Base de datos — script SQL versionado

Archivo: `SchoolManager/Scripts/optimize_student_assignment_read_performance.sql`

| Índice | Finalidad |
|--------|-----------|
| `ix_users_school_id_lower_role` | Soporta filtros por `school_id` y `lower(role)` alineados con listados de estudiantes por escuela. |
| `ix_student_assignments_active_student_created_at` | Parcial `WHERE is_active = true`, soporte para `(student_id, created_at DESC)` en cargas por alumno o JOIN masivo. |

**Ejecución:** `psql` con SSL contra el host de Render (sin commitear credenciales en el repo).

### 9.3 Verificación con `EXPLAIN ANALYZE`

Un plan de referencia (JOIN estudiantes de la escuela con mayor población de alumnos + asignaciones activas) mostró:

- Uso de **`ix_users_school_id_lower_role`**.
- **Index Only Scan** (con algunos heap fetches) sobre **`ix_student_assignments_active_student_created_at`**.
- Tiempo de ejecución del orden de **pocos milisegundos** en el servidor para el conjunto completo de asignaciones activas de esa escuela.

Esto confirma que, una vez desplegado el código y existiendo los índices, el cuello de botella deja de ser la consulta SQL en sí y pasan a pesar **latencia de red**, **serialización**, **renderizado Razor** y **JavaScript en el navegador**.

---

## 10. Lista priorizada de trabajo futuro

### Prioridad alta

1. **Caché de aplicación** (p. ej. `IMemoryCache` con TTL corto e invalidación al editar) para **groups**, **grade_levels**, **shifts** y otros catálogos estáticos consultados en casi cada request.
2. **Revisar consultas de `attendance`** y añadir **índice** alineado con `school_id` y criterios de fecha/listado.
3. **Revisar consultas de `student_id_cards`** y alinear índices con predicados reales.

### Prioridad media

4. **Auditar** por qué se consultan con frecuencia tablas **vacías** (`orientation_reports`, `schools` con 0 filas, `discipline_reports`, etc.) y reducir esas lecturas o cachear resultados vacíos.
5. **Investigar tamaño** de **student_activity_scores** (TOAST, columnas grandes, índices redundantes).
6. **Habilitar `pg_stat_statements`** en Render para priorizar el siguiente ciclo con evidencia de tiempo de CPU e I/O.

### Prioridad baja / mantenimiento

7. **Revisión periódica** de índices con `idx_scan = 0` (solo no únicos, tras pruebas).
8. **`VACUUM ANALYZE`** manual tras migraciones masivas o imports.
9. **Rotar secretos** si alguna vez se versionó `appsettings.json` con contraseña de producción.

---

## 11. Comandos útiles para repetir el análisis

```sql
-- Tamaño de la base
SELECT pg_database_size(current_database()) / 1024 / 1024 AS db_size_mb;

-- Tablas con más seq_scan
SELECT relname, n_live_tup::bigint, seq_scan, idx_scan
FROM pg_stat_user_tables
WHERE schemaname = 'public'
ORDER BY seq_scan DESC
LIMIT 25;

-- Índices nunca usados (excl. PK)
SELECT relname, indexrelname, idx_scan, pg_size_pretty(pg_relation_size(indexrelid))
FROM pg_stat_user_indexes
WHERE schemaname = 'public' AND idx_scan = 0 AND indexrelname NOT LIKE '%_pkey'
ORDER BY pg_relation_size(indexrelid) DESC;
```

Cliente local (Windows), sin exponer contraseña en línea de comandos persistente:

```powershell
$env:PGPASSWORD = '<secreto>'
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -h <host> -p 5432 -U <usuario> -d <database> -c "SELECT 1"
```

---

## 12. Conclusión

La instancia en Render es **pequeña en tamaño** pero acumula **patrones de acceso** típicos de aplicaciones multi-módulo: catálogos leídos miles de veces, algunas tablas transaccionales sin índices alineados con los filtros reales (`attendance`), y tablas vacías consultadas repetidamente. Las mejoras ya hechas en **StudentAssignment** y los **índices compuestos/parciales** asociados sitúan esa parte del sistema en un plan saludable a nivel SQL.

El mayor retorno adicional vendrá de **caché de catálogos**, **índices y consultas para asistencia e id cards**, **reducción de lecturas innecesarias a tablas vacías**, y de **observabilidad** (`pg_stat_statements`) para basar el siguiente refinamiento en datos objetivos.

---

*Documento generado para el proyecto SchoolManager / EduplanerIIC. Actualizar métricas ejecutando de nuevo las consultas de la sección 11 cuando se requiera un nuevo baseline.*
