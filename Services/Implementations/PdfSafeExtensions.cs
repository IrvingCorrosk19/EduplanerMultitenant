using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace SchoolManager.Services.Implementations;

/// <summary>
/// Extensiones de layout seguro para QuestPDF — carnet ID-1.
///
/// Principio: el contenido se adapta al layout, nunca al revés.
/// Cada método garantiza que texto e imágenes caben en su slot
/// sin overflow ni página extra, independientemente del dato recibido.
/// </summary>
internal static class PdfSafeExtensions
{
    /// <summary>
    /// Texto en una sola línea, truncado con "…" si excede el ancho.
    /// Seguro para slots con Height() fijo: nunca desborda verticalmente.
    /// </summary>
    public static void SafeText(
        this IContainer container,
        string text,
        float fontSize,
        bool bold = false,
        string? fontColor = null,
        float lineHeight = 1.0f)
    {
        var td = container
            .Text(text)
            .FontSize(fontSize)
            .ClampLines(1, "…")
            .LineHeight(lineHeight);

        if (bold)       td.Bold();
        if (fontColor != null) td.FontColor(fontColor);
    }

    /// <summary>
    /// Texto multilínea, truncado con "…" al superar maxLines.
    /// Seguro para slots con Height() fijo: la altura está acotada por maxLines.
    /// </summary>
    public static void SafeMultilineText(
        this IContainer container,
        string text,
        float fontSize,
        int maxLines = 2,
        float lineHeight = 1.0f,
        string? fontColor = null,
        bool bold = false)
    {
        var td = container
            .Text(text)
            .FontSize(fontSize)
            .ClampLines(maxLines, "…")
            .LineHeight(lineHeight);

        if (bold)       td.Bold();
        if (fontColor != null) td.FontColor(fontColor);
    }

    /// <summary>
    /// Imagen que siempre cabe en su contenedor: escala proporcional, sin overflow.
    /// Equivale a CSS object-fit: contain.
    /// </summary>
    public static void SafeImage(this IContainer container, byte[] imageBytes)
    {
        container.Image(imageBytes).FitArea();
    }

    /// <summary>
    /// Imagen si hay bytes válidos; placeholder de texto en caso contrario.
    /// Nunca lanza NullReferenceException ni ArrayIndexOutOfBoundsException.
    /// </summary>
    public static void SafeImageOrPlaceholder(
        this IContainer container,
        byte[]? imageBytes,
        string placeholder,
        float placeholderFontSize,
        string? fontColor = null)
    {
        if (imageBytes != null && imageBytes.Length > 0)
            container.Image(imageBytes).FitArea();
        else
            container
                .AlignCenter()
                .AlignMiddle()
                .SafeText(placeholder, placeholderFontSize, fontColor: fontColor);
    }
}
