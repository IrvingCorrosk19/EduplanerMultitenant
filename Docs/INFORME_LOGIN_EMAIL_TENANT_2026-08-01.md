# Informe – Login MultiTenant por correo + Deploy VPS

**Fecha:** 2026-08-01  
**Proyecto:** `C:\Proyectos\EduplanerMultitenant\SchoolManager`

---

## Causa raíz

El `GET /Auth/Login` cargaba **todas** las escuelas activas (`_context.Schools…Where(IsActive)`) en `ViewBag.TenantSchools`.

La vista `Login.cshtml` mostraba el selector siempre que `tenantSchools.Count > 0`, es decir, en cuanto existía cualquier colegio en el sistema — **sin importar el correo**.

Eso contradice el flujo enterprise: el tenant debe resolverse por email; el selector solo si el mismo correo existe en **múltiples** colegios activos.

---

## Flujo implementado

| Caso | Comportamiento |
|------|----------------|
| 1 correo → 1 cuenta activa (escuela activa) | Sin selector. Login directo con email+password. |
| 1 correo → N cuentas (N>1 escuelas activas) | AJAX `ResolveLoginSchools` muestra solo esas N escuelas. |
| Correo inexistente / password incorrecta | Mensaje genérico: «Correo o contraseña incorrectos.» |
| Usuario / escuela inactiva | No entra en coincidencias de login; no se ofrece en el selector. |
| SuperAdmin (`SchoolId` null) | Sin selector; login directo. |

---

## Archivos modificados

- `Controllers/AuthController.cs` — sin lista global; endpoint AJAX; repoblación condicional en fallos
- `Views/Auth/Login.cshtml` — selector oculto + JS blur/input/debounce
- `Services/Implementations/AuthService.cs` — mensaje genérico; multi vía `GetLoginSchoolsByEmailAsync`
- `Services/Implementations/UserService.cs` — `GetByEmailForLoginAsync` (activos + escuela activa); `GetLoginSchoolsByEmailAsync`
- `Services/Interfaces/IUserService.cs`
- `Scripts/EnsureLoginEmailIndex.cs` + `.sql` — índice `ix_users_lower_email`
- `Program.cs` — aplica EnsureLoginEmailIndex al arranque
- `e2e/tests/login.spec.ts` — smoke del nuevo flujo
- `Docs/INFORME_LOGIN_EMAIL_TENANT_2026-08-01.md` (este archivo)

---

## Consultas / seguridad

- **No** se listan todos los colegios en el GET.
- `GetLoginSchoolsByEmailAsync`: 1 query join Users↔Schools, `AsNoTracking`, solo activos.
- AJAX anti-enumeración: si 0 o 1 coincidencia → `{ requiresSelection: false, schools: [] }` (misma forma).
- Rate limit `LoginPolicy` en AJAX y POST.
- Anti-CSRF en POST Login.
- Claims `school_id` intactos; GQF / RBAC no modificados.
- Índice ya existente multi-tenant: `uq_users_school_email_ci (school_id, lower(email))`.
- Nuevo: `ix_users_lower_email` para lookup por correo.

---

## Pruebas

- Compilación Release: ver sección deploy.
- Playwright e2e actualizado (`login.spec.ts`).
- Smoke producción: selector oculto al cargar; HTTP 200 Login; ResolveLoginSchools para correo único sin escuelas.

---

## Deploy VPS

- URL: http://164.68.99.83:8087/Auth/Login  
- Compilación Release: **0 errores**  
- Contenedor `eduplaner_web` reiniciado; HTTP Login **200**

### Evidencias smoke producción

| Prueba | Resultado |
|--------|-----------|
| Selector `#schoolSelectorGroup` oculto al cargar | **Sí** (`display:none`) |
| Texto antiguo “Mismo correo en varios colegios…” | **Eliminado** |
| `ResolveLoginSchools?email=admin.central.qa@test.local` | `requiresSelection=false`, `schools=[]` |
| `ResolveLoginSchools?email=noexiste@fake.local` | Misma respuesta (anti-enumeración) |
| `ResolveLoginSchools?email=director.multi@test.local` (2 escuelas) | `requiresSelection=true`, escuelas: Colegio Central, Instituto Norte |

Cuenta de prueba multi-institución creada en VPS: `director.multi@test.local` (Central + Norte).

---

## Confirmaciones

- Aislamiento MultiTenant: **Sí** (selector solo escuelas del correo; login filtra SchoolId).
- Selector solo con multi-cuenta: **Sí** (evidencia smoke).
- Despliegue exitoso al VPS: **Sí**.
