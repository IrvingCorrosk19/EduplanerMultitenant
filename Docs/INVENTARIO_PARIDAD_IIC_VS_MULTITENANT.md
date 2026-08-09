# Inventario de paridad — Eduplaner IIC → MultiTenant

**Fecha:** 2026-08-01  
**Origen:** `C:\Proyectos\EduplanerIIC\SchoolManager` (monotenant)  
**Destino:** `C:\Proyectos\EduplanerMultitenant\SchoolManager` (MultiTenant — prioridad arquitectónica)  
**Estado:** Fases 1–3 completadas (inventario + matriz + compatibilidad). Migración pendiente por módulos.

---

## Resumen ejecutivo

| Métrica | IIC | MultiTenant | Delta |
|---|---:|---:|---:|
| Controllers | 55 | 50 | **-5** (faltantes en MT) |
| Services (Implementations) | 81 | 71 | **-10** |
| Service Interfaces | +9 IIC-only | — | **-9** |
| Models | 58 | 57 | **-3 entidades** (+2 infra MT) |
| ViewModels | 49 | 42 | **-7** |
| DTOs | 64 | 60 | **-4** |
| Carpetas Views | 51 | 46 | **-7 / +2** |
| Vistas `.cshtml` | 169 | 153 | **-16** |
| Helpers | 12 | 9 | **-3 / +1** |
| CSS propios | 9 | 1 | **-8** |
| Paquete NPOI | Sí | **No** | Requerido para informes |
| SignalR | 0 | 0 | Paridad |
| Areas MVC | 0 | 0 | Paridad |
| e2e Playwright | — | Sí (9 specs) | Ventaja MT |
| Data Protection EF | No | Sí | Ventaja MT |
| Tenant GQF | No | Sí | Ventaja MT |

**Conclusión (actualizada 2026-08-01):** Gaps P0/P1 inventariados **migrados y compilando**. Ver `CERTIFICACION_FINAL_PARIDAD_IIC.md`.

Los gaps reales se concentraban en **3 bloques** (todos cerrados):

1. **Reportes institucionales** — M1 ✅
2. **Credencial institucional del personal + Staff Directory** — M2+M3 ✅
3. **PDF del registro docente** — M4 ✅

Adicionales: M5 (CSS), M7 (download foto) ✅. M6 ignorado.

---

## Patrón MultiTenant actual (obligatorio al migrar)

- Discriminador: `SchoolId` (claim `school_id`)
- `ITenantProvider` / `TenantProvider` (scoped)
- Global Query Filters en `SchoolDbContext` / `SchoolDbContextTenantFilters`
- Fail-closed: sin tenant y no SuperAdmin → vacío
- SuperAdmin puede operar cross-school con `IgnoreQueryFilters` + filtros manuales
- **No** hay `TenantId` separado: el “tenant” es la escuela (`SchoolId`)
- Banner: `_TenantContextBanner.cshtml`
- Ventajas MT a preservar: Data Protection en PostgreSQL, `FileUploadValidator`, cookies 4h, e2e, CSP/headers

**Regla de migración:** toda entidad nueva debe tener `SchoolId` (o aislamiento vía navegación a `User.SchoolId` + GQF/filtro explícito). Nunca copiar consultas sin filtro.

---

## Matriz de diferencias — Módulos faltantes / parciales

### M1 — Reportes Institucionales (catálogo + 4 informes)

