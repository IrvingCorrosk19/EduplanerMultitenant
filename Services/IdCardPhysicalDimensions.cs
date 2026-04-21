namespace SchoolManager.Services;

/// <summary>
/// Carnet tipo CR80 exacto (85.60 mm × 53.98 mm). Misma base para vista HTML, captura PDF y render Skia.
/// </summary>
public static class IdCardPhysicalDimensions
{
    public const float LongMm = 85.60f;
    public const float ShortMm = 53.98f;
    public const float RenderDpi = 300f;

    public static int LandscapeWidthPx => (int)MathF.Round(LongMm / 25.4f * RenderDpi);
    public static int LandscapeHeightPx => (int)MathF.Round(ShortMm / 25.4f * RenderDpi);
    public static int PortraitWidthPx => LandscapeHeightPx;
    public static int PortraitHeightPx => LandscapeWidthPx;
}
