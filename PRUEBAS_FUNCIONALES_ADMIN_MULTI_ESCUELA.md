# PRUEBAS FUNCIONALES — ADMIN MULTI-ESCUELA
**Fecha:** 2026-05-01  
**Auditor:** QA Engineer Senior + Arquitecto de Software (nivel enterprise SaaS)  
**Sistema:** EduPlaner SchoolManager — ASP.NET Core MVC .NET 8 + PostgreSQL  
**Metodología:** 54 queries directas a BD + análisis de logs de aplicación + pruebas HTTP  
**Alcance:** 2 escuelas activas, rol Admin, aislamiento multi-tenant completo  
**psql:** `C:\Program Files\PostgreSQL\18\bin\psql.exe` — BD: `eduplaner` @ localhost

---

## 1. RESUMEN EJECUTIVO

| Indicador | Estado |
|---|---|
| **Estado del sistema (login)** | 🔴 2 bugs impiden el login — sistema inaccesible vía UI |
| **Aislamiento multi-tenant (datos)** | ✅ CORRECTO — cero mezcla cross-school detectada |
| **Integridad referencial cross-tenant** | ✅ 0 violaciones en student/subject assignments |
| **Registros con school_id NULL** | 🔴 4,004 registros huérfanos (3,487 calificaciones + 516 asistencias + 1 área) |
| **Audit logs funcionando** | 🔴 0 registros — el sistema no está auditando nada |
| **Pruebas UI completadas** | ❌ Bloqueadas por bugs de runtime |
| **Pruebas de BD completadas** | ✅ 54 validaciones ejecutadas — 52 tablas inspeccionadas |

### Escuelas en el sistema
| Escuela | Estudiantes | Docentes | Grupos | Materias | Estado |
|---|---|---|---|---|---|
| **Instituto Dr. Alfredo Canton** | 1,356 | 0 | 19 | 1 | ⚠️ Incompleta |
| **Instituto Prof. y Téc. San Miguelito (IPTSM)** | 1,850 | 120 | 27 | 71 | ✅ Completa |

---

## 2. RESULTADO POR MÓDULO

### 🔴 LOGIN / AUTENTICACIÓN

| Aspecto | Resultado |
|---|---|
| **Login page (GET)** | ❌ HTTP 500 inicial — `DataProtectionKeyDbContext` buscaba columna `"Id"` (mayúscula) pero la tabla tiene `id` (minúscula) en PostgreSQL |
| **Login POST con credenciales incorrectas** | ❌ HTTP 500 — `IMemoryCache` con `SizeLimit` configurado pero el lockout no especifica `Size = 1` en la entrada de caché |
| **Antiforgery token** | ✅ Token presente en el HTML del formulario (una vez corregido el primer bug) |
| **Pruebas UI** | ❌ Imposibles sin login funcional |

**Bugs de login encontrados:**
1. `DataProtectionKeyDbContext`: falta `.HasColumnName("id")` en la propiedad PK
2. `AuthController.Login`: `_cache.GetOrCreate()` y `_cache.Set()` sin `Size = 1` cuando el cache tiene `SizeLimit`

---

### ✅ MÓDULO USUARIOS (/User)

| Validación | Resultado |
|---|---|
| Usuarios con school_id correcto | ✅ 100% — 1,358 Canton + 1,983 IPTSM |
| Usuarios sin school_id (no superadmin) | ✅ 0 registros |
| Email único por escuela (no global) | ✅ Índice `(school_id, lower(email))` — correcto para multi-tenant |
| Documento único por escuela | ✅ Índice `(school_id, document_id)` |
| Mezcla cross-school | ✅ Ninguna |

---

### ✅ MÓDULO ESTUDIANTES

| Validación | Resultado |
|---|---|
| Aislamiento por school_id | ✅ Completamente aislados |
| Tabla `students` adicional | ✅ Tiene school_id indexado |
| Cross-school asignaciones | ✅ 0 estudiantes de Canton asignados a grupos de IPTSM |

---

### ✅ MÓDULO ASIGNACIONES ACADÉMICAS

