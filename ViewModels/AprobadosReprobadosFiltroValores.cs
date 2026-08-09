namespace SchoolManager.ViewModels;

/// <summary>
/// Valores sentinela para filtros "Todos" en el reporte de aprobados/reprobados.
/// </summary>
public static class AprobadosReprobadosFiltroValores
{
    public const string Todos = "__TODOS__";

    public static bool EsTodos(string? valor) =>
        string.Equals(valor, Todos, StringComparison.OrdinalIgnoreCase);

    public static bool EsTodos(Guid id) => id == Guid.Empty;
}
