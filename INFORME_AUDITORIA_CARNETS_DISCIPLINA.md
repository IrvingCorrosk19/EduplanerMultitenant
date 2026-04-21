# INFORME DE AUDITORÍA TÉCNICA — MÓDULO CARNETS Y DISCIPLINA

**Proyecto:** SchoolManager (ASP.NET Core MVC)  
**Fecha:** 17 de febrero de 2026  
**Alcance:** Carnets digitales, diseños personalizables, fondos/marca de agua, historial disciplinario, registro de incidentes por profesores e inspectores  

---

## 1. Resumen ejecutivo

El sistema **SchoolManager** tiene ya implementados varios componentes que soportan parcialmente el futuro módulo de carnets digitales y disciplina. Existen tablas, servicios y vistas para carnets, reportes disciplinarios y QR, pero **no hay** soporte para diseños por estudiante, fondos tipo marca de agua ni rol de inspector. El historial disciplinario está cubierto por la tabla `discipline_reports` y su integración en StudentReport y TeacherGradebookDuplicate, pero no existe una separación conceptual entre incidente, tema y sanción.

**Conclusión:** El sistema está parcialmente preparado. Se puede reutilizar estructura de carnets, disciplina, QR y almacenamiento de imágenes, pero será necesario extender modelos, añadir rol Inspector y nuevas estructuras para fondos y personalización por estudiante.

---

## 2. Hallazgos de base de datos

### 2.1 Tablas relacionadas con carnets

| Tabla | Propósito | Campos principales | PK | FKs | Relación Student/User | ¿Sirve para nuevo módulo? |
|-------|-----------|--------------------|----|-----|------------------------|----------------------------|
| **student_id_cards** | Carnets emitidos | Id, StudentId, CardNumber, IssuedAt, ExpiresAt, Status | Id | StudentId → users(id) | Sí, User como estudiante | **Sí.** Falta diseño por estudiante y fondo. |
| **school_id_card_settings** | Configuración carnet por escuela | Id, SchoolId, TemplateKey, PageWidthMm, PageHeightMm, BackgroundColor, PrimaryColor, TextColor, ShowQr, ShowPhoto | Id | SchoolId | No directa | **Parcial.** Solo configuración global; no fondo imagen ni por estudiante. |
| **id_card_template_fields** | Posición de campos en carnet | Id, SchoolId, FieldKey, IsEnabled, XMm, YMm, WMm, HMm, FontSize | Id | SchoolId | No directa | **Parcial.** Solo a nivel escuela; no por estudiante. |

**No existe:** tabla de fondos, imágenes de marca de agua, temas o diseños por estudiante.

### 2.2 Tablas relacionadas con QR

| Tabla | Propósito | Campos principales | PK | FKs | Relación Student/User | ¿Sirve para nuevo módulo? |
|-------|-----------|--------------------|----|-----|------------------------|----------------------------|
| **student_qr_tokens** | Tokens QR de carnets | Id, StudentId, Token, ExpiresAt, IsRevoked | Id | StudentId → users(id) | Sí | **Sí.** Reutilizable sin cambios. |
| **scan_logs** | Registro de escaneos | Id, StudentId, ScanType, Result, ScannedBy, ScannedAt | Id | StudentId → users(id) | Sí | **Sí.** Reutilizable. |

### 2.3 Tablas relacionadas con disciplina

| Tabla | Propósito | Campos principales | PK | FKs | Relación Student/User | ¿Sirve para nuevo módulo? |
|-------|-----------|--------------------|----|-----|------------------------|----------------------------|
| **discipline_reports** | Reportes/incidentes disciplina | Id, SchoolId, StudentId, TeacherId, Date, ReportType, Description, Status, Category, Documents, SubjectId, GroupId, GradeLevelId, CreatedBy | Id | SchoolId, StudentId, TeacherId, SubjectId, GroupId, GradeLevelId, CreatedBy, UpdatedBy | Student y Teacher como User | **Sí.** Ya hay historial. No separa incidente/sanción. |
| **orientation_reports** | Reportes de orientación (consejería) | Misma estructura que discipline_reports | Id | Idem | Idem | **No** para disciplina; sí para referencias de diseño. |

**No existe:** tabla separada para sanciones, observaciones, incidentes vs reportes, ni validación por inspector.

### 2.4 Estructuras para imágenes, fondos y plantillas

