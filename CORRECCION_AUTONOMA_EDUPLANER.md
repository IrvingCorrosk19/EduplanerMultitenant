# Corrección autónoma — Eduplaner SchoolManager

**Fecha:** 2026-05-03  
**Ciclo:** Preparar → Probar/romper (código + DB) → Corregir → Validar (compilación + SQL) → Documentar.

---

## 1. Resumen ejecutivo

| Aspecto | Estado inicial | Estado final (esta iteración) |
|--------|----------------|-------------------------------|
| Multi-escuela en DB | 2 escuelas activas; roles distribuidos; usuarios `@test.local` ya presentes | Confirmado por PostgreSQL; sin duplicados detectados en `subject_assignments` (clave natural con `school_id`). |
| `/User/*` por URL | Dependencia principal en GQF + `GetByIdAsync` → otro tenant suele aparecer como 404 (enumeración mezclada) | **403 explícito** si el usuario objetivo existe pero `school_id` ≠ escuela del admin autenticado. |
| Exposición de datos | `GetUserJson` devolvía `PasswordHash` | **Eliminado** del payload JSON. |
| Compilación | Sensible a bloqueo de `.exe` en `bin\Debug` | `dotnet build -o _verify_build_autonoma` → **OK**. |

**Conclusión:** se cierra un vector explícito de **ataque por ID en URL** en el módulo de usuarios escolares y una fuga de **hash de contraseña** por API. El sistema **no** queda certificado “100 % endpoints con ownership” en un solo ciclo.

---

## 2. Hallazgos detectados

### Críticos

| ID | Hallazgo | Evidencia / impacto |
|----|----------|---------------------|
| SEC-A1 | Admin escolar podía obtener **metadata sensible** vía `GetUserJson` incluyendo `PasswordHash` (aunque fuera hash BCrypt, es secreto de autenticación y permite ataques offline). | Código previo en `UserController.GetUserJson`. |
| SEC-A2 | Política de respuesta ante **ID de usuario de otra escuela**: solo GQF → **404**; no diferenciaba “no existe” de “existe pero prohibido” y no aplicaba **403** explícito como exige el criterio de negocio. | Requisito del usuario: `403` si `resource.school_id != user.school_id`. |

### Importantes

| ID | Hallazgo | Nota |
|----|----------|------|
| OPS-A1 | `dotnet build` estándar puede fallar por **bloqueo** de `SchoolManager.exe` | Usar salida `-o` o detener el proceso en ejecución. |
| COV-A1 | Superficie grande de controladores con `{id}` sin patrón unificado documentado | Pendiente barrido sistemático (gradebook, asistencia, mensajería, etc.). |

---

## 3. Correcciones aplicadas

### Código

| Archivo | Cambio |
|---------|--------|
| `Controllers/UserController.cs` | Inyección de `SchoolDbContext` y método `RequireManagedUserSameSchoolAsync(Guid userId)`: consulta `IgnoreQueryFilters` + comparación `SchoolId` con `GetCurrentSchoolIdAsync()` → **404** si no hay fila; **403** si la fila no pertenece al tenant. |
| `Controllers/UserController.cs` | Gate invocado en: `Details`, `Edit` (GET/POST), `UpdatePhoto`, `RemovePhoto`, `Delete`, `DeleteConfirmed`, `GetById`, `GetUserJson`, `SendPasswordEmail`, `UpdateJson`. |
| `Controllers/UserController.cs` | `GetUserJson`: **removido** `PasswordHash` del objeto anónimo de respuesta. |

**Contexto repo (iteraciones previas, ver `CORRECCION_TOTAL_EDUPLANER.md`):** `StudentController` (ownership estudiante), `StudentReportController` (gate por `studentId`), `AuthService.BuildRoleClaims`, exclusión `_pwtemp` en `.csproj`. Esta ejecución autónoma **añade** el endurecimiento explícito en `UserController` y la retirada de `PasswordHash` en JSON.

**Comportamiento:** si un admin de la escuela A manipula la URL con un `id` válido de la escuela B, la API/vista responde **403 Forbidden** y se registra advertencia en log (sin exponer el `id` en mensaje al cliente más allá del estándar de `Forbid()`).

### Base de datos (DEV)

| Acción | Resultado |
|--------|-------------|
| Consulta duplicados `subject_assignments` | `GROUP BY school_id, subject_id, grade_level_id, group_id, area_id, specialty_id HAVING COUNT(*) > 1` → **0 filas**. |
| Índices `users` + `school_id` | Ya existen: `IX_users_school_id`, `ix_users_school_id_lower_role`, `uq_users_school_document`, `uq_users_school_email_ci`. **No se ejecutó DDL nuevo** en esta pasada. |

**Scripts ejecutados (solo lectura / inventario):**

