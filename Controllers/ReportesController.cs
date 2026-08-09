using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Services.Interfaces;
using SchoolManager.ViewModels;

namespace SchoolManager.Controllers;

[Authorize(Roles = "admin,director,teacher")]
public class ReportesController : Controller
{
    private readonly ICurrentUserService _currentUserService;

    public ReportesController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        if (currentUser?.SchoolId == null)
        {
            TempData["Error"] = "No se pudo obtener la información de la escuela.";
            return RedirectToAction("Index", "Home");
        }

        ViewBag.EsDocente = EsDocente(currentUser.Role);
        ViewBag.Catalogo = new List<ReporteCatalogoItemViewModel>
        {
            new()
            {
                Titulo = "Aprobados y Reprobados",
                Descripcion = "Cuadro estadístico por grado, materia y grupo según sus asignaciones.",
                Icono = "bi-bar-chart-fill",
                Controller = "AprobadosReprobados",
                Action = "Index"
            },
            new()
            {
                Titulo = "Hábitos y Actitudes",
                Descripcion = "Cuadro de hábitos y actitudes por estudiante (S / X / R) para consejería.",
                Icono = "bi-clipboard-check",
                Controller = "HabitosActitudesReport",
                Action = "Index"
            },
            new()
            {
                Titulo = "Calificaciones — Expresiones Artísticas",
                Descripcion = "Informe trimestral de Educ. Artística y Educ. Musical.",
                Icono = "bi-palette",
                Controller = "CalificacionesInforme",
                Action = "ExpresionesArtisticas"
            },
            new()
            {
                Titulo = "Calificaciones — Tecnología",
                Descripcion = "Informe trimestral por áreas de tecnología (Comercio/Contabilidad, Hogar, Industriales).",
                Icono = "bi-cpu",
                Controller = "CalificacionesInforme",
                Action = "Tecnologia"
            },
            new()
            {
                Titulo = "Formato para Carpetas",
                Descripcion = "Premedia: promedios, ausencias (A) y tardanzas (T) por trimestre y materia.",
                Icono = "bi-folder2-open",
                Controller = "FormatoCarpetasReport",
                Action = "Index"
            }
        };

        return View();
    }

    private static bool EsDocente(string? role) =>
        string.Equals(role, "teacher", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, "docente", StringComparison.OrdinalIgnoreCase);
}
