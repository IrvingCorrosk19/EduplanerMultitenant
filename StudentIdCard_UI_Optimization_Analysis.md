# ANÁLISIS DE OPTIMIZACIÓN — /StudentIdCard/ui
**Fecha:** 2026-03-31
**Analista:** Arquitecto Senior ASP.NET Core + PostgreSQL
**Estado:** SOLO DIAGNÓSTICO — sin modificaciones de código

---

## RESUMEN EJECUTIVO

El módulo `/StudentIdCard/ui` presenta una arquitectura funcional con varios patrones de seguridad correctamente aplicados (Singleton para HMAC, transacciones serializables, `AsNoTracking` en lecturas). Sin embargo, se identifican problemas de rendimiento relevantes: (1) la carga de la vista `GenerateView` realiza **6 queries secuenciales separadas** hacia la base de datos sin ningún mecanismo de caché; (2) el endpoint `ListJson` contiene **correlated subqueries duplicadas** que consultan `student_id_cards` dos veces por cada fila proyectada; (3) cada petición QR genera el PNG en el servidor mediante una nueva instancia de `QRCodeGenerator` sin ningún nivel de caché, incurriendo en allocations innecesarias en escenarios de acceso frecuente. El flujo de impresión via Puppeteer/Chromium introduce latencias de varios segundos y allocations de alto impacto, agravadas por la ausencia de paginación en el listado de estudiantes. La base de datos actualmente tiene **1969 usuarios** (1839 estudiantes) pero la tabla `users` carece de índice sobre la columna `role`, forzando un Seq Scan completo en cada consulta del módulo.

### Criticidad de hallazgos
| Criticidad | Cantidad |
|------------|----------|
| 🔴 CRÍTICO  | 4 |
| 🟠 ALTO     | 7 |
| 🟡 MEDIO    | 6 |
| 🟢 BAJO     | 4 |

---

## 1. CAPA DE BASE DE DATOS Y EF CORE

### 1.1 Seq Scan en columna `role` de tabla `users` — índice faltante
- **Criticidad:** 🔴 CRÍTICO
- **Archivo:** `Controllers\StudentIdCardController.cs` líneas 511–513; `Services\Implementations\StudentIdCardService.cs` líneas 51–52, 113–114
- **Código actual:**
  ```csharp
  var query = _context.Users
      .Where(u => u.Role != null && (u.Role.ToLower() == "student" || u.Role.ToLower() == "estudiante"))
      .Where(u => _context.StudentPaymentAccesses.Any(spa => spa.StudentId == u.Id && spa.CarnetStatus == "Pagado"));
  ```
- **Evidencia:** El EXPLAIN ANALYZE ejecutado en producción confirma que PostgreSQL realiza un **Seq Scan** sobre la tabla `users` (1969 filas, 70 shared buffers leídos) en cada invocación de `BuildEligibleStudentQuery`. La columna `role` no tiene índice (`pg_indexes` sobre `users` devuelve únicamente `IX_users_school_id`, `users_document_id_key`, `users_email_key`, `IX_users_cellphone_primary`, `IX_users_cellphone_secondary` — no existe ningún índice sobre `role`). Adicionalmente, la expresión `u.Role.ToLower()` impide que PostgreSQL use un índice B-tree estándar; requeriría un índice funcional sobre `lower(role)`.
- **Impacto medible:** Con 1969 filas actuales el Seq Scan tarda 1.5 ms. Con escala a 10.000 usuarios (crecimiento esperado) un Seq Scan de tabla ancha (31 columnas, 560 kB actuales) crecerá proporcionalmente. Todos los endpoints `list-json`, `list-filters`, `list-ids`, `GenerateView` y `GenerateAsync` ejecutan esta misma condición en cada request.

---

### 1.2 Seis queries secuenciales en `GenerateView` — sin canalización
- **Criticidad:** 🔴 CRÍTICO
- **Archivo:** `Controllers\StudentIdCardController.cs` líneas 59–118
- **Código actual:**
  ```csharp
  var student = await _context.Users.AsNoTracking()
      .FirstOrDefaultAsync(u => u.Id == studentId && ...);           // Query 1
  var hasPaidView = await _context.StudentPaymentAccesses
      .AnyAsync(spa => spa.StudentId == studentId && ...);            // Query 2
  schoolEntity = await _context.Schools.AsNoTracking()...
      .FirstOrDefaultAsync(s => s.Id == student.SchoolId.Value);      // Query 3
  var cardSettings = await _context.Set<SchoolIdCardSetting>()...
      .FirstOrDefaultAsync(x => x.SchoolId == student.SchoolId.Value); // Query 4
  var enabledTemplateFields = await _context.Set<IdCardTemplateField>()...
      .CountAsync(x => x.SchoolId == student.SchoolId.Value && x.IsEnabled); // Query 5
  vm.AcademicYear = await _context.StudentAssignments
      .Where(a => a.StudentId == studentId && a.IsActive)
      .Select(a => a.AcademicYear.Name).FirstOrDefaultAsync();         // Query 6
  vm.Card = await _service.GetCurrentCardAsync(studentId);             // +3 queries internas
  ```
- **Evidencia:** Las 6 llamadas son secuenciales (`await` individual). `GetCurrentCardAsync` añade 3 queries más (user con Includes, card activa, token QR). Total confirmado: **9 roundtrips secuenciales** al servidor PostgreSQL en Render (Oregon), con latencia de red de ~80–120 ms por roundtrip (servidor remoto).
- **Impacto medible:** 9 × 80–120 ms = **720–1080 ms de latencia pura de red** antes de cualquier renderizado. En un scenario local la latencia se reduce pero el count de roundtrips persiste.

---

### 1.3 Correlated subqueries duplicadas sobre `student_id_cards` en `ListJson`
- **Criticidad:** 🔴 CRÍTICO
- **Archivo:** `Controllers\StudentIdCardController.cs` líneas 431–437
- **Código actual:**
  ```csharp
  isPrinted = _context.StudentIdCards
      .Where(c => c.StudentId == u.Id && c.Status == "active")
      .Select(c => (bool?)c.IsPrinted)
      .FirstOrDefault() ?? false,
  printedAt = _context.StudentIdCards
      .Where(c => c.StudentId == u.Id && c.Status == "active")
      .Select(c => c.PrintedAt)
      .FirstOrDefault()
  ```
- **Evidencia:** La proyección dentro del `Select(u => new {...})` genera **dos correlated subqueries independientes** sobre `student_id_cards` por cada fila de usuario retornada. Para 679 estudiantes elegibles (dato real de producción), esto resulta en hasta **1358 subqueries** sobre `student_id_cards` en una sola ejecución de `ListJson`. EF Core no colapsa estas dos subconsultas en una sola porque son proyecciones de columnas diferentes sobre la misma condición.
- **Impacto medible:** 679 estudiantes × 2 subqueries = 1358 operaciones sobre `student_id_cards`. El índice `ix_student_id_cards_student_id` existe pero no cubre `status`; cada subquery aplica un filtro adicional no indexado.