- **school_id_card_settings.BackgroundColor:** color de fondo (hex), no imagen.
- **schools.LogoUrl:** logo de la escuela; no fondo de carnet.
- **users.photo_url:** foto del usuario/estudiante.

**No existe:** tabla para fondos de carnet, marcas de agua, plantillas por tema ni imágenes por estudiante.

### 2.5 Relación estudiante–archivos multimedia

- **users.photo_url:** ruta/URL de la foto del usuario.
- **discipline_reports.documents:** JSON con lista de archivos subidos (nombre, ruta, tamaño, fecha).
- Los archivos de disciplina se guardan en `wwwroot/uploads/discipline/`.

**No existe:** tabla de archivos multimedia genérica vinculada a estudiante (galería, fondos).

### 2.6 Campos reutilizables en User/estudiante

| Campo | Tabla | Uso actual | Reutilizable para carnet/disciplina |
|-------|-------|------------|-------------------------------------|
| DocumentId | users | Cédula/identificación | Sí, ya se usa en carnet |
| Name, LastName | users | Nombre completo | Sí |
| PhotoUrl | users | Foto del usuario | Sí |
| Shift | users | Jornada (Mañana/Tarde/Noche) | Sí |
| Status | users | Estado activo/inactivo | Sí |
| SchoolId | users | Escuela | Sí |

---

## 3. Hallazgos de entidades y modelos

### 3.1 Student

- **Modelo:** `Student` (parcialmente legacy). Los estudiantes activos se manejan como `User` con role `student` o `estudiante`.
- **StudentAssignment:** relaciona User (estudiante) con Grade, Group, Shift.
- **Propiedades:** Id, SchoolId, Name, BirthDate, Grade, GroupName, ParentId.
- **Uso:** La mayoría de flujos usa `User` + `StudentAssignment`; `Student` queda como modelo secundario.

### 3.2 User

- **Propiedades relevantes:** Id, SchoolId, Name, LastName, DocumentId, DateOfBirth, Role, Status, Shift, PhotoUrl, Disciplina, Orientacion, Inclusivo, Inclusion.
- **Relaciones:** DisciplineReportStudents, DisciplineReportTeachers, StudentAssignments, OrientationReportStudents, OrientationReportTeachers.
- **Navegación:** `UpdatePhoto(photoUrl)` para actualizar foto.
- **Soporta:** identidad, foto, documento, estado; no soporta diseño/tema propio.

### 3.3 DisciplineReport

- **Propiedades:** Id, SchoolId, StudentId, TeacherId, Date, ReportType, Description, Status, Category, Documents, SubjectId, GroupId, GradeLevelId, CreatedBy, UpdatedBy.
- **Relaciones:** Student, Teacher, GradeLevel, Group, Subject, School, CreatedByUser, UpdatedByUser.
- **Uso actual:** incidentes/reportes unificados; ReportType y Category diferencian tipos.
- **No hay:** campos explícitos para incidente vs sanción, ni validación por inspector.

### 3.4 StudentIdCard

- **Propiedades:** Id, StudentId, CardNumber, IssuedAt, ExpiresAt, Status.
- **Relación:** Student (User).
- **Uso:** registro de carnets emitidos; no almacena diseño ni fondo.

### 3.5 SchoolIdCardSetting

- **Propiedades:** Id, SchoolId, TemplateKey, PageWidthMm, PageHeightMm, BleedMm, BackgroundColor, PrimaryColor, TextColor, ShowQr, ShowPhoto.
- **No hay:** BackgroundImageUrl, WatermarkUrl ni campos para imagen de fondo.

### 3.6 IdCardTemplateField

- **Propiedades:** Id, SchoolId, FieldKey, IsEnabled, XMm, YMm, WMm, HMm, FontSize, FontWeight.
- **FieldKey soportados:** FullName, DocumentId, Grade, Group, Shift, CardNumber, SchoolName, SchoolLogo, Photo, Qr.
- **Uso:** solo a nivel escuela; no por estudiante.

### 3.7 StudentQrToken, ScanLog, OrientationReport

- **StudentQrToken:** Token, ExpiresAt, IsRevoked; relación con User (Student).
- **ScanLog:** ScanType, Result, ScannedBy; relación con User (Student).
- **OrientationReport:** mismo esquema que DisciplineReport, orientado a consejería.

---

## 4. Hallazgos de controladores

### 4.1 DisciplineReportController

