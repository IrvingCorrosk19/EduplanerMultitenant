using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using SchoolManager.Services.Interfaces;

using SchoolManager.ViewModels;



namespace SchoolManager.Controllers;



[Authorize(Roles = "admin,director,teacher")]

public class HabitosActitudesReportController : InformeInstitucionalControllerBase

{

    private readonly IReportesInstitucionalesService _reportesService;

    private readonly IInformeInstitucionalHtmlPdfService _pdfService;

    private readonly IInformeInstitucionalRazorRenderService _razorRenderService;



    public HabitosActitudesReportController(

        ICurrentUserService currentUserService,

        IAprobadosReprobadosService aprobadosReprobadosService,

        ICounselorAssignmentService counselorAssignmentService,

        IReportesInstitucionalesService reportesService,

        IInformeInstitucionalHtmlPdfService pdfService,

        IInformeInstitucionalRazorRenderService razorRenderService)

        : base(currentUserService, aprobadosReprobadosService, counselorAssignmentService)

    {

        _reportesService = reportesService;

        _pdfService = pdfService;

        _razorRenderService = razorRenderService;

    }



    [HttpGet]

    public async Task<IActionResult> Index()

    {

        var (user, error) = await ObtenerUsuarioEscuelaAsync();

        if (error != null) return error;

        ViewBag.EsDocente = GetTeacherScopeId(user).HasValue;

        ViewBag.TrimestresDisponibles =

            await _aprobadosReprobadosService.ObtenerTrimestresDisponiblesAsync(user!.SchoolId!.Value);

        return View();

    }



    [HttpGet]

    public async Task<IActionResult> VistaPrevia(

        string trimestre, string nivelEducativo, Guid groupId, Guid gradeLevelId)

    {

        try

        {

            var (user, error) = await ObtenerUsuarioEscuelaAsync();

            if (error != null) return error;

            if (string.IsNullOrWhiteSpace(trimestre) || string.IsNullOrWhiteSpace(nivelEducativo))

            {

                TempData["Error"] = "Trimestre y grado son requeridos.";

                return RedirectToAction(nameof(Index));

            }



            var consejeroNombre = await ResolverConsejeroNombreAsync(user!.SchoolId!.Value, groupId, gradeLevelId);

            var model = await _reportesService.ObtenerHabitosActitudesReporteAsync(

                user.SchoolId!.Value, trimestre, nivelEducativo, groupId, gradeLevelId,

                GetTeacherScopeId(user), consejeroNombre);

            return View(model);

        }

        catch (UnauthorizedAccessException ex)

        {

            TempData["Error"] = ex.Message;

            return RedirectToAction(nameof(Index));

        }

    }



    [HttpGet]

    public async Task<IActionResult> ExportarPdf(

        string trimestre, string nivelEducativo, Guid groupId, Guid gradeLevelId)

    {

        try

        {

            var (user, error) = await ObtenerUsuarioEscuelaAsync();

            if (error != null) return error;

            if (string.IsNullOrWhiteSpace(trimestre) || string.IsNullOrWhiteSpace(nivelEducativo))

                return BadRequest("Trimestre y grado son requeridos.");



            var consejeroNombre = await ResolverConsejeroNombreAsync(user!.SchoolId!.Value, groupId, gradeLevelId);

            var model = await _reportesService.ObtenerHabitosActitudesReporteAsync(

                user.SchoolId!.Value, trimestre, nivelEducativo, groupId, gradeLevelId,

                GetTeacherScopeId(user), consejeroNombre);

            var html = await _razorRenderService.RenderViewToStringAsync(

                "~/Views/HabitosActitudesReport/VistaPrevia.cshtml", model);

            var bytes = await _pdfService.GenerarPdfDesdeHtmlAsync(html, landscape: true);

            return File(bytes, "application/pdf", $"Habitos_Actitudes_{trimestre}.pdf");

        }

        catch (UnauthorizedAccessException ex)

        {

            TempData["Error"] = ex.Message;

            return RedirectToAction(nameof(Index));

        }

    }



