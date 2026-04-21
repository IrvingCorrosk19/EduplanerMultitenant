# Auditoría técnica del módulo de Carnet Estudiantil (SchoolManager)

**Tipo de documento:** informe de ingeniería de software — revisión post-implementación  
**Ámbito:** ASP.NET Core MVC, Entity Framework Core, QuestPDF, arquitectura por escuela del estudiante  
**Versión del análisis:** posterior a correcciones de coherencia configuración–vista–PDF  

---

## 1. Abstract

El presente documento describe una auditoría técnica del módulo de **Carnet Estudiantil** del sistema SchoolManager, con énfasis en el flujo *configuración → vista previa → generación de datos del carnet → PDF → impresión*. Se verifica que la configuración persistida en `school_id_card_settings` y los datos institucionales asociados al estudiante se apliquen de forma **consistente con la escuela del alumno** (`User.SchoolId`), y no con la escuela del usuario autenticado, en las rutas críticas de generación visual y PDF. Se identifican los componentes arquitectónicos, las ramas de renderizado (layout CarnetQR frente a plantilla por campos), las limitaciones residuales y recomendaciones para evolución del producto. El análisis se basa en revisión estática del código fuente y del modelo de datos; no se incluyen pruebas de carga ni pentesting exhaustivo.

**Palabras clave:** carnet digital, QuestPDF, multi-tenant escolar, configuración por institución, trazabilidad PDF–UI.

---

## 2. Introducción

SchoolManager incorpora un subsistema de carnets que combina persistencia relacional, generación de tokens QR firmados, vistas Razor para operación y generación de documentos PDF. Tras intervenciones recientes, el requisito central es garantizar que **toda manifestación visual del carnet para un estudiante dado** derive de la **misma entidad escolar** que posee al alumno, eliminando ambigüedades cuando el operador es SuperAdmin o pertenece a otro contexto organizacional.

Este informe estructura el conocimiento del módulo para **arquitectos de software**, **auditores técnicos** e **ingenieros backend**, siguiendo convenciones de documentación académica (secciones numeradas, discusión explícita de limitaciones y referencias a artefactos del repositorio).

---

## 3. Arquitectura del módulo

### 3.1 Panorama de capas

| Capa | Artefactos principales | Función |
|------|------------------------|---------|
| **Modelo / persistencia** | `SchoolIdCardSetting`, `IdCardTemplateField`, `StudentIdCard`, `StudentQrToken`, `School.IdCardPolicy` | Configuración por escuela, plantilla posicional opcional, estado del carnet y token QR. |
| **Controladores** | `IdCardSettingsController`, `StudentIdCardController` | CRU de configuración; UI de listado/generación/impresión PDF y APIs de generación y escaneo. |
| **Servicios** | `StudentIdCardService`, `StudentIdCardPdfService` | Lógica de negocio del carnet y token; composición del PDF (QuestPDF, SkiaSharp para marca de agua). |
| **Presentación** | `Views/IdCardSettings/Index.cshtml`, `Views/StudentIdCard/Generate.cshtml`, `Scan.cshtml`, `Index.cshtml` | Configuración, vista previa modal, pantalla de generación con ViewModel fuerte. |
| **DTO / ViewModel** | `StudentIdCardDto`, `StudentIdCardGenerateViewModel` | Transporte de datos de carnet; agregación escuela + settings + flags PDF personalizado. |

### 3.2 Flujo lógico end-to-end

