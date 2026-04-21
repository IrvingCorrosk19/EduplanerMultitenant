using System.ComponentModel.DataAnnotations;

namespace SchoolManager.ViewModels
{
    /// <summary>
    /// ViewModel para crear y enviar mensajes
    /// </summary>
    public class SendMessageViewModel
    {
        [Required(ErrorMessage = "El asunto es obligatorio")]
        [StringLength(200, ErrorMessage = "El asunto no puede tener más de 200 caracteres")]
        [Display(Name = "Asunto")]
        public string Subject { get; set; } = null!;

        [Required(ErrorMessage = "El mensaje es obligatorio")]
        [StringLength(5000, ErrorMessage = "El mensaje no puede tener más de 5000 caracteres")]
        [Display(Name = "Mensaje")]
        public string Content { get; set; } = null!;

        [Required(ErrorMessage = "Debe seleccionar el tipo de destinatario")]
        [Display(Name = "Enviar a")]
        public string RecipientType { get; set; } = null!; // "Individual", "Group", "AllTeachers", "AllStudents", "Broadcast"

        [Display(Name = "Destinatario")]
        public Guid? RecipientId { get; set; }

        [Display(Name = "Grupo")]
        public Guid? GroupId { get; set; }

        [Display(Name = "Prioridad")]
        public string Priority { get; set; } = "Normal";

        // Para respuestas
        public Guid? ParentMessageId { get; set; }
    }

    /// <summary>
    /// ViewModel para mostrar mensajes en la bandeja
    /// </summary>
    public class MessageListViewModel
    {
        public Guid Id { get; set; }
        public string Subject { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string SenderName { get; set; } = null!;
        public string SenderRole { get; set; } = null!;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public string Priority { get; set; } = null!;
        public string MessageType { get; set; } = null!;
        public int RepliesCount { get; set; }
        public Guid? ParentMessageId { get; set; }
    }

    /// <summary>
    /// ViewModel para mostrar detalles del mensaje
    /// </summary>
    public class MessageDetailViewModel
    {
        public Guid Id { get; set; }
        public string Subject { get; set; } = null!;
        public string Content { get; set; } = null!;
        public Guid SenderId { get; set; }
        public string SenderName { get; set; } = null!;
        public string SenderRole { get; set; } = null!;
        public string SenderEmail { get; set; } = null!;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public string Priority { get; set; } = null!;
        public string MessageType { get; set; } = null!;
        public string? RecipientInfo { get; set; }
        public List<MessageListViewModel> Replies { get; set; } = new();
        public Guid? ParentMessageId { get; set; }
    }

    /// <summary>
    /// ViewModel para las opciones de destinatarios según el rol
    /// </summary>
    public class RecipientOptionsViewModel
    {
        public List<RecipientOption> Teachers { get; set; } = new();
        public List<RecipientOption> Students { get; set; } = new();
        public List<RecipientOption> Administrators { get; set; } = new();
        public List<RecipientOption> Groups { get; set; } = new();
        public bool CanSendToAllTeachers { get; set; }
        public bool CanSendToAllStudents { get; set; }
        public bool CanSendToBroadcast { get; set; }
    }

    public class RecipientOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? AdditionalInfo { get; set; }
    }

    /// <summary>
    /// DTO para opciones de destinatarios en autocomplete
    /// </summary>
    public class RecipientOptionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? AdditionalInfo { get; set; }
        public string? Role { get; set; }
    }

    /// <summary>
    /// ViewModel para estadísticas de mensajes
    /// </summary>
    public class MessageStatsViewModel
    {
        public int TotalReceived { get; set; }
        public int Unread { get; set; }
        public int TotalSent { get; set; }
        public int RepliesReceived { get; set; }
    }
}

