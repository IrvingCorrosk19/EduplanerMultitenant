namespace SchoolManager.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Ruta base persistente (p. ej. volumen Render en /var/data).</summary>
    public string BasePath { get; set; } = "/var/data";
}
