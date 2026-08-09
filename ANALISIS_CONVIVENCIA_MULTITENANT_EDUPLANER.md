# ANALISIS_CONVIVENCIA_MULTITENANT_EDUPLANER

## 1. Resumen ejecutivo

Se auditó en modo solo lectura:

- `eduplaner2` en Render (`schoolmanager_hx5i`)
- `EduplanerIIC` en Render (`schoolmanagement_xqks`)
- `EduplanerMultitenant` en localhost (`eduplaner`)

Hallazgo central: **las dos bases remotas (eduplaner2 e IIC) son casi gemelas a nivel de estructura**, pero la base local multitenant no está idéntica (falta una tabla y hay divergencias puntuales de columnas/constraints/índices). A nivel de datos, **IIC y local son casi el mismo dataset**, mientras que eduplaner2 es mayormente distinto.

Conclusión ejecutiva (sin diseñar solución):

- **Convivencia técnica en una sola DB:** posible a nivel físico (PK UUID, bajo solapamiento entre instancias remotas).
- **Convivencia segura multi-tenant en operación real hoy:** **NO** (riesgo alto) por aislamiento incompleto en tablas clave sin `school_id` directo y dependencia de disciplina en consultas/aplicación.

---

## 2. Comparación estructural

### 2.1 Inventario global

| Base | Tablas | Columnas | Constraints | Índices |
|---|---:|---:|---:|---:|
| eduplaner2_render | 51 | 595 | 472 | 209 |
| eduiic_render | 51 | 595 | 189 | 209 |
| multitenant_local | 50 | 582 | 457 | 207 |

Observaciones críticas:

1. **eduplaner2 vs EduplanerIIC**
   - Misma cantidad de tablas/columnas/índices.
   - Diferencias de constraints reportadas: 287 (285 de eduplaner2, 2 de IIC), concentradas en representación de NOT NULL por versión de motor.
   - Motores distintos:
     - eduplaner2: PostgreSQL 18.3
     - EduplanerIIC: PostgreSQL 17.9
   - El delta de `constraints` está sesgado por versión del engine, no necesariamente por modelo funcional distinto.

2. **Render vs local multitenant**
   - `multitenant_local` tiene **1 tabla menos**: `EmailConfigurations` (presente en ambas remotas).
   - Diferencias de firma de columnas: 15 (14 por ausencia de `EmailConfigurations` + 1 por naming en `subject_assignments` `school_id` vs `SchoolId`).
   - Diferencias de índices: 4 (2 por `EmailConfigurations`, 2 por naming de `subject_assignments`).

### 2.2 Tablas y columnas divergentes

Divergencias estructurales verificadas:

- Tabla faltante en local: `EmailConfigurations`.
- Divergencia de naming en `subject_assignments`:
  - Render: `SchoolId` (y su índice/constraint asociado)
  - eduplaner2 (y parcialmente local): `school_id`

Esto rompe equivalencia de esquema 1:1 para consolidación directa sin mapeo.

---

## 3. Compatibilidad entre bases

### 3.1 Claves primarias / colisiones

- El modelo usa UUID en entidades centrales (`users`, `schools`, etc.).
- Riesgo de colisión de PK por merge masivo: **bajo**.

### 3.2 Solapamiento de identidades de usuario (email)

Emails únicos por base:

- eduplaner2: 1,359
- EduplanerIIC: 1,984
- multitenant_local: 1,983

Overlap:

- eduplaner2 vs EduplanerIIC: **3** emails
- eduplaner2 vs local: **3** emails
- EduplanerIIC vs local: **1,982** emails

Lectura crítica:

- eduplaner2 e IIC son casi disjuntas en identidades de usuario.
- local es prácticamente espejo de IIC, con pequeñas desviaciones (2 emails en IIC no presentes en local y 1 email en local no presente en IIC).

### 3.3 Solapamiento de tenants (escuelas)

- eduplaner2: `Instituto Dr. Alfredo Canton`
- IIC/local: `Instituto Profesional y Técnico San Miguelito`

Overlap de nombres de escuela:

- eduplaner2 vs IIC: 0
- eduplaner2 vs local: 0
- IIC vs local: 1

### 3.4 Integridad observable en datos

Checks ejecutados:

