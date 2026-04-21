using System;
using System.Collections.Generic;

namespace SchoolManager.Models
{
    /// <summary>
    /// Modelo para el sistema de mensajería interna
    /// </summary>
    public partial class Message
    {
        public Guid Id { get; set; }

        // Remitente
        public Guid SenderId { get; set; }
        public virtual User? Sender { get; set; }

        // Escuela (para multi-tenancy)
        public Guid? SchoolId { get; set; }
        public virtual School? School { get; set; }

        // Asunto y contenido
        public string Subject { get; set; } = null!;
        public string Content { get; set; } = null!;

        // Tipo de mensaje
        public string MessageType { get; set; } = null!; // "Individual", "Group", "AllTeachers", "AllStudents", "Broadcast"

        // Destinatarios específicos (para mensajes individuales)
        public Guid? RecipientId { get; set; }
        public virtual User? Recipient { get; set; }

        // Para mensajes a grupos
        public Guid? GroupId { get; set; }
        public virtual Group? Group { get; set; }

        // Metadatos
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Prioridad
        public string Priority { get; set; } = "Normal"; // "Low", "Normal", "High", "Urgent"

        // Relación con respuestas
        public Guid? ParentMessageId { get; set; }
        public virtual Message? ParentMessage { get; set; }
        public virtual ICollection<Message> Replies { get; set; } = new List<Message>();

        // Archivos adjuntos (opcional - JSON)
        public string? Attachments { get; set; }

        // Auditoría
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

