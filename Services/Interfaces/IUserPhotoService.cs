namespace SchoolManager.Services.Interfaces;

/// <summary>
/// Servicio de aplicación para la foto del usuario (identidad visual).
/// </summary>
public interface IUserPhotoService
{
    /// <summary>
    /// Actualiza la foto del usuario: valida, guarda en almacenamiento, actualiza entidad y elimina foto anterior si existe.
    /// </summary>
    Task UpdatePhotoAsync(Guid userId, IFormFile file);

    /// <summary>
    /// Elimina la foto del usuario (almacenamiento + BD).</summary>
    Task RemovePhotoAsync(Guid userId);
}