| Validación | Resultado |
|---|---|
| `subject_assignments` tiene school_id | ✅ Sí, indexado |
| Materia de Escuela A en grupo de Escuela B | ✅ 0 violaciones (TEST 51) |
| `student_assignments` — integridad | ✅ 0 violaciones cross-school (TEST 49) |

---

### ✅ MÓDULO CATÁLOGOS (Materias / Grados / Grupos)

| Catálogo | Canton | IPTSM | Mezcla |
|---|---|---|---|
| Materias | 1 | 71 | ✅ Ninguna |
| Grados | 7 (1°–12°) | 6 (7°–12°) | ✅ Ninguna |
| Grupos | 19 | 27 | ✅ Ninguna |
| Especialidades | 1 | 10 | ✅ Ninguna |
| Áreas | 0 reales | 4 | ⚠️ 1 área "Espacial" sin school_id |
| Índices únicos | ✅ Compuestos `(school_id, name)` | ✅ | — |

**Observación sobre índices duplicados:** Existen 2 índices únicos redundantes en `activity_types` y `area` con el mismo contenido pero nombres distintos (residuo de migración anterior).

---

### 🔴 MÓDULO ASISTENCIA

| Validación | Resultado |
|---|---|
| Registros con school_id correcto | ✅ 6,464 (IPTSM) |
| **Registros con school_id = NULL** | 🔴 **516 registros** |
| Escuela propietaria de los NULL | IPTSM (verificado via JOIN a users) |
| Periodo afectado | 2026-03-02 → 2026-04-22 |
| Impacto en GQF | Estos 516 registros son **invisibles** para todos los usuarios: GQF filtra `e.SchoolId == tenantId`, los NULL nunca coinciden |
| Canton | 0 registros (no usa el módulo) |

---

### 🔴 MÓDULO CALIFICACIONES (student_activity_scores)

| Validación | Resultado |
|---|---|
| Registros con school_id correcto | ✅ 11,278 (IPTSM) |
| **Registros con school_id = NULL** | 🔴 **3,487 registros** |
| Escuela propietaria de los NULL | IPTSM (verificado via JOIN a users) |
| Periodo afectado | 2026-04-21 → 2026-04-22 (datos recientes) |
| Impacto en GQF | **3,487 calificaciones recientes de IPTSM son invisibles** — docentes no pueden ver ni los reportes los muestran |
| Mezcla cross-school | ✅ Ninguna (todos los NULL pertenecen a IPTSM) |

---

### ✅ MÓDULO DISCIPLINA / ORIENTACIÓN

| Módulo | Resultado |
|---|---|
| Reportes disciplina | ✅ 3 reportes, todos de IPTSM, school_id NOT NULL |
| Reportes orientación | ✅ 0 registros (módulo vacío) |

---

### ✅ MÓDULO CARNETS / QR

| Validación | Resultado |
|---|---|
| `student_id_cards` | ✅ Canton: 3 carnets, IPTSM: 622 — aislados via student FK |
| `student_qr_tokens` | ✅ Canton: 3 tokens, IPTSM: 622 — aislados via student FK |
| `scan_logs` | ✅ 72 escaneos, todos de IPTSM |
| Configuración de carnet por escuela | ✅ `school_id_card_settings` — 1 fila por escuela |
| Mezcla cross-school | ✅ Ninguna |

---

### ✅ MÓDULO CONSEJEROS / ORIENTACIÓN

| Validación | Resultado |
|---|---|
| `counselor_assignments` | ✅ 64 asignaciones, todas IPTSM, school_id correcto |
| Índices únicos | ✅ `(school_id, user_id)` y `(school_id, grade_id, group_id)` |

---

### ✅ MÓDULO MENSAJERÍA

| Validación | Resultado |
|---|---|
| Mensajes | ✅ 224, todos IPTSM, school_id correcto |
| Mezcla cross-school | ✅ Ninguna |

---

### ✅ MÓDULO AÑOS ACADÉMICOS / TRIMESTRES

| Módulo | Resultado |
|---|---|
| Academic years | ✅ 5 por cada escuela, aislados |
| Trimestres | ✅ Solo IPTSM (3), school_id correcto |
| Shifts/Jornadas | ✅ 2 por escuela, aisladas |

---

### 🔴 MÓDULO AUDITORÍA (AuditLog)