    [HttpGet]

    public async Task<IActionResult> ExportarExcel(

        string trimestre, string nivelEducativo, Guid groupId, Guid gradeLevelId)

    {

        try

        {

            var (user, error) = await ObtenerUsuarioEscuelaAsync();

            if (error != null) return error;

            if (string.IsNullOrWhiteSpace(trimestre) || string.IsNullOrWhiteSpace(nivelEducativo))

                return BadRequest("Trimestre y nivel son requeridos.");



            var consejeroNombre = await ResolverConsejeroNombreAsync(user!.SchoolId!.Value, groupId, gradeLevelId);

            var bytes = await _reportesService.ExportarHabitosActitudesExcelAsync(

                user.SchoolId!.Value, trimestre, nivelEducativo, groupId, gradeLevelId,

                GetTeacherScopeId(user), consejeroNombre);



            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                $"Habitos_Actitudes_{trimestre}_{DateTime.UtcNow:yyyyMMdd}.xlsx");

        }

        catch (UnauthorizedAccessException ex)

        {

            TempData["Error"] = ex.Message;

            return RedirectToAction(nameof(Index));

        }

    }

}



[Authorize(Roles = "admin,director,teacher")]

public class CalificacionesInformeController : InformeInstitucionalControllerBase

{

    private readonly IReportesInstitucionalesService _reportesService;

    private readonly IInformeInstitucionalHtmlPdfService _pdfService;

    private readonly IInformeInstitucionalRazorRenderService _razorRenderService;



    public CalificacionesInformeController(

        ICurrentUserService currentUserService,

        IAprobadosReprobadosService aprobadosReprobadosService,

        ICounselorAssignmentService counselorAssignmentService,

        IReportesInstitucionalesService reportesService,

        IInformeInstitucionalHtmlPdfService pdfService,

        IInformeInstitucionalRazorRenderService razorRenderService)

        : base(currentUserService, aprobadosReprobadosService, counselorAssignmentService)

    {

        _reportesService = reportesService;

        _pdfService = pdfService;

        _razorRenderService = razorRenderService;

    }



    [HttpGet]

    public async Task<IActionResult> ExpresionesArtisticas()

    {

        var (user, error) = await ObtenerUsuarioEscuelaAsync();

        if (error != null) return error;

        ViewBag.EsDocente = GetTeacherScopeId(user).HasValue;

        return View();

    }



    [HttpGet]

    public async Task<IActionResult> Tecnologia()

    {

        var (user, error) = await ObtenerUsuarioEscuelaAsync();

        if (error != null) return error;

        ViewBag.EsDocente = GetTeacherScopeId(user).HasValue;

        return View();

    }



    [HttpGet]

    public async Task<IActionResult> VistaPreviaTecnologia(string nivelEducativo, Guid groupId, Guid gradeLevelId)

    {

        try

        {

            var (user, error) = await ObtenerUsuarioEscuelaAsync();

            if (error != null) return error;

            if (string.IsNullOrWhiteSpace(nivelEducativo))

            {

                TempData["Error"] = "Seleccione un grado/nivel.";

                return RedirectToAction(nameof(Tecnologia));

            }



            var consejeroNombre = await ResolverConsejeroNombreAsync(user!.SchoolId!.Value, groupId, gradeLevelId);

            var model = await _reportesService.ObtenerCalificacionesTecnologiaReporteAsync(

                user.SchoolId!.Value, nivelEducativo, groupId, gradeLevelId,

                GetTeacherScopeId(user), consejeroNombre);

            return View(model);

        }

        catch (UnauthorizedAccessException ex)

        {

            TempData["Error"] = ex.Message;

            return RedirectToAction(nameof(Tecnologia));

        }

    }



    [HttpGet]

    public async Task<IActionResult> VistaPreviaExpresionesArtisticas(

        string nivelEducativo, Guid groupId, Guid gradeLevelId)

