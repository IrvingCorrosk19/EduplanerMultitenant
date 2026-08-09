# Pruebas funcionales completas en browser tab (Chrome / navegador real)

**Fecha:** 4 de mayo de 2026  
**Alcance:** simulación **solo por interfaz web** (clicks, formularios, navegación). Sin Playwright headless como sustituto del informe. Sin crear el flujo principal vía SQL.  
**URL usada en la sesión:** `http://127.0.0.1:5173` (equivalente operativo a `http://localhost:5173`).

---

## Resumen ejecutivo

| Fase del escenario | Estado en esta ejecución |
|--------------------|---------------------------|
| Fase 1 — Login inicial | **OK:** `GET /Auth/Login` carga bien; login **admin Cantón** (`admin.canton@test.local`, institución Cantón, `Test#2026`) deja sesión válida — comprobado con **`/Home/Index`** (“E2E Admin Canton”, enlace **Instituto Dr. Alfredo Canton**). |
| Fase 2 — Crear **Colegio Central** e **Instituto Norte** desde UI | **BLOQUEADA:** requiere **SuperAdmin** → `SuperAdmin/CreateSchoolWithAdmin`. Login `superadmin@schoolmanager.com` / `Admin123!` devolvió **“Error de Autenticación”** en esta instancia (usuario inexistente o contraseña distinta en la BD actual). **No se crearon** las dos escuelas con esos nombres. |
| Fases 3–7 | **No ejecutadas** en esta sesión (dependen de Fase 2 o de datos ya existentes; no se crearon 5+ estudiantes por escuela ni actividades/notas/asistencia completas desde UI en esta corrida). |
| Fase 8–10 | **Parcial:** multi-escuela validada en sesiones previas con **Cantón vs San Miguelito** (no “Central/Norte”). |
| Fase 11 — URL cruzada | **Ejecutada** en esta pestaña: `GET /User/Edit/b0b35595-cc47-4a3e-9233-1c57809daca5` → **`/Auth/AccessDenied`** (página 403 / acceso denegado). |
| Fase 12 — Consola | En visitas a `User/Index` / `Home` (sesiones previas documentadas): CSP fuentes, warnings DataTables, un `console.error` por celdas vacías. Tras corrección en código de CSP, hace falta **reiniciar app** para validar de nuevo. |

---

## 1. Escuelas creadas

| Escuela solicitada en el escenario | ¿Creada en UI en esta prueba? | Notas |
|-----------------------------------|-------------------------------|--------|
| **Colegio Central** | **No** | Requiere SuperAdmin; login SuperAdmin falló. |
| **Instituto Norte** | **No** | Idem. |
| Escuelas **existentes** en el combo de login | **Sí** (solo listado) | “Instituto Dr. Alfredo Canton” e “Instituto Profesional y Técnico San Miguelito” con `SchoolId` distintos en el desplegable. |

---

## 2. Usuarios creados por rol

En esta sesión **no** se crearon desde UI los usuarios del escenario (Profesor 1/2, 5 estudiantes por escuela, etc.) porque **no se inició** el flujo en escuelas nuevas.

**Referencia E2E** ya presente en muchos entornos (script `migration_artifacts/insert_e2e_roles_per_school.sql`): admin, secretaría, profesor y estudiante **por escuela** Cantón / San Miguelito — útil como **proxy** de datos, pero **no sustituye** el escenario “Central/Norte” pedido.

---

## 3. Flujos probados por rol

| Rol | Flujo en browser (esta corrida o consolidado reciente) |
|-----|--------------------------------------------------------|
| SuperAdmin | Intento de login documentado → **fallo**; no acceso a creación de escuelas. |
| Admin | En otras pestañas de la misma campaña: login Cantón, `User/Index`, dashboard. |
| Profesor | `TeacherGradebook/Index` — portal docente visible. |
| Estudiante | `StudentReport/Index` — portal de calificaciones; `StudentIdCard/ui` → **403**. |
| Secretaría | `StudentAssignment/Index` — asignaciones; **`User/Index` → 403** (esperado: solo admin gestiona usuarios). |

---

## 4. Actividades creadas

**No aplicable en esta ejecución** (no se completó Fase 7: no se enviaron formularios de “Agregar” actividad en el libro de calificaciones en esta sesión para no asumir datos sin el escenario de escuela y grupos creados).

---

## 5. Notas registradas

**No ejecutado** en esta sesión (requisito de selección de grupo/materia y estudiantes matriculados en el escenario).

---

## 6. Notas editadas

**No ejecutado.**

---

## 7. Asistencia registrada

**No ejecutado** (misma dependencia que Fase 7).

---

## 8. Validación estudiante

- **Portal:** `StudentReport/Index` carga con pestañas y datos del usuario E2E (ej. grado “Sin asignación” si no hay matrícula completa).
- **Carnet “si aplica”:** con el diseño actual, el estudiante **no** tiene acceso a `/StudentIdCard/ui` → **403** (módulo orientado a SuperAdmin).
- **Aislamiento:** ver §12 (URL de otro estudiante/escuela debe bloquearse; validado en campaña previa con `Student/Details/{id ajeno}`).

---

## 9. Validación secretaria

- **Asignación de estudiantes:** `StudentAssignment/Index` operativo.
- **“Crear estudiantes” desde UI tipo CRM:** `/Student/Create` responde **403 / Forbid** para todos los roles (no es flujo de producto); alta de rol estudiante vía **admin** en `User/Index` + asignación en secretaría.
- **SuperAdmin:** secretaría **no** accede (403 en rutas admin globales).