| Campo | Valor |
|---|---|
| Existe en IIC | Sí |
| Existe en MultiTenant | **Sí (migrado 2026-08-01)** |
| Estado | **Migrado** — ver `CERTIFICACION_M1_REPORTES_INSTITUCIONALES.md` |
| Acción | **Debe migrarse + adaptarse** |
| Riesgo | **Alto** (PDF/Excel, plantillas NPOI, Puppeteer, roles) |
| Impacto | Alto — funcionalidad de dirección/consejería/docentes |
| Prioridad | **P0** |
| Tiempo estimado | 5–8 días |
| Dependencias | NPOI, plantillas Excel, CSS reportes, SchoolId, AcademicYear, grupos/grados, asistencia, calificaciones |
| Archivos IIC origen | `ReportesController`, `InformesInstitucionalesControllers` (Habitos/Calificaciones/FormatoCarpetas), `ReportesInstitucionalesService`, `InformeInstitucionalHtmlPdfService`, `InformeInstitucionalRazorRenderService`, `ReportePlantillaNpoiHelper`, Views `Reportes/*`, `HabitosActitudesReport/*`, `CalificacionesInforme/*`, `FormatoCarpetasReport/*`, CSS `reporte-*.css` |
| Resultado esperado | Catálogo `/Reportes` + 4 reportes con vista previa, PDF y Excel, filtrados por SchoolId/año, menú para admin/director/teacher |

### M2 — Credencial institucional del personal

| Campo | Valor |
|---|---|
| Existe en IIC | Sí |
| Existe en MultiTenant | **No** |
| Estado | **No existe** |
| Acción | **Debe migrarse + adaptarse** |
| Riesgo | **Alto** (QR público, IDOR, fuga entre escuelas) |
| Impacto | Alto — SuperAdmin / personal |
| Prioridad | **P0** |
| Tiempo estimado | 4–6 días |
| Dependencias | Entidades nuevas + migración EF, Cloudinary, Puppeteer/PDF, QrSignature, menú SuperAdmin |
| Archivos IIC | Controllers `InstitutionalCredential`, `StaffInstitutionalProfile`; Services `InstitutionalCredential*`, `StaffInstitutionalProfile*`; Models `InstitutionalCredentialCard`, `StaffInstitutionalProfile`, `StaffQrToken`; Helpers institucionales; Options `InstitutionalCredentialOptions`; Views + CSS `superadmin-staff-pages.css` |
| Adaptación MT | Añadir `SchoolId` a cards/tokens o filtrar siempre por `User.SchoolId`; GQF; endpoints públicos con firma; no filtrar por tenant en perfil público pero validar token |
| Resultado esperado | Generación/listado/impresión de credencial, perfil laboral, QR público firmado, aislamiento por escuela |

### M3 — SuperAdmin Staff Directory

| Campo | Valor |
|---|---|
| Existe en IIC | Sí (`StaffDirectory` + foto/perfil) |
| Existe en MultiTenant | **No** |
| Estado | **No existe** |
| Acción | **Debe migrarse** (junto a M2) |
| Riesgo | Medio-Alto |
| Impacto | Medio — operación SuperAdmin |
| Prioridad | **P0** (bloque M2) |
| Tiempo estimado | 1–2 días (si M2 listo) |
| Archivos | `SuperAdminController` acciones Staff*, View `StaffDirectory.cshtml`, ViewModels `SuperAdminStaffDirectory*` |
| Resultado esperado | Directorio de personal multi-escuela con edición de perfil/foto |

### M4 — PDF Registro Docente (Gradebook)

| Campo | Valor |
|---|---|
| Existe en IIC | Sí (`ExportRegistroPdf` + `TeacherGradebookPdfService`) |
| Existe en MultiTenant | **Parcial** (gradebook CRUD existe; PDF registro no) |
| Estado | **Parcial** |
| Acción | **Debe migrarse + adaptarse** |
| Riesgo | Medio |
| Impacto | Medio — docentes |
| Prioridad | **P1** |
| Tiempo estimado | 2–3 días |
| Dependencias | `ITeacherGradebookPdfService`, DTO `GradebookPdfDto` |
| Resultado esperado | Botón/acción ExportRegistroPdf en gradebook MT con filtro SchoolId |

### M5 — Aprobados/Reprobados (filtros UI)

