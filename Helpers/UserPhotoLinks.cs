namespace SchoolManager.Helpers;

/// <summary>
/// URL del endpoint que sirve la foto de usuario (Cloudinary, disco o imagen por defecto como el logo de escuela).
/// </summary>
public static class UserPhotoLinks
{
    public static string Href(string? photoUrlStored) =>
        "/File/GetUserPhoto?photoUrl=" + Uri.EscapeDataString(photoUrlStored ?? string.Empty);
}