    {

        try

        {

            var (user, error) = await ObtenerUsuarioEscuelaAsync();

            if (error != null) return error;

            if (string.IsNullOrWhiteSpace(nivelEducativo))

            {

                TempData["Error"] = "Seleccione un grado/nivel.";

                return RedirectToAction(nameof(ExpresionesArtisticas));

            }



            var consejeroNombre = await ResolverConsejeroNombreAsync(user!.SchoolId!.Value, groupId, gradeLevelId);

            var model = await _reportesService.ObtenerCalificacionesExpresionesArtisticasReporteAsync(

                user.SchoolId!.Value, nivelEducativo, groupId, gradeLevelId,

                GetTeacherScopeId(user), consejeroNombre);

            return View(model);

        }

        catch (UnauthorizedAccessException ex)

        {

            TempData["Error"] = ex.Message;

            return RedirectToAction(nameof(ExpresionesArtisticas));

        }

    }



    [HttpGet]

    public async Task<IActionResult> ExportarPdfTecnologia(string nivelEducativo, Guid groupId, Guid gradeLevelId)

    {

        try

        {

            var (user, error) = await ObtenerUsuarioEscuelaAsync();

            if (error != null) return error;

            if (string.IsNullOrWhiteSpace(nivelEducativo))

            {

                TempData["Error"] = "Seleccione un grado/nivel.";

                return RedirectToAction(nameof(Tecnologia));

            }



            var consejeroNombre = await ResolverConsejeroNombreAsync(user!.SchoolId!.Value, groupId, gradeLevelId);

            var model = await _reportesService.ObtenerCalificacionesTecnologiaReporteAsync(

                user.SchoolId!.Value, nivelEducativo, groupId, gradeLevelId,

                GetTeacherScopeId(user), consejeroNombre);

            var html = await _razorRenderService.RenderViewToStringAsync(

                "~/Views/CalificacionesInforme/VistaPreviaTecnologia.cshtml", model);

            var bytes = await _pdfService.GenerarPdfDesdeHtmlAsync(html, landscape: false);

            return File(bytes, "application/pdf", "Calificaciones_Tecnologia.pdf");

        }

        catch (UnauthorizedAccessException ex)

        {

            TempData["Error"] = ex.Message;

            return RedirectToAction(nameof(Tecnologia));

        }

    }



    [HttpGet]

    public async Task<IActionResult> ExportarPdfExpresionesArtisticas(

        string nivelEducativo, Guid groupId, Guid gradeLevelId)

    {

        try

        {

            var (user, error) = await ObtenerUsuarioEscuelaAsync();

            if (error != null) return error;

            if (string.IsNullOrWhiteSpace(nivelEducativo))

            {

                TempData["Error"] = "Seleccione un grado/nivel.";

                return RedirectToAction(nameof(ExpresionesArtisticas));

            }



            var consejeroNombre = await ResolverConsejeroNombreAsync(user!.SchoolId!.Value, groupId, gradeLevelId);

            var model = await _reportesService.ObtenerCalificacionesExpresionesArtisticasReporteAsync(

                user.SchoolId!.Value, nivelEducativo, groupId, gradeLevelId,

                GetTeacherScopeId(user), consejeroNombre);

            var html = await _razorRenderService.RenderViewToStringAsync(

                "~/Views/CalificacionesInforme/VistaPreviaExpresionesArtisticas.cshtml", model);

            var bytes = await _pdfService.GenerarPdfDesdeHtmlAsync(html, landscape: false);

            return File(bytes, "application/pdf", "Calificaciones_Expresiones_Artisticas.pdf");

        }

        catch (UnauthorizedAccessException ex)

        {

            TempData["Error"] = ex.Message;

            return RedirectToAction(nameof(ExpresionesArtisticas));

        }

    }



    [HttpGet]

    public async Task<IActionResult> ExportarExcel(

        string tipo, string nivelEducativo, Guid groupId, Guid gradeLevelId)

