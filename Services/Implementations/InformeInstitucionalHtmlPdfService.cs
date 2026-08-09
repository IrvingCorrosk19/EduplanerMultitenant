using Microsoft.AspNetCore.Http;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using SchoolManager.Services.Interfaces;
using System.Runtime.InteropServices;

namespace SchoolManager.Services.Implementations;

public class InformeInstitucionalHtmlPdfService : IInformeInstitucionalHtmlPdfService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<InformeInstitucionalHtmlPdfService> _logger;

    public InformeInstitucionalHtmlPdfService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<InformeInstitucionalHtmlPdfService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<byte[]> GenerarPdfDesdeUrlAsync(string urlAbsoluta, bool landscape = true)
    {
        var executablePath = await ResolveChromiumExecutablePathAsync();
        var launchOpts = CreateLaunchOptions(executablePath);

        await using var browser = await Puppeteer.LaunchAsync(launchOpts);
        await using var page = await browser.NewPageAsync();
        await ApplyRequestCookiesAsync(page);

        await page.GoToAsync(urlAbsoluta, new NavigationOptions
        {
            WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
            Timeout = 90_000
        });

        return await RenderPdfAsync(page, landscape);
    }

    public async Task<byte[]> GenerarPdfDesdeHtmlAsync(string html, bool landscape = true)
    {
        var executablePath = await ResolveChromiumExecutablePathAsync();
        var launchOpts = CreateLaunchOptions(executablePath);

        await using var browser = await Puppeteer.LaunchAsync(launchOpts);
        await using var page = await browser.NewPageAsync();

        await page.SetContentAsync(html, new NavigationOptions
        {
            WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
            Timeout = 90_000
        });

        return await RenderPdfAsync(page, landscape);
    }

    private static LaunchOptions CreateLaunchOptions(string? executablePath)
    {
        var launchOpts = new LaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" }
        };
        if (!string.IsNullOrEmpty(executablePath))
            launchOpts.ExecutablePath = executablePath;
        return launchOpts;
    }

    private static async Task<byte[]> RenderPdfAsync(IPage page, bool landscape)
    {
        await page.EvaluateExpressionAsync("document.fonts.ready");
        await Task.Delay(300);

        return await page.PdfDataAsync(new PdfOptions
        {
            Landscape = landscape,
            PrintBackground = true,
            PreferCSSPageSize = true,
            Width = landscape ? null : "8.5in",
            Height = landscape ? null : "14in",
            MarginOptions = new MarginOptions
            {
                Top = "6mm",
                Bottom = "6mm",
                Left = "5mm",
                Right = "5mm"
            }
        });
    }

    private async Task ApplyRequestCookiesAsync(IPage page)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx?.Request.Cookies == null || ctx.Request.Host.Value == null)
            return;

        var cookies = ctx.Request.Cookies
            .Where(c => !string.IsNullOrEmpty(c.Value))
            .Select(c => new CookieParam
            {
                Name = c.Key,
                Value = c.Value,
                Domain = ctx.Request.Host.Host,
                Path = "/"
            })
            .ToArray();

        if (cookies.Length > 0)
            await page.SetCookieAsync(cookies);
    }

    private static async Task<string?> ResolveChromiumExecutablePathAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var candidates = new[]
            {
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH") ?? ""
            };
            foreach (var path in candidates)
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    return path;
            }
        }

        try
        {
            var fetcher = new BrowserFetcher();
            var result = await fetcher.DownloadAsync();
            return result.GetExecutablePath();
        }
        catch
        {
            return null;
        }
    }
}