| Acción | Propósito | Cubre necesidad |
|--------|-----------|-----------------|
| Index | Listado de reportes | Sí |
| Details | Detalle de reporte | Sí |
| CreateWithFiles | Crear reporte con archivos | Sí (AJAX desde TeacherGradebookDuplicate) |
| Edit / Delete | Editar y eliminar | Sí |
| GetByStudent | Historial por estudiante | Sí |
| GetFiltered | Filtros por fecha, grado, grupo | Sí |
| ExportToExcel | Exportar a CSV | Sí |
| SendEmailToStudent | Enviar reporte por email | Sí |
| GetByCounselor | Reportes para consejeros | Sí |
| GetVisibleDisciplineInfo | Disciplina visible (permisos) | Sí |
| UpdateStatus | Cambiar estado (ej. escalado) | Sí |

- **Permisos:** Director puede aplicar sanciones graves; Profesor solo puede escalar.
- **No hay:** acciones específicas para Inspector ni validación de incidentes.

### 4.2 StudentIdCardController

| Acción | Propósito | Cubre necesidad |
|--------|-----------|-----------------|
| Index | UI lista de estudiantes | Sí |
| GenerateView | Vista previa carnet | Sí |
| Scan | Vista de escaneo | Sí |
| Print | Descargar PDF | Sí |
| GenerateApi | API para generar carnet | Sí |
| ScanApi | API escaneo (AllowAnonymous) | Sí |
| ListJson | Lista estudiantes (Admin/Director) | Sí |

- **Autorización:** Admin, SuperAdmin, Director.

### 4.3 IdCardSettingsController

| Acción | Propósito | Cubre necesidad |
|--------|-----------|-----------------|
| Index | Configuración carnet | Sí |
| Save | Guardar colores, dimensiones, ShowQr/ShowPhoto | Sí |

- **No hay:** fondos, marcas de agua ni configuración por estudiante.

### 4.4 StudentProfileController, StudentReportController

- **StudentProfile:** perfil del estudiante, incluye foto.
- **StudentReport:** reporte académico del estudiante, incluye `DisciplineReports` y exportación PDF de disciplina.

### 4.5 FileController

- **GetSchoolLogo, GetUserAvatar, DownloadTemplate:** soporte básico de archivos.
- **No hay:** endpoint genérico para fondos o imágenes de carnet.

### 4.6 TeacherGradebookDuplicateController

- Expone Portal Disciplina (para `User.Disciplina == true`).
- Crea reportes vía `POST /DisciplineReport/CreateWithFiles`.
- No usa rol Inspector; solo Teacher y Director.

---

## 5. Hallazgos de servicios e interfaces

### 5.1 IDisciplineReportService

| Método | Responsabilidad | Reutilizable |
|--------|-----------------|--------------|
| GetAllAsync, GetByIdAsync | CRUD básico | Sí |
| CreateAsync, UpdateAsync, DeleteAsync | Persistencia | Sí |
| GetByStudentAsync | Historial por estudiante | Sí |
| GetFilteredAsync | Filtros | Sí |
| GetByStudentDtoAsync | DTO por estudiante y trimestre | Sí |
| GetByCounselorAsync | Reportes para consejeros | Sí |
| UpdateStatusAsync | Cambio de estado | Sí |

**Pendiente:** métodos para flujo Inspector (validar, clasificar incidente vs sanción).

### 5.2 IStudentIdCardService

| Método | Responsabilidad | Reutilizable |
|--------|-----------------|--------------|
| GenerateAsync | Generar DTO carnet (con token QR) | Sí |
| ScanAsync | Validar token y registrar escaneo | Sí |

**Pendiente:** soporte para diseño y fondo por estudiante.

### 5.3 IStudentIdCardPdfService

| Método | Responsabilidad | Reutilizable |
|--------|-----------------|--------------|
| GenerateCardPdfAsync | PDF del carnet con QuestPDF | Sí |

- Usa SchoolIdCardSetting, IdCardTemplateField, logo, foto y QR.
- **No soporta:** fondo imagen, marca de agua ni diseño por estudiante.

### 5.4 IFileStorageService

| Método | Responsabilidad | Reutilizable |
|--------|-----------------|--------------|
| SaveUserPhotoAsync | Guardar foto de usuario | Sí (posible extensión para fondos) |
| DeleteUserPhotoAsync | Eliminar foto | Sí |
| GetUserPhotoBytesAsync | Obtener bytes (PDF, etc.) | Sí |

### 5.5 IUserPhotoService

- Guarda/elimina fotos de usuario.
- Actualiza `User.PhotoUrl`.