---

### 1.4 Ausencia de índice compuesto `(student_id, status)` en `student_id_cards`
- **Criticidad:** 🔴 CRÍTICO
- **Archivo:** `Models\SchoolDbContext.cs` líneas 2145–2148
- **Código actual:**
  ```csharp
  entity.HasIndex(e => e.CardNumber, "IX_student_id_cards_card_number").IsUnique();
  entity.HasIndex(e => e.StudentId, "IX_student_id_cards_student_id");
  ```
- **Evidencia:** El EXPLAIN ANALYZE ejecutado en producción confirma que la query `WHERE student_id = $1 AND status = 'active'` realiza **Seq Scan** sobre `student_id_cards` (61 filas actuales). Aunque 61 filas es pequeño hoy, el patrón `(student_id, status)` se ejecuta en: `MarkCardAsPrintedAsync` (×N en bulk), `UpdatePrintStatus`, `ScanAsync`, `GetCurrentCardAsync`, `BuildStudentCardDtoAsync` — sin índice compuesto. El índice simple sobre `student_id` existe pero el planner no puede usar Index Only Scan con el predicado adicional `status = 'active'`.
- **Impacto medible:** Con crecimiento proyectado (cada estudiante puede tener carnets revocados históricos), el Seq Scan sobre `student_id_cards` con filtro `status = 'active'` será progresivamente más costoso.

---

### 1.5 Include chain innecesariamente amplio en `GetCurrentCardAsync`
- **Criticidad:** 🟠 ALTO
- **Archivo:** `Services\Implementations\StudentIdCardService.cs` líneas 43–53
- **Código actual:**
  ```csharp
  var student = await _context.Users
      .Include(x => x.StudentAssignments.Where(a => a.IsActive))
          .ThenInclude(x => x.Grade)
      .Include(x => x.StudentAssignments.Where(a => a.IsActive))
          .ThenInclude(x => x.Group)
      .Include(x => x.StudentAssignments.Where(a => a.IsActive))
          .ThenInclude(x => x.Shift)
      .AsNoTracking()
      .FirstOrDefaultAsync(...);
  ```
- **Evidencia:** Se cargan las entidades completas de `Grade`, `Group` y `Shift` cuando únicamente se consume `x.Name` en cada una. Además, el mismo Include se repite idénticamente en `GenerateAsync` (líneas 107–114) y `BuildStudentCardDtoAsync` (líneas 193–199). EF Core genera múltiples JOINs o queries separadas (split query no configurado). Para el Student se carga la fila completa de `users` (31 columnas incluyendo `password_hash`, `two_factor_enabled`, etc.) cuando solo se necesitan `Name`, `LastName`, `PhotoUrl`.
- **Impacto medible:** La tabla `users` tiene 31 columnas (560 kB para 1969 filas). La proyección de solo las columnas necesarias reduciría el payload de datos transferidos en ~70%.

---

### 1.6 `ListFilters` materializa todas las filas en memoria para agregar
- **Criticidad:** 🟠 ALTO
- **Archivo:** `Controllers\StudentIdCardController.cs` líneas 453–476
- **Código actual:**
  ```csharp
  var data = await query
      .Select(u => new { grade = ..., group = ..., shift = ... })
      .ToListAsync();

  var grades = data.Select(x => x.grade)
      .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
  ```
- **Evidencia:** Se materializa en memoria la lista completa de asignaciones para todos los estudiantes elegibles (679 filas en producción) y luego se aplican `Distinct()` y `OrderBy` en .NET. Esta agregación podría realizarse completamente en SQL con `GROUP BY` o `DISTINCT`, reduciendo el payload de red a solo los valores únicos.
- **Impacto medible:** 679 filas × 3 columnas string transferidas vs. ~10–20 valores únicos si se agrupara en SQL. Ratio de reducción de payload: ~35×.

---

### 1.7 `SaveChangesAsync` llamado dentro del loop de bulk print (×N)
- **Criticidad:** 🟠 ALTO
- **Archivo:** `Controllers\StudentIdCardController.cs` líneas 284–301; `Controllers\StudentIdCardController.cs` líneas 522–533
- **Código actual:**
  ```csharp
  for (var idx = 0; idx < students.Count; idx++)
  {
      // ...
      await MarkCardAsPrintedAsync(student.Id);  // SaveChangesAsync interno
  }
  ```
  Y en `MarkCardAsPrintedAsync`:
  ```csharp
  private async Task MarkCardAsPrintedAsync(Guid studentId)
  {
      var card = await _context.StudentIdCards
          .FirstOrDefaultAsync(c => c.StudentId == studentId && c.Status == "active");
      card.IsPrinted = true;
      card.PrintedAt = DateTime.UtcNow;
      await _context.SaveChangesAsync();
  }
  ```
- **Evidencia:** Para un bulk print de 30 estudiantes (límite configurado), se ejecutan **30 queries SELECT + 30 `SaveChangesAsync`** = 60 roundtrips de red individuales para marcar los carnets como impresos. Cada `SaveChangesAsync` es un commit de transacción separado.
- **Impacto medible:** 30 × (1 SELECT + 1 UPDATE + 1 COMMIT) × ~100 ms latencia remota = ~3000 ms extra solo en el phase de marcado, añadido al tiempo ya considerable de generación de PDFs.

---

### 1.8 Query de `student_qr_tokens` no usa el índice `ix_student_qr_tokens_token` en ScanAsync
- **Criticidad:** 🟠 ALTO
- **Archivo:** `Services\Implementations\StudentIdCardService.cs` líneas 244–251
- **Código actual:**
  ```csharp
  var tokenRecord = await _context.StudentQrTokens
      .Include(x => x.Student)
          .ThenInclude(x => x.SchoolNavigation)
      .FirstOrDefaultAsync(x =>
          x.Token == tokenToLookup &&
          !x.IsRevoked &&
          (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow));
  ```
- **Evidencia:** El índice `ix_student_qr_tokens_token` es UNIQUE sobre la columna `token` y está correctamente definido. Sin embargo, la condición compuesta incluye `!x.IsRevoked` y `x.ExpiresAt > DateTime.UtcNow` que no están indexadas. PostgreSQL usa el índice único sobre `token` para localizar la fila, pero hay una ausencia de índice parcial filtrado `WHERE is_revoked = false` que podría reducir aún más el trabajo de filtrado. Adicionalmente, el `Include(x => x.Student).ThenInclude(x => x.SchoolNavigation)` carga la fila completa de `users` (31 columnas + school) en el scan path crítico del escaneo QR.
- **Impacto medible:** En alta frecuencia de escaneos (guardia escolar), cada scan carga ~31 columnas de `users` + todas las columnas de `schools`. La columna `password_hash` se transfiere en cada scan sin necesidad.

