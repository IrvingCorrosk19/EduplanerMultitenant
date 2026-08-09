# Certificación final — Paridad IIC → MultiTenant

**Fecha:** 2026-08-01  
**Resultado:** Paridad funcional de gaps inventariados **cerrada** (M1–M5, M7). M6 ignorado a propósito.

## Compilación

```
Build succeeded.
0 Warning(s)
0 Error(s)
```

Migración aplicada en BD local:
`20260801144145_AddInstitutionalStaffCredentialTables`

---

## Módulos certificados

| Módulo | Estado | Evidencia |
|---|---|---|
| **M1 Reportes institucionales** | Completo | Controllers/servicios/vistas/CSS/NPOI/menú; ver `CERTIFICACION_M1_REPORTES_INSTITUCIONALES.md` |
| **M2 Credencial institucional** | Completo | Entidades + SchoolId + GQF + PDF/QR/perfil público + DI + menú SuperAdmin |
| **M3 Staff Directory** | Completo | `StaffDirectory` + foto/perfil; menú Personal |
| **M4 PDF Gradebook** | Completo | `ExportRegistroPdf` + servicio + botón UI; aislamiento SchoolId |
| **M5 Aprobados UX** | Parcial útil | CSS `aprobados-reprobados-vista-previa.css`; partial IIC descartado (incompatible con ViewModel MT — no degradar) |
| **M6 GradebookDuplicate** | Ignorado | Fork experimental; MT usa `TeacherGradebook` certificado |
| **M7 Download foto StudentDirectory** | Completo | `StudentDirectoryDownloadPhoto` |

---

## Adaptaciones MultiTenant aplicadas (resumen)

1. Reportes: `ValidarAlcanceAsync` + filtros SchoolId en bulk/asistencia.
2. Credencial/perfil/QR: columna `school_id` NOT NULL + GQF + set al generar.
3. Endpoints públicos QR: `IgnoreQueryFilters` + validación token/escuela.
4. SuperAdmin Staff/Student: `IgnoreQueryFilters` solo en contexto SuperAdmin.
5. Gradebook PDF: valida `TeacherAssignments.SchoolId` y pertenencia de materia/grupo/grado.

---

## Archivos clave nuevos/modificados (M2–M7)

### Nuevos
- Controllers: `InstitutionalCredential`, `StaffInstitutionalProfile`
- Models: `InstitutionalCredentialCard`, `StaffInstitutionalProfile`, `StaffQrToken` (+ SchoolId)
- Services Institutional* / StaffInstitutional* / TeacherGradebookPdf*
- Helpers institucionales
- Views InstitutionalCredential/*, StaffInstitutionalProfile/*, SuperAdmin/StaffDirectory
- Migration `AddInstitutionalStaffCredentialTables`
- CSS `superadmin-staff-pages.css`, `aprobados-reprobados-vista-previa.css`

### Modificados
- `SchoolDbContext` + `SchoolDbContextTenantFilters`
- `Program.cs`, `appsettings.json`
- `SuperAdminController` / `SuperAdminService` / `ISuperAdminService`
- `TeacherGradebookController` + vista Index
- `_SuperAdminLayout`, `_AdminLayout`
- `MenuService` (Reportes — M1)

---

## Descartado / no migrado

| Ítem | Motivo |
|---|---|
| `TeacherGradebookDuplicateController` | Duplicado experimental; MT gradebook es la fuente oficial |
| `_TablaReporte.cshtml` de IIC | Requiere propiedades inexistentes en ViewModel MT (`MostrarColumnaMateria`) |

---

## Riesgos residuales

| Riesgo | Nivel | Nota |
|---|---|---|
| Puppeteer/Chromium en PDF informes y credenciales | Medio | Requiere Chrome en deploy |
| Unicidad email/documento global en perfil staff | Bajo | Misma semántica IIC |
| Pruebas E2E Playwright de módulos nuevos | Medio | Specs pendientes de runtime |
| AcademicYear explícito en reportes | Medio | Mejora futura |

---

## Criterios de aceptación (estado)

- [x] Gaps inventariados migrados o descartados con justificación
- [x] Compila sin errores ni warnings críticos
- [x] Aislamiento SchoolId en módulos nuevos
- [x] Menús actualizados
- [x] Migración EF creada y aplicada (local)
- [ ] Smoke/E2E runtime completo (requiere sesión con datos y Chromium)

---

## Próximos pasos operativos recomendados (fuera de código)

1. Desplegar y aplicar migración en staging/producción.
2. Configurar `InstitutionalCredential:PublicBaseUrl` y `QrSecurity:SecretKey`.
3. Ejecutar smoke: Reportes, Credencial, Staff Directory, ExportRegistroPdf.
4. Ampliar Playwright con specs de los módulos nuevos.
