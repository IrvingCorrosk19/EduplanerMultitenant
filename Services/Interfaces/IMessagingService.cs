using SchoolManager.ViewModels;

namespace SchoolManager.Services.Interfaces
{
    /// <summary>
    /// Servicio para el sistema de mensajería interna
    /// </summary>
    public interface IMessagingService
    {
        // Enviar mensajes
        Task<bool> SendMessageAsync(SendMessageViewModel model, Guid senderId);
        Task<bool> SendReplyAsync(Guid parentMessageId, string content, Guid senderId);

        // Recibir mensajes
        Task<List<MessageListViewModel>> GetInboxAsync(Guid userId, bool unreadOnly = false);
        Task<List<MessageListViewModel>> GetSentMessagesAsync(Guid userId);
        Task<MessageDetailViewModel?> GetMessageDetailAsync(Guid messageId, Guid currentUserId);

        // Marcar como leído
        Task<bool> MarkAsReadAsync(Guid messageId, Guid userId);
        Task<bool> MarkMultipleAsReadAsync(List<Guid> messageIds, Guid userId);

        // Eliminar
        Task<bool> DeleteMessageAsync(Guid messageId, Guid userId);

        // Opciones de destinatarios según rol del usuario
        Task<RecipientOptionsViewModel> GetRecipientOptionsAsync(Guid userId);

        // Estadísticas
        Task<MessageStatsViewModel> GetStatsAsync(Guid userId);

        // Búsqueda
        Task<List<MessageListViewModel>> SearchMessagesAsync(Guid userId, string searchTerm);
        
        // Búsqueda de usuarios para autocomplete
        Task<List<RecipientOptionDto>> SearchUsersForMessagingAsync(Guid userId, string searchTerm, string type = "all");

        // Validaciones
        Task<bool> CanSendToRecipientAsync(Guid senderId, string recipientType, Guid? recipientId, Guid? groupId);
    }
}