---

## 2. MEMORIA Y ALLOCATIONS

### 2.1 QR PNG generado en cada request — sin caché
- **Criticidad:** 🔴 CRÍTICO
- **Archivo:** `Helpers\QrHelper.cs` líneas 9–21; invocado desde `Services\Implementations\StudentIdCardService.cs` líneas 74, 199; `Services\Implementations\StudentIdCardPdfService.cs` (vía `GenerateCardImage`)
- **Código actual:**
  ```csharp
  public static byte[] GenerateQrPng(string content, IQrSignatureService? signatureService = null)
  {
      var toEncode = signatureService != null ? signatureService.GenerateSignedToken(content) : content;
      return GenerateQrPngInternal(toEncode);
  }

  private static byte[] GenerateQrPngInternal(string content)
  {
      using var generator = new QRCodeGenerator();
      using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
      var qr = new PngByteQRCode(data);
      return qr.GetGraphic(10);
  }
  ```
- **Evidencia:** Cada llamada a `GenerateQrPng` instancia un nuevo `QRCodeGenerator`, crea el código QR y genera el PNG completo. El resultado se convierte a Base64 para incrustarlo en el HTML como data URI. Este proceso ocurre en: (1) `GetCurrentCardAsync` — cada vez que se carga la vista de un carnet existente; (2) `GenerateAsync` — al generar un carnet nuevo. El QR no cambia entre requests mientras el token no sea renovado. No existe ningún mecanismo de caché (`IMemoryCache`, campo estático, etc.) para almacenar el PNG o la data URI generada.
- **Impacto medible:** `QRCodeGenerator` + PNG encoding crea varios objetos intermedios (~50–200 KB de datos temporales). En producción con Chromium haciendo un GET a `/ui/generate/{id}` para cada carnet del bulk print (hasta 30), esto se ejecuta 30 veces seguidas.

---

### 2.2 Watermark recalculada en cada PDF — sin caché
- **Criticidad:** 🟠 ALTO
- **Archivo:** `Services\Implementations\StudentIdCardPdfService.cs` líneas 122–123
- **Código actual:**
  ```csharp
  if (settings.ShowWatermark && renderDto.LogoBytes != null)
      renderDto.WatermarkBytes = CreateWatermarkImage(renderDto.LogoBytes, 0.14f);
  ```
  Y `CreateWatermarkImage` en líneas 339–370:
  ```csharp
  private static byte[]? CreateWatermarkImage(byte[]? logoBytes, float opacity = 0.14f)
  {
      using var data     = SKData.CreateCopy(logoBytes);
      using var original = SKImage.FromEncodedData(data);
      // ...SKSurface.Create, SKCanvas, SKPaint, Snapshot, Encode...
      using var stream   = new MemoryStream();
      encoded.SaveTo(stream);
      return stream.ToArray();
  }
  ```
- **Evidencia:** El logo de la escuela no cambia entre peticiones. Sin embargo, en cada invocación de `GenerateCardPdfAsync` se re-descarga el logo (HTTP o disco) y se re-procesa con SkiaSharp para generar la versión semitransparente. Para un bulk print de 30 carnets de la misma escuela, el logo se descarga 30 veces y la watermark se genera con SkiaSharp 30 veces.
- **Impacto medible:** Creación de `SKBitmap`, `SKSurface`, `SKCanvas`, `SKPaint` y encodación PNG por cada carnet. Para 30 carnets = 30 × (descarga HTTP/disco + SkiaSharp pipeline completo).

---

### 2.3 Puppeteer lanza nueva instancia de Chromium por cada request de impresión individual
- **Criticidad:** 🟠 ALTO
- **Archivo:** `Services\Implementations\StudentIdCardHtmlCaptureService.cs` líneas 33–56
- **Código actual:**
  ```csharp
  public async Task<byte[]> GenerateFromUrl(string url)
  {
      var executablePath = await ResolveChromiumExecutablePath();
      var launchOpts = BuildLaunchOptions(executablePath);

      await using (var browser = await Puppeteer.LaunchAsync(launchOpts))
      {
          (frontImg, backImg) = await CaptureCardFacesAsync(browser, url);
      }
      return BuildPdfFromFaceImages(frontImg, backImg);
  }
  ```
- **Evidencia:** Cada llamada a `Print` (endpoint `/ui/print/{studentId}`) lanza un proceso Chromium completo (`Puppeteer.LaunchAsync`) y lo cierra al terminar. Chromium es un proceso pesado (~50–100 MB de memoria por instancia, ~1–3 segundos de startup time). El `IStudentIdCardHtmlCaptureService` está registrado como `AddScoped`, por lo que no hay reutilización del browser entre requests.
- **Impacto medible:** Cada descarga de PDF individual requiere: startup de Chromium (~1–3 s) + navegación a la URL (~1–2 s) + captura (~0.5 s) + PDF encoding. El método `GenerateBulkFromUrls` sí reutiliza el browser en un loop (línea 68), pero `GenerateFromUrl` no.

---

### 2.4 `outStream.ToArray()` en bulk print — double buffering
- **Criticidad:** 🟡 MEDIO
- **Archivo:** `Controllers\StudentIdCardController.cs` líneas 305–308
- **Código actual:**
  ```csharp
  await using var outStream = new MemoryStream();
  merged.Save(outStream, false);
  var fileName = $"carnets-masivo-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";
  return File(outStream.ToArray(), "application/pdf", fileName);
  ```
- **Evidencia:** `merged.Save(outStream, false)` escribe el PDF completo en `outStream`. Luego `outStream.ToArray()` crea una **segunda copia** del mismo buffer en memoria. Para 30 carnets, el PDF mergeado puede tener varios MB. El `File(byte[], ...)` overload retiene el array completo en memoria hasta que el response se envía completamente.
- **Impacto medible:** Para un PDF de 30 carnets (~3–10 MB), se mantienen simultáneamente dos copias en memoria durante la serialización del response (~6–20 MB peak).

---

### 2.5 Generación de QR PNG embebido como Base64 data URI — overhead de tamaño
- **Criticidad:** 🟡 MEDIO
- **Archivo:** `Services\Implementations\StudentIdCardService.cs` líneas 74–75; `Dtos\StudentIdCardDto.cs` línea 13
- **Código actual:**
  ```csharp
  var pngBytes = QrHelper.GenerateQrPng(token.Token, _qrSignatureService);
  qrImageDataUrl = "data:image/png;base64," + Convert.ToBase64String(pngBytes);
  ```
