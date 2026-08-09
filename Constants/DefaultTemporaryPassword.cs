namespace SchoolManager.Constants;

/// <summary>
/// Genera contraseñas temporales aleatorias y seguras para nuevos usuarios o restablecimiento de acceso.
/// Cada llamada produce una contraseña única — nunca use un valor fijo como "123456".
/// El usuario DEBE cambiarla en el primer inicio de sesión.
/// </summary>
public static class DefaultTemporaryPassword
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";

    /// <summary>Genera una contraseña temporal aleatoria y criptográficamente segura de 12 caracteres.</summary>
    public static string Generate()
    {
        var bytes = new byte[12];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return new string(bytes.Select(b => Chars[b % Chars.Length]).ToArray());
    }
}