**Pendiente:** interfaz para fondos de carnet o imágenes por tipo (watermark, etc.).

---

## 6. Hallazgos de vistas

### 6.1 Vista de perfil de estudiante

- **Ruta:** `Views/StudentProfile/Index.cshtml`
- **Contenido:** perfil personal, foto, datos.
- **Reutilizable:** estructura y sección de foto para evolución hacia carnet digital.

### 6.2 Vista detallada del estudiante

- **Ruta:** `Views/StudentReport/Index.cshtml`
- **Contenido:** calificaciones, asistencia, historial disciplinario (`DisciplineReports`), export PDF disciplina.
- **Reutilizable:** bloque de disciplina para historial y posible integración con carnet.

### 6.3 Vista de historial disciplinario

- **DisciplineReport/Index.cshtml, Details.cshtml:** vistas CRUD básicas.
- **StudentReport:** sección de reportes de disciplina.
- **TeacherGradebookDuplicate:** formulario para crear reportes y ver historial.
- **Reutilizable:** estructura de listado y detalle; falta vista centralizada de historial disciplinario.

### 6.4 Vista para registro de novedades

- **TeacherGradebookDuplicate/Index.cshtml:** profesores con `Disciplina == true` crean reportes.
- **OrientationReport/Index.cshtml:** profesores con `Orientacion == true` crean reportes de orientación.
- **No hay:** vista específica para Inspector.

### 6.5 Vista tipo carnet digital

- **StudentIdCard/Generate.cshtml:** vista previa carnet (HTML/CSS, proporción 85.6×54 mm).
- **StudentIdCard/Index.cshtml:** selección de estudiante y generación.
- **Reutilizable:** base para carnet digital; faltan fondos y personalización por estudiante.

### 6.6 Visualización de foto del estudiante

- **StudentProfile, StudentReport, StudentIdCard/Generate, User/Edit:** muestran foto.
- Soporte vía `User.PhotoUrl` y `IFileStorageService`.

### 6.7 Badges, cards, credenciales

- **StudentIdCard/Generate:** diseño tipo credencial con logo, foto, datos y QR.
- **IdCardSettings:** configuración de colores y dimensiones.
- No hay badges visuales de disciplina ni indicadores en carnet.

---

## 7. Hallazgos de roles y permisos

### 7.1 Roles actuales

- superadmin, admin, director, teacher, parent, student, estudiante, acudiente, contable, contabilidad, secretaria.
- **No existe:** Inspector.

### 7.2 Permisos por rol (relevantes)

| Rol | Disciplina | Carnets | Notas |
|-----|------------|---------|-------|
| Director | Puede ver todo, aplicar sanciones graves, escalar | Acceso IdCard | Autoridad máxima disciplina |
| Teacher (Disciplina) | Crear reportes, escalar; no sanciones graves | No | Portal Disciplina |
| Teacher (Orientación) | No | No | Portal Orientación (consejería) |
| Admin | Acceso general | Sí | IdCard, IdCardSettings |
| Parent | Ver disciplina de hijos (implementación incompleta) | No | CanParentViewStudentDiscipline |
| Student | Ver su reporte (incl. disciplina) | No | StudentReport |

### 7.3 Limitaciones

- No hay rol Inspector para validar incidentes.
- `CanParentViewStudentDiscipline` retorna `true` para cualquier parent sin verificar parentesco.
- No hay diferenciación permisos profesor vs inspector en disciplina.

---

## 8. Capacidades actuales del sistema

| Capacidad | ¿Existe? | Detalle |
|-----------|----------|---------|
| Carga de imágenes | Sí | UserPhotoService, IFormFile, FileController.GetUserAvatar |
| Almacenamiento de rutas/URLs | Sí | users.photo_url, schools.logo_url |
| Almacenamiento de blobs | Parcial | Archivos en disco (wwwroot/uploads); no Blob Storage |
| Generación de QR | Sí | QrHelper.GenerateQrPng, StudentQrToken |
| Generación de PDF | Sí | QuestPDF, StudentIdCardPdfService |
| Render de credenciales | Sí | StudentIdCard/Generate (HTML) y PDF |
| Plantillas visuales | Parcial | IdCardTemplateField, SchoolIdCardSetting; solo escuela |
| Temas por usuario/estudiante | No | No existe tabla ni lógica |
| Bitácora por estudiante | Parcial | DisciplineReports como historial; no audit trail completo |

---

## 9. Brechas funcionales y técnicas

