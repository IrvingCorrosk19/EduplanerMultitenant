# Certificación Módulo 1 — Reportes Institucionales

**Fecha:** 2026-08-01  
**Proyecto:** Eduplaner MultiTenant  
**Estado:** COMPILADO Y MIGRADADO (pendiente smoke E2E en runtime)

---

## Estado inicial

El módulo no existía en MultiTenant. Solo Aprobados/Reprobados estaba disponible bajo el menú Reportes.

## Diferencias encontradas (IIC → MT)

| Elemento | Acción |
|---|---|
| Catálogo `/Reportes` | Migrado |
| Hábitos y Actitudes | Migrado |
| Calificaciones Expresiones Artísticas / Tecnología | Migrado |
| Formato para Carpetas | Migrado |
| Servicios PDF (Puppeteer HTML) + Excel (EPPlus/NPOI) | Migrado |
| Plantillas `.xls` | Copiadas a `Reportes/` |
| Filtros AJAX `ObtenerNiveles/Materias/GruposFiltro` | Añadidos a MT sin reemplazar API existente |
| Asistencia por `TrimesterId` | Adaptada a rangos de fecha (modelo MT sin TrimesterId) |

## Archivos analizados (origen IIC)

- Controllers: `ReportesController`, `InformesInstitucionalesControllers`
- Services: `ReportesInstitucionalesService`, `InformeInstitucionalHtmlPdfService`, `InformeInstitucionalRazorRenderService`, `ReportePlantillaNpoiHelper`
- Helpers: `ReportesInstitucionalesBulkLoader`, `GradebookFinalGradeCalculator`
- Views/CSS/plantillas Excel

## Archivos modificados / creados en MultiTenant

### Nuevos
- `Controllers/ReportesController.cs`
- `Controllers/InformesInstitucionalesControllers.cs`
- `Services/Interfaces/IReportesInstitucionalesService.cs`
- `Services/Interfaces/IInformeInstitucionalHtmlPdfService.cs`
- `Services/Interfaces/IInformeInstitucionalRazorRenderService.cs`
- `Services/Implementations/ReportesInstitucionalesService.cs`
- `Services/Implementations/InformeInstitucionalHtmlPdfService.cs`
- `Services/Implementations/InformeInstitucionalRazorRenderService.cs`
- `Services/Implementations/ReportePlantillaNpoiHelper.cs`
- `Services/Helpers/ReportesInstitucionalesBulkLoader.cs`
- `Services/Helpers/GradebookFinalGradeCalculator.cs`
- `ViewModels/ReportesInstitucionalesViewModels.cs`
- `ViewModels/AprobadosReprobadosFiltroDtos.cs`
- `ViewModels/AprobadosReprobadosFiltroValores.cs`
- `Views/Reportes/*`
- `Views/HabitosActitudesReport/*`
- `Views/CalificacionesInforme/*`
- `Views/FormatoCarpetasReport/*`
- `Views/Shared/_InformeInstitucionalFiltros.cshtml`
- `wwwroot/css/reporte-*.css` (6 archivos)
- `Reportes/*.xls` (3 plantillas)

### Adaptados
- `SchoolManager.csproj` — NPOI + CopyToOutput `Reportes/**`
- `Program.cs` — DI de 3 servicios
- `MenuService.cs` — ítem Reportes
- `_AdminLayout.cshtml` — submenú completo
- `IAprobadosReprobadosService` / `AprobadosReprobadosService` / `AprobadosReprobadosController` — filtros institucionales (aditivos)
- `ICounselorAssignmentService` / `CounselorAssignmentService` — `GetConsejeroNombrePorGrupoGradoAsync`

## Adaptaciones MultiTenant aplicadas

1. `SchoolId` del usuario actual (nunca desde query string).
2. `ValidarAlcanceAsync`: grupo/grado/materia deben pertenecer a la escuela; docentes validados por `TeacherAssignments.SchoolId`.
3. Consultas de estudiantes/scores/asistencia filtradas por `SchoolId`.
4. Bulk cache key incluye `SchoolId`.
5. GQF existente actúa como segunda barrera.
6. Asistencia adaptada al schema MT (sin `TrimesterId`).

## Riesgos encontrados

| Riesgo | Mitigación | Residual |
|---|---|---|
| PDF Puppeteer requiere Chrome | Igual que carnets; configurar `PUPPETEER_EXECUTABLE_PATH` en deploy | Medio en CI sin Chromium |
| Plantillas `.xls` NPOI | Copiadas y publicadas | Bajo |
| Año académico no explícito en filtros | Misma semántica IIC; mejora futura con AcademicYear activo | Medio |
| CSS `@@page` en IIC | Corregido a `@page` en MT | Resuelto |

## Resultado de compilación

```
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Menú / navegación

- `MenuService`: `/Reportes/Index` para admin, director, teacher
- `_AdminLayout`: Catálogo + 5 reportes (incluye Aprobados)

## Confirmación de aislamiento

- Controllers: roles `admin,director,teacher` + `SchoolId` del usuario autenticado
- Service: validación explícita de pertenencia de IDs a la escuela
- Docente: solo sus asignaciones (`TeacherAssignments.SchoolId`)
- Queries: filtros `SchoolId` en bulk loader y alcance

## Pruebas pendientes (smoke / Playwright)

- [ ] Login admin → `/Reportes/Index` carga catálogo
- [ ] Abrir cada informe Index
- [ ] Filtros AJAX cargan grados/grupos
- [ ] Vista previa con datos reales
- [ ] Export Excel / PDF
- [ ] Intento cross-tenant con `groupId` ajeno → Unauthorized/error
- [ ] Rol teacher solo ve sus grupos

## Evidencia funcional

Pendiente ejecución contra instancia con datos. Compilación y cableado de rutas/menú/DI verificados.

## Siguiente módulo

**M2 — Credencial institucional del personal + Staff Directory**
