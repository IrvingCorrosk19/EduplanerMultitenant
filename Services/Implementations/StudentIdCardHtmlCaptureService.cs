using PuppeteerSharp;
using SchoolManager.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SchoolManager.Services.Interfaces;
using SkiaSharp;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using System.Security.Claims;

namespace SchoolManager.Services.Implementations;

public class StudentIdCardHtmlCaptureService : IStudentIdCardHtmlCaptureService
{
    private readonly ILogger<StudentIdCardHtmlCaptureService> _logger;
    private readonly IHttpContextAccessor                     _http;
    private readonly StudentIdCardPdfPrintOptions             _printOptions;
    private readonly IStudentIdCardPdfService                 _pdfService;

    public StudentIdCardHtmlCaptureService(
        ILogger<StudentIdCardHtmlCaptureService> logger,
        IHttpContextAccessor http,
        IOptions<StudentIdCardPdfPrintOptions> printOptions,
        IStudentIdCardPdfService pdfService)
    {
        _logger       = logger;
        _http         = http;
        _printOptions = printOptions.Value ?? new StudentIdCardPdfPrintOptions();
        _pdfService   = pdfService;
    }

    public async Task<byte[]> GenerateFromUrl(string url)
    {
        var executablePath = await ResolveChromiumExecutablePath();
        _logger.LogInformation("[CardPdf] Using Chromium path: {Path}", executablePath);
        var launchOpts = BuildLaunchOptions(executablePath);

        byte[] frontImg;
        byte[]? backImg;
        await using (var browser = await Puppeteer.LaunchAsync(launchOpts))
        {
            try
            {
                (frontImg, backImg) = await CaptureCardFacesAsync(browser, url);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[CardPdf] First capture attempt failed. Retrying once...");
                await Task.Delay(500);
                (frontImg, backImg) = await CaptureCardFacesAsync(browser, url);
            }
        }

        return BuildPdfFromFaceImages(frontImg, backImg);
    }