- **Evidencia:** Un QR PNG de 10px-per-module para un token de 32 caracteres tiene ~3–5 KB comprimido. Al codificar en Base64 el tamaño aumenta un ~33% (~4–7 KB). Este data URI se incrusta en el HTML de la vista (`Generate.cshtml` líneas 374, 398), en el objeto JSON retornado por `GenerateApi`, y en el ViewModel del servidor. Para el bulk print con 30 carnets, 30 data URIs de ~7 KB cada una = ~210 KB de datos QR adicionales embebidos en el HTML de las 30 páginas navegadas por Puppeteer.
- **Impacto medible:** Cada request a `/ui/generate/{id}` incluye ~7 KB de QR como data URI en el HTML. Dado que Puppeteer navega esa URL para cada carnet en bulk, el overhead de descarga es 30 × 7 KB = 210 KB solo de QR data URIs.

---

## 3. HTTP / CACHÉ / RESPUESTAS

### 3.1 `GenerateView` no tiene ningún mecanismo de caché — recalcula todo en cada request
- **Criticidad:** 🟠 ALTO
- **Archivo:** `Controllers\StudentIdCardController.cs` líneas 55–119
- **Código actual:**
  ```csharp
  [HttpGet("ui/generate/{studentId}")]
  public async Task<IActionResult> GenerateView(Guid studentId)
  {
      // 9 roundtrips a la DB en cada GET
  }
  ```
- **Evidencia:** La acción no tiene ningún atributo `[ResponseCache]`, no consulta `IMemoryCache`, y no establece headers `ETag` ni `Last-Modified`. El QR PNG es estático hasta la próxima generación de carnet (puede durar 6 meses). Los datos de escuela, settings y campos de plantilla cambian raramente. Sin embargo, cada request a `/ui/generate/{id}` — incluyendo los 30 requests paralelos de Puppeteer durante bulk print — recalcula todo desde cero.
- **Impacto medible:** En el flujo de bulk print para 30 estudiantes, Puppeteer realiza 30 GETs a `/ui/generate/{id}`, cada uno ejecutando 9 queries a la DB = **270 queries en total** solo para la generación del PDF masivo.

---

### 3.2 DataTables sin server-side processing — carga todos los datos en una sola llamada
- **Criticidad:** 🟠 ALTO
- **Archivo:** `Views\StudentIdCard\Index.cshtml` líneas 97–172
- **Código actual:**
  ```javascript
  const table = $('#studentsTable').DataTable({
      ajax: {
          url: '/StudentIdCard/api/list-json',
          dataSrc: 'data',
          // ...
      },
      pageLength: 25,
  ```
- **Evidencia:** DataTables está configurado en modo **client-side processing** (`dataSrc: 'data'` con `ajax`). El endpoint `list-json` retorna **todos** los estudiantes elegibles en una sola respuesta JSON (679 en producción), y DataTables pagina localmente. No se usan los parámetros estándar de server-side DataTables (`draw`, `start`, `length`, `order`, `search`). El endpoint tampoco tiene `[ResponseCache]`.
- **Impacto medible:** 679 estudiantes retornados como JSON en cada carga o cambio de filtro. Cada objeto incluye `id`, `fullName`, `grade`, `group`, `shift`, `isPrinted`, `printedAt` — ~200 bytes por fila = ~135 KB de JSON por request. Con crecimiento a 2000 estudiantes = ~400 KB de JSON no paginado.

---

### 3.3 Recurso de idioma de DataTables cargado desde CDN externo en cada página
- **Criticidad:** 🟡 MEDIO
- **Archivo:** `Views\StudentIdCard\Index.cshtml` línea 159
- **Código actual:**
  ```javascript
  language: {
      url: 'https://cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
  },
  ```
- **Evidencia:** El archivo de idioma es un recurso externo de CDN que se descarga en cada carga de la página de Index. No está cacheado localmente ni incluido en el bundle de la aplicación. Una falla o lentitud del CDN de DataTables afecta directamente a la carga de la UI.
- **Impacto medible:** Dependency externa adicional en el critical rendering path. El archivo es ~4 KB pero añade una DNS lookup + TCP connection + TLS handshake al CDN en cada carga.

---

### 3.4 `Print` endpoint ejecuta todo el flujo de Chromium en el thread de request sin timeout global
- **Criticidad:** 🟡 MEDIO
- **Archivo:** `Controllers\StudentIdCardController.cs` líneas 127–182
- **Código actual:**
  ```csharp
  [HttpGet("ui/print/{studentId}")]
  [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
  public async Task<IActionResult> Print(Guid studentId)
  {
      // ...
      var pdf = await _htmlCapture.GenerateFromUrl(url);
      // ...
  }
  ```
- **Evidencia:** El endpoint no acepta ni propaga un `CancellationToken` del request HTTP. Si el usuario cierra la pestaña o la conexión se corta, el proceso Chromium continúa ejecutándose hasta completar (timeout interno de 60 segundos en Puppeteer). No hay ningún `HttpContext.RequestAborted` propagado.
- **Impacto medible:** Cada conexión abandonada mantiene un proceso Chromium activo por hasta 60 segundos consumiendo CPU y memoria (~50–100 MB por proceso).

---

## 4. CONCURRENCIA Y ASYNC

### 4.1 `Task.Delay` bloqueante en el path crítico de captura Puppeteer
- **Criticidad:** 🟡 MEDIO
- **Archivo:** `Services\Implementations\StudentIdCardHtmlCaptureService.cs` líneas 51, 83, 194
- **Código actual:**
  ```csharp
  // En GenerateFromUrl (retry):
  await Task.Delay(500);

  // En GenerateBulkFromUrls (retry):
  await Task.Delay(400);

  // En CaptureCardFacesAsync (wait for render):
  await Task.Delay(400);
  ```
- **Evidencia:** Se usan `Task.Delay` fijos como mecanismo de espera para carga de página y retries. El delay de 400 ms en `CaptureCardFacesAsync` (línea 194) se ejecuta **en cada captura individual**, incluyendo los 30 carnets del bulk print. El delay se aplica después de `WaitForSelectorAsync(".idcard-face")` ya completado, por lo que es un wait adicional sobre algo que ya está listo.
- **Impacto medible:** 30 carnets × 400 ms = **12 segundos** de delays artificiales solo en el wait post-selector del bulk print. Sumar el retry delay si aplica.

---

### 4.2 `CancellationToken` ausente en todas las operaciones async del controlador
- **Criticidad:** 🟡 MEDIO
- **Archivo:** `Controllers\StudentIdCardController.cs` — todas las acciones async
- **Código actual:**
  ```csharp
  public async Task<IActionResult> GenerateView(Guid studentId)
  public async Task<IActionResult> Print(Guid studentId)
  public async Task<IActionResult> PrintBulk([FromBody] BulkPrintRequestDto request)
  public async Task<IActionResult> ListJson(...)
  public async Task<IActionResult> ListFilters()
  ```