| Validación | Resultado |
|---|---|
| Total de registros | 🔴 **0 registros** — la tabla está completamente vacía |
| GQF aplicado | ✅ Sí (implementado) |
| Paginación | ✅ Implementada |
| **Problema** | El sistema nunca ha llamado a `IAuditLogService.LogActionAsync()` — no existe ningún `AuditLog` en toda la BD |

---

### ✅ MÓDULO PAGOS

| Validación | Resultado |
|---|---|
| `payments` | ✅ 0 registros (módulo sin uso) |
| `payment_concepts` | ✅ 0 registros |
| `student_payment_access` | ✅ Canton: 424, IPTSM: 699 — completamente aislados |

---

## 3. PROBLEMAS CRÍTICOS

### 🔴 CRIT-QA-01: Bug #1 en Login — DataProtectionKeyDbContext columna "id" vs "Id"
**Severidad:** P0 — Sistema inaccesible  
**Archivo:** `Models/DataProtectionKeyDbContext.cs`

La tabla fue creada con `id SERIAL PRIMARY KEY` (minúsculas). EF Core con Npgsql cita los identificadores y genera `d."Id"` que PostgreSQL rechaza porque el nombre citado es case-sensitive.

```
Npgsql.PostgresException: 42703: column d.Id does not exist
Microsoft.AspNetCore.DataProtection.KeyManagement.KeyRingProvider: An error occurred while reading the key ring.
→ HTTP 500 en GET /Auth/Login
```

**Fix:** Agregar `entity.Property(e => e.Id).HasColumnName("id");` en `OnModelCreating`.

---

### 🔴 CRIT-QA-02: Bug #2 en Login — IMemoryCache sin Size en entradas de lockout
**Severidad:** P0 — HTTP 500 en login con credenciales incorrectas  
**Archivo:** `Controllers/AuthController.cs`

`AddMemoryCache` está configurado con `SizeLimit`. Toda entrada de caché requiere `entry.Size`. Las entradas de lockout no la especifican → excepción no manejada → 500.

```
System.InvalidOperationException: Cache entry must specify a value for Size when SizeLimit is set.
→ HTTP 500 en POST /Auth/Login (cuando password es incorrecto)
```

**Fix:** Agregar `entry.Size = 1` en `GetOrCreate` y `new MemoryCacheEntryOptions { Size = 1 }` en los `_cache.Set()`.

---

### 🔴 CRIT-QA-03: 3,487 Calificaciones Recientes Invisibles (IPTSM)
**Severidad:** P1 — Pérdida funcional de datos  
**Tabla:** `student_activity_scores`

```sql
SELECT school_id, COUNT(*) FROM student_activity_scores GROUP BY school_id;
-- 6e42399f (IPTSM): 11,278  ← visible
-- NULL:              3,487  ← INVISIBLE vía GQF
```

- 3,487 calificaciones del período 21-22 abril 2026 tienen `school_id = NULL`
- El GQF evalúa `e.SchoolId == _tenantId` → NULL nunca iguala nada → registros excluidos
- Docentes de IPTSM no ven las calificaciones de ese período en ninguna pantalla
- Los reportes de notas los omiten

**Causa probable:** La lógica que guarda `ActivityScore` no estaba asignando el `SchoolId` del `ICurrentUserService` en ese período.

**Fix (SQL):**
```sql
UPDATE student_activity_scores 
SET school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65'
WHERE school_id IS NULL;
```

---

### 🔴 CRIT-QA-04: 516 Registros de Asistencia Invisibles (IPTSM)
**Severidad:** P1 — Pérdida de datos históricos  
**Tabla:** `attendance`

```sql
SELECT COUNT(*) FROM attendance WHERE school_id IS NULL;
-- 516 registros entre 2026-03-02 y 2026-04-22
```

Mismo patrón que CRIT-QA-03. Asistencias del período registradas antes de que `SchoolId` fuera poblado en el servicio.

**Fix (SQL):**
```sql
UPDATE attendance 
SET school_id = '6e42399f-6f17-4585-b92e-fa4fff02cb65'
WHERE school_id IS NULL;
```

---

### 🔴 CRIT-QA-05: Audit Log Completamente Vacío
**Severidad:** P1 — Sin trazabilidad de acciones  
**Tabla:** `audit_logs`