| Campo | Valor |
|---|---|
| Existe en IIC | Sí |
| Existe en MultiTenant | Sí (implementación **diferente/más moderna** en endpoints de filtro) |
| Estado | **Parcial / divergente** |
| Acción | **Adaptar solo gaps útiles**; **NO reemplazar** implementación MT |
| Riesgo | Medio (regresión) |
| Impacto | Bajo-Medio |
| Prioridad | **P2** |
| Tiempo estimado | 0.5–1 día |
| Nota | IIC: `ObtenerNivelesFiltro/Materias/Grupos/AsignacionesCombo`. MT: `ObtenerEspecialidades/Areas/Materias`. Evaluar si falta `_TablaReporte.cshtml` y CSS `aprobados-reprobados-vista-previa.css` |
| Resultado esperado | Paridad UX sin degradar API MT |

### M6 — TeacherGradebookDuplicate

| Campo | Valor |
|---|---|
| Existe en IIC | Sí |
| Existe en MultiTenant | No (usa `TeacherGradebook`) |
| Estado | **Debe ignorarse** (salvo evidencia de features únicas no presentes en gradebook MT) |
| Acción | **Ignorar** por defecto |
| Riesgo | Alto si se duplica código |
| Prioridad | — |
| Tiempo | 0 (auditoría 2–4 h si se requiere certificar) |

### M7 — Descarga foto StudentDirectory SuperAdmin

| Campo | Valor |
|---|---|
| Existe en IIC | `StudentDirectoryDownloadPhoto` |
| Existe en MultiTenant | No detectado |
| Estado | **Parcial** |
| Acción | Migrar si se usa operativamente |
| Prioridad | **P2** |
| Tiempo | 0.5 día |

---

## Matriz — Módulos compartidos (paridad OK)

Estado **Completo** (misma familia funcional; MT prioridad). No migrar a ciegas; solo auditar regresiones puntuales:

| Módulo | Controllers | Notas |
|---|---|---|
| Auth / ChangePassword | Auth, ChangePassword | MT: Data Protection, cookie 4h |
| Usuarios | User, Admin/UserPasswordManagement | MT: FileUploadValidator |
| Estudiantes | Student, StudentProfile, StudentAssignment, StudentSchedule, StudentOrientation, StudentReport | |
| Docentes | TeacherAssignment, TeacherGradebook*, TeacherWorkPlan | *PDF pendiente M4 |
| Director | Director, DirectorWorkPlans | |
| Catálogo académico | AcademicCatalog, Area, GradeLevel, Group, Specialty, Subject, SubjectAssignment, AcademicAssignment, TimeSlot, Schedule* | |
| Asistencia | Attendance | |
| Actividades | Activity | |
| Consejería | CounselorAssignment | DTO `CounselorSubjectAverageDto` solo IIC — verificar uso |
| Disciplina / Orientación | DisciplineReport, OrientationReport | |
| Pagos | Payment, PaymentConcept, ClubParents | |
| Prematrícula | Prematriculation, PrematriculationPeriod | |
| Carnet estudiantil | StudentIdCard, IdCardSettings, QlServicesCarnet | |
| Mensajería / Email | Messaging, EmailConfiguration, Admin/EmailJobs | |
| Auditoría / Seguridad | AuditLog, SecuritySetting | |
| SuperAdmin base | SuperAdmin (sin StaffDirectory) | |
| Escuelas | School | |
| Archivos / Home | File, Home | |
| API Docs | Api/Documents | |

---

## Matriz — Capas técnicas

| Capa | IIC only | MT only | Acción |
|---|---|---|---|
| DbSets | StaffInstitutionalProfiles, InstitutionalCredentialCards, StaffQrTokens | — | Migrar + GQF |
| Infra | — | TenantProvider, SchoolDbContextTenantFilters, DataProtectionKeyDbContext | Preservar |
| Packages | NPOI 2.7.5 | DataProtection.EFCore | Añadir NPOI al migrar M1 |
| CSS reportes | 7 archivos reporte + staff | — | Migrar con tema MT |
| Options | InstitutionalCredentialOptions | — | Migrar |
| Helpers | InstitutionalCardNumber*, Staff* | FileUploadValidator | Migrar staff helpers |
| Menú SuperAdmin | StaffDirectory, InstitutionalCredential, StaffInstitutionalProfile | — | Agregar |
| Menú operativo | Reportes **no** está en MenuService IIC ni MT | — | **Agregar** `/Reportes` a MenuService MT (admin/director/teacher) |
| Localization | Ninguno formal | Ninguno | Sin acción |
| SignalR | No | No | N/A |
| Background | EmailQueueWorker | EmailQueueWorker | Paridad |

