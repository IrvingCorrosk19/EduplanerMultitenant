# Eduplaner / SchoolManager — Qué ofrece la plataforma (módulo por módulo)

Este documento describe la plataforma **como producto**, con enfoque comercial: qué resuelve, para quién y qué valor entrega cada módulo. La información se deriva de la estructura del proyecto (controladores, vistas y menú por rol) dentro de este repositorio.

---

## Resumen ejecutivo (propuesta de valor)

Eduplaner es un **Sistema de Gestión Escolar** construido en ASP.NET Core, orientado a operar el ciclo académico de forma centralizada:

- **Administración**: usuarios/roles, catálogo académico, asignaciones, importaciones masivas.
- **Docentes**: portal docente, calificaciones/gradebook, planes de trabajo.
- **Estudiantes**: acceso a su información académica y funcionalidades del portal.
- **Dirección**: portal de director con visión de gestión.
- **Operación institucional**: reportes, orientación, disciplina, asistencia, pagos, auditoría.
- **Credenciales**: generación y administración de **carnet estudiantil** (ID Card) y configuración.

Además, incluye un módulo administrativo para **envío masivo de contraseñas temporales por correo** para restablecer accesos.

---

## Roles principales (orientación de uso)

Según el menú y autorizaciones, se evidencian estos roles típicos:

- **SuperAdmin**: operación global (p. ej. carnet, configuraciones avanzadas).
- **Admin**: operación de una institución/escuela (catálogo, usuarios, asignaciones, importaciones).
- **Teacher**: portal docente, calificaciones y planificación.
- **Student / Estudiante**: portal del estudiante.
- **Director**: portal de director.
- **ClubParentsAdmin**: módulo de Club de Padres.

> Nota: existen variantes de rol en español/inglés (`student`/`estudiante`, `superadmin`/`SuperAdmin`, etc.).

---

## Módulos del producto (por área funcional)

### 1) Autenticación y acceso

**Qué ofrece**
- Inicio de sesión y control de acceso por rol (autorización por controlador).
- Soporte para flujos del portal según el rol autenticado.

**Valor**
- Segmenta la experiencia por tipo de usuario (admin/docente/estudiante/director).
- Base de seguridad para operar datos académicos con permisos.

Pantallas/controladores relacionados: `AuthController`, `HomeController`.

---

### 2) Dashboard (inicio)

**Qué ofrece**
- Punto de entrada “Dashboard” para usuarios autorizados.

**Valor**
- Navegación central a los módulos del día a día.

Pantallas/controladores relacionados: `Home/Index`.

---

### 3) Gestión de usuarios y roles

**Qué ofrece**
- Administración de usuarios desde el panel administrativo.
- Segmentación por roles (admin, teacher, student/estudiante, director, etc.).

**Valor**
- Control total del “quién” accede a la plataforma y qué puede ver/hacer.
- Base para asignaciones académicas, reportes y comunicaciones.

Pantallas/controladores relacionados: `UserController` (`Views/User/Index.cshtml`).

---

### 4) Cambio de contraseña (autogestión)

**Qué ofrece**
- Pantalla para que usuarios cambien su contraseña.

**Valor**
- Reducción de tickets operativos: el usuario resuelve su actualización de credenciales.

Pantallas/controladores relacionados: `ChangePasswordController` (`Views/ChangePassword/Index.cshtml`).

---

### 5) Envío masivo de contraseñas temporales (Administración)

**Qué ofrece**
- Módulo administrativo para seleccionar múltiples usuarios y **enviar una contraseña temporal por correo**.
- Control de estado por usuario (p. ej. enviado/fallido) para trazabilidad operativa.
- Filtros y búsqueda para localizar población objetivo (por grado/grupo/rol y texto).

**Valor**
- Onboarding rápido (escuelas nuevas / usuarios nuevos).
- Recuperación operativa cuando hay rotación o reseteos masivos (inicio de año).
- Disminuye soporte manual: el admin resuelve en minutos.

Pantallas/controladores relacionados:
- `Admin/UserPasswordManagementController` (`Views/Admin/UserPasswordManagement/Index.cshtml`)
- Servicios: `BulkPasswordEmailService`, `EmailService` (proveedor vía API).

---

### 6) Catálogo académico (base institucional)

**Qué ofrece**
- Gestión de entidades de catálogo para operar el año escolar:
  - Áreas, asignaturas, grados/niveles, grupos, especialidades y afines.

**Valor**
- Estandariza la estructura académica.
- Reduce inconsistencias al momento de asignar materias, docentes y estudiantes.

Pantallas/controladores relacionados (según controladores y vistas):
- `AcademicCatalogController` (`Views/AcademicCatalog/Index.cshtml`)
- `AreaController`, `SubjectController`, `GradeLevelController`, `GroupController`, `SpecialtyController`.

---

### 7) Asignaciones académicas (operación)

