using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SchoolManager.Dtos;
using SchoolManager.Interfaces;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using System.Globalization;

namespace SchoolManager.Services.Implementations;

public class TeacherGradebookPdfService : ITeacherGradebookPdfService
{
    private const string PdfColorPrimary = "#2563eb";
    private const string PdfColorPrimaryDark = "#1e40af";
    private const string PdfColorApproved = "#15803d";
    private const string PdfColorFailed = "#b91c1c";
    private const string PdfColorMuted = "#64748b";

    private static readonly string[] TypeOrder =
    {
        "notas de apreciación",
        "ejercicios diarios",
        "examen final",
        "recuperación"
    };

    private readonly SchoolDbContext _context;
    private readonly IStudentActivityScoreService _scoreService;
    private readonly IStudentService _studentService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISuperAdminService _superAdminService;

    public TeacherGradebookPdfService(
        SchoolDbContext context,
        IStudentActivityScoreService scoreService,
        IStudentService studentService,
        ICurrentUserService currentUserService,
        ISuperAdminService superAdminService)
    {
        _context = context;
        _scoreService = scoreService;
        _studentService = studentService;
        _currentUserService = currentUserService;
        _superAdminService = superAdminService;
    }

    public async Task<byte[]> GenerateRegistroPdfAsync(
        Guid teacherId,
        Guid groupId,
        string trimester,
        Guid subjectId,
        Guid gradeLevelId,
        byte[]? logoBytes = null)
    {
        var model = await BuildModelAsync(teacherId, groupId, trimester, subjectId, gradeLevelId);

        if (logoBytes == null &&
            !string.IsNullOrWhiteSpace(model.LogoUrl) &&
            !model.LogoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !model.LogoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                logoBytes = await _superAdminService.GetLogoAsync(model.LogoUrl);
            }
            catch
            {
                logoBytes = null;
            }
        }