```sql
SELECT id, name, is_active FROM schools ORDER BY name LIMIT 10;
SELECT role, COUNT(*) FROM users GROUP BY role ORDER BY COUNT(*) DESC;
SELECT email, school_id FROM users WHERE email LIKE '%@test.local' LIMIT 8;
-- + consulta de duplicados subject_assignments (0 filas)
```

---

## 4. Pruebas realizadas

### E2E por rol y escuela (navegador)

| Ámbito | Estado |
|--------|--------|
| Login / clic / formularios en **todos** los módulos listados | **No ejecutado** en esta sesión como automatización Playwright/Selenium ni con MCP browser completo contra `localhost`. |
| Datos reales en DEV | Hay **2 escuelas** y cuentas `@test.local` (admin por escuela, secretaria, estudiante en San Miguelito, etc.) aptas para prueba manual según `PRUEBAS_E2E_COMPLETAS_ROLES_ESCUELAS.md`. |

### Pruebas de ataque (diseño + verificación parcial)

| Ataque | Resultado esperado tras el fix |
|--------|-------------------------------|
| `GET /User/Edit/{id_otra_escuela}` con admin escuela A | **403** si el `id` existe en escuela B. |
| `GET /User/GetUserJson/{id_otra_escuela}` | **403**; ya no incluye hash de contraseña para IDs válidos de la misma escuela. |
| Forzar `SchoolId` en JSON de usuario | `CreateJson` asigna `SchoolId` desde `GetCurrentSchoolIdAsync`, no desde el cliente de forma directa sobre entidad persistida con `SchoolId` libre (sigue siendo recomendable DTO estricto en todos los POST). |

### Compilación

```text
dotnet build -o _verify_build_autonoma
→ Build succeeded (0 errores)
```

---

## 5. Evidencia (UI vs DB / queries)

**Escuelas (PostgreSQL, localhost / `eduplaner`):**

| id | name |
|----|------|
| `cc4e5e11-1be8-42de-8193-428f4484041c` | Instituto Dr. Alfredo Canton |
| `6e42399f-6f17-4585-b92e-fa4fff02cb65` | Instituto Profesional y Técnico San Miguelito |

**Roles (`users.role`, conteos):** `estudiante` (3210), `teacher` (127), `secretaria` (10), `inspector` (8), `admin` (7), `clubparentsadmin` (4), `director` (3), `superadmin` (2).

**Usuarios de prueba:** 21 filas con email `%@test.local`; ejemplos enlazados a cada `school_id` (ver consulta en §3).

---

## 6. Iteraciones realizadas

| Iteración | Qué se hizo | Resultado |
|-----------|-------------|-----------|
| 1 | Conexión PG + inventario escuelas/roles/usuarios + revisión `UserController` | Datos DEV coherentes; identificado SEC-A1/A2. |
| 2 | Implementación `RequireManagedUserSameSchoolAsync` + limpieza JSON | Compilación OK; criterio 403/404 explícito en módulo User. |

**No hubo segunda pasada E2E completa post-fix** (recomendada como siguiente paso).

---

## 7. Problemas pendientes y riesgos

1. **Barrido global de endpoints** con `{id}` (asistencia, carnet para roles no superadmin, mensajería, gradebook, etc.) sin gate explícito documentado.
2. **Mass assignment** en `Edit(User user)` y otros POST que bindean entidades — endurecer con DTOs + mapeo controlado.
3. **E2E automatizado** y regresión visual no integrados en este ciclo.
4. **Secretos** en `appsettings` / historial git (ver `ANALISIS_PRODUCCION_EDUPLANER.md` CRIT-01) — fuera del alcance de este parche.
5. **Estudiante sin fila en `students`:** si aplica el endurecimiento previo en `StudentController`, validar datos o flujo de provisión.

---

## 8. Veredicto final

**NO LISTO** para declarar cumplimiento total de los criterios obligatorios del enunciado (“ningún endpoint sin validar ownership”, “E2E todos los roles y módulos”, “sin overposting en todo el sistema”).

**Justificación técnica:** esta iteración **sí** cumple el criterio estricto de **403 por tenant** en el **controlador de usuarios escolares** y elimina una **fuga de hash** por JSON. Queda trabajo **sistemático** en el resto de la superficie MVC/API y pruebas de navegador repetibles.

**Condición para pasar a “LISTO” (propuesta):**  
(i) Matriz de endpoints × gate de escuela/ownership con prueba de URL manipulada;  
(ii) suite E2E mínima (login × 2 escuelas × 4 roles) en CI;  
(iii) revisión de POST con DTOs;  
(iv) sign-off de seguridad sobre los módulos de calificaciones y asistencia.

---

*Conexión utilizada:* `Host=localhost;Database=eduplaner;Username=postgres;Port=5432` (clave vía `PGPASSWORD` en sesión, no logueada en este archivo).*
