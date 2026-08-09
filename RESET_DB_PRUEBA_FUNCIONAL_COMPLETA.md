# Reset DB y prueba funcional completa (DEV)

**Fecha:** 2026-05-03  
**Entorno:** solo DEV. Cliente PostgreSQL: `C:\Program Files\PostgreSQL\18\bin\psql.exe`.  
**Aplicación:** `http://localhost:5173` (login y flujos verificados en pestaña tipo Chrome vía automatización).

---

## 1. Base de datos limpiada correctamente

- **Conexión** leída de `appsettings.Development.json` → `ConnectionStrings:DefaultConnection`:
  - Host: `localhost`
  - Base de datos: `eduplaner`
  - Usuario: `postgres`
  - Puerto: `5432`
  - *(contraseña en el archivo local; no se reproduce aquí)*

- **Método:** script transaccional `migration_artifacts/reset_dev_data_ui_test.sql` (solo `DELETE` / `UPDATE`, **sin** `DROP` de tablas; **no** se toca `__EFMigrationsHistory` ni `data_protection_keys`).

- **Ajuste respecto a la lista DELETE del enunciado:** en este esquema no existen tablas `grades` ni el orden literal del prompt; el script sigue el grafo real de FKs. **Corrección aplicada:** `DELETE FROM schedule_entries` **antes** de `DELETE FROM teacher_assignments` (FK `schedule_entries_teacher_assignment_id_fkey`); la primera ejecución falló y hizo `ROLLBACK` completo; tras el cambio, **COMMIT** exitoso.

- **Usuarios conservados:** `DELETE FROM users WHERE LOWER(COALESCE(role,'')) IS DISTINCT FROM 'superadmin'` deja solo cuentas `superadmin` (en esta BD quedaron **2** filas: `admin@correo.com`, `superadmin.rolesmatrix@test.local`).

---

## 2. Escuelas creadas

Desde la UI (SuperAdmin), formulario **Crear Escuela con Administrador** (`/SuperAdmin/CreateSchoolWithAdmin`):

| Escuela           | Admin (email)                 | Contraseña inicial (formulario) |
|-------------------|-------------------------------|----------------------------------|
| Colegio Central   | `admin.central.qa@test.local` | `Test#2026`                      |
| Instituto Norte   | `admin.norte.qa@test.local`   | `Test#2026`                      |

**Validación:** redirección a `/SuperAdmin/ListSchools` con ambas escuelas **Activas** y datos de contacto visibles.

**IDs en BD (referencia):**

- Colegio Central: `81166291-47b9-40f8-8d18-f939a4516aac`
- Instituto Norte: `a2a9a324-9ec6-4629-8b4c-91fcb4c275a5`

---

## 3. Usuarios por rol

| Rol / alcance | Estado en esta corrida |
|---------------|-------------------------|
| **superadmin** | Login OK (`superadmin.rolesmatrix@test.local` + contraseña documentada en `PRUEBAS_ROLES_REALES_MULTI_ESCUELA.md`). Panel `/SuperAdmin/Index`. |
| **admin** (por escuela) | Creado junto con cada escuela; login OK como `admin.central.qa@test.local` + institución **Colegio Central** → `/Home/Index` con cabecera de escuela correcta. |
| **secretaria**, **teacher**, **estudiante** (5+), otros roles | **No creados ni probados** en esta sesión (pendiente para repetición completa del plan Fases 5–10). |

---

## 4. Configuración académica

**No ejecutada** en esta sesión (grados 10°/11°, grupos A/B, materias, asignaciones docente). Requiere sesión **admin** por escuela y recorrer catálogo / asignaciones según menú actual.

---

## 5. Estudiantes creados

**Ninguno** (`SELECT COUNT(*) FROM students` → **0** tras el reset; no se abrió flujo de secretaría/alta de estudiantes).

---

## 6. Actividades creadas

**No aplicable** (sin docentes ni estudiantes asignados).

---

## 7. Notas registradas

**No aplicable.**

---

## 8. Notas editadas

**No aplicable.**

---

## 9. Asistencia registrada

**No aplicable.**

---

## 10. Validación por rol

