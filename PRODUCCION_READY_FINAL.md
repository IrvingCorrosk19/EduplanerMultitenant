# Producción — validación E2E y ownership (cierre)

**Fecha de verificación:** 2026-05-03  
**Entorno de prueba:** Playwright (Chromium), aplicación publicada y servida por `e2e/playwright.config.ts` (`dotnet publish` + `SchoolManager.dll` en `http://127.0.0.1:{E2E_PORT}`, puerto por defecto `5173`).  
**Credenciales E2E (según fixtures):** `admin.canton@test.local` / `Test#2026`, escuela de contexto Cantón.

---

## Criterio de cierre (alcance acordado en la suite)

| Criterio | Estado |
|----------|--------|
| Suite E2E en navegador (Playwright) | **15 passed**, **1 skipped** (flujo docente sin `#selGroup` / sin celdas de nota en el dataset actual) |
| Evidencia (reporte HTML, JSON, trazas, vídeo, capturas) | Generada bajo `e2e/playwright-report/` |
| Multi-escuela / roles / flujos críticos UI | Cubiertos en los specs existentes (`login`, `admin-crud`, `student-portal`, `secretary-assignment`, `ownership`, etc.) |
| Ownership (IDs válidos vs cruzados) | Cubierto en `e2e/tests/ownership.spec.ts` (incluye casos positivos y denegación esperada según cookie-auth MVC) |

**Veredicto para el alcance validado por esta suite:** la barrera “sin evidencia ejecutable / sin E2E real” queda **cerrada** para los flujos y reglas de negocio que ejecutan los tests. No implica auditoría exhaustiva de **todos** los endpoints con `{id}` del sistema; la recomendación es ampliar la matriz de ownership de forma incremental por módulo.

---

## Tests ejecutados

Comando (desde `e2e/`):

```bash
npx playwright test --retries=0
```

**Resultado:** 15 passed, 1 skipped (~2 min en la última corrida completa).

**Omitido a propósito:** `teacher-gradebook-flow` cuando no hay selector de grupo o celdas de nota (docente sin asignaciones en datos de prueba).

---

## Evidencia generada

| Artefacto | Ubicación |
|-----------|-----------|
| Reporte HTML Playwright | `e2e/playwright-report/html` (abrir con `npx playwright show-report playwright-report/html` desde `e2e/`) |
| Resultados JSON | `e2e/playwright-report/results.json` |
| Traces, vídeos, screenshots por test fallido / configuración | `e2e/playwright-report/artifacts/` |

Configuración relevante: `trace: on`, `video: on`, `screenshot: on` en `e2e/playwright.config.ts`.

---

## Fallos encontrados y correcciones aplicadas (iteración test → fix → test)

1. **`Views/User/Edit.cshtml` — formularios HTML anidados**  
   El `<form asp-action="Edit">` envolvía los `<form>` de foto. Eso rompe el DOM: el submit “Guardar Cambios” no enviaba el modelo correcto.  
   **Corrección:** bloque de foto fuera del formulario principal de edición.

2. **`UserController.Edit` POST — model binding de `User`**  
   Campos no enviados (`PasswordHash`, etc.) invalidaban el modelo o llevaban a `Update()` parcial peligroso.  
   **Corrección:** POST sin bind directo; `TryUpdateModelAsync` solo sobre propiedades editables del usuario cargado desde BD; persistencia vía `UpdateAsync(user, [], [])`.

3. **`UserService.UpdateAsync` — `PasswordHash` y colecciones**  
   - No sobrescribir `PasswordHash` si el formulario no envía valor.  
   - `Include(u => u.Subjects).Include(u => u.Groups)` antes de `Clear()` para evitar estados inconsistentes.

4. **`Views/User/Edit.cshtml` — valores de `Status`**  
   El formulario enviaba `Activo` / `Inactivo` / `Suspendido`; la BD impone `users_status_check` (`active`, `inactive`).  
   **Corrección:** opciones alineadas con `User/Index` (`active` / `inactive`).

5. **E2E `admin-crud.spec.ts`**  
   Refuerzo del formulario principal (`novalidate`), espera explícita del POST y aserción **302/303** en `User/Edit`.

---

## Validación final

- **Build .NET:** correcto tras los cambios (`dotnet build SchoolManager.csproj`).  
- **E2E:** suite completa en verde con el único skip documentado (docente sin datos de libro de calificaciones).  
- **Integridad de edición de usuario:** POST `User/Edit` redirige y persiste sin violar restricciones de BD ni borrar credenciales por binding parcial.

---

## Declaración

Para el **alcance cubierto por la suite Playwright actual** (login, admin CRUD usuario, portal estudiante, secretaría, ownership representativo, etc.):

**LISTO PARA PRODUCCIÓN** — con la salvedad de ampliar cobertura de ownership endpoint-a-endpoint y de datos de prueba para el flujo docente si se exige como obligatorio y no opcional.