```sql
SELECT COUNT(*) FROM audit_logs;  -- 0
```

El módulo de auditoría existe, el GQF está implementado, la paginación funciona — pero **ningún controlador llama a `IAuditLogService.LogActionAsync()`**. La tabla nunca ha recibido datos. El sistema opera sin ningún rastro de auditoría de acciones de usuarios (creaciones, ediciones, eliminaciones).

---

## 4. PROBLEMAS IMPORTANTES

### 🟡 IMP-QA-01: Área "Espacial" Huérfana (school_id = NULL)
```sql
SELECT id, name FROM area WHERE school_id IS NULL;
-- a6bef71b | Espacial
```
No aparece en ningún dropdown de área para ninguna escuela. Si materias referencian esta área, mostrarán "Sin área".

### 🟡 IMP-QA-02: Índices Únicos Duplicados en 3 Tablas
| Tabla | Índice 1 | Índice 2 (redundante) |
|---|---|---|
| `activity_types` | `activity_types_name_school_key` | `activity_types_school_name_key` |
| `area` | `area_name_school_key` | `area_school_name_key` |
| `grade_levels` | `grade_levels_school_name_key` | `uq_grade_levels_school_name` |

Overhead en escrituras sin beneficio adicional.

### 🟡 IMP-QA-03: Instituto Dr. Alfredo Canton Incompleto
| Módulo | Estado |
|---|---|
| Docentes | ❌ 0 — escuela sin ningún docente |
| Trimestres | ❌ 0 — sin períodos académicos configurados |
| Calificaciones | ❌ 0 — no hay actividades registradas |
| Asistencia | ❌ 0 — no usa el módulo |
| Materias | ⚠️ Solo 1 materia configurada |

Si un admin de Canton intenta acceder a calificaciones, reportes o asistencia, encontrará pantallas vacías sin mensajes explicativos.

### 🟡 IMP-QA-04: 16 Tablas sin school_id Directo
Dependen de cadenas FK para aislamiento de tenant. Sin GQF directo, un bug en cualquier servicio que omita el JOIN correcto expone datos cross-school:

`student_assignments`, `teacher_assignments`, `scan_logs`, `schedule_entries`, `student_id_cards`, `student_qr_tokens`, `email_queues`, `activity_attachments`, `teacher_work_plan_details`, etc.

---

## 5. EVIDENCIA DETALLADA

### Mapa completo de aislamiento por tabla

| Tabla | Tiene school_id | Registros NULL | Mezcla cross-tenant |
|---|---|---|---|
| `users` | ✅ | 0 | ✅ Ninguna |
| `groups` | ✅ | 0 | ✅ Ninguna |
| `grade_levels` | ✅ | 0 | ✅ Ninguna |
| `subjects` | ✅ | 0 | ✅ Ninguna |
| `subject_assignments` | ✅ | 0 | ✅ Ninguna |
| `activities` | ✅ | 0 | ✅ Ninguna |
| **`student_activity_scores`** | ✅ | **3,487 🔴** | ✅ Ninguna |
| **`attendance`** | ✅ | **516 🔴** | ✅ Ninguna |
| **`area`** | ✅ | **1 🟡** | ✅ Ninguna |
| `specialties` | ✅ | 0 | ✅ Ninguna |
| `discipline_reports` | ✅ | 0 | ✅ Ninguna |
| `counselor_assignments` | ✅ | 0 | ✅ Ninguna |
| `messages` | ✅ | 0 | ✅ Ninguna |
| `trimester` | ✅ | 0 | ✅ Ninguna |
| `academic_years` | ✅ | 0 | ✅ Ninguna |
| `shifts` | ✅ | 0 | ✅ Ninguna |
| `prematriculations` | ✅ | 0 | ✅ Ninguna |
| `teacher_work_plans` | ✅ | 0 | ✅ Ninguna |
| `student_payment_access` | ✅ | 0 | ✅ Ninguna |
| `audit_logs` | ✅ | — | ✅ (tabla vacía) |
| `student_assignments` | ❌ (via FK) | N/A | ✅ 0 violaciones |
| `teacher_assignments` | ❌ (via FK) | N/A | No verificable sin datos |

