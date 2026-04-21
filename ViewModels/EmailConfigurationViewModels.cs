using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SchoolManager.ViewModels
{
    public class EmailConfigurationCreateViewModel
    {
        [Required(ErrorMessage = "La escuela es requerida")]
        [Display(Name = "Escuela")]
        public Guid SchoolId { get; set; }

        [Required(ErrorMessage = "El servidor SMTP es requerido")]
        [StringLength(255, ErrorMessage = "El servidor SMTP no puede exceder 255 caracteres")]
        [Display(Name = "Servidor SMTP")]
        public string SmtpServer { get; set; } = string.Empty;

        [Required(ErrorMessage = "El puerto SMTP es requerido")]
        [Range(1, 65535, ErrorMessage = "El puerto debe estar entre 1 y 65535")]
        [Display(Name = "Puerto SMTP")]
        public int SmtpPort { get; set; } = 587;

        [Required(ErrorMessage = "El nombre de usuario SMTP es requerido")]
        [StringLength(255, ErrorMessage = "El nombre de usuario no puede exceder 255 caracteres")]
        [Display(Name = "Usuario SMTP")]
        public string SmtpUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña SMTP es requerida")]
        [Display(Name = "Contraseña SMTP")]
        [DataType(DataType.Password)]
        public string SmtpPassword { get; set; } = string.Empty;

        [Display(Name = "Usar SSL")]
        public bool SmtpUseSsl { get; set; } = true;

        [Display(Name = "Usar TLS")]
        public bool SmtpUseTls { get; set; } = true;

        [Required(ErrorMessage = "El email de origen es requerido")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [StringLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
        [Display(Name = "Email de Origen")]
        public string FromEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de origen es requerido")]
        [StringLength(255, ErrorMessage = "El nombre no puede exceder 255 caracteres")]
        [Display(Name = "Nombre de Origen")]
        public string FromName { get; set; } = string.Empty;

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        public List<SelectListItem> Schools { get; set; } = new();
    }

    public class EmailConfigurationEditViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "La escuela es requerida")]
        [Display(Name = "Escuela")]
        public Guid SchoolId { get; set; }

        [Required(ErrorMessage = "El servidor SMTP es requerido")]
        [StringLength(255, ErrorMessage = "El servidor SMTP no puede exceder 255 caracteres")]
        [Display(Name = "Servidor SMTP")]
        public string SmtpServer { get; set; } = string.Empty;

        [Required(ErrorMessage = "El puerto SMTP es requerido")]
        [Range(1, 65535, ErrorMessage = "El puerto debe estar entre 1 y 65535")]
        [Display(Name = "Puerto SMTP")]
        public int SmtpPort { get; set; }

        [Required(ErrorMessage = "El nombre de usuario SMTP es requerido")]
        [StringLength(255, ErrorMessage = "El nombre de usuario no puede exceder 255 caracteres")]
        [Display(Name = "Usuario SMTP")]
        public string SmtpUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña SMTP es requerida")]
        [Display(Name = "Contraseña SMTP")]
        [DataType(DataType.Password)]
        public string SmtpPassword { get; set; } = string.Empty;

        [Display(Name = "Usar SSL")]
        public bool SmtpUseSsl { get; set; }

        [Display(Name = "Usar TLS")]
        public bool SmtpUseTls { get; set; }

        [Required(ErrorMessage = "El email de origen es requerido")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [StringLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
        [Display(Name = "Email de Origen")]
        public string FromEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de origen es requerido")]
        [StringLength(255, ErrorMessage = "El nombre no puede exceder 255 caracteres")]
        [Display(Name = "Nombre de Origen")]
        public string FromName { get; set; } = string.Empty;

        [Display(Name = "Activo")]
        public bool IsActive { get; set; }

        public List<SelectListItem> Schools { get; set; } = new();
    }
}
