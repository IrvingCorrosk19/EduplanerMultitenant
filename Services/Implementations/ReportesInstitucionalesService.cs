using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SchoolManager.Models;
using SchoolManager.Services.Helpers;
using SchoolManager.Services.Interfaces;
using SchoolManager.ViewModels;
using System.Drawing;
using System.Text.RegularExpressions;

namespace SchoolManager.Services.Implementations;

public class ReportesInstitucionalesService : IReportesInstitucionalesService
{
    private const int FilaDatosCalificaciones0 = 9;
    private const int MaxFilasEstudiantesCalificaciones = 75;
    private const int FilaDatosCarpetas0 = 12;
    private const int MaxFilasEstudiantesCarpetas = 40;

    private readonly SchoolDbContext _context;
    private readonly IAprobadosReprobadosService _aprobadosReprobadosService;
    private readonly IWebHostEnvironment _environment;
    private readonly Dictionary<(Guid SchoolId, Guid GroupId, Guid GradeLevelId), ReportesGrupoBulkData> _bulkCache = new();

    private static readonly string[] ColumnasHabitos =
    {
        "Responsabilidad", "Puntualidad", "Honradez", "Conciencia cívica", "Org. Del Trabajo",
        "Autodominio y confianza en sí mismo", "Iniciativa", "Cooperación",
        "Respeto a la propiedad ajena", "Modales", "Orden y Aseo", "Empleo en tiempo libre"
    };

    public ReportesInstitucionalesService(
        SchoolDbContext context,
        IAprobadosReprobadosService aprobadosReprobadosService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _aprobadosReprobadosService = aprobadosReprobadosService;
        _environment = environment;
    }

    private string ReportesDir => Path.Combine(_environment.ContentRootPath, "Reportes");

    private async Task<ReportesGrupoBulkData> GetBulkAsync(Guid schoolId, Guid groupId, Guid gradeLevelId)
    {
        var key = (schoolId, groupId, gradeLevelId);
        if (!_bulkCache.TryGetValue(key, out var bulk))
        {
            bulk = await ReportesInstitucionalesBulkLoader.LoadAsync(_context, schoolId, groupId, gradeLevelId);
            _bulkCache[key] = bulk;
        }
        return bulk;
    }

    private static void LlenarNotasPorSlotDesdeBulk(
        ReportesGrupoBulkData bulk,
        Guid studentId,
        IReadOnlyList<string> trimestres,
        List<(string Nombre, string[] PalabrasClave)> columnas,
        decimal?[][] notasPorSlot)
    {
        foreach (var trimNombre in trimestres)
        {
            var slot = ResolverSlotTrimestreInforme(trimNombre);
            if (!slot.HasValue || slot.Value < 0 || slot.Value > 2)
                continue;

            for (var j = 0; j < columnas.Count; j++)
            {
                notasPorSlot[slot.Value][j] = ReportesInstitucionalesBulkLoader.CalcularNotaFinal(
                    bulk, studentId, trimNombre, columnas[j].PalabrasClave);
            }
        }
    }

    public async Task<List<InformeEstudianteFilaDto>> ObtenerEstudiantesGrupoAsync(
        Guid schoolId, Guid groupId, Guid gradeLevelId, Guid? teacherScopeId, Guid? materiaId = null)
    {
        await ValidarAlcanceAsync(schoolId, groupId, gradeLevelId, teacherScopeId, materiaId);

        var estudiantes = await _context.StudentAssignments
            .AsNoTracking()
            .Where(sa =>
                sa.GroupId == groupId &&
                sa.GradeId == gradeLevelId &&
                sa.IsActive &&
                sa.Group != null &&
                sa.Group.SchoolId == schoolId)
            .Join(_context.Users.AsNoTracking().Where(u => u.SchoolId == null || u.SchoolId == schoolId),
                sa => sa.StudentId,
                u => u.Id,
                (sa, u) => new { u.Id, u.Name, u.LastName })
            .OrderBy(x => x.LastName).ThenBy(x => x.Name)
            .ToListAsync();

        return estudiantes.Select((e, i) => new InformeEstudianteFilaDto
        {
            Numero = i + 1,
            StudentId = e.Id,
            Nombre = $"{e.Name} {e.LastName}".Trim()
        }).ToList();
    }