**Qué ofrece**
- **Asignar docentes** a materias/grupos/grados.
- **Asignar estudiantes** a su grado/grupo y mantener historial.
- Vistas para revisar y ajustar asignaciones.

**Valor**
- Permite pasar del catálogo a la ejecución: quién enseña qué y a quién.
- Control real del aula por período/año.

Pantallas/controladores relacionados:
- `TeacherAssignmentController` (`Views/TeacherAssignment/Index.cshtml`)
- `StudentAssignmentController` (`Views/StudentAssignment/Index.cshtml`)
- `AcademicAssignmentController` (`Views/AcademicAssignment/Index.cshtml` y `Upload`)
- `SubjectAssignmentController` (`Views/SubjectAssignment/Index.cshtml`) para catálogo de combinaciones/relaciones.

---

### 8) Cargas masivas (importaciones)

**Qué ofrece**
- Subida de asignaciones (docentes/estudiantes) mediante archivos (flujo “Upload”).

**Valor**
- Implementaciones más rápidas (migración desde Excel/otros sistemas).
- Menos trabajo manual y menos errores por carga uno-a-uno.

Pantallas/controladores relacionados:
- `AcademicAssignmentController.Upload`
- `StudentAssignmentController.Upload`

---

### 9) Portal docente (calificaciones y gestión académica)

**Qué ofrece**
- Portal del docente para operar calificaciones/gradebook.
- Variante “duplicado” para escenarios de trabajo o compatibilidad.

**Valor**
- Digitaliza la evaluación y reduce procesos en papel.
- Disponibilidad y control de información académica para seguimiento.

Pantallas/controladores relacionados:
- `TeacherGradebookController` (`Views/TeacherGradebook/Index.cshtml`)
- `TeacherGradebookDuplicateController` (`Views/TeacherGradebookDuplicate/Index.cshtml`)

---

### 10) Planes de trabajo (docente y dirección)

**Qué ofrece**
- Planificación trimestral/periodos: construcción y seguimiento de planes.
- Portal específico para dirección y para docentes.

**Valor**
- Mejora la trazabilidad pedagógica.
- Alinea planificación con ejecución y evidencias.

Pantallas/controladores relacionados:
- `TeacherWorkPlanController` (`Views/TeacherWorkPlan/Index.cshtml`)
- `DirectorWorkPlansController` (`Views/DirectorWorkPlans/Index.cshtml`)
- `DirectorController` (`Views/Director/Index.cshtml`)

---

### 11) Horarios (schedule) y configuración

**Qué ofrece**
- Configuración de horarios, franjas (timeslots) y estructuras relacionadas.
- Vista de horarios para estudiantes.

**Valor**
- Menos choques operativos (horas, grupos, asignaciones).
- Visibilidad clara del plan semanal.

Pantallas/controladores relacionados:
- `ScheduleController`
- `ScheduleConfigurationController` (`Views/ScheduleConfiguration/Index.cshtml`)
- `TimeSlotController` (`Views/TimeSlot/Index.cshtml`)
- `StudentScheduleController`

---

### 12) Carnet estudiantil (ID Card)

**Qué ofrece**
- Generación/visualización de carnet estudiantil.
- Ajustes/configuración del carnet.
- Componentes de soporte para “carnet” (incluye módulos con sello/branding QL Services).

**Valor**
- Identificación institucional y control en campus.
- Integración de datos del estudiante (grado/grupo) para uso cotidiano.

Pantallas/controladores relacionados:
- `StudentIdCardController` (`Views/StudentIdCard/Index.cshtml` y ruta UI)
- `IdCardSettingsController` (`Views/IdCardSettings/Index.cshtml`)
- `QlServicesCarnetController`

---

### 13) Perfil del estudiante

**Qué ofrece**
- Vista/gestión del perfil del estudiante (datos personales y académicos asociados).

**Valor**
- Centraliza información clave para administración y orientación.

Pantallas/controladores relacionados: `StudentProfileController` (`Views/StudentProfile/Index.cshtml`).

---

### 14) Asistencia (Attendance)

**Qué ofrece**
- Módulo de asistencia con pantalla propia.

**Valor**
- Control de presencia y seguimiento de estudiantes.

Pantallas/controladores relacionados: `AttendanceController` (`Views/Attendance/Index.cshtml`).

---

### 15) Disciplina y convivencia

**Qué ofrece**
- Gestión de reportes disciplinarios con su propia operación y listados.

**Valor**
- Trazabilidad de incidentes, soporte a medidas y comunicación.

Pantallas/controladores relacionados: `DisciplineReportController` (`Views/DisciplineReport/Index.cshtml`).

---

### 16) Orientación (seguimiento y reportes)

**Qué ofrece**
- Reportes de orientación y módulos asociados.
- Módulo de orientación por estudiante.

**Valor**
- Acompañamiento integral del estudiante y evidencias para intervención.