        QuestPDF.Settings.License = LicenseType.Community;
        return BuildPdf(model, logoBytes);
    }

    private async Task<GradebookPdfDto> BuildModelAsync(
        Guid teacherId,
        Guid groupId,
        string trimester,
        Guid subjectId,
        Guid gradeLevelId)
    {
        var currentSchoolId = await _currentUserService.GetCurrentSchoolIdAsync();
        if (!currentSchoolId.HasValue)
            throw new UnauthorizedAccessException("No se pudo determinar la escuela del docente.");

        var schoolId = currentSchoolId.Value;
        var hasAssignment = await _context.TeacherAssignments.AnyAsync(ta =>
            ta.TeacherId == teacherId &&
            ta.SchoolId == schoolId &&
            ta.SubjectAssignment.GroupId == groupId &&
            ta.SubjectAssignment.SubjectId == subjectId &&
            ta.SubjectAssignment.GradeLevelId == gradeLevelId);

        if (!hasAssignment)
            throw new UnauthorizedAccessException("No tiene asignación para esta materia y grupo.");

        var book = await _scoreService.GetGradeBookAsync(teacherId, groupId, trimester, subjectId, gradeLevelId);
        var students = (await _studentService.GetBySubjectGroupAndGradeAsync(subjectId, groupId, gradeLevelId))
            .OrderBy(s => s.FullName)
            .ToList();

        var teacher = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == teacherId && u.SchoolId == currentSchoolId)
            ?? throw new InvalidOperationException("Docente no encontrado.");
        var subject = await _context.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == subjectId && s.SchoolId == currentSchoolId);
        var group = await _context.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.SchoolId == currentSchoolId);
        var gradeLevel = await _context.GradeLevels.AsNoTracking().FirstOrDefaultAsync(g => g.Id == gradeLevelId && g.SchoolId == currentSchoolId);

        if (subject == null || group == null || gradeLevel == null)
            throw new UnauthorizedAccessException("La materia/grupo/grado no pertenece a la escuela actual.");

        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var school = await _context.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == currentSchoolId);

        var activitiesByType = book.Activities
            .GroupBy(a => NormalizeType(a.Type))
            .ToDictionary(g => g.Key, g => g.ToList());

        var typeSections = BuildTypeSections(activitiesByType);

        var activityIds = book.Activities.Select(a => a.Id).ToList();
        var scoresByStudentId = activityIds.Count == 0
            ? new Dictionary<Guid, Dictionary<Guid, decimal?>>()
            : (await _context.StudentActivityScores
                .AsNoTracking()
                .Where(s => activityIds.Contains(s.ActivityId))
                .ToListAsync())
                .GroupBy(s => s.StudentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(x => x.ActivityId, x => x.Score));

        var studentRows = new List<GradebookPdfStudentRowDto>();

        var index = 1;
        foreach (var stu in students)
        {
            scoresByStudentId.TryGetValue(stu.StudentId, out var scores);
            scores ??= new Dictionary<Guid, decimal?>();

            var typeAvgs = new Dictionary<string, decimal>();
            var typesWithScores = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var section in typeSections)
            {
                var values = section.Activities
                    .Select(a => scores.TryGetValue(a.Id, out var v) ? v : null)
                    .Where(v => v.HasValue)
                    .Select(v => v!.Value)
                    .ToList();

                if (values.Count > 0)
                    typesWithScores.Add(section.TypeKey);

                var avg = values.Count > 0 ? TruncateOneDecimal(values.Average()) : 0m;
                typeAvgs[section.TypeKey] = avg;
            }

            var finalGrade = ComputeFinalGrade(typeAvgs, typesWithScores, typeSections.Select(s => s.TypeKey).ToList());

            studentRows.Add(new GradebookPdfStudentRowDto
            {
                Number = index++,
                Name = stu.FullName,
                DocumentId = stu.DocumentId ?? "",
                ScoresByActivityId = scores.ToDictionary(k => k.Key, k => k.Value),
                TypeAverages = typeAvgs,
                FinalGrade = finalGrade
            });
        }

        return new GradebookPdfDto
        {
            SchoolName = school?.Name ?? "Institución educativa",
            LogoUrl = school?.LogoUrl,
            TeacherName = $"{teacher.Name} {teacher.LastName}".Trim(),
            SubjectName = subject?.Name ?? "Materia",
            GroupLabel = $"{gradeLevel?.Name} {group?.Name}".Trim(),
            Trimester = trimester,
            AcademicYear = DateTime.UtcNow.Year.ToString(),
            GeneratedAt = DateTime.UtcNow,
            TypeSections = typeSections,
            Students = studentRows
        };
    }

    private static List<GradebookPdfTypeSectionDto> BuildTypeSections(
        Dictionary<string, List<ActivityHeaderDto>> activitiesByType)
    {
        var sections = new List<GradebookPdfTypeSectionDto>();
        var usedKeys = new HashSet<string>();

        foreach (var typeKey in TypeOrder)
        {
            if (!activitiesByType.TryGetValue(typeKey, out var acts) || acts.Count == 0)
                continue;

            sections.Add(CreateTypeSection(typeKey, acts));
            usedKeys.Add(typeKey);
        }

        foreach (var (typeKey, acts) in activitiesByType.OrderBy(k => k.Key))
        {
            if (usedKeys.Contains(typeKey) || acts.Count == 0)
                continue;

            sections.Add(CreateTypeSection(typeKey, acts));
        }

        return sections;
    }

    private static GradebookPdfTypeSectionDto CreateTypeSection(string typeKey, List<ActivityHeaderDto> acts) =>
        new()
        {
            TypeKey = typeKey,
            TypeLabel = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(typeKey),
            ShortLabel = TypeShortLabel(typeKey),
            Activities = acts.Select(a => new GradebookPdfActivityColDto
            {
                Id = a.Id,
                Name = a.Name,
                DueDateDisplay = a.DueDate?.ToString("dd/MM/yyyy")
            }).ToList()
        };

    private static byte[] BuildPdf(GradebookPdfDto model, byte[]? logoBytes)
    {
        var activityCols = model.TypeSections.Sum(s => s.Activities.Count);
        var totalCols = 3 + activityCols + model.TypeSections.Count + 1;
        var compact = totalCols > 14;
        var fontSize = totalCols > 22 ? 5f : totalCols > 16 ? 6f : 7f;
        var nameTruncate = totalCols > 22 ? 6 : totalCols > 16 ? 10 : totalCols > 12 ? 14 : 18;
        var scale = totalCols <= 14 ? 1f : Math.Max(0.55f, 14f / totalCols);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginHorizontal(16);
                page.MarginVertical(20);
                page.DefaultTextStyle(x => x.FontSize(fontSize).FontFamily("Arial").FontColor(Colors.Grey.Darken3));
                page.Background().AlignCenter().AlignMiddle()
                    .Text(model.SchoolName)
                    .FontSize(64)
                    .FontColor(Colors.Grey.Lighten3);

                page.Content().Column(col =>
                {
                    col.Item().ShowOnce().Element(c => BuildHeader(c, model, logoBytes, compact));
                    col.Item().PaddingTop(6).Element(c =>
                    {
                        if (scale < 1f)
                            c.Scale(scale).Element(inner => BuildTable(inner, model, fontSize, nameTruncate));
                        else
                            BuildTable(c, model, fontSize, nameTruncate);
                    });
                });

                page.Footer().Element(c => BuildFooter(c, model));
            });
        }).GeneratePdf();
    }

    private static void BuildHeader(IContainer container, GradebookPdfDto model, byte[]? logoBytes, bool compact)
    {
        var hasLogo = logoBytes is { Length: > 0 };
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                if (hasLogo)
                {
                    try
                    {
                        row.ConstantItem(compact ? 40 : 48).Height(compact ? 40 : 48).Image(logoBytes!);
                    }
                    catch
                    {
                        hasLogo = false;
                    }
                }

                row.RelativeItem().PaddingLeft(hasLogo ? 10 : 0).Column(c =>
                {
                    c.Item().Text("REGISTRO DE CALIFICACIONES")
                        .FontSize(compact ? 14 : 16).Bold().FontColor(PdfColorPrimary);
                    c.Item().PaddingTop(2).Text(model.SchoolName)
                        .FontSize(compact ? 9 : 10).SemiBold().FontColor(PdfColorPrimaryDark);
                });

                row.ConstantItem(compact ? 130 : 150).AlignRight().Column(c =>
                {
                    c.Item().Text($"Trimestre: {model.Trimester}").FontSize(8).Bold();
                    c.Item().Text($"Ano lectivo {model.AcademicYear}").FontSize(7).FontColor(PdfColorMuted);
                    c.Item().PaddingTop(2).Text(model.GeneratedAt.ToString("dd/MM/yyyy HH:mm"))
                        .FontSize(6).FontColor(PdfColorMuted);
                });
            });

            col.Item().PaddingVertical(compact ? 6 : 8).LineHorizontal(1f).LineColor(PdfColorPrimary);

            col.Item().Background(Colors.Grey.Lighten4).Padding(compact ? 6 : 8).Row(r =>
            {
                InfoCell(r.RelativeItem(), "Docente", model.TeacherName, compact);
                InfoCell(r.RelativeItem(), "Materia", model.SubjectName, compact);
                InfoCell(r.RelativeItem(), "Grupo", model.GroupLabel, compact);
                InfoCell(r.RelativeItem(), "Estudiantes", model.Students.Count.ToString(), compact);
            });

            if (model.TypeSections.Count > 0)
            {
                col.Item().PaddingTop(4).Text(text =>
                {
                    text.Span("Leyenda: ").FontSize(6).FontColor(PdfColorMuted);
                    foreach (var section in model.TypeSections)
                    {
                        text.Span($"{section.ShortLabel}={section.TypeLabel}; ")
                            .FontSize(6).FontColor(PdfColorPrimaryDark);
                    }
                    text.Span("Aprobado >= 3.0").FontSize(6).FontColor(PdfColorApproved);
                });
            }
        });
    }

    private static void InfoCell(IContainer container, string label, string value, bool compact)
    {
        container.Column(c =>
        {
            c.Item().Text(label).FontSize(6).FontColor(PdfColorMuted);
            c.Item().Text(value).FontSize(compact ? 7 : 8).Bold();
        });
    }

    private static void BuildTable(IContainer container, GradebookPdfDto model, float fontSize, int nameTruncate)
    {
        if (model.Students.Count == 0)
        {
            container.PaddingVertical(40).AlignCenter()
                .Text("No hay estudiantes ni actividades para este registro.")
                .FontSize(11).FontColor(PdfColorMuted);
            return;
        }

        if (model.TypeSections.Count == 0)
        {
            container.PaddingVertical(40).AlignCenter()
                .Text("No hay actividades registradas en este trimestre.")
                .FontSize(11).FontColor(PdfColorMuted);
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(def =>
            {
                def.RelativeColumn(0.4f);
                def.RelativeColumn(2.2f);
                def.RelativeColumn(1f);
                foreach (var section in model.TypeSections)
                {
                    foreach (var _ in section.Activities)
                        def.RelativeColumn(0.55f);
                    def.RelativeColumn(0.5f);
                }
                def.RelativeColumn(0.55f);
            });

            table.Header(header =>
            {
                header.Cell().Background(PdfColorPrimaryDark).Padding(2)
                    .AlignCenter().Text("#").FontSize(fontSize).Bold().FontColor(Colors.White);
                header.Cell().Background(PdfColorPrimaryDark).Padding(2)
                    .Text("Nombre").FontSize(fontSize).Bold().FontColor(Colors.White);
                header.Cell().Background(PdfColorPrimaryDark).Padding(2)
                    .Text("Cedula").FontSize(fontSize).Bold().FontColor(Colors.White);

                foreach (var section in model.TypeSections)
                {
                    var sectionColor = SectionHeaderColor(section.TypeKey);
                    var actIndex = 1;
                    foreach (var act in section.Activities)
                    {
                        header.Cell().Background(sectionColor).Padding(1).AlignCenter()
                            .Text($"{section.ShortLabel}{actIndex}\n{TruncateText(act.Name, nameTruncate)}")
                            .FontSize(Math.Max(4f, fontSize - 1f)).Bold().FontColor(Colors.White);
                        actIndex++;
                    }

                    header.Cell().Background(PdfColorPrimary).Padding(1).AlignCenter()
                        .Text($"Prom.\n{section.ShortLabel}").FontSize(fontSize).Bold().FontColor(Colors.White);
                }

                header.Cell().Background(PdfColorPrimaryDark).Padding(2).AlignCenter()
                    .Text("Nota\nfinal").FontSize(fontSize).Bold().FontColor(Colors.White);
            });

            var rowIndex = 0;
            foreach (var student in model.Students)
            {
                var bg = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                rowIndex++;

                table.Cell().Background(bg).Padding(1).AlignCenter().Text(student.Number.ToString()).FontSize(fontSize);
                table.Cell().Background(bg).Padding(1).Text(TruncateText(student.Name, 28)).FontSize(fontSize);
                table.Cell().Background(bg).Padding(1).Text(string.IsNullOrWhiteSpace(student.DocumentId) ? "-" : student.DocumentId).FontSize(fontSize);

                foreach (var section in model.TypeSections)
                {
                    foreach (var act in section.Activities)
                    {
                        student.ScoresByActivityId.TryGetValue(act.Id, out var score);
                        var display = score.HasValue ? score.Value.ToString("0.0") : "-";
                        table.Cell().Background(bg).Padding(1).AlignCenter().Text(display).FontSize(fontSize);
                    }

                    student.TypeAverages.TryGetValue(section.TypeKey, out var typeAvg);
                    table.Cell().Background(bg).Padding(1).AlignCenter()
                        .Text(typeAvg.ToString("0.0")).FontSize(fontSize).SemiBold().FontColor(PdfColorPrimary);
                }

                var finalColor = student.FinalGrade >= 3.0m && student.FinalGrade > 0
                    ? PdfColorApproved
                    : student.FinalGrade > 0 ? PdfColorFailed : PdfColorMuted;

                table.Cell().Background(bg).Padding(1).AlignCenter()
                    .Text(student.FinalGrade.ToString("0.0")).FontSize(fontSize + 1).Bold().FontColor(finalColor);
            }
        });
    }

    private static void BuildFooter(IContainer container, GradebookPdfDto model)
    {
        var code = $"GB-{model.GeneratedAt:yyyyMMdd}-{model.Trimester}-{model.GeneratedAt:HHmm}";
        container.PaddingTop(6).BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).Row(r =>
        {
            r.RelativeItem().Text($"SchoolManager · {model.SubjectName} · {model.GroupLabel} · Cód. {code}")
                .FontSize(7).FontColor(PdfColorMuted);
            r.RelativeItem().AlignRight().Text(t =>
            {
                t.Span("Página ").FontSize(7).FontColor(PdfColorMuted);
                t.CurrentPageNumber().FontSize(7).FontColor(PdfColorMuted);
                t.Span(" de ").FontSize(7).FontColor(PdfColorMuted);
                t.TotalPages().FontSize(7).FontColor(PdfColorMuted);
            });
        });
    }

    private static string SectionHeaderColor(string typeKey) => typeKey switch
    {
        "notas de apreciación" => "#3b82f6",
        "ejercicios diarios" => "#2563eb",
        "examen final" => "#1d4ed8",
        "recuperación" => "#1e3a8a",
        _ => PdfColorPrimary
    };

    private static string TruncateText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "-";

        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }

    private static string NormalizeType(string? type) => (type ?? "").Trim().ToLowerInvariant();

    private static string TypeShortLabel(string typeKey) => typeKey switch
    {
        "notas de apreciación" => "Aprec.",
        "ejercicios diarios" => "Ejerc.",
        "examen final" => "Examen",
        "recuperación" => "Recup.",
        _ => typeKey.Length > 10 ? typeKey[..10] : typeKey
    };

    private static decimal TruncateOneDecimal(decimal value) =>
        Math.Floor(value * 10m) / 10m;

    private static decimal ComputeFinalGrade(
        Dictionary<string, decimal> typeAvgs,
        HashSet<string> typesWithScores,
        List<string> activeTypeKeys)
    {
        var working = new Dictionary<string, decimal>(typeAvgs);

        if (typesWithScores.Contains("recuperación"))
        {
            working["examen final"] = working.GetValueOrDefault("recuperación", 0m);
        }

        var typesForFinal = activeTypeKeys
            .Where(t => t != "recuperación")
            .Where(t => typesWithScores.Contains(t))
            .ToList();

        if (typesForFinal.Count == 0) return 0m;

        var final = typesForFinal.Average(t => working[t]);
        return TruncateOneDecimal(final);
    }
}