1. **Configuración:** director/admin/SuperAdmin edita `/id-card/settings`; se persisten filas en `school_id_card_settings` y texto de política en `schools.id_card_policy`.  
2. **Vista previa (settings):** modal en `Index.cshtml` refleja colores, orientación y toggles leyendo el formulario en cliente.  
3. **Generación de carnet:** `POST /StudentIdCard/api/generate/{studentId}` invoca `StudentIdCardService.GenerateAsync` (revocación de carnet/token previo, nuevo número y token).  
4. **Pantalla de generación:** `GET /StudentIdCard/ui/generate/{studentId}` resuelve estudiante → `SchoolId` → escuela + settings → `StudentIdCardGenerateViewModel` + `GetCurrentCardAsync`.  
5. **PDF:** `GET /StudentIdCard/ui/print/{studentId}` → `StudentIdCardPdfService.GenerateCardPdfAsync` con la **misma** escuela derivada del estudiante.  
6. **Impresión:** navegador (frente únicamente, por diseño UX) o PDF completo (frente/reverso según rama).

### 3.3 Dependencias externas relevantes

- **QuestPDF:** composición declarativa del PDF.  
- **SkiaSharp:** imagen de marca de agua a partir del logo.  
- **Servicios de firma QR** (`IQrSignatureService`): coherencia entre vista (PNG en base64) y PDF.

---

## 4. Metodología de auditoría

Se aplicó **revisión estática dirigida por riesgos** (Bowles & Hackney, enfoque de inspección de código para sistemas empresariales):

1. Trazado de referencias desde rutas HTTP hasta consultas EF Core.  
2. Verificación de **fuente de verdad** para `SchoolId` en generación de vista y PDF.  
3. Comparación de ramas de renderizado (CarnetQR vs. campos de plantilla).  
4. Revisión de atributos `[Authorize]` y superficies anónimas (`ScanApi`).  
5. Contraste cualitativo entre las tres superficies de “vista previa”: modal de configuración, pantalla Generate y PDF.

No se ejecutaron suites de prueba automatizadas como parte de este informe; las conclusiones sobre corrección funcional se apoyan en **consistencia lógica del código** y en la alineación explícita escuela-del-estudiante en puntos previamente divergentes.

---

## 5. Análisis técnico del sistema

### 5.1 Persistencia de configuración

**Modelo `SchoolIdCardSetting`:** mapeado a `school_id_card_settings` con índice único por `school_id`. Propiedades alineadas con el dominio: colores (`PrimaryColor`, `TextColor`, `BackgroundColor`), dimensiones y orientación, flags de visibilidad (QR, foto, teléfono institucional, emergencia, alergias, marca de agua) y `TemplateKey`.

**`TemplateKey`:** según documentación en modelo y comentarios en `StudentIdCardPdfService`, **no actúa como selector de motor de plantillas múltiples**; el PDF discrimina únicamente entre (a) existencia de filas activas en `id_card_template_fields` y (b) layout denominado CarnetQR. La clave permanece como metadato de compatibilidad y trazabilidad.

**Relación con `School`:** FK en cascada; política del carnet reside en `School.IdCardPolicy`, coherente con “un texto institucional por escuela”.

### 5.2 Configuración del carnet (UI administrativa)

`IdCardSettingsController` persiste configuración vía `POST /id-card/settings/save`, sincronizando dimensiones con orientación vertical/horizontal. La vista previa del modal **no consulta** `id_card_template_fields`; reproduce un **prototipo CarnetQR** mediante JavaScript. Por tanto, la vista previa de settings es **fiel a toggles y colores del formulario**, pero **no** al layout posicional PDF cuando existen campos personalizados.

### 5.3 Generación del carnet (UI operativa)

`StudentIdCardController.GenerateView`:

- Localiza al usuario con rol estudiante por `studentId`.  
- Carga `School` y `SchoolIdCardSetting` con **`student.SchoolId`**, con `IgnoreQueryFilters` donde aplica.  
- Calcula si hay plantilla PDF personalizada (`IdCardTemplateField` con `IsEnabled`).  
- Construye `StudentIdCardGenerateViewModel` (incluye emergencia, alergias desde el usuario estudiante).

**Conclusión:** la dependencia de la escuela del **usuario autenticado** como fuente de configuración visual para esta pantalla **ha quedado eliminada** para el contenido del carnet; el SuperAdmin sin escuela obtiene la misma configuración que el PDF para ese alumno.

