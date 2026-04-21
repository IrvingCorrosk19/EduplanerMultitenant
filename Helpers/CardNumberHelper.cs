namespace SchoolManager.Helpers;

/// <summary>
/// Generador centralizado de números de carnet. Única fuente de verdad para el formato
/// SM-{fecha}-{studentId 8 chars}-{random 6 chars}.
/// </summary>
public static class CardNumberHelper
{
    public static string Generate(Guid studentId)
        => $"SM-{DateTime.UtcNow:yyyyMMdd}-{studentId.ToString("N")[..8].ToUpper()}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
}
