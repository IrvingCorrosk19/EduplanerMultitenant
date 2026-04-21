using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using SchoolManager.ViewModels;

namespace SchoolManager.Services.Implementations
{
    public class MessagingService : IMessagingService
    {
        private readonly SchoolDbContext _context;
        private readonly ILogger<MessagingService> _logger;
        private readonly ICurrentUserService _currentUserService;

        public MessagingService(
            SchoolDbContext _context,
            ILogger<MessagingService> logger,
            ICurrentUserService currentUserService)
        {
            this._context = _context;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<bool> SendMessageAsync(SendMessageViewModel model, Guid senderId)
        {
            try
            {
                _logger.LogInformation("📧 Enviando mensaje de {SenderId} - Tipo: {Type}", senderId, model.RecipientType);

                var sender = await _context.Users.FindAsync(senderId);
                if (sender == null)
                {
                    _logger.LogWarning("⚠️ Remitente no encontrado: {SenderId}", senderId);
                    return false;
                }

                // Validar permisos
                if (!await CanSendToRecipientAsync(senderId, model.RecipientType, model.RecipientId, model.GroupId))
                {
                    _logger.LogWarning("⚠️ Usuario no tiene permisos para enviar este tipo de mensaje");
                    return false;
                }

                var messages = new List<Message>();

                switch (model.RecipientType)
                {
                    case "Individual":
                        if (!model.RecipientId.HasValue)
                            return false;

                        messages.Add(new Message
                        {
                            Id = Guid.NewGuid(),
                            SenderId = senderId,
                            RecipientId = model.RecipientId.Value,
                            SchoolId = sender.SchoolId,
                            Subject = model.Subject,
                            Content = model.Content,
                            MessageType = "Individual",
                            Priority = model.Priority,
                            ParentMessageId = model.ParentMessageId,
                            SentAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false,
                            IsDeleted = false
                        });
                        break;

                    case "Group":
                        if (!model.GroupId.HasValue)
                            return false;

                        var groupStudents = await _context.StudentAssignments
                            .Where(sa => sa.GroupId == model.GroupId.Value)
                            .Select(sa => sa.StudentId)
                            .Distinct()
                            .ToListAsync();

                        foreach (var studentId in groupStudents)
                        {
                            messages.Add(new Message
                            {
                                Id = Guid.NewGuid(),
                                SenderId = senderId,
                                RecipientId = studentId,
                                GroupId = model.GroupId.Value,
                                SchoolId = sender.SchoolId,
                                Subject = model.Subject,
                                Content = model.Content,
                                MessageType = "Group",
                                Priority = model.Priority,
                                SentAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow,
                                IsRead = false,
                                IsDeleted = false
                            });
                        }
                        break;

                    case "AllTeachers":
                        var teachers = await _context.Users
                            .Where(u => u.SchoolId == sender.SchoolId && u.Role.ToLower() == "teacher" && u.Status == "active")
                            .Select(u => u.Id)
                            .ToListAsync();

                        foreach (var teacherId in teachers)
                        {
                            messages.Add(new Message
                            {
                                Id = Guid.NewGuid(),
                                SenderId = senderId,
                                RecipientId = teacherId,
                                SchoolId = sender.SchoolId,
                                Subject = model.Subject,
                                Content = model.Content,
                                MessageType = "AllTeachers",
                                Priority = model.Priority,
                                SentAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow,
                                IsRead = false,
                                IsDeleted = false
                            });
                        }
                        break;

                    case "AllStudents":
                        var students = await _context.Users
                            .Where(u => u.SchoolId == sender.SchoolId && 
                                   (u.Role.ToLower() == "student" || u.Role.ToLower() == "estudiante") && 
                                   u.Status == "active")
                            .Select(u => u.Id)
                            .ToListAsync();

                        foreach (var studentId in students)
                        {
                            messages.Add(new Message
                            {
                                Id = Guid.NewGuid(),
                                SenderId = senderId,
                                RecipientId = studentId,
                                SchoolId = sender.SchoolId,
                                Subject = model.Subject,
                                Content = model.Content,
                                MessageType = "AllStudents",
                                Priority = model.Priority,
                                SentAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow,
                                IsRead = false,
                                IsDeleted = false
                            });
                        }
                        break;

                    case "Broadcast":
                        var allUsers = await _context.Users
                            .Where(u => u.SchoolId == sender.SchoolId && 
                                   u.Id != senderId && 
                                   u.Status == "active")
                            .Select(u => u.Id)
                            .ToListAsync();

                        foreach (var userId in allUsers)
                        {
                            messages.Add(new Message
                            {
                                Id = Guid.NewGuid(),
                                SenderId = senderId,
                                RecipientId = userId,
                                SchoolId = sender.SchoolId,
                                Subject = model.Subject,
                                Content = model.Content,
                                MessageType = "Broadcast",
                                Priority = model.Priority,
                                SentAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow,
                                IsRead = false,
                                IsDeleted = false
                            });
                        }
                        break;

                    default:
                        _logger.LogWarning("⚠️ Tipo de mensaje no válido: {Type}", model.RecipientType);
                        return false;
                }

                if (messages.Count == 0)
                {
                    _logger.LogWarning("⚠️ No se generaron mensajes para enviar");
                    return false;
                }

                await _context.Messages.AddRangeAsync(messages);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ {Count} mensaje(s) enviado(s) correctamente", messages.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error enviando mensaje");
                throw;
            }
        }

