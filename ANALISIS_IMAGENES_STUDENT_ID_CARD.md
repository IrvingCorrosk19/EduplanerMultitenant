# Análisis técnico: pipeline de imagen — módulo StudentIdCard (UI / PDF / impresión)

**Alcance:** auditoría estática del código en el repositorio SchoolManager. Sin mediciones instrumentales de laboratorio (fotómetro, perfiles ICC incrustados en PDF generado).  
**Restricción:** solo diagnóstico; sin propuestas de remedio.

---

## 1. Resumen ejecutivo

El módulo **StudentIdCard** expone la vista previa en `GET /StudentIdCard/ui/generate/{studentId}` y la descarga de PDF en `GET /StudentIdCard/ui/print/{studentId}`. El flujo **principal de PDF** no compone la foto con QuestPDF/Skia a partir de píxeles crudos en ese path: **captura el HTML ya renderizado** con **PuppeteerSharp (Chromium en modo headless)**, obtiene **PNG por cara** del elemento `#idCardFront` (y opcionalmente la segunda `.idcard-face`), y **embebe esos PNG en un PDF vía QuestPDF** (`Image(bytes).FitArea()`). Una **ruta alternativa** usa solo **SkiaSharp + QuestPDF** (`StudentIdCardPdfService.GenerateCardPdfAsync`) cuando Chromium falla o en recuperación masiva por carnet.

La foto del estudiante **se sube a Cloudinary** sin transformaciones declaradas en la API de subida; en BD se guarda la **`SecureUrl`** devuelta. En la vista HTML la foto se muestra vía **`/File/GetUserPhoto?photoUrl=...`**, que para URLs de Cloudinary hace **`Redirect` a la URL almacenada** (sin cadena de transformación `q_auto`, `f_auto`, etc. añadida por la aplicación en ese flujo).

**Antes de imprimir en papel**, la foto en el PDF generado por el camino HTML→Chromium **ya ha sido rasterizada en el layout web**, donde el recuadro de la foto es **fijo en CSS (~100×100 px lógicos)**. Eso impone un **techo de muestreo espacial** mucho menor que la resolución nativa del archivo en Cloudinary; el PNG de captura y el escalado posterior a tamaño fijo CR80 a **300 DPI** implican **reescalado agresivo** del área facial, con pérdida de detalle en medios tonos que el ojo humano suele interpretar como **“más oscuro” o más sucio**, independientemente de la impresora.

En paralelo, en la **subida** la aplicación puede **recomprimir JPEG con calidad decreciente (88→28)** y **redimensionar con `SKFilterQuality.Medium`** si el archivo supera 2 MB, lo que **reduce rango dinámico y microcontraste** en el activo que luego sirve Cloudinary.

**Conclusión operativa:** el problema “real” en el sentido de **controlabilidad desde el código** está en la **interacción (1) compresión/submuestreo en origen** + **(2) decisión de layout HTML que fija la foto a ~100 CSS px** + **(3) captura Puppeteer + reescalado SkiaSharp a lienzo 300 DPI** + **(4) posible divergencia de renderizado headless (`--disable-gpu`) vs. navegador interactivo**. La impresora física puede **añadir** oscurecimiento, pero el código ya condiciona fuertemente el resultado antes de llegar al driver.

---

## 2. Diagrama del pipeline completo

```mermaid
flowchart TB
  subgraph upload["1. Subida foto estudiante"]
    A[IFormFile JPEG/PNG] --> B[LocalFileStorageService.SaveUserPhotoAsync]
    B --> C{> 2 MB?}
    C -->|Sí| D[SkiaSharp: JPEG calidad 88→28, resize Medium, max lado 2048]
    C -->|No| E[bytes originales o PNG]
    D --> F[CloudinaryService.UploadImageAsync]
    E --> F
    F --> G[ImageUploadParams: carpeta users/photos, sin transforms explícitos]
    G --> H[(BD users.PhotoUrl = SecureUrl)]
  end

  subgraph preview["2. Vista previa GET /StudentIdCard/ui/generate/{id}"]
    H --> I[Generate.cshtml img src = UserPhotoLinks.Href]
    I --> J[GET /File/GetUserPhoto]
    J -->|https res.cloudinary.com| K[302 Redirect a misma URL almacenada]
    J -->|ruta local| L[bytes desde disco]
    K --> M[Chromium del usuario: img en caja 100x100 CSS px, object-fit:cover]
    L --> M
  end

  subgraph pdfPrimary["3. PDF principal GET /StudentIdCard/ui/print/{id}"]
    M --> N[Print → URL interna …/ui/generate/{id}]
    N --> O[PuppeteerSharp Launch headless]
    O --> P[SetViewport: WidthPx+120, HeightPx+120, DeviceScaleFactor 2..4]
    P --> Q["documentElement.style.zoom = ContentScale default 0.96"]
    Q --> R[GoToAsync generate URL + cookies sesión]
    R --> S[ScreenshotDataAsync #idCardFront type PNG]
    S --> T[SkiaSharp: decode PNG, Resize High si ≠ target 638×1011]
    T --> U[QuestPDF: página mm CR80, Image(png).FitArea]
    U --> V[bytes PDF al cliente]
  end

  subgraph pdfFallback["4. PDF nativo si Chromium falla"]
    H --> W[HttpClient GetByteArrayAsync PhotoUrl directo]
    W --> X[StudentIdCardImageService.DrawPhoto: Skia decode + FitRect + High filter]
    X --> Y[QuestPDF embebe PNG frente/reverso Skia]
    Y --> V
  end

  subgraph print["5. Impresión física"]
    V --> Z[Visor PDF OS + colormanagement + driver PCL/PS/GDI]
    Z --> AA[Papel]
  end
```

