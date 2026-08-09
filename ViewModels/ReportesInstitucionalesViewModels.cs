namespace SchoolManager.ViewModels;

public enum InformeCalificacionesTipo
{
    ExpresionesArtisticas,
    Tecnologia
}

public class ReporteCatalogoItemViewModel
{
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Icono { get; set; } = "bi-file-earmark-text";
    public string Action { get; set; } = "Index";
    public string Controller { get; set; } = "";
    public bool Disponible { get; set; } = true;
}

public class InformeInstitucionalFiltroViewModel
{
    public string Trimestre { get; set; } = "";
    public string NivelEducativo { get; set; } = "";
    public Guid MateriaId { get; set; }
    public Guid GroupId { get; set; }
    public Guid GradeLevelId { get; set; }
}

public class InformeEstudianteFilaDto
{
    public int Numero { get; set; }
    public Guid StudentId { get; set; }
    public string Nombre { get; set; } = "";
}

/// <summary>Informe imprimible Calificaciones de Tecnología (formato MEDUCA / plantilla oficial).</summary>
public class CalificacionesTecnologiaReportViewModel
{
    public string LogoUrl { get; set; } = "";
    public string MinisterioLinea { get; set; } = "Ministerio de Educación";
    public string InstitutoNombre { get; set; } = "";
    public string TituloInforme { get; set; } = "";
    public string ConsejeroNombre { get; set; } = "";
    public string GrupoEtiqueta { get; set; } = "";
    public string AsignaturaLinea { get; set; } = "ASIGNATURA: TECNOLOGIA";
    public IReadOnlyList<string> Areas { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> TrimestresEncabezado { get; set; } = Array.Empty<string>();
    public List<CalificacionesTecnologiaFilaViewModel> Filas { get; set; } = new();
    public int FilasPlantillaVacias { get; set; }
}

public class CalificacionesTecnologiaFilaViewModel
{
    public int Numero { get; set; }
    public string Nombre { get; set; } = "";
    public decimal? NotaT1Area1 { get; set; }
    public decimal? NotaT1Area2 { get; set; }
    public decimal? NotaT1Area3 { get; set; }
    public decimal? NotaT2Area1 { get; set; }
    public decimal? NotaT2Area2 { get; set; }
    public decimal? NotaT2Area3 { get; set; }
    public decimal? NotaT3Area1 { get; set; }
    public decimal? NotaT3Area2 { get; set; }
    public decimal? NotaT3Area3 { get; set; }
    public decimal? PromedioTrim1 { get; set; }
    public decimal? PromedioTrim2 { get; set; }
    public decimal? PromedioTrim3 { get; set; }
    public decimal? PromedioFinal { get; set; }

    public static string FormatearNota(decimal? nota) =>
        nota.HasValue ? nota.Value.ToString("0.0") : "";
}

public class HabitosActitudesReportViewModel
{
    public string LogoUrl { get; set; } = "";
    public string MinisterioLinea { get; set; } = "MINISTERIO DE EDUCACIÓN";
    public string RegionalLinea { get; set; } = "DIRECCIÓN REGIONAL DE SAN MIGUELITO";
    public string InstitutoNombre { get; set; } = "";
    public string AnioLectivoLinea { get; set; } = "";
    public string TrimestreLinea { get; set; } = "";
    public string ConsejeroNombre { get; set; } = "";
    public string GrupoEtiqueta { get; set; } = "";
    public IReadOnlyList<string> ColumnasHabitos { get; set; } = Array.Empty<string>();
    public List<HabitosActitudesFilaViewModel> Filas { get; set; } = new();
    public int FilasPlantillaVacias { get; set; }
}

public class HabitosActitudesFilaViewModel
{
    public int Numero { get; set; }
    public string Nombre { get; set; } = "";
}

public class CalificacionesExpresionesArtisticasReportViewModel
{
    public string LogoUrl { get; set; } = "";
    public string MinisterioLinea { get; set; } = "Ministerio de Educación";
    public string InstitutoNombre { get; set; } = "";
    public string TituloInforme { get; set; } = "";
    public string ConsejeroNombre { get; set; } = "";
    public string GradoEtiqueta { get; set; } = "";
    public string AsignaturaLinea { get; set; } = "ASIGNATURA: EXPRESIONES ARTÍSTICAS";
    public IReadOnlyList<string> TrimestresEncabezado { get; set; } = Array.Empty<string>();
    public List<CalificacionesExpresionesArtisticasFilaViewModel> Filas { get; set; } = new();
    public int FilasPlantillaVacias { get; set; }
}

public class CalificacionesExpresionesArtisticasFilaViewModel
{
    public int Numero { get; set; }
    public string Nombre { get; set; } = "";
    public decimal? NotaT1Artistica { get; set; }
    public decimal? NotaT1Musical { get; set; }
    public decimal? NotaT2Artistica { get; set; }
    public decimal? NotaT2Musical { get; set; }
    public decimal? NotaT3Artistica { get; set; }
    public decimal? NotaT3Musical { get; set; }
    public decimal? PromedioTrim1 { get; set; }
    public decimal? PromedioTrim2 { get; set; }
    public decimal? PromedioTrim3 { get; set; }
    public decimal? PromedioFinal { get; set; }

    public static string FormatearNota(decimal? nota) =>
        CalificacionesTecnologiaFilaViewModel.FormatearNota(nota);
}

public class FormatoCarpetasReportViewModel
{
    public string LogoUrl { get; set; } = "";
    public string InstitutoNombre { get; set; } = "";
    public string TituloInforme { get; set; } = "Informe de Calificaciones, Ausencias y Tardanzas";
    public string AnioLectivoLinea { get; set; } = "";
    public string ConsejeroNombre { get; set; } = "";
    public string ProfesorNombre { get; set; } = "";
    public string GrupoEtiqueta { get; set; } = "";
    public string MateriaNombre { get; set; } = "";
    public IReadOnlyList<string> TrimestresEncabezado { get; set; } = Array.Empty<string>();
    public List<FormatoCarpetasFilaViewModel> Filas { get; set; } = new();
    public int FilasPlantillaVacias { get; set; }
}

public class FormatoCarpetasFilaViewModel
{
    public int Numero { get; set; }
    public string Nombre { get; set; } = "";
    public decimal? NotaTrim1 { get; set; }
    public decimal? NotaTrim2 { get; set; }
    public decimal? NotaTrim3 { get; set; }
    public decimal? PromedioFinal { get; set; }
    public int AusenciasT1 { get; set; }
    public int TardanzasT1 { get; set; }
    public int AusenciasT2 { get; set; }
    public int TardanzasT2 { get; set; }
    public int AusenciasT3 { get; set; }
    public int TardanzasT3 { get; set; }
    public int TotalAusencias { get; set; }
    public int TotalTardanzas { get; set; }

    public static string FormatearNota(decimal? nota) =>
        CalificacionesTecnologiaFilaViewModel.FormatearNota(nota);
}
