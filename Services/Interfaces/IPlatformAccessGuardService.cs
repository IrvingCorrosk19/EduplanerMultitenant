namespace SchoolManager.Services.Interfaces;

/// <summary>Consulta si un usuario (estudiante) tiene acceso a plataforma (PlatformAccessStatus = Activo). Para bloqueo de rutas académicas.</summary>
public interface IPlatformAccessGuardService
{
    /// <summary>True si el usuario tiene acceso (no es estudiante, o es estudiante con platform_access_status = Activo). False si es estudiante sin registro o Pendiente.</summary>
    Task<bool> ValidatePlatformAccessAsync(Guid? userId = null);
}
