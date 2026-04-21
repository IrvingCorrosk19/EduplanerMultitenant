using System.ComponentModel.DataAnnotations;

namespace SchoolManager.ViewModels;

public class SchoolListViewModel
{
    public Guid SchoolId { get; set; }
    public Guid AdminId { get; set; }

    [Display(Name = "Nombre de la Escuela")]
    public string SchoolName { get; set; } = string.Empty;

    [Display(Name = "Dirección")]
    public string SchoolAddress { get; set; } = string.Empty;

    [Display(Name = "Teléfono")]
    public string SchoolPhone { get; set; } = string.Empty;

    [Display(Name = "Logo")]
    public string? SchoolLogoUrl { get; set; }

    [Display(Name = "Nombre del Administrador")]
    public string AdminName { get; set; } = string.Empty;

    [Display(Name = "Apellido del Administrador")]
    public string AdminLastName { get; set; } = string.Empty;

    [Display(Name = "Email del Administrador")]
    public string AdminEmail { get; set; } = string.Empty;

    [Display(Name = "Estado del Administrador")]
    public string AdminStatus { get; set; } = string.Empty;

    [Display(Name = "Fecha de Creación")]
    public DateTime? CreatedAt { get; set; }

    [Display(Name = "Activa")]
    public bool IsActive { get; set; } = true;
} 