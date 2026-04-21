using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoolManager.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es requerido")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = null!;

        [Display(Name = "Recordarme")]
        public bool RememberMe { get; set; }
        
        public string? SchoolId { get; set; }  // Para login multi-escuela
        public string? ReturnUrl { get; set; }
        public bool IsMultiSchool { get; set; }
        public List<SchoolInfo>? AvailableSchools { get; set; }
    }

    public class SchoolInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? LogoUrl { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
    }
} 