### 5.4 Servicio `StudentIdCardService`

Responsabilidades acotadas: ciclo de vida del carnet (`StudentIdCard`), token QR (`StudentQrToken`), generación de imagen QR para DTO, y flujo de escaneo con reglas de negocio (asignación activa, disciplina, roles para datos sensibles). **No contiene lógica de maquetación** ni lectura de `SchoolIdCardSetting`, lo cual respeta la separación presentación/render vs. dominio del carnet.

### 5.5 Generación de PDF (`StudentIdCardPdfService`)

- **Librería:** QuestPDF; imagen auxiliar vía SkiaSharp.  
- **Escuela:** resolución **exclusiva** por `Users.SchoolId` del `studentId`; fallo explícito si el alumno carece de escuela o la fila de escuela no existe.  
- **Rama A — campos activos:** una página, tamaño `PageWidthMm` × `PageHeightMm` de settings, capas por campo. **No hay reverso** en esta rama.  
- **Rama B — CarnetQR:** página frente (`RenderCarnetQrFront`) y, si `ShowQr`, página reverso (`RenderCarnetQrBack`) con política, teléfono, emergencia y alergias según flags.

### 5.6 Tabla `id_card_template_fields`

Define posiciones en milímetros y claves de campo (`SchoolName`, `Photo`, `Qr`, etc.). **Cualquier** registro habilitado para la escuela del estudiante activa la rama A del PDF. La pantalla `Generate.cshtml` muestra un **aviso UX** cuando `UsesCustomPdfTemplate` es verdadero, mitigando expectativas de paridad pixel-perfect con el HTML.

### 5.7 Seguridad y control de acceso

| Recurso | Roles observados | Notas |
|---------|------------------|-------|
| `/id-card/settings` | SuperAdmin, admin, director | Configuración multi-escuela para SuperAdmin vía query `schoolId`. |
| `/StudentIdCard/ui/*` y APIs generate/print/list | SuperAdmin, superadmin (controlador a nivel clase) | Listado de estudiantes filtrado por escuela del usuario en `ListJson` cuando aplica. |
| `POST /StudentIdCard/api/scan` | AllowAnonymous + rate limiting | Superficie de exposición controlada; rol efectivo para datos sensibles proviene del JWT. |

**Riesgo residual:** un actor autenticado como SuperAdmin puede invocar generación/PDF para **cualquier** `studentId` conocido; es coherente con el rol pero implica **contención organizacional** (procesos y auditoría) más que técnica por escuela en esa ruta.

### 5.8 Coherencia UI ↔ PDF

| Superficie | Alineación con settings del estudiante | Limitación |
|------------|----------------------------------------|------------|
| Modal vista previa (settings) | Alta para toggles/colores del formulario | No refleja plantilla por campos ni política detallada en reverso. |
| `Generate.cshtml` | Alta en modo CarnetQR (frente/reverso, marca de agua, política, etc.) | HTML/CSS ≠ QuestPDF; layout aproximado. Con plantilla personalizada, aviso de divergencia. |
| PDF CarnetQR | Referencia normativa para impresión profesional | Estudiante sin `SchoolId` no obtiene PDF. |

---

## 6. Resultados de la auditoría

1. **Configuración por escuela del estudiante:** cumplida en `GenerateView` y en `GenerateCardPdfAsync` tras las correcciones.  
2. **Trazabilidad del flujo:** documentada en comentarios en código (PDF y plantillas).  
3. **Separación de responsabilidades:** `StudentIdCardService` permanece libre de maquetación; PDF centraliza render documental.  
4. **Dos layouts PDF:** comportamiento explícito; riesgo de expectativa del usuario mitigado en UI para plantilla personalizada.  
5. **Persistencia:** esquema coherente con requisitos de negocio; `TemplateKey` documentado como no selector de layout múltiple.

---

## 7. Discusión técnica