    {

        try

        {

            var (user, error) = await ObtenerUsuarioEscuelaAsync();

            if (error != null) return error;

            if (string.IsNullOrWhiteSpace(nivelEducativo))

                return BadRequest("Nivel educativo requerido.");



            var informeTipo = string.Equals(tipo, "tecnologia", StringComparison.OrdinalIgnoreCase)

                ? InformeCalificacionesTipo.Tecnologia

                : InformeCalificacionesTipo.ExpresionesArtisticas;



            var consejeroNombre = await ResolverConsejeroNombreAsync(user!.SchoolId!.Value, groupId, gradeLevelId);

            var bytes = await _reportesService.ExportarCalificacionesInformeExcelAsync(

                informeTipo, user.SchoolId!.Value, nivelEducativo, groupId, gradeLevelId,

                GetTeacherScopeId(user), consejeroNombre);



            var nombre = informeTipo == InformeCalificacionesTipo.Tecnologia

                ? "Calificaciones_Tecnologia"

                : "Calificaciones_Expresiones_Artisticas";



            return File(bytes, "application/vnd.ms-excel", $"{nombre}.xls");

        }

        catch (UnauthorizedAccessException ex)

        {

            TempData["Error"] = ex.Message;

            return RedirectToAction(informeTipoRedirect(tipo));

        }

    }



    private static string informeTipoRedirect(string? tipo) =>

        string.Equals(tipo, "tecnologia", StringComparison.OrdinalIgnoreCase)

            ? nameof(Tecnologia)

            : nameof(ExpresionesArtisticas);

}



[Authorize(Roles = "admin,director,teacher")]

public class FormatoCarpetasReportController : InformeInstitucionalControllerBase

{

    private readonly IReportesInstitucionalesService _reportesService;

    private readonly IInformeInstitucionalHtmlPdfService _pdfService;

    private readonly IInformeInstitucionalRazorRenderService _razorRenderService;



    public FormatoCarpetasReportController(

        ICurrentUserService currentUserService,

        IAprobadosReprobadosService aprobadosReprobadosService,

        ICounselorAssignmentService counselorAssignmentService,

        IReportesInstitucionalesService reportesService,

        IInformeInstitucionalHtmlPdfService pdfService,

        IInformeInstitucionalRazorRenderService razorRenderService)

        : base(currentUserService, aprobadosReprobadosService, counselorAssignmentService)

    {

        _reportesService = reportesService;

        _pdfService = pdfService;

        _razorRenderService = razorRenderService;

    }



    [HttpGet]

    public async Task<IActionResult> Index()

    {

        var (user, error) = await ObtenerUsuarioEscuelaAsync();

        if (error != null) return error;

        ViewBag.EsDocente = GetTeacherScopeId(user).HasValue;

        return View();

    }



    [HttpGet]

    public async Task<IActionResult> VistaPrevia(

        string nivelEducativo, Guid materiaId, Guid groupId, Guid gradeLevelId)

    {

        try

        {

            var (user, error) = await ObtenerUsuarioEscuelaAsync();

            if (error != null) return error;

            if (string.IsNullOrWhiteSpace(nivelEducativo) || materiaId == Guid.Empty)

            {

                TempData["Error"] = "Nivel y materia son requeridos.";

                return RedirectToAction(nameof(Index));

            }



            var consejeroNombre = await ResolverConsejeroNombreAsync(user!.SchoolId!.Value, groupId, gradeLevelId);

            var model = await _reportesService.ObtenerFormatoCarpetasReporteAsync(

                user.SchoolId!.Value, nivelEducativo, materiaId, groupId, gradeLevelId,

                GetTeacherScopeId(user), consejeroNombre, $"{user.Name} {user.LastName}");

            return View(model);

        }

        catch (UnauthorizedAccessException ex)

        {

            TempData["Error"] = ex.Message;

            return RedirectToAction(nameof(Index));

        }

    }



    [HttpGet]

    public async Task<IActionResult> ExportarPdf(

        string nivelEducativo, Guid materiaId, Guid groupId, Guid gradeLevelId)