    public async Task<byte[]> ExportarHabitosActitudesExcelAsync(
        Guid schoolId,
        string trimestre,
        string nivelEducativo,
        Guid groupId,
        Guid gradeLevelId,
        Guid? teacherScopeId,
        string profesorNombre)
    {
        var school = await _context.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == schoolId)
            ?? throw new Exception("Escuela no encontrada");
        var grupo = await _context.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.SchoolId == schoolId)
            ?? throw new Exception("Grupo no encontrado");
        var gradeLevel = await _context.GradeLevels.AsNoTracking().FirstOrDefaultAsync(g => g.Id == gradeLevelId && g.SchoolId == schoolId);

        await ValidarAlcanceAsync(schoolId, groupId, gradeLevelId, teacherScopeId, null);

        var estudiantes = await ObtenerEstudiantesGrupoAsync(schoolId, groupId, gradeLevelId, teacherScopeId);
        var trimestreLabel = FormatearEtiquetaTrimestre(trimestre);
        var etiquetaGrupo = FormatearEtiquetaGrupoInforme(gradeLevel?.Name, grupo.Name, grupo.Grade);
        var anio = DateTime.UtcNow.Year;

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Hábitos y Actitudes");
        var ultimaCol = 2 + ColumnasHabitos.Length;

        ws.Cells[1, 1, 1, ultimaCol].Merge = true;
        ws.Cells[1, 1].Value = "MINISTERIO DE EDUCACIÓN";
        ws.Cells[2, 1, 2, ultimaCol].Merge = true;
        ws.Cells[2, 1].Value = "DIRECCIÓN REGIONAL DE SAN MIGUELITO";
        ws.Cells[3, 1, 3, ultimaCol].Merge = true;
        ws.Cells[3, 1].Value = school.Name.ToUpperInvariant();
        ws.Cells[4, 1, 4, ultimaCol].Merge = true;
        ws.Cells[4, 1].Value = $"AÑO LECTIVO {anio}";

        ws.Cells[6, 1, 6, ultimaCol - 2].Merge = true;
        ws.Cells[6, 1].Value = "HÁBITOS Y ACTITUDES";
        ws.Cells[6, ultimaCol - 1, 6, ultimaCol].Merge = true;
        ws.Cells[6, ultimaCol - 1].Value = trimestreLabel;

        ws.Cells[7, 1, 7, 6].Merge = true;
        ws.Cells[7, 1].Value = $"Profesor(a) Consejero(a): {profesorNombre}";
        ws.Cells[7, 7, 7, ultimaCol].Merge = true;
        ws.Cells[7, 7].Value = $"Grupo: {etiquetaGrupo}";

        ws.Cells[8, 1, 8, ultimaCol].Merge = true;
        ws.Cells[8, 1].Value = "OBSERVACIONES: S = satisfecho, X = no satisfecho, R = regular";

        const int headerRow = 10;
        ws.Cells[headerRow, 1].Value = "N°";
        ws.Cells[headerRow, 2].Value = "NOMBRE DEL ESTUDIANTE";
        for (var i = 0; i < ColumnasHabitos.Length; i++)
            ws.Cells[headerRow, 3 + i].Value = ColumnasHabitos[i];

        var filaFin = headerRow + Math.Max(estudiantes.Count, 35);
        for (var i = 0; i < estudiantes.Count; i++)
        {
            var est = estudiantes[i];
            var row = headerRow + 1 + i;
            ws.Cells[row, 1].Value = est.Numero;
            ws.Cells[row, 2].Value = est.Nombre;
        }

        AplicarEstiloCuadroOficial(ws, 1, filaFin, ultimaCol, headerRow);
        ws.Column(1).Width = 5;
        ws.Column(2).Width = 38;
        for (var c = 3; c <= ultimaCol; c++)
            ws.Column(c).Width = 11;

        var filaPie = filaFin + 2;
        ws.Cells[filaPie, 1, filaPie, 6].Merge = true;
        ws.Cells[filaPie, 1].Value = "Profesor(a): _________________________";
        ws.Cells[filaPie, 7, filaPie, ultimaCol].Merge = true;
        ws.Cells[filaPie, 7].Value = "Consejero(a): _________________________";
        ws.Cells[filaPie + 1, 1, filaPie + 1, 4].Merge = true;
        ws.Cells[filaPie + 1, 1].Value = $"Grupo: {etiquetaGrupo}";
        ws.Cells[filaPie + 1, 5, filaPie + 1, ultimaCol].Merge = true;
        ws.Cells[filaPie + 1, 5].Value = "Jornada: _________________________";

        return package.GetAsByteArray();
    }

    public async Task<byte[]> ExportarCalificacionesInformeExcelAsync(
        InformeCalificacionesTipo tipo,
        Guid schoolId,
        string nivelEducativo,
        Guid groupId,
        Guid gradeLevelId,
        Guid? teacherScopeId,
        string consejeroNombre)
    {
        var school = await _context.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == schoolId)
            ?? throw new Exception("Escuela no encontrada");
        var grupo = await _context.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.SchoolId == schoolId)
            ?? throw new Exception("Grupo no encontrado");
        var gradeLevel = await _context.GradeLevels.AsNoTracking().FirstOrDefaultAsync(g => g.Id == gradeLevelId && g.SchoolId == schoolId);

        await ValidarAlcanceAsync(schoolId, groupId, gradeLevelId, teacherScopeId, null);

        var bulk = await GetBulkAsync(schoolId, groupId, gradeLevelId);
        var estudiantes = bulk.Estudiantes;
        var trimestres = await _aprobadosReprobadosService.ObtenerTrimestresDisponiblesAsync(schoolId);
        var columnas = ObtenerColumnasCalificaciones(tipo, gradeLevel?.Name);
        var etiquetaGrupo = FormatearEtiquetaGrupoInforme(gradeLevel?.Name, grupo.Name, grupo.Grade);
        var anio = DateTime.UtcNow.Year;

        var nombreParcial = tipo == InformeCalificacionesTipo.ExpresionesArtisticas
            ? "Exp"
            : "Tecnolog";
        var ruta = ReportePlantillaNpoiHelper.ResolverPlantilla(ReportesDir, nombreParcial);
        var workbook = ReportePlantillaNpoiHelper.CargarPlantilla(ruta);
        var sheet = workbook.GetSheetAt(0);

        ReportePlantillaNpoiHelper.EstablecerTexto(sheet, 2, 0, school.Name.ToUpperInvariant());
        ReportePlantillaNpoiHelper.EstablecerTexto(sheet, 3, 0, $"Informe de Calificaciones-{anio}");

        if (tipo == InformeCalificacionesTipo.Tecnologia)
        {
            ReportePlantillaNpoiHelper.EstablecerTexto(sheet, 6, 0, $"Consejero (a): {consejeroNombre}");
            ReportePlantillaNpoiHelper.EstablecerTexto(sheet, 6, 11, $"GRUPO: {etiquetaGrupo}");
        }
        else
        {
            ReportePlantillaNpoiHelper.EstablecerTexto(sheet, 5, 0, $"CONSEJERO (A): {consejeroNombre}");
            ReportePlantillaNpoiHelper.EstablecerTexto(sheet, 5, 10, $"GRADO: {etiquetaGrupo}");
        }

        var ultimaCol = tipo == InformeCalificacionesTipo.Tecnologia ? 14 : 11;
        ReportePlantillaNpoiHelper.LimpiarRangoDatos(
            sheet, FilaDatosCalificaciones0, FilaDatosCalificaciones0 + MaxFilasEstudiantesCalificaciones - 1,
            0, ultimaCol);

        var bloquesColumnas = tipo == InformeCalificacionesTipo.Tecnologia
            ? new[] { new[] { 2, 3, 4 }, new[] { 6, 7, 8 }, new[] { 10, 11, 12 } }
            : new[] { new[] { 2, 3 }, new[] { 5, 6 }, new[] { 8, 9 } };
        var colsPromedioTrimestre = tipo == InformeCalificacionesTipo.Tecnologia
            ? new[] { 5, 9, 13 }
            : new[] { 4, 7, 10 };
        var colPromedio = tipo == InformeCalificacionesTipo.Tecnologia ? 14 : 11;

        for (var i = 0; i < estudiantes.Count; i++)
        {
            var est = estudiantes[i];
            var fila = FilaDatosCalificaciones0 + i;
            ReportePlantillaNpoiHelper.EstablecerNumero(sheet, fila, 0, est.Numero);
            ReportePlantillaNpoiHelper.EstablecerTexto(sheet, fila, 1, est.Nombre);

            var notasPorSlot = new decimal?[3][];
            for (var s = 0; s < 3; s++)
                notasPorSlot[s] = new decimal?[columnas.Count];

            foreach (var trimNombre in trimestres)
            {
                var slot = ResolverSlotTrimestreInforme(trimNombre);
                if (!slot.HasValue || slot.Value < 0 || slot.Value >= bloquesColumnas.Length)
                    continue;

                var cols = bloquesColumnas[slot.Value];
                for (var j = 0; j < columnas.Count && j < cols.Length; j++)
                {
                    var nota = ReportesInstitucionalesBulkLoader.CalcularNotaFinal(
                        bulk, est.StudentId, trimNombre, columnas[j].PalabrasClave);
                    ReportePlantillaNpoiHelper.EstablecerNota(sheet, fila, cols[j], nota);
                    notasPorSlot[slot.Value][j] = nota;
                }
            }

            var (promediosTrimestre, final) = CalcularPromediosInformeCalificaciones(notasPorSlot);
            for (var t = 0; t < promediosTrimestre.Length && t < colsPromedioTrimestre.Length; t++)
                ReportePlantillaNpoiHelper.EstablecerNota(sheet, fila, colsPromedioTrimestre[t], promediosTrimestre[t]);
            ReportePlantillaNpoiHelper.EstablecerNota(sheet, fila, colPromedio, final);
        }

        return ReportePlantillaNpoiHelper.EscribirLibro(workbook);
    }

    public async Task<CalificacionesTecnologiaReportViewModel> ObtenerCalificacionesTecnologiaReporteAsync(
        Guid schoolId,
        string nivelEducativo,
        Guid groupId,
        Guid gradeLevelId,
        Guid? teacherScopeId,
        string consejeroNombre)
    {
        var school = await _context.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == schoolId)
            ?? throw new Exception("Escuela no encontrada");
        var grupo = await _context.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.SchoolId == schoolId)
            ?? throw new Exception("Grupo no encontrado");
        var gradeLevel = await _context.GradeLevels.AsNoTracking().FirstOrDefaultAsync(g => g.Id == gradeLevelId && g.SchoolId == schoolId);

        await ValidarAlcanceAsync(schoolId, groupId, gradeLevelId, teacherScopeId, null);

        var bulk = await GetBulkAsync(schoolId, groupId, gradeLevelId);
        var estudiantes = bulk.Estudiantes;
        var trimestres = await _aprobadosReprobadosService.ObtenerTrimestresDisponiblesAsync(schoolId);
        var columnas = ObtenerColumnasCalificaciones(InformeCalificacionesTipo.Tecnologia, gradeLevel?.Name);
        var etiquetaGrupo = FormatearEtiquetaGrupoInforme(gradeLevel?.Name, grupo.Name, grupo.Grade);
        var anio = DateTime.UtcNow.Year;

        var filas = new List<CalificacionesTecnologiaFilaViewModel>();
        foreach (var est in estudiantes)
        {
            var notasPorSlot = new decimal?[3][];
            for (var s = 0; s < 3; s++)
                notasPorSlot[s] = new decimal?[columnas.Count];

            LlenarNotasPorSlotDesdeBulk(bulk, est.StudentId, trimestres, columnas, notasPorSlot);

            var (promediosTrimestre, promedioFinal) = CalcularPromediosInformeCalificaciones(notasPorSlot);
            filas.Add(new CalificacionesTecnologiaFilaViewModel
            {
                Numero = est.Numero,
                Nombre = est.Nombre,
                NotaT1Area1 = notasPorSlot[0].ElementAtOrDefault(0),
                NotaT1Area2 = notasPorSlot[0].ElementAtOrDefault(1),
                NotaT1Area3 = notasPorSlot[0].ElementAtOrDefault(2),
                NotaT2Area1 = notasPorSlot[1].ElementAtOrDefault(0),
                NotaT2Area2 = notasPorSlot[1].ElementAtOrDefault(1),
                NotaT2Area3 = notasPorSlot[1].ElementAtOrDefault(2),
                NotaT3Area1 = notasPorSlot[2].ElementAtOrDefault(0),
                NotaT3Area2 = notasPorSlot[2].ElementAtOrDefault(1),
                NotaT3Area3 = notasPorSlot[2].ElementAtOrDefault(2),
                PromedioTrim1 = promediosTrimestre.ElementAtOrDefault(0),
                PromedioTrim2 = promediosTrimestre.ElementAtOrDefault(1),
                PromedioTrim3 = promediosTrimestre.ElementAtOrDefault(2),
                PromedioFinal = promedioFinal
            });
        }

        var trimestresEncabezado = new List<string>();
        for (var i = 0; i < 3; i++)
            trimestresEncabezado.Add(FormatearEncabezadoTrimestrePlantilla(trimestres, i));

        return new CalificacionesTecnologiaReportViewModel
        {
            LogoUrl = school.LogoUrl ?? "",
            InstitutoNombre = school.Name.ToUpperInvariant(),
            TituloInforme = $"Informe de Calificaciones-{anio}",
            ConsejeroNombre = consejeroNombre,
            GrupoEtiqueta = etiquetaGrupo,
            Areas = columnas.Select(c => c.Nombre).ToList(),
            TrimestresEncabezado = trimestresEncabezado,
            Filas = filas,
            FilasPlantillaVacias = Math.Max(0, MaxFilasEstudiantesCalificaciones - filas.Count)
        };
    }

    public async Task<HabitosActitudesReportViewModel> ObtenerHabitosActitudesReporteAsync(
        Guid schoolId,
        string trimestre,
        string nivelEducativo,
        Guid groupId,
        Guid gradeLevelId,
        Guid? teacherScopeId,
        string consejeroNombre)
    {
        var school = await _context.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == schoolId)
            ?? throw new Exception("Escuela no encontrada");
        var grupo = await _context.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.SchoolId == schoolId)
            ?? throw new Exception("Grupo no encontrado");
        var gradeLevel = await _context.GradeLevels.AsNoTracking().FirstOrDefaultAsync(g => g.Id == gradeLevelId && g.SchoolId == schoolId);

        await ValidarAlcanceAsync(schoolId, groupId, gradeLevelId, teacherScopeId, null);

        var estudiantes = await ObtenerEstudiantesGrupoAsync(schoolId, groupId, gradeLevelId, teacherScopeId);
        var etiquetaGrupo = FormatearEtiquetaGrupoInforme(gradeLevel?.Name, grupo.Name, grupo.Grade);
        var anio = DateTime.UtcNow.Year;
        const int maxFilas = 40;

        return new HabitosActitudesReportViewModel
        {
            LogoUrl = school.LogoUrl ?? "",
            InstitutoNombre = school.Name.ToUpperInvariant(),
            AnioLectivoLinea = $"AÑO LECTIVO {anio}",
            TrimestreLinea = FormatearEtiquetaTrimestre(trimestre),
            ConsejeroNombre = consejeroNombre,
            GrupoEtiqueta = etiquetaGrupo,
            ColumnasHabitos = ColumnasHabitos.ToList(),
            Filas = estudiantes.Select(e => new HabitosActitudesFilaViewModel
            {
                Numero = e.Numero,
                Nombre = e.Nombre
            }).ToList(),
            FilasPlantillaVacias = Math.Max(0, maxFilas - estudiantes.Count)
        };
    }

    public async Task<CalificacionesExpresionesArtisticasReportViewModel> ObtenerCalificacionesExpresionesArtisticasReporteAsync(
        Guid schoolId,
        string nivelEducativo,
        Guid groupId,
        Guid gradeLevelId,
        Guid? teacherScopeId,
        string consejeroNombre)
    {
        var school = await _context.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == schoolId)
            ?? throw new Exception("Escuela no encontrada");
        var grupo = await _context.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.SchoolId == schoolId)
            ?? throw new Exception("Grupo no encontrado");
        var gradeLevel = await _context.GradeLevels.AsNoTracking().FirstOrDefaultAsync(g => g.Id == gradeLevelId && g.SchoolId == schoolId);

        await ValidarAlcanceAsync(schoolId, groupId, gradeLevelId, teacherScopeId, null);

        var bulk = await GetBulkAsync(schoolId, groupId, gradeLevelId);
        var estudiantes = bulk.Estudiantes;
        var trimestres = await _aprobadosReprobadosService.ObtenerTrimestresDisponiblesAsync(schoolId);
        var columnas = ObtenerColumnasCalificaciones(InformeCalificacionesTipo.ExpresionesArtisticas, gradeLevel?.Name);
        var etiquetaGrado = FormatearEtiquetaGrupoInforme(gradeLevel?.Name, grupo.Name, grupo.Grade);
        var anio = DateTime.UtcNow.Year;

        var filas = new List<CalificacionesExpresionesArtisticasFilaViewModel>();
        foreach (var est in estudiantes)
        {
            var notasPorSlot = new decimal?[3][];
            for (var s = 0; s < 3; s++)
                notasPorSlot[s] = new decimal?[columnas.Count];

            LlenarNotasPorSlotDesdeBulk(bulk, est.StudentId, trimestres, columnas, notasPorSlot);

            var (promediosTrimestre, promedioFinal) = CalcularPromediosInformeCalificaciones(notasPorSlot);
            filas.Add(new CalificacionesExpresionesArtisticasFilaViewModel
            {
                Numero = est.Numero,
                Nombre = est.Nombre,
                NotaT1Artistica = notasPorSlot[0].ElementAtOrDefault(0),
                NotaT1Musical = notasPorSlot[0].ElementAtOrDefault(1),
                NotaT2Artistica = notasPorSlot[1].ElementAtOrDefault(0),
                NotaT2Musical = notasPorSlot[1].ElementAtOrDefault(1),
                NotaT3Artistica = notasPorSlot[2].ElementAtOrDefault(0),
                NotaT3Musical = notasPorSlot[2].ElementAtOrDefault(1),
                PromedioTrim1 = promediosTrimestre.ElementAtOrDefault(0),
                PromedioTrim2 = promediosTrimestre.ElementAtOrDefault(1),
                PromedioTrim3 = promediosTrimestre.ElementAtOrDefault(2),
                PromedioFinal = promedioFinal
            });
        }

        var trimestresEncabezado = new List<string>();
        for (var i = 0; i < 3; i++)
            trimestresEncabezado.Add(FormatearEncabezadoTrimestrePlantilla(trimestres, i));

        return new CalificacionesExpresionesArtisticasReportViewModel
        {
            LogoUrl = school.LogoUrl ?? "",
            InstitutoNombre = school.Name.ToUpperInvariant(),
            TituloInforme = $"Informe de Calificaciones-{anio}",
            ConsejeroNombre = consejeroNombre,
            GradoEtiqueta = etiquetaGrado,
            TrimestresEncabezado = trimestresEncabezado,
            Filas = filas,
            FilasPlantillaVacias = Math.Max(0, MaxFilasEstudiantesCalificaciones - filas.Count)
        };
    }

    public async Task<FormatoCarpetasReportViewModel> ObtenerFormatoCarpetasReporteAsync(
        Guid schoolId,
        string nivelEducativo,
        Guid materiaId,
        Guid groupId,
        Guid gradeLevelId,
        Guid? teacherScopeId,
        string consejeroNombre,
        string profesorNombre)
    {
        var school = await _context.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == schoolId)
            ?? throw new Exception("Escuela no encontrada");
        var grupo = await _context.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.SchoolId == schoolId)
            ?? throw new Exception("Grupo no encontrado");
        var gradeLevel = await _context.GradeLevels.AsNoTracking().FirstOrDefaultAsync(g => g.Id == gradeLevelId && g.SchoolId == schoolId);
        var materia = await _context.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == materiaId && s.SchoolId == schoolId)
            ?? throw new Exception("Materia no encontrada");

        await ValidarAlcanceAsync(schoolId, groupId, gradeLevelId, teacherScopeId, materiaId);

        var bulk = await GetBulkAsync(schoolId, groupId, gradeLevelId);
        var estudiantes = bulk.Estudiantes;
        var trimestres = await _aprobadosReprobadosService.ObtenerTrimestresDisponiblesAsync(schoolId);
        var trimesterEntities = bulk.TrimesterEntities
            .Where(t => trimestres.Contains(t.Name))
            .ToList();

        var etiquetaGrupo = FormatearEtiquetaGrupoInforme(gradeLevel?.Name, grupo.Name, grupo.Grade);
        var anio = DateTime.UtcNow.Year;
        var filas = new List<FormatoCarpetasFilaViewModel>();
        var palabrasMateria = new[] { materia.Name };

        foreach (var est in estudiantes)
        {
            var promediosTrim = new List<decimal>();
            var totalA = 0;
            var totalT = 0;
            decimal? n1 = null, n2 = null, n3 = null;
            int a1 = 0, t1 = 0, a2 = 0, t2 = 0, a3 = 0, t3 = 0;

            for (var i = 0; i < trimestres.Count && i < 3; i++)
            {
                var trimesterEntity = trimesterEntities.FirstOrDefault(x => x.Name == trimestres[i]);
                var prom = ReportesInstitucionalesBulkLoader.CalcularNotaFinal(
                    bulk, est.StudentId, trimestres[i], palabrasMateria);
                var (ausencias, tardanzas) = ReportesInstitucionalesBulkLoader.ContarAsistencia(
                    bulk, est.StudentId, trimesterEntity);

                if (i == 0) { n1 = prom; a1 = ausencias; t1 = tardanzas; }
                else if (i == 1) { n2 = prom; a2 = ausencias; t2 = tardanzas; }
                else { n3 = prom; a3 = ausencias; t3 = tardanzas; }

                if (prom.HasValue) promediosTrim.Add(prom.Value);
                totalA += ausencias;
                totalT += tardanzas;
            }

            filas.Add(new FormatoCarpetasFilaViewModel
            {
                Numero = est.Numero,
                Nombre = est.Nombre,
                NotaTrim1 = n1,
                NotaTrim2 = n2,
                NotaTrim3 = n3,
                PromedioFinal = promediosTrim.Count > 0 ? Math.Round(promediosTrim.Average(), 1) : null,
                AusenciasT1 = a1,
                TardanzasT1 = t1,
                AusenciasT2 = a2,
                TardanzasT2 = t2,
                AusenciasT3 = a3,
                TardanzasT3 = t3,
                TotalAusencias = totalA,
                TotalTardanzas = totalT
            });
        }

        return new FormatoCarpetasReportViewModel
        {
            LogoUrl = school.LogoUrl ?? "",
            InstitutoNombre = school.Name.ToUpperInvariant(),
            AnioLectivoLinea = $" Año Lectivo {anio}",
            ConsejeroNombre = consejeroNombre,
            ProfesorNombre = profesorNombre,
            GrupoEtiqueta = etiquetaGrupo,
            MateriaNombre = materia.Name,
            TrimestresEncabezado = trimestres.Take(3).ToList(),
            Filas = filas,
            FilasPlantillaVacias = Math.Max(0, MaxFilasEstudiantesCarpetas - filas.Count)
        };
    }

    public async Task<byte[]> ExportarFormatoCarpetasExcelAsync(
        Guid schoolId,
        string nivelEducativo,
        Guid materiaId,
        Guid groupId,
        Guid gradeLevelId,
        Guid? teacherScopeId,
        string consejeroNombre,
        string profesorNombre)
    {
        var school = await _context.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == schoolId)
            ?? throw new Exception("Escuela no encontrada");
        var grupo = await _context.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.SchoolId == schoolId)
            ?? throw new Exception("Grupo no encontrado");
        var gradeLevel = await _context.GradeLevels.AsNoTracking().FirstOrDefaultAsync(g => g.Id == gradeLevelId && g.SchoolId == schoolId);
        var materia = await _context.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == materiaId && s.SchoolId == schoolId)
            ?? throw new Exception("Materia no encontrada");

        await ValidarAlcanceAsync(schoolId, groupId, gradeLevelId, teacherScopeId, materiaId);

        var bulk = await GetBulkAsync(schoolId, groupId, gradeLevelId);
        var estudiantes = bulk.Estudiantes;
        var trimestres = await _aprobadosReprobadosService.ObtenerTrimestresDisponiblesAsync(schoolId);
        var trimesterEntities = bulk.TrimesterEntities
            .Where(t => trimestres.Contains(t.Name))
            .ToList();

        var etiquetaGrupo = FormatearEtiquetaGrupoInforme(gradeLevel?.Name, grupo.Name, grupo.Grade);
        var anio = DateTime.UtcNow.Year;
        var ruta = ReportePlantillaNpoiHelper.ResolverPlantilla(ReportesDir, "Carpetas");
        var workbook = ReportePlantillaNpoiHelper.CargarPlantilla(ruta);
        var sheet = workbook.GetSheetAt(0);
        var palabrasMateria = new[] { materia.Name };

        ReportePlantillaNpoiHelper.EstablecerTexto(sheet, 1, 0, school.Name.ToUpperInvariant());
        ReportePlantillaNpoiHelper.EstablecerTexto(sheet, 2, 0, "Informe de Calificaciones, Ausencias y Tardanzas");
        ReportePlantillaNpoiHelper.EstablecerTexto(sheet, 3, 1, $" Año Lectivo {anio}");
        ReportePlantillaNpoiHelper.EstablecerTexto(sheet, 5, 1, $"Consejero (a): {consejeroNombre}");
        ReportePlantillaNpoiHelper.EstablecerTexto(sheet, 5, 4, $"Profesor (a): {profesorNombre}");
        ReportePlantillaNpoiHelper.EstablecerTexto(sheet, 7, 1, $"Grupo: {etiquetaGrupo}");
        ReportePlantillaNpoiHelper.EstablecerTexto(sheet, 7, 6, $"Asignatura: {materia.Name}");

        ReportePlantillaNpoiHelper.LimpiarRangoDatos(
            sheet, FilaDatosCarpetas0, FilaDatosCarpetas0 + MaxFilasEstudiantesCarpetas - 1,
            0, 13);

        var colsNotaTrim = new[] { 2, 3, 4 };
        var colsAt = new[] { (6, 7), (8, 9), (10, 11) };

        for (var i = 0; i < estudiantes.Count; i++)
        {
            var est = estudiantes[i];
            var fila = FilaDatosCarpetas0 + i;
            ReportePlantillaNpoiHelper.EstablecerNumero(sheet, fila, 0, est.Numero);
            ReportePlantillaNpoiHelper.EstablecerTexto(sheet, fila, 1, est.Nombre);

            var promediosTrim = new List<decimal>();
            var totalA = 0;
            var totalT = 0;

            for (var t = 0; t < trimestres.Count && t < colsNotaTrim.Length; t++)
            {
                var trimesterEntity = trimesterEntities.FirstOrDefault(x => x.Name == trimestres[t]);
                var prom = ReportesInstitucionalesBulkLoader.CalcularNotaFinal(
                    bulk, est.StudentId, trimestres[t], palabrasMateria);
                ReportePlantillaNpoiHelper.EstablecerNota(sheet, fila, colsNotaTrim[t], prom);
                if (prom.HasValue)
                    promediosTrim.Add(prom.Value);

                var (ausencias, tardanzas) = ReportesInstitucionalesBulkLoader.ContarAsistencia(
                    bulk, est.StudentId, trimesterEntity);
                var (colA, colT) = colsAt[t];
                ReportePlantillaNpoiHelper.EstablecerNumero(sheet, fila, colA, ausencias);
                ReportePlantillaNpoiHelper.EstablecerNumero(sheet, fila, colT, tardanzas);
                totalA += ausencias;
                totalT += tardanzas;
            }

            var promFinal = promediosTrim.Count > 0
                ? Math.Round(promediosTrim.Average(), 1)
                : (decimal?)null;
            ReportePlantillaNpoiHelper.EstablecerNota(sheet, fila, 5, promFinal);
            ReportePlantillaNpoiHelper.EstablecerNumero(sheet, fila, 12, totalA);
            ReportePlantillaNpoiHelper.EstablecerNumero(sheet, fila, 13, totalT);
        }

        AgregarPieFirmasFormatoCarpetas(workbook, sheet);

        return ReportePlantillaNpoiHelper.EscribirLibro(workbook);
    }

    private static void AgregarPieFirmasFormatoCarpetas(HSSFWorkbook workbook, ISheet sheet)
    {
        const int primeraCol = 0;
        const int ultimaCol = 13;
        var filaInicio = FilaDatosCarpetas0 + MaxFilasEstudiantesCarpetas + 2;

        RemoverRegionesCombinadasEnRango(sheet, filaInicio, filaInicio + 9);

        var titleFont = workbook.CreateFont();
        titleFont.IsBold = true;
        titleFont.FontHeightInPoints = 11;

        var labelFont = workbook.CreateFont();
        labelFont.IsBold = true;
        labelFont.FontHeightInPoints = 10;

        var centeredStyle = workbook.CreateCellStyle();
        centeredStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
        centeredStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
        centeredStyle.SetFont(labelFont);

        var lineStyle = workbook.CreateCellStyle();
        lineStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Medium;
        lineStyle.BottomBorderColor = IndexedColors.Black.Index;

        var titleStyle = workbook.CreateCellStyle();
        titleStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
        titleStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
        titleStyle.SetFont(titleFont);

        var observationLineStyle = workbook.CreateCellStyle();
        observationLineStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
        observationLineStyle.BottomBorderColor = IndexedColors.Black.Index;

        CrearFila(sheet, filaInicio, 20);
        Combinar(sheet, filaInicio, 1, 5);
        AplicarEstilo(sheet, filaInicio, 1, 5, lineStyle);
        Combinar(sheet, filaInicio, 8, 12);
        AplicarEstilo(sheet, filaInicio, 8, 12, lineStyle);

        CrearFila(sheet, filaInicio + 1, 18);
        EstablecerCelda(sheet, filaInicio + 1, 1, "Firma del Docente", centeredStyle);
        Combinar(sheet, filaInicio + 1, 1, 5);
        EstablecerCelda(sheet, filaInicio + 1, 8, "Firma del Director(a)", centeredStyle);
        Combinar(sheet, filaInicio + 1, 8, 12);

        CrearFila(sheet, filaInicio + 3, 20);
        EstablecerCelda(sheet, filaInicio + 3, primeraCol, "Observación:", titleStyle);
        Combinar(sheet, filaInicio + 3, primeraCol, ultimaCol);

        for (var row = filaInicio + 5; row <= filaInicio + 8; row++)
        {
            CrearFila(sheet, row, 19);
            Combinar(sheet, row, primeraCol, ultimaCol);
            AplicarEstilo(sheet, row, primeraCol, ultimaCol, observationLineStyle);
        }
    }

    private static IRow CrearFila(ISheet sheet, int rowIndex, short heightPoints)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        row.HeightInPoints = heightPoints;
        return row;
    }

    private static void EstablecerCelda(ISheet sheet, int rowIndex, int columnIndex, string text, ICellStyle style)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        var cell = row.GetCell(columnIndex) ?? row.CreateCell(columnIndex);
        cell.SetCellValue(text);
        cell.CellStyle = style;
    }

    private static void AplicarEstilo(ISheet sheet, int rowIndex, int fromColumn, int toColumn, ICellStyle style)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        for (var column = fromColumn; column <= toColumn; column++)
        {
            var cell = row.GetCell(column) ?? row.CreateCell(column);
            cell.CellStyle = style;
        }
    }

    private static void Combinar(ISheet sheet, int rowIndex, int fromColumn, int toColumn)
    {
        sheet.AddMergedRegion(new CellRangeAddress(rowIndex, rowIndex, fromColumn, toColumn));
    }

    private static void RemoverRegionesCombinadasEnRango(ISheet sheet, int firstRow, int lastRow)
    {
        for (var i = sheet.NumMergedRegions - 1; i >= 0; i--)
        {
            var region = sheet.GetMergedRegion(i);
            if (region.FirstRow <= lastRow && region.LastRow >= firstRow)
            {
                sheet.RemoveMergedRegion(i);
            }
        }
    }

    private async Task ValidarAlcanceAsync(
        Guid schoolId, Guid groupId, Guid gradeLevelId, Guid? teacherScopeId, Guid? materiaId)
    {
        var grupoOk = await _context.Groups.AsNoTracking()
            .AnyAsync(g => g.Id == groupId && g.SchoolId == schoolId);
        if (!grupoOk)
            throw new UnauthorizedAccessException("El grupo no pertenece a la escuela actual.");

        var gradoOk = await _context.GradeLevels.AsNoTracking()
            .AnyAsync(g => g.Id == gradeLevelId && g.SchoolId == schoolId);
        if (!gradoOk)
            throw new UnauthorizedAccessException("El grado no pertenece a la escuela actual.");

        if (materiaId.HasValue && materiaId.Value != Guid.Empty)
        {
            var materiaOk = await _context.Subjects.AsNoTracking()
                .AnyAsync(s => s.Id == materiaId.Value && s.SchoolId == schoolId);
            if (!materiaOk)
                throw new UnauthorizedAccessException("La materia no pertenece a la escuela actual.");
        }

        if (!teacherScopeId.HasValue) return;

        var query = _context.TeacherAssignments
            .AsNoTracking()
            .Where(ta =>
                ta.TeacherId == teacherScopeId.Value &&
                ta.SchoolId == schoolId &&
                ta.SubjectAssignment.GroupId == groupId &&
                ta.SubjectAssignment.GradeLevelId == gradeLevelId);

        if (materiaId.HasValue && materiaId.Value != Guid.Empty)
            query = query.Where(ta => ta.SubjectAssignment.SubjectId == materiaId.Value);

        if (!await query.AnyAsync())
            throw new UnauthorizedAccessException("La asignación seleccionada no está disponible para este docente.");
    }

    private static int? ExtractGradeNumber(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        var match = Regex.Match(name, @"(\d+)");
        return match.Success && int.TryParse(match.Value, out var n) ? n : null;
    }

    /// <summary>
    /// Etiqueta tipo "9-G" para encabezados. Prioriza el grado académico seleccionado (grade_levels)
    /// sobre groups.grade del catálogo, que puede no coincidir con la asignación real.
    /// </summary>
    private static string FormatearEtiquetaGrupoInforme(string? gradeLevelName, string groupName, string? groupGrade)
    {
        var grado = !string.IsNullOrWhiteSpace(gradeLevelName)
            ? gradeLevelName.Trim()
            : !string.IsNullOrWhiteSpace(groupGrade)
                ? groupGrade.Trim()
                : "";

        var nombre = groupName.Trim();
        if (string.IsNullOrEmpty(nombre))
            return grado;

        if (nombre.Contains('-', StringComparison.Ordinal))
            return nombre;

        var gradoCorto = grado.Replace("°", "", StringComparison.Ordinal).Trim();
        return string.IsNullOrEmpty(gradoCorto) ? nombre : $"{gradoCorto}-{nombre}";
    }

    private static List<(string Nombre, string[] PalabrasClave)> ObtenerColumnasCalificaciones(
        InformeCalificacionesTipo tipo, string? gradeLevelName)
    {
        if (tipo == InformeCalificacionesTipo.ExpresionesArtisticas)
        {
            return new()
            {
                ("EDUC. ARTÍSTICA", new[] { "ART", "ARTÍST", "ARTIST" }),
                ("EDUC. MUSICAL", new[] { "MUSICAL", "MÚSICA", "MUSICA" })
            };
        }

        var esGrado9 = ExtractGradeNumber(gradeLevelName) == 9;
        var col1 = esGrado9
            ? ("CONTABILIDAD", new[] { "CONTABIL" })
            : ("COMERCIO", new[] { "COMERC" });

        return new()
        {
            col1,
            ("EDUC. HOGAR", new[] { "HOGAR" }),
            ("ART. INDUSTRIALES", new[] { "INDUSTRIAL", "INDUST" })
        };
    }

    private static string FormatearEncabezadoTrimestrePlantilla(IReadOnlyList<string> trimestres, int indice)
    {
        if (indice < trimestres.Count)
        {
            var nombre = trimestres[indice].Trim().ToUpperInvariant();
            if (nombre.Contains("TRIMESTRE", StringComparison.OrdinalIgnoreCase))
                return nombre.EndsWith(' ') ? nombre : nombre + " ";
            return indice switch
            {
                0 => "I- TRIMESTRE ",
                1 => "II- TRIMESTRE ",
                2 => "III- TRIMESTRE ",
                _ => $"{nombre} "
            };
        }

        return indice switch
        {
            0 => "I- TRIMESTRE ",
            1 => "II- TRIMESTRE ",
            2 => "III- TRIMESTRE ",
            _ => ""
        };
    }

    /// <summary>
    /// Promedio trimestral: solo asignaturas con nota. Promedio final: promedio de los trimestres que tengan al menos una nota.
    /// </summary>
    private static (decimal?[] PromediosTrimestre, decimal? PromedioFinal) CalcularPromediosInformeCalificaciones(
        decimal?[][] notasPorSlot)
    {
        var promediosTrimestre = new decimal?[3];
        var promediosParaFinal = new List<decimal>();

        for (var i = 0; i < notasPorSlot.Length && i < 3; i++)
        {
            var prom = CalcularPromedioTrimestreInforme(notasPorSlot[i]);
            promediosTrimestre[i] = prom;
            if (prom.HasValue)
                promediosParaFinal.Add(prom.Value);
        }

        var final = promediosParaFinal.Count > 0
            ? GradebookFinalGradeCalculator.TruncateOneDecimal(promediosParaFinal.Average())
            : (decimal?)null;

        return (promediosTrimestre, final);
    }

    private static decimal? CalcularPromedioTrimestreInforme(decimal?[]? slot)
    {
        if (slot == null || slot.Length == 0)
            return null;

        var validas = slot.Where(n => n.HasValue).Select(n => n!.Value).ToList();
        return validas.Count > 0 ? GradebookFinalGradeCalculator.TruncateOneDecimal(validas.Average()) : null;
    }

    /// <summary>
    /// Columna del informe (0 = I trimestre, 1 = II, 2 = III) según el nombre en BD (p. ej. 1T, 2T).
    /// </summary>
    private static int? ResolverSlotTrimestreInforme(string? trimestre)
    {
        if (string.IsNullOrWhiteSpace(trimestre))
            return null;

        var t = trimestre.Trim().ToUpperInvariant();
        if (t is "1T" or "T1" or "I" or "1" or "PRIMERO" or "1RO" or "1ER")
            return 0;
        if (t is "2T" or "T2" or "II" or "2" or "SEGUNDO" or "2DO")
            return 1;
        if (t is "3T" or "T3" or "III" or "3" or "TERCERO" or "3RO" or "3ER")
            return 2;

        if (!t.Contains("TRIMESTRE", StringComparison.OrdinalIgnoreCase))
            return null;

        if (t.Contains("III", StringComparison.Ordinal) ||
            t.Contains("TERCER", StringComparison.OrdinalIgnoreCase))
            return 2;
        if (t.Contains("II", StringComparison.Ordinal) ||
            t.Contains("SEGUNDO", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (t.Contains('I') || t.Contains("PRIMER", StringComparison.OrdinalIgnoreCase))
            return 0;

        return null;
    }

    private static string FormatearEtiquetaTrimestre(string trimestre)
    {
        if (AprobadosReprobadosFiltroValores.EsTodos(trimestre))
            return "I TRIMESTRE";

        var t = trimestre.Trim().ToUpperInvariant();
        if (t is "I" or "1" or "PRIMERO" or "1RO" or "1ER")
            return "I TRIMESTRE";
        if (t is "II" or "2" or "SEGUNDO" or "2DO" or "2DO")
            return "II TRIMESTRE";
        if (t is "III" or "3" or "TERCERO" or "3RO" or "3ER")
            return "III TRIMESTRE";

        return t.Contains("TRIMESTRE", StringComparison.OrdinalIgnoreCase)
            ? t
            : $"{t} TRIMESTRE";
    }

    private static void AplicarEstiloCuadroOficial(
        ExcelWorksheet ws, int filaInicio, int filaFin, int ultimaCol, int headerRow)
    {
        var titulo = ws.Cells[filaInicio, 1, 4, ultimaCol];
        titulo.Style.Font.Bold = true;
        titulo.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        ws.Cells[6, 1].Style.Font.Bold = true;
        ws.Cells[6, ultimaCol - 1].Style.Font.Bold = true;
        ws.Cells[6, ultimaCol - 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        using var encabezado = ws.Cells[headerRow, 1, headerRow, ultimaCol];
        encabezado.Style.Font.Bold = true;
        encabezado.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        encabezado.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        encabezado.Style.WrapText = true;

        using var cuadro = ws.Cells[filaInicio, 1, filaFin, ultimaCol];
        cuadro.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        cuadro.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        cuadro.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        cuadro.Style.Border.Right.Style = ExcelBorderStyle.Thin;
    }
}