**Referencias de rutas HTTP:** `StudentIdCardController.GenerateView` (`ui/generate/{studentId}`), `StudentIdCardController.Print` (`ui/print/{studentId}`).

---

## 3. Hallazgos técnicos (cadena causal)

### 3.1 Origen y almacenamiento (Cloudinary + preproceso local)

| Paso | Evidencia en código | Implicación para luminosidad / rango |
|------|---------------------|--------------------------------------|
| Validación MIME | Solo `image/jpeg` y `image/png` (`LocalFileStorageService`) | Sin WebP en subida de producto; JPEG introduce bloques DCT ya en origen si se comprime. |
| Umbral 2 MB | Si excede, `CompressImageToMaxBytes` | Bucle JPEG calidades **88, 78, … hasta 28**; redimensiones con **`SKFilterQuality.Medium`**; puede **aplastar highlights** y generar **banding** en pieles. |
| Subida Cloudinary | `ImageUploadParams`: `Folder`, `UseFilename`, `UniqueFilename` — **sin** `EagerTransforms`, **sin** `Quality` forzado en upload | El archivo que ve el CDN es el **ya procesado por Skia** en el servidor, no una versión “master” lossless separada. |
| URL almacenada | `UploadResult.SecureUrl` guardada en BD | Consumo posterior por **URL directa** al delivery `res.cloudinary.com`. |
| `GetImageUrl` con `q_auto` / `f_auto` | Existe en `CloudinaryService` pero **no hay llamadas** desde el resto del proyecto (búsqueda global) | **No aplica** a las fotos de carnet en el flujo actual; **no** es la causa vía `q_auto` en runtime. |

### 3.2 Consumo en preview (HTML/CSS)

| Elemento | Evidencia | Implicación |
|----------|-----------|-------------|
| URL de la foto | `UserPhotoLinks.Href` → `/File/GetUserPhoto?photoUrl=…` (`UserPhotoLinks.cs`) | El `<img>` no apunta al CDN en el HTML; el navegador sigue **302** a Cloudinary. |
| CDN | `FileController.GetUserPhoto`: si host es `res.cloudinary.com` → **`Redirect(trimmed)`** | Misma URL que en BD; **sin** transformaciones añadidas por la app. |
| Tamaño visual | `.idcard-photo-inner { width: 100px; height: 100px; }` y `img { object-fit: cover; }` (`Generate.cshtml`) | El motor del navegador **escala y recorta** la imagen de alta resolución a **~10 kpx²** lógicos (× DPR del monitor para pintura en pantalla). |
| Fondo del marco | `background: rgba(0,0,0,0.04)` | Contribución marginal; **no** explica oscurecimiento fuerte de la foto en sí. |
| Filtros CSS en la foto | No hay `filter`, `opacity` ni `mix-blend-mode` en el `img` de la foto | **No** hay oscurecimiento declarativo en CSS sobre la foto. |
| Marca de agua | `.idcard-watermark img { opacity: 0.14 }`, `z-index: 0` vs `.idcard-z1` para foto | La foto está **por encima** del watermark en el apilamiento; el watermark **no** se superpone a la cara en el frente. |

### 3.3 Captura HTML → imagen (PuppeteerSharp + SkiaSharp)

