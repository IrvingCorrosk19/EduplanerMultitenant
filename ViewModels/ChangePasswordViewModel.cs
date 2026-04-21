using System.ComponentModel.DataAnnotations;

namespace SchoolManager.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria")]
        [Display(Name = "Contraseña Actual")]
        public string CurrentPassword { get; set; } = null!;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
        [Display(Name = "Nueva Contraseña")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria")]
        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Confirmar Nueva Contraseña")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
