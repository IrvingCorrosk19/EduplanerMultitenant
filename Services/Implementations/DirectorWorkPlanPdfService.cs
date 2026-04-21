using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SchoolManager.Dtos;
using SchoolManager.Models;

namespace SchoolManager.Services.Implementations;

/// <summary>
/// Generación de PDF para Dirección Académica: plan individual (ministerial) y reporte consolidado.
/// </summary>
public static class DirectorWorkPlanPdfService
{
    public static Task<byte[]> GeneratePlanPdfAsync(TeacherWorkPlan plan)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var school = plan.School;
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
                    col.Item().AlignCenter().Text("PLANIFICACIÓN TRIMESTRAL").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Height(8);
                    if (school != null) col.Item().AlignCenter().Text(school.Name).FontSize(12).SemiBold();
                    col.Item().Height(12);
                });
                page.Content().Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c => { c.Item().Text("Docente:").Bold(); c.Item().Text(teacherName); c.Item().Height(4); c.Item().Text("Materia:").Bold(); c.Item().Text(subjectName); });
                        r.RelativeItem().Column(c => { c.Item().Text("Grado:").Bold(); c.Item().Text(gradeName); c.Item().Height(4); c.Item().Text("Grupo:").Bold(); c.Item().Text(groupName); });
                        r.RelativeItem().Column(c => { c.Item().Text("Trimestre:").Bold(); c.Item().Text(trimLabel); c.Item().Height(4); c.Item().Text("Año:").Bold(); c.Item().Text(yearName); });
                    });
                    col.Item().Height(8);
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
                        table.ColumnsDefinition(def => { def.ConstantColumn(45); def.RelativeColumn(2); def.RelativeColumn(2); def.RelativeColumn(2); def.RelativeColumn(2); def.RelativeColumn(2); def.RelativeColumn(2); });
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
                        foreach (var d in plan.Details.OrderBy(x => x.DisplayOrder))
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
                page.Footer().AlignCenter().Text($"Documento generado el {DateTime.Now:dd/MM/yyyy HH:mm} - Dirección Académica").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();

        return Task.FromResult(document);
    }

    public static byte[] GenerateConsolidatedPdfAsync(DirectorWorkPlanDashboardDto dashboard, string schoolName)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken2));
                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("REPORTE CONSOLIDADO - PLANES DE TRABAJO TRIMESTRAL").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().AlignCenter().Text(schoolName).FontSize(12).SemiBold();
                    col.Item().Height(12);
                });
                page.Content().Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().AlignCenter().Background(Colors.Blue.Lighten3).Padding(10).Column(c => { c.Item().Text("Total").FontSize(9); c.Item().Text(dashboard.TotalPlans.ToString()).Bold().FontSize(16); });
                        r.RelativeItem().AlignCenter().Background(Colors.Orange.Lighten3).Padding(10).Column(c => { c.Item().Text("Pendientes").FontSize(9); c.Item().Text(dashboard.SubmittedCount.ToString()).Bold().FontSize(16); });
                        r.RelativeItem().AlignCenter().Background(Colors.Green.Lighten3).Padding(10).Column(c => { c.Item().Text("Aprobados").FontSize(9); c.Item().Text(dashboard.ApprovedCount.ToString()).Bold().FontSize(16); });
                        r.RelativeItem().AlignCenter().Background(Colors.Red.Lighten3).Padding(10).Column(c => { c.Item().Text("Rechazados").FontSize(9); c.Item().Text(dashboard.RejectedCount.ToString()).Bold().FontSize(16); });
                    });
                    col.Item().Height(16);
                    col.Item().Text("Listado de planes").FontSize(11).Bold();
                    col.Item().Height(6);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(def =>
                        {
                            def.RelativeColumn(2);
                            def.RelativeColumn(1);
                            def.RelativeColumn(1);
                            def.ConstantColumn(50);
                            def.ConstantColumn(60);
                            def.ConstantColumn(55);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Docente").FontColor(Colors.White).FontSize(8);
                            h.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Materia").FontColor(Colors.White).FontSize(8);
                            h.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Grado/Grupo").FontColor(Colors.White).FontSize(8);
                            h.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Trim.").FontColor(Colors.White).FontSize(8);
                            h.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Año").FontColor(Colors.White).FontSize(8);
                            h.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Estado").FontColor(Colors.White).FontSize(8);
                        });
                        foreach (var i in dashboard.Items)
                        {
                            table.Cell().Padding(3).Text(i.TeacherName).FontSize(8);
                            table.Cell().Padding(3).Text(i.SubjectName).FontSize(8);
                            table.Cell().Padding(3).Text($"{i.GradeLevelName} / {i.GroupName}").FontSize(8);
                            table.Cell().Padding(3).Text(i.Trimester == 1 ? "I" : i.Trimester == 2 ? "II" : "III").FontSize(8);
                            table.Cell().Padding(3).Text(i.AcademicYearName).FontSize(8);
                            table.Cell().Padding(3).Text(i.Status).FontSize(8);
                        }
                    });
                });
                page.Footer().AlignCenter().Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm} - Dirección Académica").FontSize(8).FontColor(Colors.Grey.Medium);
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