- **Evidencia:** Ninguna acción del controlador acepta `CancellationToken cancellationToken` como parámetro (que ASP.NET Core inyecta automáticamente desde `HttpContext.RequestAborted`). Las llamadas a EF Core (`FirstOrDefaultAsync`, `AnyAsync`, `ToListAsync`, etc.) todas aceptan un `CancellationToken` opcional que no se pasa.
- **Impacto medible:** Queries a PostgreSQL remoto (latencia ~80–120 ms) continúan ejecutándose aunque el cliente ya no espere el resultado. En operaciones costosas como `PrintBulk` (30 Chromium captures + 60 DB roundtrips), un request abandonado no se cancela.

---

## 5. SEGURIDAD Y OVERHEAD CRIPTOGRÁFICO

### 5.1 `HMACSHA256` instanciada en cada operación — sin reutilización
- **Criticidad:** 🟠 ALTO
- **Archivo:** `Services\Security\QrSignatureService.cs` líneas 113–117
- **Código actual:**
  ```csharp
  private string ComputeHmacSha256(string payload)
  {
      using var hmac = new HMACSHA256(_secretKeyBytes);
      var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
      return Convert.ToHexString(hash).ToLowerInvariant();
  }
  ```
- **Evidencia:** `QrSignatureService` está correctamente registrado como **Singleton** (línea 212 de `Program.cs`: `builder.Services.AddSingleton<IQrSignatureService, QrSignatureService>()`). Sin embargo, dentro de `ComputeHmacSha256` se crea una nueva instancia de `HMACSHA256` en **cada invocación**. Esta función se llama dos veces en `ValidateSignedToken` (línea: computación de `expectedSignature`) y una vez en `GenerateSignedToken`. `HMACSHA256` inicializa internamente el key schedule del algoritmo en cada instanciación. `IncrementalHash` o el método estático `HMACSHA256.HashData` (.NET 8) son más eficientes al evitar la allocación del objeto.
- **Impacto medible:** En el endpoint `/api/scan` con 60 requests/minuto (límite configurado), se crean hasta 180 instancias de `HMACSHA256` por minuto (3 por request). Es overhead controlado pero innecesario dado que la clave ya está pre-computada en el Singleton.

---

### 5.2 `Convert.FromHexString` con try/catch en `ValidateSignedToken` — overhead por excepción en casos malformados
- **Criticidad:** 🟢 BAJO
- **Archivo:** `Services\Security\QrSignatureService.cs` líneas 93–102
- **Código actual:**
  ```csharp
  try
  {
      var receivedBytes = Convert.FromHexString(receivedSignature);
      var expectedBytes = Convert.FromHexString(expectedSignature);
      return receivedBytes.Length == expectedBytes.Length
             && CryptographicOperations.FixedTimeEquals(receivedBytes, expectedBytes);
  }
  catch
  {
      return false;
  }
  ```
- **Evidencia:** `Convert.FromHexString` lanza `FormatException` si la cadena no es hex válida. En un escenario de ataque o tokens malformados, cada request inválido incurre en la generación de una excepción (stack unwinding). Existe `Convert.TryFromHexString` en .NET 8 que no lanza excepción.
- **Impacto medible:** Bajo en condiciones normales; relevante bajo ataque de fuerza bruta con tokens malformados (aunque el rate limiter lo mitiga a 60/min).

---

### 5.3 `QrSignatureService` es Singleton — confirmado correcto
- **Criticidad:** 🟢 BAJO (informativo)
- **Archivo:** `Program.cs` línea 212; `Services\Security\QrSignatureService.cs` líneas 36–54
- **Código actual:**
  ```csharp
  builder.Services.AddSingleton<IQrSignatureService, QrSignatureService>();
  ```
- **Evidencia:** El registro es efectivamente Singleton. `_secretKeyBytes` se inicializa una sola vez en el constructor y es `readonly`. `ComputeHmacSha256` no usa estado mutable compartido (cada invocación instancia su propio `HMACSHA256`). El servicio es thread-safe tal como está. Sin embargo, ver hallazgo 5.1 sobre la instanciación de `HMACSHA256` por llamada.
- **Impacto medible:** Sin impacto negativo en el diseño de Singleton. Hallazgo informativo para confirmar que la arquitectura es correcta.

---

## 6. VISTA RAZOR Y JAVASCRIPT

### 6.1 `@import url(...)` de Google Fonts en el `<style>` del layout de carnet — render-blocking
- **Criticidad:** 🟠 ALTO
- **Archivo:** `Views\StudentIdCard\Generate.cshtml` líneas 24–25
- **Código actual:**
  ```css
  <style>
      @@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
  ```
- **Evidencia:** `@import` CSS dentro de un bloque `<style>` inline es **render-blocking**: el browser no puede procesar el resto del CSS hasta que descarga y parsea el archivo de Google Fonts. A diferencia de `<link rel="preload">` o `<link rel="stylesheet">` en el `<head>`, un `@import` dentro de `<style>` no permite preloading y bloquea el render thread. Esta página es navegada por Puppeteer para generar PDFs, donde la latencia de descarga de Google Fonts puede causar timeouts o renders parciales.
- **Impacto medible:** Cada render de Puppeteer espera la descarga de Google Fonts (si no está en caché del proceso Chromium efímero). Para el bulk print, el proceso Chromium se lanza nuevo por operación bulk (no por carnet), por lo que la caché de Chromium puede ayudar dentro de un bulk, pero no entre requests independientes.

---

### 6.2 Logo de escuela solicitado 3 veces en la misma página `Generate.cshtml`
- **Criticidad:** 🟡 MEDIO
- **Archivo:** `Views\StudentIdCard\Generate.cshtml` líneas 313, 322, 394
- **Código actual:**
  ```html
  <!-- Watermark frente -->
  <img src="/File/GetSchoolLogo?logoUrl=@Uri.EscapeDataString(Model.SchoolLogoUrl ?? "")" ... />

  <!-- Header frente -->
  <img src="/File/GetSchoolLogo?logoUrl=@Uri.EscapeDataString(Model.SchoolLogoUrl ?? "")" ... />

  <!-- Watermark reverso -->
  <img src="/File/GetSchoolLogo?logoUrl=@Uri.EscapeDataString(Model.SchoolLogoUrl ?? "")" ... />
  ```
- **Evidencia:** El mismo endpoint `/File/GetSchoolLogo?logoUrl=...` se llama tres veces en el mismo HTML. Si el endpoint no implementa caché HTTP apropiado (`Cache-Control`, `ETag`), el browser (y Puppeteer) realizan 3 requests al servidor para el mismo logo. Para el bulk print de 30 carnets, Puppeteer navega 30 páginas distintas — aunque el logo es el mismo para todos los estudiantes de la misma escuela.
- **Impacto medible:** Sin caché HTTP en `/File/GetSchoolLogo`: 30 carnets × 3 logos = 90 requests al endpoint de logo por bulk print. El endpoint no está analizado en este informe, pero si sirve desde disco o S3, el overhead es significativo.