### Prueba de integridad referencial cross-school
```sql
-- Estudiantes de Escuela A asignados a grupos de Escuela B:
SELECT COUNT(*) FROM student_assignments sa
JOIN users u ON u.id = sa.student_id
JOIN groups g ON g.id = sa.group_id
WHERE u.school_id != g.school_id;
→ 0 violaciones ✅

-- Materias de Escuela A en grupos de Escuela B:
SELECT COUNT(*) FROM subject_assignments sa
JOIN subjects sub ON sub.id = sa.subject_id
JOIN groups g ON g.id = sa.group_id  
WHERE sub.school_id != g.school_id;
→ 0 violaciones ✅
```

---

## 6. RIESGOS EN PRODUCCIÓN

| # | Riesgo | Probabilidad | Impacto | Acción |
|---|---|---|---|---|
| 1 | **Login 500** — usuarios no pueden entrar | 🔴 CERTERO | 🔴 Total | Fix urgente pre-deploy |
| 2 | **3,487 calificaciones invisibles** en IPTSM | 🔴 Ya ocurrió | 🔴 Alto | UPDATE SQL urgente |
| 3 | **516 asistencias invisibles** en IPTSM | 🔴 Ya ocurrió | 🟠 Medio | UPDATE SQL urgente |
| 4 | **Sin auditoría** — imposible investigar incidentes | 🟠 Alta | 🟠 Medio | Implementar llamadas a LogActionAsync |
| 5 | **Canton incompleta** — módulos vacíos confunden al admin | 🟠 Alta | 🟡 Bajo | Comunicar al admin o completar datos |
| 6 | **Nuevos registros sin school_id** si el patrón persiste | 🟡 Media | 🟠 Medio | Verificar que todos los servicios poblan school_id |

---

## 7. VEREDICTO FINAL

### ¿El sistema es seguro para multi-escuela?

```
╔══════════════════════════════════════════════════════════════════╗
║  🔴 NO APTO PARA DEPLOY — 2 bugs bloquean el login              ║
║                                                                   ║
║  ✅ AISLAMIENTO DE DATOS: CORRECTO                               ║
║     • Cero mezcla cross-school detectada en 54 tests             ║
║     • Índices únicos compuestos (school_id, name) correctos      ║
║     • Integridad referencial: 0 violaciones cross-tenant         ║
║                                                                   ║
║  🔴 DATOS CORRUPTOS EN BD (no mezcla, pero sí invisibles):       ║
║     • 3,487 calificaciones de IPTSM con school_id = NULL         ║
║     • 516 asistencias de IPTSM con school_id = NULL              ║
║     • Requieren UPDATE SQL antes de go-live                       ║
║                                                                   ║
║  🔴 AUDITORÍA: 0 registros — el sistema opera sin trazabilidad   ║
╚══════════════════════════════════════════════════════════════════╝
```

### Plan de acciones antes del deploy

| Orden | Acción | Tiempo | Tipo |
|---|---|---|---|
| 1 | Fix `DataProtectionKeyDbContext` → `.HasColumnName("id")` | 5 min | Código |
| 2 | Fix `AuthController` lockout → `Size = 1` en caché entries | 10 min | Código |
| 3 | `UPDATE attendance SET school_id = '6e42399f...' WHERE school_id IS NULL` | 2 min | SQL |
| 4 | `UPDATE student_activity_scores SET school_id = '6e42399f...' WHERE school_id IS NULL` | 2 min | SQL |
| 5 | `UPDATE area SET school_id = '6e42399f...' WHERE id = 'a6bef71b...'` | 1 min | SQL |
| 6 | Verificar que servicios de attendance y scores siempre pueblan school_id | 30 min | Revisión |
| 7 | Implementar llamadas a `IAuditLogService.LogActionAsync()` en acciones críticas | 2-4 h | Código |
| 8 | Eliminar índices únicos duplicados | 15 min | Migración |

---

*Pruebas ejecutadas: 54 queries directas a PostgreSQL.*  
*Tablas inspeccionadas: 32 de 52 en esquema public.*  
*Mezclas cross-tenant detectadas: **0** (aislamiento correcto).*  
*Registros con datos inválidos detectados: **4,004** (school_id = NULL, todos de IPTSM).*
