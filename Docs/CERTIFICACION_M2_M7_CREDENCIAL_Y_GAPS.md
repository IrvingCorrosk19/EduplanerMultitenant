# Certificación M2–M7 — Credencial, Staff, Gradebook PDF, gaps

**Fecha:** 2026-08-01  
**Build:** OK (0 errores, 0 warnings)

## M2 Credencial institucional

- Controllers/servicios/vistas migrados desde IIC
- Entidades con `SchoolId` + GQF + migración aplicada
- QR firmado HMAC; perfil público con `IgnoreQueryFilters` controlado
- Menú SuperAdmin: Credencial Institucional + Mi perfil institucional

## M3 Staff Directory

- `GetStaffDirectoryPageAsync` + acciones foto/perfil
- Vista `StaffDirectory.cshtml` + CSS
- Menú: Personal

## M4 PDF Gradebook

- `ITeacherGradebookPdfService` / implementación
- `ExportRegistroPdf` en controller
- Botón “Imprimir PDF” en portal docente
- Validación SchoolId en generación

## M5 Aprobados

- CSS de vista previa migrado
- Partial IIC no migrado (incompatible); API MT de filtros preservada

## M7 Download foto

- `StudentDirectoryDownloadPhoto` en SuperAdmin

## Migración

`20260801144145_AddInstitutionalStaffCredentialTables` — aplicada en BD local.