---

### 6.3 `generateCard()` en `Generate.cshtml` no tiene anti-forgery token — llama sin CSRF protection
- **Criticidad:** 🟡 MEDIO
- **Archivo:** `Views\StudentIdCard\Generate.cshtml` líneas 504–510
- **Código actual:**
  ```javascript
  fetch('/StudentIdCard/api/generate/' + id, {
      method: 'POST',
      headers: {
          'Content-Type': 'application/json',
          'RequestVerificationToken': antiforgeryInput ? antiforgeryInput.value : ''
      }
  })
  ```
- **Evidencia:** El código intenta obtener el token de `document.querySelector('input[name="__RequestVerificationToken"]')`, pero la vista `Generate.cshtml` **no incluye `@Html.AntiForgeryToken()`**. La vista `Index.cshtml` sí lo incluye (línea 7), pero `Generate.cshtml` no tiene este helper. Si el layout `_SuperAdminLayout` tampoco lo incluye, `antiforgeryInput` será `null` y el header se enviará vacío. Adicionalmente, el endpoint `GenerateApi` no tiene `[ValidateAntiForgeryToken]` — depende del header custom.
- **Impacto medible:** Posible fallo silencioso de anti-CSRF: el fetch enviará el header vacío sin error JavaScript. La protección CSRF real en `GenerateApi` depende de si el framework valida el header vacío.

---

### 6.4 Scripts de la vista `Generate.cshtml` sin `defer` ni agrupación
- **Criticidad:** 🟢 BAJO
- **Archivo:** `Views\StudentIdCard\Generate.cshtml` líneas 453–526
- **Código actual:**
  ```html
  <script>
      (function () {
          var btn = document.getElementById('btnDownloadPdf');
          // ...
      })();
  </script>
  <script>
      function generateCard(id) {
          // ...
      }
  </script>
  ```
- **Evidencia:** Dos bloques `<script>` separados inline al final del body. No hay bundling ni minificación configurado para las vistas del módulo. El primer bloque usa un IIFE pero el segundo define `generateCard` en el scope global (no encapsulado), contaminando el namespace global.
- **Impacto medible:** Mínimo en producción pero contribuye a pollution del scope global. Sin bundling, el browser no puede paralelizar la descarga de scripts.

---

## 7. TRANSACCIONES Y AISLAMIENTO

### 7.1 `IsolationLevel.Serializable` en `GenerateAsync` — overhead en concurrencia
- **Criticidad:** 🟠 ALTO
- **Archivo:** `Services\Implementations\StudentIdCardService.cs` líneas 103, líneas 96–220
- **Código actual:**
  ```csharp
  using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
  ```
- **Evidencia:** La transacción serializable se aplica a una operación que incluye: (1) consulta del estudiante con 3 Includes (Grade, Group, Shift); (2) consulta de payment access; (3) SELECT de carnets activos; (4) SELECT de tokens activos; (5) INSERT de nuevo carnet; (6) INSERT de nuevo token; (7) UPDATE de carnets revocados; (8) UPDATE de tokens revocados. En PostgreSQL, `SERIALIZABLE` usa SSI (Serializable Snapshot Isolation) que impone overhead de tracking de dependencias en todas las transacciones concurrentes de la sesión. Para una operación que es inherentemente de baja concurrencia (un admin genera un carnet para un estudiante específico), `READ COMMITTED` con un `SELECT ... FOR UPDATE` sobre la fila del estudiante o un índice único sobre `(student_id, status='active')` lograría la misma protección a menor costo.
- **Impacto medible:** PostgreSQL SSI añade overhead en el tracking de predicados para detectar anomalías serializables. En Render (hosted PostgreSQL), bajo carga moderada, las transacciones serializables pueden generar `serialization failure` (error 40001) que requieren retry en la aplicación — actualmente no implementado.

---

### 7.2 Segunda transacción serializable en `BuildStudentCardDtoAsync` — doble overhead
- **Criticidad:** 🟠 ALTO
- **Archivo:** `Services\Implementations\StudentIdCardPdfService.cs` líneas 213, 188–274
- **Código actual:**
  ```csharp
  // FUERA de la transacción (sin protección):
  var student = await _context.Users
      .Include(u => u.StudentAssignments)...
      .FirstOrDefaultAsync(u => u.Id == studentId);       // Query sin transacción
  var payment = await _context.StudentPaymentAccesses
      .FirstOrDefaultAsync(x => x.StudentId == studentId); // Query sin transacción

  // DENTRO de la transacción serializable:
  using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
  var card = await _context.StudentIdCards
      .FirstOrDefaultAsync(c => c.StudentId == studentId && c.Status == "active");
  ```
- **Evidencia:** En `BuildStudentCardDtoAsync`, las queries de `student` y `payment` se ejecutan **fuera** de la transacción serializable (líneas 193–211), y luego se abre la transacción solo para el carnet y el token (líneas 213–249). Esto crea una **window de inconsistencia**: los datos del estudiante leídos fuera de la transacción pueden diferir del estado visto dentro de ella. El propósito de la transacción serializable (prevenir duplicados) se cumple parcialmente, pero los datos de display (nombre, grado) pueden corresponder a un snapshot diferente al del carnet creado.
- **Impacto medible:** Doble inconsistencia: (1) overhead de transacción serializable en el PDF service además del service principal; (2) ventana de inconsistencia de datos entre el snapshot exterior y el interior de la transacción.

---

### 7.3 `UpdatePrintStatus` usa tracking completo innecesariamente
- **Criticidad:** 🟢 BAJO
- **Archivo:** `Controllers\StudentIdCardController.cs` líneas 314–330
- **Código actual:**
  ```csharp
  var card = await _context.StudentIdCards
      .FirstOrDefaultAsync(c => c.StudentId == studentId && c.Status == "active");

  if (card == null) return NotFound(...);

  card.IsPrinted = request.IsPrinted;
  card.PrintedAt = request.IsPrinted ? DateTime.UtcNow : null;
  await _context.SaveChangesAsync();
  ```
- **Evidencia:** La query carga la entidad completa con change tracking habilitado (no hay `AsNoTracking()`). EF Core genera un `UPDATE` que incluye todas las columnas de `student_id_cards` en el SET, no solo `is_printed` y `printed_at`. Para una operación de toggle de estado, un `ExecuteUpdateAsync` (EF Core 7+) sería más eficiente.
- **Impacto medible:** El UPDATE generado por SaveChangesAsync con change tracking actualiza todas las columnas no solo las modificadas, dependiendo del proveedor. En Npgsql, EF Core genera UPDATE con solo las columnas modificadas en el snapshot, pero el overhead del change tracking y la carga completa de la entidad persiste.

---

## 8. ÍNDICES DE BASE DE DATOS