La corrección adoptada — **anclar escuela al estudiante** en vista de generación y PDF — es el patrón adecuado en sistemas **multi-institución** donde operadores globales actúan sobre alumnos de distintas sedes (cf. modelos de *tenant context* derivado de entidad de negocio). La duplicación de defaults entre PDF (objeto en memoria si no hay fila) y `StudentIdCardGenerateViewModel.ForStudent` introduce **deuda ligera**: una futura fuente única (p. ej. factory o configuración compartida) reduciría divergencia si los defaults cambian.

La vista previa de configuración basada en JavaScript permanece **desacoplada** del motor PDF; ello es aceptable para agilidad de UX, siempre que los administradores entiendan el significado del aviso de plantilla personalizada en la pantalla de generación.

---

## 8. Limitaciones del sistema

1. **Estudiante sin `SchoolId`:** no se genera PDF; la vista puede mostrar “sin escuela asignada” con defaults visuales.  
2. **Plantilla por campos:** una sola cara en PDF; no hay reverso equivalente en HTML detallado.  
3. **`BleedMm`:** persistido en settings; uso en PDF CarnetQR no auditado como crítico en este informe (posible no uso en todas las ramas).  
4. **Impresión desde navegador:** deliberadamente frente único; coherencia operativa depende del flujo “Descargar PDF”.  
5. **Roles:** solo SuperAdmin en UI de generación actual; directores no acceden a esa ruta sin ampliar `[Authorize]`.

---

## 9. Recomendaciones técnicas

1. Centralizar **defaults** de `SchoolIdCardSetting` en un único componente reutilizable por PDF y ViewModel.  
2. Opcional: vista previa en settings que **simule** presencia de campos de plantilla o enlace a documentación.  
3. Pruebas de integración: matriz escuela con/sin settings, con/sin `IdCardTemplateField`, orientación vertical/horizontal.  
4. Revisión periódica de dependencias (QuestPDF, SkiaSharp) y licenciamiento.  
5. Si el negocio requiere que directores generen carnet, **extender roles** en `StudentIdCardController` y reforzar autorización por `studentId` ∈ escuela del usuario.

---

## 10. Conclusiones

El módulo de Carnet Estudiantil presenta una **arquitectura modular clara** y, tras las correcciones auditadas, **alinea la configuración visual y el PDF con la escuela del estudiante**, cerrando la brecha crítica que afectaba a operadores sin escuela en contexto. Persisten diferencias **intrínsecas** entre HTML y QuestPDF y entre las dos ramas de PDF; estas están **parcialmente gestionadas** mediante mensajes en UI. El sistema se considera **coherente para uso operativo** bajo el supuesto de que los flujos oficiales de impresión pasan por el PDF y que los administradores interpretan los avisos de plantilla personalizada.

---

## 11. Referencias técnicas

1. Microsoft Corporation. *ASP.NET Core MVC — Overview.* Documentación oficial.  
2. Microsoft Corporation. *Entity Framework Core — Querying and tracking.* Documentación oficial.  
3. QuestPDF Community. *QuestPDF — Document composition API.* Documentación del proyecto.  
4. Artefactos de código analizados (ruta relativa al proyecto SchoolManager):  
   - `Controllers/StudentIdCardController.cs`  
   - `Controllers/IdCardSettingsController.cs`  
   - `Services/Implementations/StudentIdCardService.cs`  
   - `Services/Implementations/StudentIdCardPdfService.cs`  
   - `ViewModels/StudentIdCardGenerateViewModel.cs`  
   - `Models/SchoolIdCardSetting.cs`  
   - `Models/IdCardTemplateField.cs`  
   - `Views/IdCardSettings/Index.cshtml`  
   - `Views/StudentIdCard/Generate.cshtml`  
   - `Models/SchoolDbContext.cs` (configuración de entidades `school_id_card_settings`, `id_card_template_fields`)

---

*Fin del informe.*