    public async Task<IReadOnlyList<byte[]>> GenerateBulkFromUrls(IReadOnlyList<string> urls)
    {
        if (urls == null || urls.Count == 0)
            return Array.Empty<byte[]>();

        var executablePath = await ResolveChromiumExecutablePath();
        _logger.LogInformation("[CardPdf] Bulk: Chromium path: {Path}, count={Count}", executablePath, urls.Count);
        var launchOpts = BuildLaunchOptions(executablePath);

        var results = new List<byte[]>(urls.Count);
        await using var browser = await Puppeteer.LaunchAsync(launchOpts);

        foreach (var url in urls)
        {
            try
            {
                byte[] frontImg;
                byte[]? backImg;
                try
                {
                    (frontImg, backImg) = await CaptureCardFacesAsync(browser, url);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[CardPdf] Bulk capture failed for {Url}, retry once", url);
                    await Task.Delay(400);
                    (frontImg, backImg) = await CaptureCardFacesAsync(browser, url);
                }

                results.Add(BuildPdfFromFaceImages(frontImg, backImg));
            }
            catch (Exception ex)
            {
                // Misma política que Print(): HTML/Chromium falló → PDF nativo solo para este carnet.
                if (!TryParseStudentIdFromGenerateUrl(url, out var studentId))
                    throw;

                var userIdClaim = _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var currentUserId))
                    throw;

                _logger.LogWarning(ex,
                    "[StudentIdCard] PDF masivo: HTML falló; generación nativa StudentId={StudentId} (igual que impresión única).",
                    studentId);
                results.Add(await _pdfService.GenerateCardPdfAsync(studentId, currentUserId));
            }
        }

        return results;
    }

    /// <summary>Espera ruta …/StudentIdCard/ui/generate/{guid}.</summary>
    private static bool TryParseStudentIdFromGenerateUrl(string url, out Guid studentId)
    {
        studentId = default;
        try
        {
            var path = new Uri(url, UriKind.Absolute).AbsolutePath.TrimEnd('/');
            var last = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
            return last != null && Guid.TryParse(last, out studentId);
        }
        catch
        {
            return false;
        }
    }

    private byte[] BuildPdfFromFaceImages(byte[] frontImg, byte[]? backImg)
    {
        var pageSize = ResolvePageSize();
        QuestPDF.Settings.License         = LicenseType.Community;
        QuestPDF.Settings.EnableDebugging = false;

        return Document.Create(container =>
        {
            container.Page(p =>
            {
                p.Size(pageSize.WidthMm, pageSize.HeightMm, Unit.Millimetre);
                p.Margin(0);
                p.Content().Image(frontImg).FitArea();
            });

            if (backImg != null)
            {
                container.Page(p =>
                {
                    p.Size(pageSize.WidthMm, pageSize.HeightMm, Unit.Millimetre);
                    p.Margin(0);
                    p.Content().Image(backImg).FitArea();
                });
            }
        }).GeneratePdf();
    }

    private LaunchOptions BuildLaunchOptions(string executablePath) =>
        new()
        {
            Headless       = true,
            ExecutablePath = executablePath,
            Timeout        = 60000,
            Args           = BuildLaunchArgs()
        };

    private async Task<(byte[] Front, byte[]? Back)> CaptureCardFacesAsync(IBrowser browser, string url)
    {
        await using var page = await browser.NewPageAsync();
        var pageSize = ResolvePageSize();

        page.DefaultNavigationTimeout = 60000;
        page.DefaultTimeout           = 60000;

        var maxDpr = Math.Max(1, _printOptions.MaxDeviceScaleFactor);
        var baseDpr  = Math.Clamp(_printOptions.DeviceScaleFactor, 1, maxDpr);
        await SetCaptureViewportAsync(page, pageSize, baseDpr);

        var reqCookies = _http.HttpContext?.Request.Cookies;
        if (reqCookies?.Count > 0)
        {
            var host   = new Uri(url).Host;
            var cpList = reqCookies
                .Select(c => new CookieParam { Name = c.Key, Value = c.Value, Domain = host })
                .ToArray();
            await page.SetCookieAsync(cpList);
        }

        await page.GoToAsync(url, new NavigationOptions
        {
            WaitUntil = [WaitUntilNavigation.DOMContentLoaded, WaitUntilNavigation.Networkidle2],
            Timeout   = 60000
        });
        await ApplyContentScale(page);
        await page.WaitForSelectorAsync(".idcard-face", new WaitForSelectorOptions { Timeout = 60000 });
        await Task.Delay(400);

        var front = await page.QuerySelectorAsync("#idCardFront")
            ?? throw new InvalidOperationException("No se encontró #idCardFront.");

        if (string.Equals(_printOptions.Profile, "CardPrinter", StringComparison.OrdinalIgnoreCase))
        {
            var box = await front.BoundingBoxAsync();
            if (box != null)
            {
                var bw = (double)box.Width;
                var bh = (double)box.Height;
                if (bw > 0.5 && bh > 0.5)
                {
                    var needDpr = Math.Max(pageSize.WidthPx / bw, pageSize.HeightPx / bh);
                    var optimal = (int)Math.Ceiling(needDpr - 1e-6);
                    optimal = Math.Clamp(Math.Max(baseDpr, optimal), 1, maxDpr);
                    if (optimal != baseDpr)
                    {
                        _logger.LogInformation(
                            "[CardPdf] Ajuste DPR captura {From}->{To} (caja HTML ~{W:F1}×{H:F1}px → PDF {Tw}×{Th}px, need≈{Need:F2})",
                            baseDpr, optimal, bw, bh, pageSize.WidthPx, pageSize.HeightPx, needDpr);
                        await SetCaptureViewportAsync(page, pageSize, optimal);
                        await Task.Delay(250);
                        front = await page.QuerySelectorAsync("#idCardFront")
                                ?? throw new InvalidOperationException("No se encontró #idCardFront.");
                    }
                }
            }
        }

        var allFaces = await page.QuerySelectorAllAsync(".idcard-face");
        var back     = allFaces.Length > 1 ? allFaces[1] : null;

        var frontImg = await Capture(front, pageSize.WidthPx, pageSize.HeightPx);
        var backImg  = back != null ? await Capture(back, pageSize.WidthPx, pageSize.HeightPx) : null;
        return (frontImg, backImg);
    }

    private static Task SetCaptureViewportAsync(IPage page, (float WidthMm, float HeightMm, int WidthPx, int HeightPx) pageSize, int dpr) =>
        page.SetViewportAsync(new ViewPortOptions
        {
            Width             = pageSize.WidthPx + 120,
            Height            = pageSize.HeightPx + 120,
            DeviceScaleFactor = dpr
        });

    private async Task ApplyContentScale(IPage page)
    {
        var scale = (double)Math.Clamp((float)_printOptions.ContentScale, 0.85f, 1.00f);
        await page.EvaluateExpressionAsync(
            $"document.documentElement.style.zoom = '{scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}';");
    }

    private (float WidthMm, float HeightMm, int WidthPx, int HeightPx) ResolvePageSize()
    {
        if (string.Equals(_printOptions.Profile, "A4Portrait", StringComparison.OrdinalIgnoreCase))
            return (148.0f, 235.0f, 1750, 2777);

        // Carnet tipo CR80 exacto en vertical (53.98 mm × 85.60 mm) — mismo que IdCardPhysicalDimensions.
        return (
            IdCardPhysicalDimensions.ShortMm,
            IdCardPhysicalDimensions.LongMm,
            IdCardPhysicalDimensions.PortraitWidthPx,
            IdCardPhysicalDimensions.PortraitHeightPx);
    }

    private static string[] BuildLaunchArgs()
    {
        var args = new List<string>
        {
            "--disable-dev-shm-usage",
            "--disable-gpu",
            "--disable-background-networking",
            "--disable-background-timer-throttling",
            "--disable-renderer-backgrounding"
        };

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            args.Add("--no-sandbox");
            args.Add("--disable-setuid-sandbox");
        }

        return args.ToArray();
    }

    private async Task<string> ResolveChromiumExecutablePath()
    {
        var envPath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
            return envPath;

        var candidates = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new[]
            {
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
            }
            : new[]
            {
                "/usr/bin/chromium",
                "/usr/bin/chromium-browser",
                "/usr/bin/google-chrome",
                "/snap/bin/chromium"
            };

        var localExecutable = candidates.FirstOrDefault(File.Exists);
        if (!string.IsNullOrWhiteSpace(localExecutable))
            return localExecutable;

        _logger.LogInformation("[CardPdf] No local Chromium/Chrome found, downloading managed browser...");
        var browserFetcher = new BrowserFetcher();
        var installed      = await browserFetcher.DownloadAsync();
        return installed.GetExecutablePath();
    }

    private async Task<byte[]> Capture(IElementHandle el, int targetWidth, int targetHeight)
    {
        var img = await el.ScreenshotDataAsync(new ElementScreenshotOptions
        {
            Type = ScreenshotType.Png
        });

        using var input = SKBitmap.Decode(img);
        if (input.Width == targetWidth && input.Height == targetHeight)
        {
            using var data = input.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        // Tras subir el DPR, suele haber un ligero downscale; High evita artefactos duros en tipografía.
        using var resized = input.Resize(new SKImageInfo(targetWidth, targetHeight), SKFilterQuality.High);
        using var image   = SKImage.FromBitmap(resized);
        using var data2   = image.Encode(SKEncodedImageFormat.Png, 100);
        return data2.ToArray();
    }
}