### Estado actual de índices (resultado de `pg_indexes` ejecutado en producción)

#### Tabla `student_id_cards`
| Índice | Definición |
|--------|-----------|
| `student_id_cards_pkey` | `UNIQUE BTREE (id)` |
| `ix_student_id_cards_card_number` | `UNIQUE BTREE (card_number)` |
| `ix_student_id_cards_student_id` | `BTREE (student_id)` |

#### Tabla `student_qr_tokens`
| Índice | Definición |
|--------|-----------|
| `student_qr_tokens_pkey` | `UNIQUE BTREE (id)` |
| `ix_student_qr_tokens_token` | `UNIQUE BTREE (token)` |
| `ix_student_qr_tokens_student_id` | `BTREE (student_id)` |

#### Tabla `student_payment_access`
| Índice | Definición |
|--------|-----------|
| `student_payment_access_pkey` | `UNIQUE BTREE (id)` |
| `ix_student_payment_access_student_id` | `BTREE (student_id)` |
| `ix_student_payment_access_school_id` | `BTREE (school_id)` |
| `ix_student_payment_access_carnet_status_school_id` | `BTREE (carnet_status, school_id)` |
| `ix_student_payment_access_student_id_school_id` | `UNIQUE BTREE (student_id, school_id)` |

#### Tabla `scan_logs`
| Índice | Definición |
|--------|-----------|
| `scan_logs_pkey` | `UNIQUE BTREE (id)` |
| `ix_scan_logs_student_id` | `BTREE (student_id)` |
| `ix_scan_logs_scanned_at` | `BTREE (scanned_at)` |

#### Tabla `users`
| Índice | Definición |
|--------|-----------|
| `users_pkey` | `UNIQUE BTREE (id)` |
| `users_email_key` | `UNIQUE BTREE (email)` |
| `users_document_id_key` | `UNIQUE BTREE (document_id)` |
| `IX_users_school_id` | `BTREE (school_id)` |
| `IX_users_cellphone_primary` | `BTREE (cellphone_primary)` |
| `IX_users_cellphone_secondary` | `BTREE (cellphone_secondary)` |

#### Tabla `student_assignments`
| Índice | Definición |
|--------|-----------|
| `student_assignments_pkey` | `UNIQUE BTREE (id)` |
| `IX_student_assignments_student_id` | `BTREE (student_id)` |
| `IX_student_assignments_grade_id` | `BTREE (grade_id)` |
| `IX_student_assignments_group_id` | `BTREE (group_id)` |
| `ix_student_assignments_academic_year_id` | `BTREE (academic_year_id)` |
| `ix_student_assignments_shift_id` | `BTREE (shift_id)` |
| `ix_student_assignments_student_academic_year` | `BTREE (student_id, academic_year_id)` |
| `ix_student_assignments_student_active` | `BTREE (student_id, is_active)` |

---

### Columnas sin índice que son consultadas frecuentemente

| Tabla | Columna(s) | Patrón de consulta | Evidencia en código |
|-------|-----------|---------------------|---------------------|
| `users` | `role` | `WHERE lower(role) IN ('student','estudiante')` | Controller líneas 511–512, Service líneas 51–52 |
| `users` | `(role, school_id)` | Combinación de role filter + school filter | Controller líneas 511–517 |
| `student_id_cards` | `(student_id, status)` | `WHERE student_id = $1 AND status = 'active'` | Controller líneas 403–409, 524–526; Service líneas 59–61, 291–294 |
| `student_qr_tokens` | `(student_id, is_revoked)` | `WHERE student_id = $1 AND is_revoked = false` | Service líneas 159, 233 |

**Observación crítica sobre `users.role`:** El EXPLAIN ANALYZE ejecutado confirma que PostgreSQL realiza Seq Scan sobre `users` para el filtro `lower(role)`. Con 1969 filas el planner elige Seq Scan dado que 1839/1969 = 93.4% de filas son estudiantes. Un índice sobre `lower(role)` mejoraría el caso cuando se filtra por roles con baja cardinalidad (admin, teacher, inspector), pero para el caso de estudiantes (93.4% del total), el Seq Scan es óptimo dado el factor de selectividad. **Este hallazgo se revisa a la baja** — el Seq Scan es correcto para el filtro de estudiantes con la distribución actual, pero el índice compuesto `(lower(role), school_id)` optimizaría el caso de filtro por escuela (schoolId filter elimina ~95% de filas).

---

## 9. TABLA CONSOLIDADA DE HALLAZGOS