### 9.1 Existente y reutilizable

- Tablas `discipline_reports`, `student_id_cards`, `student_qr_tokens`, `scan_logs`, `school_id_card_settings`, `id_card_template_fields`.
- Servicios de carnets, PDF, QR, disciplina, fotos.
- Vistas de carnet, perfil, reporte de estudiante.
- Estructura de permisos Director/Teacher para disciplina.

### 9.2 Parcialmente existente

- **Configuración de carnet:** existe por escuela; falta por estudiante.
- **Historial disciplinario:** existe como `discipline_reports`; falta separación incidente/sanción y flujo Inspector.
- **Permisos disciplina:** Director y Teacher definidos; falta Inspector y refinamiento Parent.

### 9.3 No existente

- Rol Inspector.
- Fondos de carnet tipo imagen/marca de agua.
- Diseños o temas por estudiante.
- Tabla de fondos/plantillas de carnet.
- Validación de incidentes por Inspector.

### 9.4 Dependencias entre carnet y disciplina

- Carnet puede mostrar indicadores de disciplina (ej. estado “condicional”).
- Historial disciplinario puede enlazarse desde la vista de carnet.
- Ambos dependen de User/Student.

### 9.5 Riesgos de implementar sin rediseño

- Forzar diseño por estudiante en `SchoolIdCardSetting` rompe su naturaleza por escuela.
- Añadir fondo imagen en `school_id_card_settings` sin migración de almacenamiento (blobs) puede generar inconsistencias.
- Usar Teacher para Inspector sin rol nuevo dificulta auditoría y permisos.

### 9.6 Integridad del modelo

- `DisciplineReport.StudentId` y `TeacherId` apuntan a `users`; correcto.
- `StudentIdCard.StudentId` también a `users`; correcto.
- Mantener `Student` legacy puede confundir; conviene consolidar en User + StudentAssignment.

---

## 10. Recomendaciones arquitectónicas

### 10.1 Módulos nuevos

- **Módulo Carnet Digital:** extender el existente con fondos, marcas de agua y personalización por estudiante.
- **Módulo Disciplina:** extender con flujo Inspector, validación y clasificación incidente/sanción.

### 10.2 Tablas a crear

- `id_card_backgrounds` (o similar): SchoolId, ImageUrl, IsWatermark, IsDefault.
- `student_id_card_designs` (o `student_card_overrides`): StudentId, BackgroundId, ThemeKey, etc.
- `discipline_incident_types` o `discipline_categories`: catálogo de tipos.
- Considerar `discipline_report_validations`: ReportId, ValidatedBy (Inspector), ValidatedAt.

### 10.3 Servicios nuevos

- `IIdCardBackgroundService`: gestión de fondos y marcas de agua.
- `IStudentCardDesignService`: diseño por estudiante.
- Extender `IDisciplineReportService` con `ValidateByInspectorAsync`, `GetPendingValidationAsync`.

### 10.4 Controladores y vistas futuras

- **Inspector:** `InspectorDisciplineController` (validación, listado pendientes).
- **Carnet:** acciones para fondos, marcas de agua, selección de diseño por estudiante.
- **Vistas:** gestión de fondos, selección de tema por estudiante, panel Inspector.

### 10.5 Separación de responsabilidades

- Carnet: generación, QR, almacenamiento de fondos, diseño por estudiante.
- Disciplina: incidentes, sanciones, flujo Inspector, historial.
- Relación: el carnet puede consumir un “estado disciplinario” (ej. condicional) sin duplicar lógica.

### 10.6 Carnet vs Disciplina

- **Recomendación:** mantener Carnet y Disciplina como submódulos distintos con interfaces claras.
- Disciplina expone estado/indicadores; Carnet los consume para visualización.

---

## 11. Conclusión final

SchoolManager tiene base sólida para carnets digitales (tablas, servicios, PDF, QR, fotos) y para disciplina (reportes, historial, permisos básicos). Las brechas principales son:

1. **Carnets:** falta soporte para fondos imagen, marcas de agua y diseños por estudiante.
2. **Disciplina:** falta rol Inspector, validación de incidentes y separación incidente vs sanción.
3. **Roles:** es necesario definir Inspector y afinar permisos de Parent.
4. **Integridad:** conviene no forzar nuevas funcionalidades sobre estructuras pensadas solo a nivel escuela.

El informe se basa en el código real del proyecto. No se han realizado cambios; solo diagnóstico.
