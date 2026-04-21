using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SchoolManager.ViewModels;

public class SchoolAdminViewModel
{
    public Guid SchoolId { get; set; }
    public Guid? AdminId { get; set; }

    [Required(ErrorMessage = "El nombre de la escuela es requerido")]
    [Display(Name = "Nombre de la Escuela")]
    public string SchoolName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección de la escuela es requerida")]
    [Display(Name = "Dirección de la Escuela")]
    public string SchoolAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono de la escuela es requerido")]
    [Display(Name = "Teléfono de la Escuela")]
    public string SchoolPhone { get; set; } = string.Empty;

    [Display(Name = "Logo de la Escuela")]
    public IFormFile? LogoFile { get; set; }

    public string? SchoolLogoUrl { get; set; }

    [Display(Name = "Número de póliza institucional")]
    public string? PolicyNumber { get; set; }

    [Required(ErrorMessage = "El nombre del administrador es requerido")]
    [Display(Name = "Nombre del Administrador")]
    public string AdminName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido del administrador es requerido")]
    [Display(Name = "Apellido del Administrador")]
    public string AdminLastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico del administrador es requerido")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
    [Display(Name = "Correo Electrónico del Administrador")]
    public string AdminEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña del administrador es requerida")]
    [Display(Name = "Contraseña del Administrador")]
    [DataType(DataType.Password)]
    public string AdminPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
    [Display(Name = "Confirmar Contraseña")]
    [DataType(DataType.Password)]
    [Compare("AdminPassword", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "El estado del administrador es requerido")]
    [Display(Name = "Estado del Administrador")]
    public string AdminStatus { get; set; } = "active";
}

public class SchoolAdminEditViewModel
{
    public Guid SchoolId { get; set; }
    public Guid? AdminId { get; set; }

    [Required(ErrorMessage = "El nombre de la escuela es requerido")]
    [Display(Name = "Nombre de la Escuela")]
    public string SchoolName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección de la escuela es requerida")]
    [Display(Name = "Dirección de la Escuela")]
    public string SchoolAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono de la escuela es requerido")]
    [Display(Name = "Teléfono de la Escuela")]
    public string SchoolPhone { get; set; } = string.Empty;

    public string? SchoolLogoUrl { get; set; }

    [Display(Name = "Número de póliza institucional")]
    public string? PolicyNumber { get; set; }

    [Required(ErrorMessage = "El nombre del administrador es requerido")]
    [Display(Name = "Nombre del Administrador")]
    public string AdminName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido del administrador es requerido")]
    [Display(Name = "Apellido del Administrador")]
    public string AdminLastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico del administrador es requerido")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
    [Display(Name = "Correo Electrónico del Administrador")]
    public string AdminEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "El estado del administrador es requerido")]
    [Display(Name = "Estado del Administrador")]
    public string AdminStatus { get; set; } = "active";
}