---

## Análisis de compatibilidad (Fase 3)

| Aspecto | Compatibilidad | Decisión |
|---|---|---|
| .NET 8 | Ambos | OK |
| EF Core 9 / Npgsql 9 | Ambos | OK |
| AutoMapper 16 | Ambos | OK |
| EPPlus / QuestPDF / Puppeteer / QRCoder / Skia / Cloudinary | Ambos | OK |
| NPOI | Solo IIC | **Agregar** al migrar informes |
| Identity | Propio (cookies + BCrypt) en ambos | No introducir ASP.NET Identity |
| Authorization Roles | Aliases bilingües en ambos | Mantener patrón MT |
| DI | Scoped services | Registrar nuevos en `Program.cs` MT |
| Namespaces | `SchoolManager.*` | Compatible |
| Bootstrap / AdminLTE / jQuery / SweetAlert | Ambos | Usar layout/tema MT |
| Tenant resolution | Solo MT | **Adaptar todo lo migrado** |
| GQF replacement risk | Documentado en MT | No romper filtros al añadir entidades |
| Implementaciones más modernas en MT | Aprobados filtros, DataProtection, e2e, FileUploadValidator | **No reemplazar** |

---

## Riesgos transversales

1. Entidades institucionales IIC **sin `SchoolId`** → aislamiento solo vía `User`; en MT hay que reforzar.
2. Endpoints públicos (`InstitutionalCredential/member`) → riesgo IDOR/fuga; reutilizar `QrSignatureService`.
3. `IgnoreQueryFilters` en SuperAdmin/StaffDirectory → filtros manuales obligatorios.
4. Plantillas Excel NPOI en carpeta `Reportes` (IIC puede estar vacía en disco; verificar embebidos/rutas).
5. No romper e2e existentes ni gradebook certificado.
6. No degradar cookie/DataProtection/CSP de MT.

---

## Plan de ejecución modular (orden obligatorio)

| # | Módulo | Criterio de done |
|---|---|---|
| 1 | **M1 Reportes Institucionales** | Compila, menú, PDF/Excel, aislamiento SchoolId, smoke + Playwright |
| 2 | **M2+M3 Credencial + Staff Directory** | Migración EF, GQF, menú SuperAdmin, QR público seguro, e2e |
| 3 | **M4 PDF Gradebook** | ExportRegistroPdf, sin regresión gradebook |
| 4 | **M5 Aprobados UX gaps** | Solo gaps; no reemplazar MT |
| 5 | **M7 foto StudentDirectory** | Si se confirma necesidad |
| — | **M6 Duplicate** | Ignorado salvo auditoría |

**Regla:** no iniciar el siguiente módulo hasta compilar, probar y certificar el actual.

---

## Checklist de certificación por módulo

- [ ] Estado inicial documentado
- [ ] Diferencias y archivos analizados
- [ ] Archivos modificados listados
- [ ] Adaptaciones MultiTenant (SchoolId / GQF / claims)
- [ ] Riesgos y mitigaciones
- [ ] Compilación sin errores
- [ ] Pruebas (unit/smoke/e2e aplicables)
- [ ] Evidencia funcional
- [ ] Confirmación aislamiento Tenant/School
- [ ] Menú/roles/permisos

---

## Próximo paso recomendado

Iniciar **Módulo 1 — Reportes Institucionales** en MultiTenant, adaptando desde IIC sin sobrescribir arquitectura MT.