        public async Task<bool> SendReplyAsync(Guid parentMessageId, string content, Guid senderId)
        {
            try
            {
                var parentMessage = await _context.Messages
                    .Include(m => m.Sender)
                    .FirstOrDefaultAsync(m => m.Id == parentMessageId);

                if (parentMessage == null)
                    return false;

                var reply = new Message
                {
                    Id = Guid.NewGuid(),
                    SenderId = senderId,
                    RecipientId = parentMessage.SenderId,
                    SchoolId = parentMessage.SchoolId,
                    Subject = $"RE: {parentMessage.Subject}",
                    Content = content,
                    MessageType = "Individual",
                    Priority = parentMessage.Priority,
                    ParentMessageId = parentMessageId,
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    IsDeleted = false
                };

                await _context.Messages.AddAsync(reply);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Respuesta enviada correctamente");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error enviando respuesta");
                throw;
            }
        }

        public async Task<List<MessageListViewModel>> GetInboxAsync(Guid userId, bool unreadOnly = false)
        {
            try
            {
                var query = _context.Messages
                    .Where(m => m.RecipientId == userId && !m.IsDeleted)
                    .Include(m => m.Sender)
                    .Include(m => m.Replies)
                    .AsQueryable();

                if (unreadOnly)
                {
                    query = query.Where(m => !m.IsRead);
                }

                var messages = await query
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => new MessageListViewModel
                    {
                        Id = m.Id,
                        Subject = m.Subject,
                        Content = m.Content.Length > 100 ? m.Content.Substring(0, 100) + "..." : m.Content,
                        SenderName = m.Sender != null ? $"{m.Sender.Name} {m.Sender.LastName}" : "Desconocido",
                        SenderRole = m.Sender != null ? m.Sender.Role : "",
                        SentAt = m.SentAt,
                        IsRead = m.IsRead,
                        ReadAt = m.ReadAt,
                        Priority = m.Priority,
                        MessageType = m.MessageType,
                        RepliesCount = m.Replies.Count,
                        ParentMessageId = m.ParentMessageId
                    })
                    .ToListAsync();

                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo mensajes recibidos");
                throw;
            }
        }

        public async Task<List<MessageListViewModel>> GetSentMessagesAsync(Guid userId)
        {
            try
            {
                var messages = await _context.Messages
                    .Where(m => m.SenderId == userId)
                    .Include(m => m.Recipient)
                    .Include(m => m.Group)
                    .Include(m => m.Replies)
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => new MessageListViewModel
                    {
                        Id = m.Id,
                        Subject = m.Subject,
                        Content = m.Content.Length > 100 ? m.Content.Substring(0, 100) + "..." : m.Content,
                        SenderName = "Tú",
                        SenderRole = "",
                        SentAt = m.SentAt,
                        IsRead = m.IsRead,
                        ReadAt = m.ReadAt,
                        Priority = m.Priority,
                        MessageType = m.MessageType,
                        RepliesCount = m.Replies.Count,
                        ParentMessageId = m.ParentMessageId
                    })
                    .ToListAsync();

                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo mensajes enviados");
                throw;
            }
        }

        public async Task<MessageDetailViewModel?> GetMessageDetailAsync(Guid messageId, Guid currentUserId)
        {
            try
            {
                var message = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .Include(m => m.Group)
                    .Include(m => m.Replies)
                        .ThenInclude(r => r.Sender)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                    return null;

                // Verificar que el usuario actual es el remitente o destinatario
                if (message.SenderId != currentUserId && message.RecipientId != currentUserId)
                    return null;

                string recipientInfo = "";
                switch (message.MessageType)
                {
                    case "Individual":
                        recipientInfo = message.Recipient != null ? 
                            $"{message.Recipient.Name} {message.Recipient.LastName}" : "Desconocido";
                        break;
                    case "Group":
                        recipientInfo = message.Group != null ? 
                            $"Grupo: {message.Group.Name}" : "Grupo desconocido";
                        break;
                    case "AllTeachers":
                        recipientInfo = "Todos los profesores";
                        break;
                    case "AllStudents":
                        recipientInfo = "Todos los estudiantes";
                        break;
                    case "Broadcast":
                        recipientInfo = "Todos los usuarios";
                        break;
                }

                var detail = new MessageDetailViewModel
                {
                    Id = message.Id,
                    Subject = message.Subject,
                    Content = message.Content,
                    SenderId = message.SenderId,
                    SenderName = message.Sender != null ? 
                        $"{message.Sender.Name} {message.Sender.LastName}" : "Desconocido",
                    SenderRole = message.Sender?.Role ?? "",
                    SenderEmail = message.Sender?.Email ?? "",
                    SentAt = message.SentAt,
                    IsRead = message.IsRead,
                    ReadAt = message.ReadAt,
                    Priority = message.Priority,
                    MessageType = message.MessageType,
                    RecipientInfo = recipientInfo,
                    ParentMessageId = message.ParentMessageId,
                    Replies = message.Replies
                        .OrderBy(r => r.SentAt)
                        .Select(r => new MessageListViewModel
                        {
                            Id = r.Id,
                            Subject = r.Subject,
                            Content = r.Content,
                            SenderName = r.Sender != null ? $"{r.Sender.Name} {r.Sender.LastName}" : "Desconocido",
                            SenderRole = r.Sender?.Role ?? "",
                            SentAt = r.SentAt,
                            IsRead = r.IsRead,
                            ReadAt = r.ReadAt,
                            Priority = r.Priority,
                            MessageType = r.MessageType,
                            RepliesCount = 0,
                            ParentMessageId = r.ParentMessageId
                        })
                        .ToList()
                };

                // Marcar como leído si es el destinatario quien lo abre
                if (message.RecipientId == currentUserId && !message.IsRead)
                {
                    await MarkAsReadAsync(messageId, currentUserId);
                }

                return detail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo detalle del mensaje");
                throw;
            }
        }

        public async Task<bool> MarkAsReadAsync(Guid messageId, Guid userId)
        {
            try
            {
                var message = await _context.Messages
                    .FirstOrDefaultAsync(m => m.Id == messageId && m.RecipientId == userId);

                if (message == null)
                    return false;

                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                message.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error marcando mensaje como leído");
                throw;
            }
        }

        public async Task<bool> MarkMultipleAsReadAsync(List<Guid> messageIds, Guid userId)
        {
            try
            {
                var messages = await _context.Messages
                    .Where(m => messageIds.Contains(m.Id) && m.RecipientId == userId)
                    .ToListAsync();

                foreach (var message in messages)
                {
                    message.IsRead = true;
                    message.ReadAt = DateTime.UtcNow;
                    message.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error marcando múltiples mensajes");
                throw;
            }
        }

        public async Task<bool> DeleteMessageAsync(Guid messageId, Guid userId)
        {
            try
            {
                var message = await _context.Messages
                    .FirstOrDefaultAsync(m => m.Id == messageId && 
                                            (m.RecipientId == userId || m.SenderId == userId));

                if (message == null)
                    return false;

                message.IsDeleted = true;
                message.DeletedAt = DateTime.UtcNow;
                message.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error eliminando mensaje");
                throw;
            }
        }

        public async Task<RecipientOptionsViewModel> GetRecipientOptionsAsync(Guid userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.SchoolId.HasValue)
                    return new RecipientOptionsViewModel();

                var options = new RecipientOptionsViewModel();
                var role = user.Role.ToLower();

                // Todos pueden enviar a profesores individuales
                options.Teachers = await _context.Users
                    .Where(u => u.SchoolId == user.SchoolId && 
                           u.Role.ToLower() == "teacher" && 
                           u.Status == "active")
                    .OrderBy(u => u.Name)
                    .Select(u => new RecipientOption
                    {
                        Id = u.Id,
                        Name = $"{u.Name} {u.LastName}",
                        AdditionalInfo = u.Email
                    })
                    .ToListAsync();

                // Todos pueden enviar a administradores (importante para soporte/consultas)
                options.Administrators = await _context.Users
                    .Where(u => u.SchoolId == user.SchoolId && 
                           (u.Role.ToLower() == "admin" || u.Role.ToLower() == "director") && 
                           u.Status == "active" &&
                           u.Id != userId) // Excluir al usuario actual
                    .OrderBy(u => u.Name)
                    .Select(u => new RecipientOption
                    {
                        Id = u.Id,
                        Name = $"{u.Name} {u.LastName}",
                        AdditionalInfo = u.Email
                    })
                    .ToListAsync();

                // Permisos según rol
                switch (role)
                {
                    case "student":
                    case "estudiante":
                        // Estudiantes: pueden enviar a todos los profesores o individual
                        options.CanSendToAllTeachers = true;
                        options.CanSendToAllStudents = false;
                        options.CanSendToBroadcast = false;
                        break;

                    case "teacher":
                        // Profesores: pueden enviar a grupos, estudiantes individuales, todos los estudiantes
                        options.Students = await _context.Users
                            .Where(u => u.SchoolId == user.SchoolId && 
                                   (u.Role.ToLower() == "student" || u.Role.ToLower() == "estudiante") && 
                                   u.Status == "active")
                            .OrderBy(u => u.Name)
                            .Select(u => new RecipientOption
                            {
                                Id = u.Id,
                                Name = $"{u.Name} {u.LastName}",
                                AdditionalInfo = u.Email
                            })
                            .ToListAsync();

                        options.Groups = await _context.Groups
                            .Where(g => g.SchoolId == user.SchoolId)
                            .OrderBy(g => g.Name)
                            .Select(g => new RecipientOption
                            {
                                Id = g.Id,
                                Name = g.Name,
                                AdditionalInfo = g.Grade
                            })
                            .ToListAsync();

                        options.CanSendToAllTeachers = false;
                        options.CanSendToAllStudents = false;
                        options.CanSendToBroadcast = false;
                        break;

                    case "admin":
                    case "director":
                    case "superadmin":
                        // Administradores: pueden enviar a todos
                        options.Students = await _context.Users
                            .Where(u => u.SchoolId == user.SchoolId && 
                                   (u.Role.ToLower() == "student" || u.Role.ToLower() == "estudiante") && 
                                   u.Status == "active")
                            .OrderBy(u => u.Name)
                            .Select(u => new RecipientOption
                            {
                                Id = u.Id,
                                Name = $"{u.Name} {u.LastName}",
                                AdditionalInfo = u.Email
                            })
                            .ToListAsync();

                        options.CanSendToAllTeachers = true;
                        options.CanSendToAllStudents = true;
                        options.CanSendToBroadcast = true;
                        break;
                }

                return options;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo opciones de destinatarios");
                throw;
            }
        }

        public async Task<MessageStatsViewModel> GetStatsAsync(Guid userId)
        {
            try
            {
                var stats = new MessageStatsViewModel
                {
                    TotalReceived = await _context.Messages
                        .CountAsync(m => m.RecipientId == userId && !m.IsDeleted),
                    Unread = await _context.Messages
                        .CountAsync(m => m.RecipientId == userId && !m.IsRead && !m.IsDeleted),
                    TotalSent = await _context.Messages
                        .Where(m => m.SenderId == userId)
                        .Select(m => m.RecipientId)
                        .Distinct()
                        .CountAsync(),
                    RepliesReceived = await _context.Messages
                        .CountAsync(m => m.RecipientId == userId && m.ParentMessageId != null && !m.IsDeleted)
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo estadísticas");
                throw;
            }
        }

        public async Task<List<MessageListViewModel>> SearchMessagesAsync(Guid userId, string searchTerm)
        {
            try
            {
                searchTerm = searchTerm.ToLower();

                var messages = await _context.Messages
                    .Where(m => (m.RecipientId == userId || m.SenderId == userId) && 
                           !m.IsDeleted &&
                           (m.Subject.ToLower().Contains(searchTerm) || 
                            m.Content.ToLower().Contains(searchTerm)))
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => new MessageListViewModel
                    {
                        Id = m.Id,
                        Subject = m.Subject,
                        Content = m.Content.Length > 100 ? m.Content.Substring(0, 100) + "..." : m.Content,
                        SenderName = m.Sender != null ? $"{m.Sender.Name} {m.Sender.LastName}" : "Desconocido",
                        SenderRole = m.Sender != null ? m.Sender.Role : "",
                        SentAt = m.SentAt,
                        IsRead = m.IsRead,
                        ReadAt = m.ReadAt,
                        Priority = m.Priority,
                        MessageType = m.MessageType,
                        RepliesCount = 0,
                        ParentMessageId = m.ParentMessageId
                    })
                    .ToListAsync();

                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error buscando mensajes");
                throw;
            }
        }

        public async Task<List<RecipientOptionDto>> SearchUsersForMessagingAsync(Guid userId, string searchTerm, string type = "all")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                    return new List<RecipientOptionDto>();

                var currentUser = await _context.Users.FindAsync(userId);
                if (currentUser?.SchoolId == null)
                    return new List<RecipientOptionDto>();

                searchTerm = searchTerm.ToLower();

                var query = _context.Users
                    .Where(u => u.SchoolId == currentUser.SchoolId && 
                               u.Id != userId && 
                               u.Status == "active" &&
                               (u.Name.ToLower().Contains(searchTerm) || 
                                u.LastName.ToLower().Contains(searchTerm) ||
                                u.Email.ToLower().Contains(searchTerm)));

                // Filtrar por tipo si se especifica
                if (type == "teacher")
                {
                    query = query.Where(u => u.Role.ToLower() == "teacher");
                }
                else if (type == "student")
                {
                    query = query.Where(u => u.Role.ToLower() == "student" || u.Role.ToLower() == "estudiante");
                }
                else if (type == "admin")
                {
                    query = query.Where(u => u.Role.ToLower() == "admin" || u.Role.ToLower() == "director");
                }

                var results = await query
                    .OrderBy(u => u.Name)
                    .Take(20) // Limitar a 20 resultados
                    .Select(u => new RecipientOptionDto
                    {
                        Id = u.Id,
                        Name = $"{u.Name} {u.LastName}",
                        AdditionalInfo = u.Email,
                        Role = u.Role
                    })
                    .ToListAsync();

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error buscando usuarios para autocomplete");
                return new List<RecipientOptionDto>();
            }
        }

        public async Task<bool> CanSendToRecipientAsync(Guid senderId, string recipientType, Guid? recipientId, Guid? groupId)
        {
            try
            {
                var sender = await _context.Users.FindAsync(senderId);
                if (sender == null)
                    return false;

                var role = sender.Role.ToLower();

                switch (recipientType)
                {
                    case "Individual":
                        return recipientId.HasValue;

                    case "Group":
                        // Solo profesores y administradores pueden enviar a grupos
                        return groupId.HasValue && 
                               (role == "teacher" || role == "admin" || role == "director" || role == "superadmin");

                    case "AllTeachers":
                        // Estudiantes y administradores pueden enviar a todos los profesores
                        return role == "student" || role == "estudiante" || 
                               role == "admin" || role == "director" || role == "superadmin";

                    case "AllStudents":
                        // Solo administradores pueden enviar a todos los estudiantes
                        return role == "admin" || role == "director" || role == "superadmin";

                    case "Broadcast":
                        // Solo administradores pueden enviar broadcast
                        return role == "admin" || role == "director" || role == "superadmin";

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error validando permisos de envío");
                return false;
            }
        }
    }
}