| Paso | Evidencia | Implicación |
|------|-----------|-------------|
| Motor | `PuppeteerSharp` + `ScreenshotType.Png` en `ElementScreenshotOptions` (`StudentIdCardHtmlCaptureService.Capture`) | PNG **sin** parámetro de calidad JPEG en este paso; compresión PNG lossless pero **ya sobre píxeles premezclados** por Chromium. |
| GPU | `BuildLaunchArgs` incluye **`--disable-gpu`** siempre | Rasterizado **software** en headless; puede **diferir** ligeramente del Chrome interactivo del usuario (gamma/contraste perceptual). |
| Zoom | `document.documentElement.style.zoom = ContentScale` por defecto **0.96** (`appsettings.json` `StudentIdCardPdf:ContentScale`) | **Reduce** ligeramente el layout antes de capturar; interacción con DPI y bounding box del elemento. |
| DPR | `DeviceScaleFactor` default **2**, `MaxDeviceScaleFactor` **4**; perfil `CardPrinter` puede **subir DPR** según `BoundingBoxAsync` del `#idCardFront` | Aumenta resolución del screenshot del **elemento completo**, pero la **foto sigue siendo solo una fracción** del lienzo; el **techo** sigue anclado al diseño CSS (~100 px lógicos de alto/ancho para el hueco de foto en layout vertical institucional). |
| Post-proceso | Tras captura: `SKBitmap.Decode` → si tamaño ≠ `PortraitWidthPx`×`PortraitHeightPx` (**638×1011** @ convención del proyecto) → `Resize(..., SKFilterQuality.High)` → `Encode PNG 100` | **Segundo** escalado del **carnet entero** (incluida la foto ya submuestreada > **tercer** escalado perceptual desde archivo fuente si se cuenta el del `<img>`). |

**Cálculo de resolución de página fija:** `IdCardPhysicalDimensions.RenderDpi = 300f`; retrato **638×1011 px** (`PortraitWidthPx` / `PortraitHeightPx` derivados de mm CR80).

### 3.4 Generación del PDF (QuestPDF)

| Paso | Evidencia | Implicación |
|------|-----------|-------------|
| Inserción | `BuildPdfFromFaceImages`: `p.Content().Image(frontImg).FitArea()` (`StudentIdCardHtmlCaptureService`) | QuestPDF **trata el PNG como imagen raster** en página mm. La biblioteca puede **re-muestrear** al colocar en el box PDF; no hay en este código **incrustación explícita de perfil ICC** ni conversión CMYK declarada. |
| Ruta nativa | `StudentIdCardPdfService`: PNG generados por `StudentIdCardImageService` (Skia) | Ahí la foto usa **`DrawPhoto` → `BmpDraw` con `SKFilterQuality.High`** sobre bytes descargados de la URL (`GetUserPhotoBytesAsync`); **sí** usa resolución completa del archivo hasta el rectángulo mm de la foto en el lienzo 300 DPI — **distinto pipeline** al HTML. |

### 3.5 Impresión final

El código **no** controla el driver. Factores externos típicos: modo “foto” vs “documento”, **mejora de texto**, ahorro de tinta, simulación de sobrecubierta, falta de gestión de color del visor PDF. Solo pueden **amplificar** o **enmascarar** el tono ya fijado en el PDF.

---

## 4. Puntos donde se pierde calidad o brillo (orden lógico)

1. **Subida (servidor, SkiaSharp JPEG)** — Pérdida **irreversible** si aplica `CompressImageToMaxBytes` (calidades bajas + resize Medium).  
2. **Entrega Cloudinary** — Sirve el **mismo** binario subido; sin `q_auto` en este flujo.  
3. **Render HTML del carnet** — La foto se pinta en **~100×100 CSS px**; el motor del navegador/Chromium **submuestrea** el bitmap de alta resolución a ese destino.  
4. **Captura Puppeteer** — Screenshot del elemento; la información de alta frecuencia de la piel/pelo **ya no existe** más allá del muestreo efectivo del layout.  
5. **Resize SkiaSharp a 638×1011** — Reescala **toda la cara** del carnet; interpola de nuevo la región de la foto.  
6. **QuestPDF / visor / impresora** — Posible segunda interpretación de color y gama al rasterizar para el dispositivo de salida.

**Doble compresión:** JPEG agresivo en subida + **ninguna** recompresión JPEG adicional obligatoria en el PNG de cara; el PNG es voluminoso pero **sí** hay **doble escalado espacial** (HTML → screenshot → resize lienzo fijo).

**Perfiles de color:** No hay paso en código que asigne **ICC sRGB** al PDF; el comportamiento depende de **QuestPDF / Skia / Chromium** por defecto y del RIP de la impresora.

---

## 5. Comparación: imagen original vs preview vs PDF