- `users_without_school` = 1 en las 3 bases.
- `student_assignments` con mismatch `users.school_id != grade_levels.school_id` = 0 en las 3 bases.
- `student_payment_access` con mismatch `users.school_id != student_payment_access.school_id` = 0 en las 3 bases.
- Duplicados de email dentro de cada base: 0.

Resultado: no se observaron rupturas masivas de integridad en los checks críticos evaluados.

---

## 4. Viabilidad multi-tenant

### 4.1 Cobertura tenant-aware

Cobertura de `school_id`:

- eduplaner2: 32/51 tablas con `school_id`.
- IIC: 32/51 tablas con `school_id`.
- local: 33/50 tablas con `school_id`.

FK directas a `schools`:

- eduplaner2: 30
- IIC: 30
- local: 29

### 4.2 Tablas sin `school_id` ni FK directa a `schools` (local)

Se identificaron 17 tablas en esta categoría, incluyendo:

- `student_assignments`
- `student_id_cards`
- `student_qr_tokens`
- `teacher_assignments`
- `scan_logs`
- `schedule_entries`
- `user_grades`, `user_groups`, `user_subjects`

Diagnóstico:

- El aislamiento depende de relaciones indirectas (joins a `users`, `grade_levels`, etc.) y de filtros aplicacionales consistentes.
- Esto **no es aislamiento fuerte por diseño de datos**; es aislamiento por convención de uso.

---

## 5. Riesgos críticos

1. **Riesgo de fuga inter-tenant por consultas incompletas**
   - Tablas operativas sin `school_id` directo exigen joins/filtros correctos en todo endpoint/reporte.
   - Cualquier omisión expone datos de otro tenant.

2. **Riesgo de consolidación incompleta por drift de esquema local**
   - Falta `EmailConfigurations` en local.
   - Naming inconsistente `SchoolId` vs `school_id` en `subject_assignments`.

3. **Riesgo de conflicto de identidad funcional (no PK) al consolidar**
   - Aunque PK UUID reduce colisión técnica, emails repetidos entre sistemas remotos (3 casos) pueden colisionar con reglas de negocio/autenticación si se unifica contexto.

4. **Riesgo de desviación operacional por dataset local casi clonado de IIC**
   - local ya está fuertemente sesgada hacia IIC; consolidar eduplaner2 encima sin hardening de aislamiento aumenta superficie de error.

---

## 6. Limitaciones actuales

- La equivalencia estructural entre entornos no es total (local != remotas).
- El modelo tenant no está completamente materializado en todas las tablas críticas con `school_id` directo.
- Diferencias de versión PostgreSQL (17.9 vs 18.x) afectan representación de metadatos de constraints y complican auditoría homogénea.
- Hay 1 usuario sin `school_id` en cada base (debe tratarse como excepción de aislamiento en runtime).

---

## 7. Conclusión clara: ¿Pueden convivir o NO?

**Conclusión:**

- **Pueden convivir físicamente:** **Sí** (estructura base compatible entre remotas, UUIDs, bajo solapamiento eduplaner2 vs IIC).
- **Pueden convivir correctamente y de forma segura en multi-tenant productivo hoy:** **NO**.

Motivo directo:

- El aislamiento tenant no está suficientemente “cerrado” en el modelo de datos (tablas relevantes sin `school_id` directo), por lo que la convivencia depende de disciplina de consulta/código y no de garantías estructurales fuertes.

---

## 8. Nivel de riesgo

**ALTO**.

No es “Crítico” porque no se detectaron inconsistencias masivas de integridad en los checks ejecutados y los datasets remotos son mayormente disjuntos; pero tampoco es “Medio/Bajo” por la superficie real de fuga de datos inter-tenant y el drift de esquema entre local y remotas.

---

## Evidencia técnica (fuente del análisis)

Extracción SQL read-only realizada sobre las 3 bases con `psql`:

- inventario de tablas/columnas/constraints/índices
- cobertura `school_id`
- FKs directas a `schools`
- conteos de entidades núcleo (`schools`, `users`, `student_assignments`, `student_payment_access`, `student_id_cards`, `student_qr_tokens`)
- overlaps de emails y nombres de escuelas
- checks de mismatch de `school_id` en asignaciones/pagos

Sin cambios de estructura, sin DDL, sin migraciones, sin updates/deletes.