    {

        try

        {

            var (user, error) = await ObtenerUsuarioEscuelaAsync();

            if (error != null) return error;

            if (string.IsNullOrWhiteSpace(nivelEducativo) || materiaId == Guid.Empty)

            {

                TempData["Error"] = "Nivel y materia son requeridos.";

                return RedirectToAction(nameof(Index));

            }



            var consejeroNombre = await ResolverConsejeroNombreAsync(user!.SchoolId!.Value, groupId, gradeLevelId);

            var model = await _reportesService.ObtenerFormatoCarpetasReporteAsync(

                user.SchoolId!.Value, nivelEducativo, materiaId, groupId, gradeLevelId,

                GetTeacherScopeId(user), consejeroNombre, $"{user.Name} {user.LastName}");

            var html = await _razorRenderService.RenderViewToStringAsync(

                "~/Views/FormatoCarpetasReport/VistaPrevia.cshtml", model);

            var bytes = await _pdfService.GenerarPdfDesdeHtmlAsync(html, landscape: false);

            return File(bytes, "application/pdf", "Formato_Carpetas.pdf");

        }

        catch (UnauthorizedAccessException ex)

        {

            TempData["Error"] = ex.Message;

            return RedirectToAction(nameof(Index));

        }

    }



    [HttpGet]

    public async Task<IActionResult> ExportarExcel(

        string nivelEducativo, Guid materiaId, Guid groupId, Guid gradeLevelId)

    {

        try

        {

            var (user, error) = await ObtenerUsuarioEscuelaAsync();

            if (error != null) return error;

            if (string.IsNullOrWhiteSpace(nivelEducativo) || materiaId == Guid.Empty)

                return BadRequest("Nivel y materia son requeridos.");



            var consejeroNombre = await ResolverConsejeroNombreAsync(user!.SchoolId!.Value, groupId, gradeLevelId);

            var bytes = await _reportesService.ExportarFormatoCarpetasExcelAsync(

                user.SchoolId!.Value, nivelEducativo, materiaId, groupId, gradeLevelId,

                GetTeacherScopeId(user), consejeroNombre, $"{user.Name} {user.LastName}");



            return File(bytes, "application/vnd.ms-excel", "Formato_Carpetas.xls");

        }

        catch (UnauthorizedAccessException ex)

        {

            TempData["Error"] = ex.Message;

            return RedirectToAction(nameof(Index));

        }

    }

}



public abstract class InformeInstitucionalControllerBase : Controller

{

    protected readonly ICurrentUserService _currentUserService;

    protected readonly IAprobadosReprobadosService _aprobadosReprobadosService;

    protected readonly ICounselorAssignmentService _counselorAssignmentService;



    protected InformeInstitucionalControllerBase(

        ICurrentUserService currentUserService,

        IAprobadosReprobadosService aprobadosReprobadosService,

        ICounselorAssignmentService counselorAssignmentService)

    {

        _currentUserService = currentUserService;

        _aprobadosReprobadosService = aprobadosReprobadosService;

        _counselorAssignmentService = counselorAssignmentService;

    }



    protected static Guid? GetTeacherScopeId(Models.User? user) =>

        user != null && (string.Equals(user.Role, "teacher", StringComparison.OrdinalIgnoreCase) ||

                         string.Equals(user.Role, "docente", StringComparison.OrdinalIgnoreCase))

            ? user.Id

            : null;



    protected async Task<string> ResolverConsejeroNombreAsync(Guid schoolId, Guid groupId, Guid gradeLevelId)

    {

        var nombre = await _counselorAssignmentService.GetConsejeroNombrePorGrupoGradoAsync(

            schoolId, groupId, gradeLevelId);

        return string.IsNullOrWhiteSpace(nombre) ? "" : nombre.Trim();

    }



    protected async Task<(Models.User? user, IActionResult? error)> ObtenerUsuarioEscuelaAsync()

    {

        var user = await _currentUserService.GetCurrentUserAsync();

        if (user?.SchoolId == null)

        {

            TempData["Error"] = "No se pudo obtener la información de la escuela.";

            return (null, RedirectToAction("Index", "Home"));

        }

        return (user, null);

    }

}


