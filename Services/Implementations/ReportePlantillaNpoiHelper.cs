using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace SchoolManager.Services.Implementations;

internal static class ReportePlantillaNpoiHelper
{
    public static string ResolverPlantilla(string reportesDir, string nombreParcial)
    {
        if (!Directory.Exists(reportesDir))
            throw new FileNotFoundException($"No se encontró la carpeta de plantillas: {reportesDir}");

        var archivo = Directory
            .EnumerateFiles(reportesDir, "*.xls")
            .FirstOrDefault(f =>
                Path.GetFileName(f).Contains(nombreParcial, StringComparison.OrdinalIgnoreCase));

        if (archivo == null)
            throw new FileNotFoundException($"Plantilla no encontrada (buscando «{nombreParcial}» en {reportesDir}).");

        return archivo;
    }

    public static byte[] EscribirLibro(HSSFWorkbook workbook)
    {
        using var ms = new MemoryStream();
        workbook.Write(ms);
        return ms.ToArray();
    }

    public static HSSFWorkbook CargarPlantilla(string rutaPlantilla)
    {
        using var fs = new FileStream(rutaPlantilla, FileMode.Open, FileAccess.Read, FileShare.Read);
        return new HSSFWorkbook(fs);
    }

    public static void EstablecerTexto(ISheet sheet, int fila0, int col0, string? texto)
    {
        if (string.IsNullOrEmpty(texto))
            return;

        var fila = sheet.GetRow(fila0) ?? sheet.CreateRow(fila0);
        var celda = fila.GetCell(col0) ?? fila.CreateCell(col0);
        celda.SetCellValue(texto);
    }

    public static void EstablecerNumero(ISheet sheet, int fila0, int col0, double? valor)
    {
        var fila = sheet.GetRow(fila0) ?? sheet.CreateRow(fila0);
        var celda = fila.GetCell(col0) ?? fila.CreateCell(col0);

        if (!valor.HasValue)
        {
            celda.SetBlank();
            return;
        }

        celda.SetCellValue(valor.Value);
    }

    public static void LimpiarRangoDatos(ISheet sheet, int filaInicio0, int filaFin0, int colInicio, int colFin)
    {
        for (var r = filaInicio0; r <= filaFin0; r++)
        {
            var fila = sheet.GetRow(r);
            if (fila == null)
                continue;

            for (var c = colInicio; c <= colFin; c++)
            {
                var celda = fila.GetCell(c);
                celda?.SetBlank();
            }
        }
    }

    public static void EstablecerNota(ISheet sheet, int fila0, int col0, decimal? nota) =>
        EstablecerNumero(sheet, fila0, col0, nota.HasValue ? (double)Math.Round(nota.Value, 1) : null);
}