- **superadmin:** acceso a creación de escuelas y lista; sistema operativo en `5173`.
- **admin (Colegio Central):** login con selector de institución; dashboard admin.
- Resto de roles del plan: **pendiente**.

---

## 11. Validación multi-escuela

- Tras el reset: **0** escuelas; tras la UI: **2** escuelas en BD (`SELECT COUNT(*) FROM schools` → **2**).
- Login: combo **Institución** ofrece **Colegio Central** e **Instituto Norte** cuando aplica.
- **Aislamiento (muestra):** con sesión **admin de Colegio Central**, la URL `/User/Edit/959c77cf-473e-4de2-9c2e-bbc0c3008d1b` (admin de **Instituto Norte**) responde **`/Auth/AccessDenied` (403)** — no se expone el formulario de edición del otro colegio.

---

## 12. Validación seguridad (URLs)

| Prueba | Resultado |
|--------|-----------|
| `/Student/Details/11111111-1111-1111-1111-111111111111` (sesión previa superadmin / rol sin acceso a detalle estudiante) | Redirect a **`/Auth/AccessDenied`** |
| `/User/Edit/{id}` de admin de **otra** escuela | **`/Auth/AccessDenied` (403)** |

---

## 13. Errores encontrados

1. **Script de limpieza (primera corrida):** orden de `DELETE` violó FK en `schedule_entries` → `teacher_assignments`. Corregido en `reset_dev_data_ui_test.sql`.
2. **Navegación UI:** clic en enlace “Nueva Escuela” desde `/SuperAdmin/Index` falló por scroll/viewport en automatización; **workaround:** navegación directa a `/SuperAdmin/CreateSchoolWithAdmin`.
3. **Alcance de prueba:** no se completaron Fases 5–10 del plan original (matriz completa de usuarios, académica, secretaría, profesor, estudiante, duplicado exhaustivo por escuela).

---

## 14. Correcciones aplicadas

- **Código / artefactos:** reordenar en `migration_artifacts/reset_dev_data_ui_test.sql` los `DELETE` para que `schedule_entries` se elimine **antes** que `teacher_assignments`.
- **Sin cambios de aplicación** en esta corrida (el fallo fue de script DBA, no de bug de app).

---

## Consola del navegador (Fase 12)

En `/SuperAdmin/ListSchools`: solo **warnings** (listeners de formularios de eliminación, mensaje del runtime del navegador). **Sin errores críticos** reportados en la captura de consola de esa vista.

---

## Conteos de validación (post-reset y post-alta escuelas)

Tras **reset** (antes de crear escuelas por UI):

```sql
SELECT COUNT(*) FROM users;    -- 2 (solo superadmin)
SELECT COUNT(*) FROM students; -- 0
SELECT COUNT(*) FROM schools;  -- 0
```

Tras **crear las dos escuelas** (incluye admins):

```sql
SELECT COUNT(*) FROM schools;  -- 2
SELECT COUNT(*) FROM users;    -- 4 (2 superadmin + 2 admin)
SELECT COUNT(*) FROM students; -- 0
```

---

## Criterio final (según el plan original)

| Criterio | Estado |
|----------|--------|
| DB limpia (datos, FK respetadas) | **Sí** (script corregido y ejecutado con éxito) |
| Flujo completo extremo a extremo (todos los roles y académica) | **Parcial** — cubierto: reset, login, superadmin, alta 2 escuelas, admin una escuela, muestras de seguridad |
| CRUD completo multi-escuela | **No verificado** en esta sesión |
| Roles completos | **No** |
| Sin errores UI en lo probado | **Sí** en rutas usadas |
| Sin mezcla de datos (muestra URL) | **Sí** en prueba cruzada `User/Edit` |
| Seguridad por URL | **403 / AccessDenied** en casos probados |

**Conclusión:** el entorno quedó **listo para continuar** el plan completo desde UI (Fases 5 en adelante). Para declarar el sistema “válido” al 100% según el checklist original, hace falta una segunda corrida dedicada a creación de usuarios, catálogo académico, estudiantes, gradebook y asistencia en **ambas** escuelas.