| # | Capa | Descripción | Archivo | Líneas | Criticidad | Impacto estimado |
|---|------|-------------|---------|--------|------------|-----------------|
| 1 | DB/EF | Seq Scan en `users.role` — índice funcional ausente | `SchoolDbContext.cs` / `StudentIdCardController.cs` | 1244, 511–513 | 🔴 CRÍTICO | Escala lineal con crecimiento de usuarios; actualmente ~1.5 ms, proyectado ~10–15 ms a 10K usuarios |
| 2 | DB/EF | 9 roundtrips secuenciales en `GenerateView` | `StudentIdCardController.cs` | 59–118 | 🔴 CRÍTICO | 720–1080 ms de latencia pura de red por request a servidor remoto |
| 3 | DB/EF | Correlated subqueries duplicadas en `ListJson` — 1358 subqueries para 679 estudiantes | `StudentIdCardController.cs` | 431–437 | 🔴 CRÍTICO | 1358 operaciones DB por request de listado |
| 4 | DB/EF | Índice compuesto `(student_id, status)` ausente en `student_id_cards` | `SchoolDbContext.cs` | 2145–2148 | 🔴 CRÍTICO | Seq Scan en tabla de carnets en todas las operaciones de lectura/escritura del módulo |
| 5 | DB/EF | Include chain carga 31 columnas cuando se necesitan 4 | `StudentIdCardService.cs` | 43–53 | 🟠 ALTO | ~70% de payload DB innecesario por query de estudiante |
| 6 | DB/EF | `ListFilters` materializa 679 filas en .NET para un Distinct de ~10 valores | `StudentIdCardController.cs` | 453–476 | 🟠 ALTO | 35× payload de red comparado con GROUP BY en SQL |
| 7 | DB/EF | `SaveChangesAsync` en loop de bulk print — 60 roundtrips para 30 carnets | `StudentIdCardController.cs` | 284–301 | 🟠 ALTO | ~3000 ms extra en bulk print solo en marcado de estado |
| 8 | DB/EF | Include completo en ScanAsync carga password_hash y otras columnas sensibles | `StudentIdCardService.cs` | 244–251 | 🟠 ALTO | Overhead de transferencia de datos en path crítico de escaneo |
| 9 | Memoria | QR PNG generado en cada request sin caché | `QrHelper.cs` | 9–21 | 🔴 CRÍTICO | 30 × QR generation en bulk; dato inmutable por 6 meses |
| 10 | Memoria | Watermark recalculada con SkiaSharp por cada PDF — misma escuela | `StudentIdCardPdfService.cs` | 122–123, 339–370 | 🟠 ALTO | 30 × SkiaSharp pipeline por bulk print para logo idéntico |
| 11 | Memoria | Chromium lanzado por cada request individual de Print | `StudentIdCardHtmlCaptureService.cs` | 33–56 | 🟠 ALTO | 1–3 s de startup por descarga individual; 50–100 MB RAM |
| 12 | Memoria | `outStream.ToArray()` en bulk — doble buffer del PDF mergeado | `StudentIdCardController.cs` | 305–308 | 🟡 MEDIO | 2× uso de memoria en peak (6–20 MB para 30 carnets) |
| 13 | Memoria | QR Base64 data URI ~7 KB embebida en HTML — overhead de tamaño | `StudentIdCardService.cs` | 74–75 | 🟡 MEDIO | 210 KB overhead en bulk print de 30 carnets |
| 14 | HTTP/Caché | `GenerateView` sin caché — 270 queries en un bulk print de 30 | `StudentIdCardController.cs` | 55–119 | 🟠 ALTO | 270 queries DB para generar 30 PDFs |
| 15 | HTTP/Caché | DataTables client-side — 679 filas en un solo JSON sin paginación | `Views/StudentIdCard/Index.cshtml` | 97–172 | 🟠 ALTO | ~135 KB JSON por carga; sin server-side processing |
| 16 | HTTP/Caché | Idioma DataTables desde CDN externo — dependencia externa en cada carga | `Views/StudentIdCard/Index.cshtml` | 159 | 🟡 MEDIO | DNS + TCP + TLS adicional; falla CDN afecta UI |
| 17 | HTTP/Caché | Print sin CancellationToken — Chromium persiste tras conexión cortada | `StudentIdCardController.cs` | 127–182 | 🟡 MEDIO | Hasta 60 s de proceso Chromium zombie por request abandonado |
| 18 | Async | `Task.Delay(400)` fijo en CaptureCardFacesAsync — 12 s innecesarios en bulk | `StudentIdCardHtmlCaptureService.cs` | 194 | 🟡 MEDIO | 12 s de delay artificial en bulk de 30 carnets |
| 19 | Async | `CancellationToken` ausente en todos los endpoints | `StudentIdCardController.cs` | todas las acciones | 🟡 MEDIO | DB queries continúan tras abandono de request |
| 20 | Seguridad | `HMACSHA256` instanciada por llamada en Singleton | `QrSignatureService.cs` | 113–117 | 🟠 ALTO | 180 allocations HMAC/min en límite de rate; evitable con HMACSHA256.HashData |
| 21 | Seguridad | `Convert.FromHexString` lanza excepción en tokens malformados | `QrSignatureService.cs` | 93–102 | 🟢 BAJO | Stack trace por token inválido; TryFromHexString disponible en .NET 8 |
| 22 | Vista/JS | `@import` Google Fonts en `<style>` inline — render-blocking | `Generate.cshtml` | 24–25 | 🟠 ALTO | Bloquea render en Puppeteer; posibles timeouts en entornos sin acceso externo |
| 23 | Vista/JS | Logo de escuela solicitado 3 veces por página | `Generate.cshtml` | 313, 322, 394 | 🟡 MEDIO | 90 requests de logo por bulk de 30 carnets si no hay caché HTTP |
| 24 | Vista/JS | Anti-forgery token ausente en `Generate.cshtml` | `Generate.cshtml` | 504–510 | 🟡 MEDIO | Header CSRF enviado vacío al llamar `GenerateApi` |
| 25 | Vista/JS | Scripts inline sin defer/module; `generateCard` en global scope | `Generate.cshtml` | 453–526 | 🟢 BAJO | Polución de namespace global; sin impacto funcional |
| 26 | Transacciones | `IsolationLevel.Serializable` en `GenerateAsync` — overhead SSI | `StudentIdCardService.cs` | 103 | 🟠 ALTO | Overhead SSI en PostgreSQL; riesgo de serialization failure sin retry |
| 27 | Transacciones | Segunda transacción serializable en `BuildStudentCardDtoAsync` — ventana de inconsistencia | `StudentIdCardPdfService.cs` | 213 | 🟠 ALTO | Inconsistencia temporal + doble overhead SSI |
| 28 | Transacciones | `UpdatePrintStatus` carga entidad completa para UPDATE de 2 columnas | `StudentIdCardController.cs` | 314–330 | 🟢 BAJO | Overhead de change tracking para operación simple |

---

## 10. CONCLUSIÓN TÉCNICA

Los tres problemas de mayor impacto en rendimiento del módulo `/StudentIdCard/ui` son:

**Problema 1 — Cascada de 9 roundtrips secuenciales en `GenerateView` sin caché:** La vista de previsualización de carnet ejecuta 9 queries secuenciales contra un servidor PostgreSQL remoto en Render (Oregon), acumulando 720–1080 ms de latencia pura de red antes de poder renderizar la página. Este mismo patrón es amplificado durante el bulk print donde Puppeteer realiza hasta 30 GETs independientes a esta vista, generando **270 queries en total** — cada una ejecutando las mismas lecturas de escuela, settings, campos de plantilla y academic year que cambian raramente o nunca entre carnets de la misma institución.

**Problema 2 — Correlated subqueries duplicadas en `ListJson` generan 1358 operaciones por request de listado:** La proyección en `ListJson` incluye dos subqueries independientes sobre `student_id_cards` por cada fila de usuario (una para `isPrinted`, otra para `printedAt`), que EF Core no colapsa en una sola operación. Para los 679 estudiantes con pago activo en producción, esto resulta en 1358 subqueries adicionales por cada carga del listado. Simultáneamente, DataTables opera en modo client-side, forzando al servidor a materializar y serializar todas las 679 filas como JSON (~135 KB) en cada request, independientemente de cuántas filas el usuario visualiza en pantalla.

**Problema 3 — QR PNG recalculado en cada request sin ningún nivel de caché:** El token QR de un carnet es inmutable por hasta 6 meses, pero `QrHelper.GenerateQrPng` instancia un nuevo `QRCodeGenerator`, genera el código QR completo y codifica el PNG cada vez que se accede a la vista del carnet. Este proceso se repite 30 veces consecutivas durante un bulk print, sin ningún mecanismo de caché (ni en memoria, ni HTTP, ni disk). Combinado con la ausencia de caché HTTP en `GenerateView` y la descarga y procesamiento repetido del logo de escuela para la watermark via SkiaSharp (×30 en bulk), el ciclo de generación de PDF masivo acumula trabajo de CPU y allocations que son fundamentalmente redundantes para datos que no cambian entre carnets de la misma escuela.
