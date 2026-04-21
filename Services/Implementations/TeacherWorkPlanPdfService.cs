using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations;

public class TeacherWorkPlanPdfService : ITeacherWorkPlanPdfService
{
    private readonly SchoolDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public TeacherWorkPlanPdfService(SchoolDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<byte[]> GeneratePdfAsync(Guid planId, Guid? requestedByUserId, bool isAdmin)
    {
        var plan = await _context.TeacherWorkPlans
            .AsNoTracking()
            .Include(p => p.Teacher)
            .Include(p => p.Subject)
            .Include(p => p.GradeLevel)
            .Include(p => p.Group)
            .Include(p => p.AcademicYear)
            .Include(p => p.Details.OrderBy(d => d.DisplayOrder))
            .Include(p => p.School)
            .FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null) throw new InvalidOperationException("Plan no encontrado.");
        if (!isAdmin && plan.TeacherId != requestedByUserId)
            throw new UnauthorizedAccessException("No tiene permiso para generar el PDF de este plan.");
        var school = plan.School ?? await _context.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == plan.SchoolId);

        QuestPDF.Settings.License = LicenseType.Community;

        var teacherName = plan.Teacher != null ? $"{plan.Teacher.Name} {plan.Teacher.LastName}".Trim() : "";
        var subjectName = plan.Subject?.Name ?? "";
        var gradeName = plan.GradeLevel?.Name ?? "";
        var groupName = plan.Group?.Name ?? "";
        var yearName = plan.AcademicYear?.Name ?? "";
        var trimLabel = plan.Trimester switch { 1 => "I", 2 => "II", 3 => "III", _ => plan.Trimester.ToString() };

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken2));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("PLANIFICACIÓN TRIMESTRAL")
                        .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Height(8);
                    if (school != null)
                        col.Item().AlignCenter().Text(school.Name).FontSize(12).SemiBold();
                    col.Item().Height(12);
                });

                page.Content().Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Docente:").Bold(); c.Item().Text(teacherName);
                            c.Item().Height(4);
                            c.Item().Text("Materia:").Bold(); c.Item().Text(subjectName);
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Grado:").Bold(); c.Item().Text(gradeName);
                            c.Item().Height(4);
                            c.Item().Text("Grupo:").Bold(); c.Item().Text(groupName);
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Trimestre:").Bold(); c.Item().Text(trimLabel);
                            c.Item().Height(4);
                            c.Item().Text("Año lectivo:").Bold(); c.Item().Text(yearName);
                        });
                    });
                    col.Item().Height(12);

                    if (!string.IsNullOrWhiteSpace(plan.Objectives))
                    {
                        col.Item().Text("Objetivos de aprendizaje").FontSize(11).Bold();
                        col.Item().Height(4);
                        col.Item().Background(Colors.Grey.Lighten3).Padding(8).Text(plan.Objectives);
                        col.Item().Height(12);
                    }

                    col.Item().Text("Contenidos por bloques").FontSize(11).Bold();
                    col.Item().Height(6);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(def =>
                        {
                            def.ConstantColumn(45);
                            def.RelativeColumn(2);
                            def.RelativeColumn(2);
                            def.RelativeColumn(2);
                            def.RelativeColumn(2);
                            def.RelativeColumn(2);
                            def.RelativeColumn(2);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Semanas").FontColor(Colors.White).FontSize(8);
                            h.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Tema").FontColor(Colors.White).FontSize(8);
                            h.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Conceptual").FontColor(Colors.White).FontSize(8);
                            h.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Procedimental").FontColor(Colors.White).FontSize(8);
                            h.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Actitudinal").FontColor(Colors.White).FontSize(8);
                            h.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Competencias").FontColor(Colors.White).FontSize(8);
                            h.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Indicadores").FontColor(Colors.White).FontSize(8);
                        });
                        foreach (var d in plan.Details)
                        {
                            table.Cell().Padding(3).Text(d.WeeksRange).FontSize(8);
                            table.Cell().Padding(3).Text(d.Topic ?? "").FontSize(8);
                            table.Cell().Padding(3).Text(Truncate(d.ConceptualContent, 80)).FontSize(7);
                            table.Cell().Padding(3).Text(Truncate(d.ProceduralContent, 80)).FontSize(7);
                            table.Cell().Padding(3).Text(Truncate(d.AttitudinalContent, 80)).FontSize(7);
                            table.Cell().Padding(3).Text(Truncate(d.BasicCompetencies, 80)).FontSize(7);
                            table.Cell().Padding(3).Text(Truncate(d.AchievementIndicators, 80)).FontSize(7);
                        }
                    });
                });

                page.Footer().AlignCenter().Text($"Documento generado el {DateTime.Now:dd/MM/yyyy HH:mm} - SchoolManager").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();

        return document;
    }

    private static string Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= max ? s : s.Substring(0, max) + "...";
    }
}