| Etapa | Resolución efectiva de la “cara” en la foto | Tonos / contraste |
|-------|-----------------------------------------------|-------------------|
| **Archivo en Cloudinary** (tras subida) | Hasta **2048 px** de lado máximo si hubo compresión; o nativo si &lt; 2 MB | Puede estar ya **JPEG degradado** (calidad baja si era archivo grande) |
| **Preview en navegador** | **~100×100 px lógicos** × DPR del monitor (p. ej. 200×200 físicos en pantalla Retina) | Escalado por **GPU del cliente**; puede verse **más nítido o más claro** que el PDF por **perfil de pantalla** y por no estar aún embebido en CMYK/paper simulation |
| **PDF vía Chromium** (ruta principal) | La foto ocupa fracción del PNG **638×1011**; equivalente a **submuestreo severo** respecto a 300 DPI en el área física de la foto en el plástico | **Pérdida de medios tonos** → apariencia **más oscura/sucia**; headless **sin GPU** puede variar respecto al preview del mismo HTML en Chrome de escritorio |
| **PDF nativo Skia** (fallback) | Escala desde **bytes completos** al rectángulo de foto en puntos/mm del lienzo 300 DPI | Suele **preservar mejor** luminancia relativa que el camino HTML si el archivo de origen es bueno |

**Importante:** el **mismo** HTML alimenta preview y Chromium; si el preview “se ve bien” y el PDF “sale oscuro”, la diferencia puede venir de **DPR distinto**, **zoom 0.96**, **headless vs ventana**, o de la **fase de impresión**, no de Cloudinary `q_auto` (no usado aquí).

---

## 6. Conclusión: dónde está el problema REAL

En el sentido estrictamente **atribuible al diseño implementado**:

- **Causa estructural principal (ruta de PDF por defecto):** la foto se **integra en un layout web con resolución intrínseca muy baja en el hueco de la cara (~100 CSS px)** y luego ese resultado se **trata como fuente de verdad** para una página física a **300 DPI**. Eso es una **pérdida de información de luminancia de alta frecuencia** antes del PDF; no es recuperable en impresión.

- **Causa de activo degradado (independiente del PDF):** la pipeline de subida puede entregar a Cloudinary un JPEG **ya fuertemente comprimido** (`CompressImageToMaxBytes`), lo que **reduce contraste aparente** y puede **oscurecer tonos medios** incluso antes de cualquier render de carnet.

- **No sustentado por el código revisado:** que Cloudinary aplique **`q_auto` / `f_auto`** a las fotos de estudiantes en este flujo (el método `GetImageUrl` con esas flags **no se invoca** en el repositorio). Tampoco hay **`e_brightness`** ni transforms en `UploadImageAsync`.

- **Impresora:** factor **externo** no modelado en código; puede añadir oscurecimiento pero **no** es el primer eslabón donde se destruye detalle tonal.

---

## 7. Nivel de impacto

| Hallazgo | Impacto |
|----------|---------|
| Foto anclada a ~100×100 CSS px + captura de elemento + resize a CR80@300dpi | **Crítico** para fidelidad tonal y detalle en **ruta Puppeteer** (comportamiento por defecto en `Print`). |
| Compresión JPEG agresiva en subida para archivos &gt; 2 MB | **Alto** en calidad del activo en Cloudinary para todos los consumidores (carnet, listados, etc.). |
| `--disable-gpu` en Chromium headless | **Medio** (variación perceptual vs preview en GPU; depende de plataforma). |
| Ausencia de gestión explícita de color (ICC / CMYK) en PDF | **Medio** en coincidencia preview↔papel; **alto** en entornos con perfiles de impresión estrictos. |
| `ContentScale` 0.96 | **Bajo** directo sobre luminosidad; **medio** en interacción con mediciones de caja y DPR. |

---

## Anexo: referencias de archivo (trazabilidad)

- Controlador UI/PDF: `Controllers/StudentIdCardController.cs` (`GenerateView`, `Print`, `PrintBulk`).
- Vista previa HTML: `Views/StudentIdCard/Generate.cshtml` (estilos `.idcard-photo-inner`, `#idCardFront`).
- Captura: `Services/Implementations/StudentIdCardHtmlCaptureService.cs`.
- Opciones captura/PDF: `Services/Implementations/StudentIdCardPdfPrintOptions.cs`, sección `appsettings.json` → `StudentIdCardPdf`.
- PDF nativo / descarga bytes: `Services/Implementations/StudentIdCardPdfService.cs`, `Services/Implementations/StudentIdCardImageService.cs` (`DrawPhoto`, `BmpDraw`).
- Dimensiones físicas: `Services/IdCardPhysicalDimensions.cs`.
- Subida y lectura foto: `Services/Implementations/LocalFileStorageService.cs`, `Services/Implementations/CloudinaryService.cs`.
- Enlace foto en HTML: `Helpers/UserPhotoLinks.cs`.
- Entrega HTTP foto: `Controllers/FileController.cs` (`GetUserPhoto`).

---

*Fin del informe — diagnóstico únicamente.*