---

## 10. Validación profesor

- **`TeacherGradebook/Index`:** carga con pestañas (notas, asistencias, etc.) y formulario de nueva actividad.
- **Permisos:** `User/Index` como profesor → **403** (correcto).

---

## 11. Validación multi-escuela

- **No** se repitió el escenario completo en **Colegio Central** e **Instituto Norte** (no existen en BD por esta prueba).
- **Sí** hay evidencia de **dos tenants** en el login y logins separados **Cantón** vs **San Miguelito** en pruebas manuales previas (misma base de código, distinto `SchoolId`).

---

## 12. Pruebas de URL cruzada

| Prueba | Resultado en esta pestaña |
|--------|---------------------------|
| `GET /User/Edit/b0b35595-cc47-4a3e-9233-1c57809daca5` (admin fijo **otra** escuela en fixtures E2E) con sesión **admin Cantón** | **`/Auth/AccessDenied`** — título “Acceso Denegado”, encabezado **403**. |

**Pendiente de re-ejecutar con sesión explícita de Escuela B** tras login completo: copiar URL de estudiante de A e intentar en B (el criterio del enunciado). En campaña anterior se validó `Student/Details/{id ajeno}` → 403.

**Rutas de notas/asistencia:** no se capturaron URLs concretas en esta sesión; recomendación: repetir con IDs reales del libro de notas una vez exista el escenario académico.

---

## 13. Errores encontrados

1. **Bloqueante para el escenario:** no se pudo autenticar **SuperAdmin** con `superadmin@schoolmanager.com` / `Admin123!` → imposible crear **Colegio Central** e **Instituto Norte** desde UI en esta instancia.
2. **Producto / expectativa de negocio:** estudiante **no** ve carnet por la ruta masiva `StudentIdCard/ui` (403).
3. **Producto:** alta de “estudiante” no pasa por `/Student/Create` (ruta cerrada).
4. **Consola (documentado antes):** violaciones CSP de fuentes hasta el despliegue del fix de `Program.cs`; warnings y un error en init de DataTables en `User/Index`.
5. **Cosmético:** pie © 2025 en página 403 vs 2026 en otras vistas.

---

## 14. Correcciones aplicadas, si hubo

(en sesión de trabajo del mismo proyecto, aplicables al repositorio)

| Archivo | Cambio |
|---------|--------|
| `Program.cs` | CSP: `font-src` incluye `https://cdn.jsdelivr.net` para iconos Bootstrap desde jsdelivr. |
| `Views/User/Index.cshtml` | Log de consola aclarado: rol del **formulario de alta**, no “rol de sesión”. |

**Re-prueba:** reiniciar la aplicación y repetir Fase 12 en `Home` y `User/Index` para confirmar ausencia de violaciones `font-src` a jsdelivr.

---

## 15. Evidencia funcional

- Pestaña de navegador **nueva** (`browser_tabs` → nueva tab) y navegación a **`/Auth/Login`**: formulario con correo, contraseña, combo de **dos** instituciones.
- Intento SuperAdmin: snapshot con heading **“Error de Autenticación”** tras enviar credenciales documentadas en `Program.cs` / docs.
- Tras login admin Cantón: **`/Home/Index`** muestra usuario **E2E Admin Canton Administrador** y marca de escuela **Instituto Dr. Alfredo Canton**.
- Navegación directa **`/User/Edit/{guid otra escuela}`** → snapshot en **`/Auth/AccessDenied`** con **403** y mensaje de permisos.

---

## 16. Veredicto final

### ¿LISTO PARA PRODUCCIÓN según el criterio del enunciado?

**No.** El criterio exige, entre otros:

- todo probado en **browser tab real** en el **escenario completo** (2 escuelas nuevas, todos los roles con datos creados desde UI, actividades, notas editadas, asistencia, multi-escuela estricta, sin errores críticos);

En esta ejecución **no** se cumplió: **Fase 2 bloqueada** por autenticación SuperAdmin, y **no** se ejecutaron Fases 3–7 ni la repetición completa en “Central/Norte”.

### Valor positivo comprobado

- Login multi-tenant con **instituciones distintas** en UI.
- **RBAC** (profesor/secretaría sin `User/Index`).
- **Bloqueo por URL** en al menos un caso de edición de usuario de otra escuela (**403 / AccessDenied**).
- Ajustes de **CSP** y claridad de logs para mejorar la siguiente pasada de consola.

### Próximos pasos para poder declarar LISTO (siguiente corrida)

1. Restaurar o confirmar credenciales **SuperAdmin** en la BD de pruebas (`CreateInitialSuperAdminScript` / bootstrap documentado).
2. Crear **Colegio Central** e **Instituto Norte** con `SuperAdmin/CreateSchoolWithAdmin` y verificar en `ListSchools`.
3. Ejecutar Fases 3–10 **solo por UI**, anotando URLs y capturas de consola.
4. Cerrar con **Fase 11** con sesión B pegando URLs de A en estudiantes, notas y asistencia.
5. **Reiniciar** servidor tras cambios CSP y volver a Fase 12.

---

*Entregable: `PRUEBAS_FUNCIONALES_BROWSER_TAB_COMPLETAS.md`.*
