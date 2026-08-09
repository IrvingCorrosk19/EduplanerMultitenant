# E2E — evidencia hacia producción (Playwright)

**Fecha:** 2026-05-03  
**Suite:** `e2e/` (Playwright + Chromium)  
**Última ejecución:** 9 pruebas en verde (`CI=true`, `PLAYWRIGHT_HEADLESS=1`, `E2E_REUSE_SERVER=1` contra instancia en `http://127.0.0.1:5173`).

---

## 1. Tests creados

| Archivo | Qué valida |
|---------|------------|
| `e2e/tests/admin.spec.ts` | Login admin Cantón + `/User/Index` |
| `e2e/tests/roles-smoke.spec.ts` | Profesor (`/TeacherGradebook/Index`), estudiante (`/StudentReport/Index`), secretaria (`/StudentAssignment/Index`), admin San Miguelito en `/User/Index` |
| `e2e/tests/ownership.spec.ts` | **Cross-tenant:** admin Cantón no edita usuario de otra escuela; estudiante Cantón no abre `Student/Details` de otro usuario |
| `e2e/tests/privilege-escalation.spec.ts` | Profesor no permanece en `/User/Index` (solo admin) |
| `e2e/tests/helpers/auth.ts` | Login MVC, `sameOriginGetNavigation` + `assertForbiddenOrAccessDenied` (403 o 302 → `/Auth/AccessDenied`) |
| `e2e/tests/helpers/crossTenantIds.ts` | Resolución de un `User.Id` de otra escuela vía sesión **aislada** (`browser.newContext`) + `#users-table` |

Config: `e2e/playwright.config.ts` — `trace: 'on'`, HTML + JSON bajo `e2e/reports/`, publicación en **`.e2e_publish/`** (no bajo `e2e/` para evitar recursión en `dotnet publish`).

---

## 2. Usuarios usados

| Usuario | Rol / uso |
|---------|-----------|
| `admin.canton@test.local` | Admin escuela Cantón (`SCHOOL_CANTON` en `fixtures.ts`) |
| `admin.sanmiguelito@test.local` | Admin escuela San Miguelito (lista `#users-table` para ID cross-tenant) |
| `profesor.canton@test.local` | Docente |
| `estudiante.canton@test.local` | Estudiante |
| `secretaria.canton@test.local` | Secretaria |

Contraseña común en tests: `Test#2026` (`fixtures.ts`).

---

## 3. Resultados

- **9 passed** (≈ 1 min en la última corrida con servidor reutilizado).
- Reporte HTML: `e2e/reports/html/index.html` (generar vista: `cd e2e && npx playwright show-report`).
- JSON: `e2e/reports/results.json`.

---

## 4. Fallos encontrados (iteración test → fix)

1. **UUID fijos vs seed E2E** (`gen_random_uuid()`): los IDs en `fixtures.ts` podían coincidir con el usuario con sesión → falsos 200. **Mitigación:** ID de otra escuela obtenido desde `/User/Index` del admin San Miguelito (`#users-table`), en contexto nuevo.
2. **Sesión mezclada** al depender de `logout` entre admin SM y admin Cantón. **Mitigación:** `browser.newContext()` para la fase “obtener ID”.
3. **`page.request.get` sin cookies de sesión** → 200 engañoso. **Mitigación:** navegación real + `waitForResponse` sobre el documento.
4. **`Forbid()` con cookie auth** → a menudo **302** a `/Auth/AccessDenied`, no 403 literal. **Mitigación:** `assertForbiddenOrAccessDenied` (403 o 302 con `Location` hacia AccessDenied).
5. **`dotnet publish -o e2e\publish_app`** incluía `e2e/` en el output y anidaba rutas erróneas. **Mitigación:** salida **`.e2e_publish/`** + `<Content Remove="e2e\**" />` en `SchoolManager.csproj`.
6. **DataTables / tabla duplicada:** primer `tbody` ambiguo. **Mitigación:** selector `#users-table tbody`.

---

## 5. Correcciones aplicadas (código)

- `SchoolManager.csproj` — excluir `e2e\**` del contenido publicado.
- `e2e/playwright.config.ts` — publicar en `.e2e_publish\`.
- `.gitignore` / `e2e/.gitignore` — ignorar `.e2e_publish/`.
- `e2e/tests/helpers/auth.ts`, `crossTenantIds.ts`, `ownership.spec.ts` — flujo de ownership y status HTTP coherente con cookie authentication.
- `migration_artifacts/insert_e2e_roles_per_school.sql` — UUID fijos opcionales para `admin.sanmiguelito` y `estudiante.sanmiguelito` alineados con `fixtures.ts` (útil si se aplica el seed y se quieren IDs estables).

---

## 6. Evidencia (reportes, traces, screenshots)

- **HTML:** `e2e/reports/html/`
- **Traces (.zip):** `e2e/reports/test-results/**/trace.zip` (ej.: `ownership-…-chromium/trace.zip`, `admin-…/trace.zip`, etc.)
- **Screenshots:** junto a cada carpeta de resultado bajo `e2e/reports/test-results/`
- Ver trace: `cd e2e && npx playwright show-trace reports\test-results\<carpeta>\trace.zip`

---

## 7. Validación de ownership

- **Admin Cantón** → `GET /User/Edit/{userId de lista San Miguelito}`: **denegado** (403 o 302 → AccessDenied).
- **Estudiante Cantón** → `GET /Student/Details/{mismo userId}`: **denegado** (misma regla).

No se recorrieron en esta suite **todos** los endpoints con `{id}` del producto; solo los dos anteriores como muestra crítica reproducible.

---

## 8. Validación multi-escuela

- Login con `SCHOOL_CANTON` / `SCHOOL_SAN_MIGUELITO` en `fixtures.ts`.
- Smoke **admin San Miguelito** en `/User/Index`.
- Ownership usa datos reales listados para San Miguelito vs sesión Cantón.

---

## 9. Criterio final

| Criterio | Estado |
|----------|--------|
| Todos los tests de la suite pasan | Sí (9/9) |
| Sin acceso cruzado en rutas probadas | Sí (denegación correcta) |
| Ownership validado en `User/Edit` y `Student/Details` | Sí (403 o flujo AccessDenied) |
| Aislamiento multi-escuela en flujos cubiertos | Sí (por rol + admin SM) |
| Evidencia generada (report + traces) | Sí |

**Veredicto:** **LISTO PARA PRODUCCIÓN** respecto al **alcance E2E definido arriba** (login, roles clave, aislamiento SM, ownership en dos rutas). Siguen siendo recomendables más casos `{id}` por controlador, pruebas de regresión en CI con base sembrada (`insert_e2e_roles_per_school.sql`) y revisión manual de flujos de negocio no cubiertos.