Pantallas/controladores relacionados:
- `OrientationReportController` (`Views/OrientationReport/Index.cshtml`)
- `StudentOrientationController` (`Views/StudentOrientation/Index.cshtml`)

---

### 17) Reportes académicos y resultados

**Qué ofrece**
- Reportes generales de estudiantes.
- Módulo de “Aprobados / Reprobados”.

**Valor**
- Visión ejecutiva de resultados y rendimiento.
- Apoya decisiones de promoción, refuerzos y comunicación.

Pantallas/controladores relacionados:
- `StudentReportController` (`Views/StudentReport/Index.cshtml`)
- `AprobadosReprobadosController` (`Views/AprobadosReprobados/Index.cshtml`)

---

### 18) Prematrícula y períodos

**Qué ofrece**
- Gestión de períodos de prematrícula.
- Flujo operativo de prematrícula.

**Valor**
- Ordena el proceso de inscripción temprana.
- Reduce tiempos administrativos en inicios de año.

Pantallas/controladores relacionados:
- `PrematriculationPeriodController` (`Views/PrematriculationPeriod/Index.cshtml`)
- `PrematriculationController` (`Views/Prematriculation/Index.cshtml`)

---

### 19) Pagos y conceptos

**Qué ofrece**
- Gestión de pagos.
- Catálogo de conceptos de pago (matrícula, mensualidad, etc.).

**Valor**
- Control financiero y trazabilidad de cobros.
- Apoya la operación administrativa y reportes.

Pantallas/controladores relacionados:
- `PaymentController` (`Views/Payment/Index.cshtml`)
- `PaymentConceptController` (`Views/PaymentConcept/Index.cshtml`)

---

### 20) Club de Padres (módulo especializado)

**Qué ofrece**
- Portal/operación para “Club de Padres” (según rol `clubparentsadmin`).

**Valor**
- Administración focalizada para recaudación/gestión asociada a padres.

Pantallas/controladores relacionados: `ClubParentsController` (entrada: `/ClubParents/Students`).

---

### 21) Mensajería y comunicaciones internas

**Qué ofrece**
- Módulo de mensajería con su vista principal.

**Valor**
- Comunicación formal y rastreable dentro del sistema (sin depender de canales externos).

Pantallas/controladores relacionados: `MessagingController`.

---

### 22) Configuración de correo (operación)

**Qué ofrece**
- Configuración de envío de correo (operativo) y soporte para módulos que generan notificaciones.

**Valor**
- Permite activar comunicaciones automatizadas (reportes, accesos, notificaciones).

Pantallas/controladores relacionados: `EmailConfigurationController` (`Views/EmailConfiguration/Index.cshtml`).

---

### 23) Seguridad y políticas

**Qué ofrece**
- Pantallas/configuraciones relacionadas con seguridad.

**Valor**
- Soporte a políticas de acceso y controles administrativos.

Pantallas/controladores relacionados: `SecuritySettingController` (`Views/SecuritySetting/Index.cshtml`).

---

### 24) Auditoría (trazabilidad)

**Qué ofrece**
- Registro y consulta de auditoría.

**Valor**
- Transparencia de cambios/acciones en el sistema.
- Útil para cumplimiento y control interno.

Pantallas/controladores relacionados: `AuditLogController` (`Views/AuditLog/Index.cshtml`).

---

### 25) SuperAdmin (administración global)

**Qué ofrece**
- Panel de superadministración y configuraciones globales.
- Ajustes vinculados a proveedores externos (p. ej. API de correo).

**Valor**
- Gobierno de plataforma: configuración segura, centralizada y controlada.

Pantallas/controladores relacionados: `SuperAdminController` (`Views/SuperAdmin/Index.cshtml` y subpantallas como Email API Settings).

---

## Qué “vende” bien en demo (guion sugerido)

- **Inicio rápido**: importas catálogo y asignaciones en minutos (cargas masivas).
- **Operación completa**: asignas docentes/estudiantes y el sistema queda listo para el día a día.
- **Docentes productivos**: portal docente + calificaciones + planes trimestrales.
- **Trazabilidad**: reportes, disciplina, orientación, auditoría.
- **Identidad**: carnet estudiantil listo para imprimir/gestionar.
- **Soporte a accesos**: envío masivo de contraseñas temporales (ideal para inicio de clases).

---

## Apéndice: Mapa rápido de pantallas “Index” detectadas

Incluye (no exhaustivo): `Home`, `User`, `AcademicCatalog`, `TeacherAssignment`, `StudentAssignment`, `SubjectAssignment`, `TeacherGradebook`, `TeacherWorkPlan`, `DirectorWorkPlans`, `StudentIdCard`, `IdCardSettings`, `Attendance`, `DisciplineReport`, `OrientationReport`, `StudentReport`, `Payment`, `PaymentConcept`, `PrematriculationPeriod`, `Prematriculation`, `EmailConfiguration`, `SecuritySetting`, `AuditLog`, `Admin/UserPasswordManagement`, `SuperAdmin`.

