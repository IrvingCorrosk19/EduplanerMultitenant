using System.Globalization;
using SchoolManager.Models;

namespace SchoolManager.Services.Helpers;

/// <summary>
/// Nota final trimestral alineada a TeacherGradebook (calcAverages): promedios por tipo, truncamiento y recuperación.
/// </summary>
public static class GradebookFinalGradeCalculator
{
    public static decimal TruncateOneDecimal(decimal value) => Math.Floor(value * 10m) / 10m;

    /// <summary>Formatea una nota ya truncada (o la trunca antes de mostrar). Nunca redondea.</summary>
    public static string FormatTruncatedGrade(decimal value)
    {
        var truncated = TruncateOneDecimal(value);
        return truncated.ToString("0.0", CultureInfo.InvariantCulture);
    }

    public static decimal? GetTruncatedTypeAverage(
        IReadOnlyList<Activity> activities,
        IReadOnlyDictionary<Guid, decimal?> scores,
        string typeKey)
    {
        var acts = activities
            .Where(a => NormalizeActivityType(a.Type) == typeKey)
            .ToList();
        if (acts.Count == 0)
            return null;

        var values = acts
            .Select(a => scores.TryGetValue(a.Id, out var v) ? v : null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        return values.Count > 0 ? TruncateOneDecimal(values.Average()) : null;
    }

    public static string NormalizeActivityType(string? type) => (type ?? "").Trim().ToLowerInvariant();

    public static decimal? CalcularNotaFinal(
        IReadOnlyList<Activity> actividadesMateria,
        IReadOnlyDictionary<Guid, decimal?> scores)
    {
        var typeOrder = new[]
        {
            "notas de apreciación",
            "ejercicios diarios",
            "examen final",
            "recuperación"
        };

        var typeAvgs = new Dictionary<string, decimal>();
        var typesWithScores = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var typeKey in typeOrder)
        {
            var acts = actividadesMateria
                .Where(a => NormalizeActivityType(a.Type) == typeKey)
                .ToList();
            if (acts.Count == 0)
                continue;

            var values = acts
                .Select(a => scores.TryGetValue(a.Id, out var v) ? v : null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (values.Count > 0)
                typesWithScores.Add(typeKey);

            typeAvgs[typeKey] = values.Count > 0 ? TruncateOneDecimal(values.Average()) : 0m;
        }

        if (typeAvgs.Count == 0)
            return null;

        return ComputeFinalGradeFromTypeAverages(typeAvgs, typesWithScores);
    }

    public static decimal? ComputeFinalGradeFromTypeAverages(
        Dictionary<string, decimal> typeAvgs,
        HashSet<string> typesWithScores)
    {
        var working = new Dictionary<string, decimal>(typeAvgs);

        if (typesWithScores.Contains("recuperación"))
            working["examen final"] = working.GetValueOrDefault("recuperación", 0m);

        var typesForFinal = working.Keys
            .Where(t => t != "recuperación")
            .Where(t => typesWithScores.Contains(t))
            .ToList();

        if (typesForFinal.Count == 0)
            return null;

        return TruncateOneDecimal(typesForFinal.Average(t => working[t]));
    }
}
