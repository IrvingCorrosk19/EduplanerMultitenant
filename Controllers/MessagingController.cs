using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Services.Interfaces;
using SchoolManager.ViewModels;
using System.Security.Claims;

namespace SchoolManager.Controllers
{
    [Authorize]
    public class MessagingController : Controller
    {
        private readonly IMessagingService _messagingService;
        private readonly ILogger<MessagingController> _logger;

        public MessagingController(
            IMessagingService messagingService,
            ILogger<MessagingController> logger)
        {
            _messagingService = messagingService;
            _logger = logger;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        // GET: Messaging/Inbox
        [HttpGet]
        public async Task<IActionResult> Inbox(bool unreadOnly = false)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var messages = await _messagingService.GetInboxAsync(userId, unreadOnly);
                var stats = await _messagingService.GetStatsAsync(userId);

                ViewBag.Stats = stats;
                ViewBag.UnreadOnly = unreadOnly;

                return View(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cargando bandeja de entrada");
                TempData["Error"] = "Error al cargar los mensajes.";
                return View(new List<MessageListViewModel>());
            }
        }

        // GET: Messaging/Sent
        [HttpGet]
        public async Task<IActionResult> Sent()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var messages = await _messagingService.GetSentMessagesAsync(userId);
                var stats = await _messagingService.GetStatsAsync(userId);

                ViewBag.Stats = stats;

                return View(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cargando mensajes enviados");
                TempData["Error"] = "Error al cargar los mensajes enviados.";
                return View(new List<MessageListViewModel>());
            }
        }

        // GET: Messaging/Compose
        [HttpGet]
        public async Task<IActionResult> Compose(Guid? recipientId = null, Guid? replyTo = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var options = await _messagingService.GetRecipientOptionsAsync(userId);
                ViewBag.RecipientOptions = options;

                var model = new SendMessageViewModel();

                if (recipientId.HasValue)
                {
                    model.RecipientType = "Individual";
                    model.RecipientId = recipientId;
                }

                if (replyTo.HasValue)
                {
                    model.ParentMessageId = replyTo;
                    var originalMessage = await _messagingService.GetMessageDetailAsync(replyTo.Value, userId);
                    if (originalMessage != null)
                    {
                        model.Subject = originalMessage.Subject.StartsWith("RE:") ? 
                            originalMessage.Subject : $"RE: {originalMessage.Subject}";
                        model.RecipientType = "Individual";
                        model.RecipientId = originalMessage.SenderId;
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cargando formulario de mensaje");
                TempData["Error"] = "Error al cargar el formulario.";
                return RedirectToAction("Inbox");
            }
        }

        // POST: Messaging/Compose
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Compose(SendMessageViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (!ModelState.IsValid)
                {
                    var options = await _messagingService.GetRecipientOptionsAsync(userId);
                    ViewBag.RecipientOptions = options;
                    return View(model);
                }

                var result = await _messagingService.SendMessageAsync(model, userId);

                if (result)
                {
                    TempData["Success"] = "Mensaje enviado correctamente.";
                    return RedirectToAction("Sent");
                }
                else
                {
                    TempData["Error"] = "No se pudo enviar el mensaje. Verifique los permisos.";
                    var options = await _messagingService.GetRecipientOptionsAsync(userId);
                    ViewBag.RecipientOptions = options;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                var userId = GetCurrentUserId();
                _logger.LogError(ex, "❌ Error enviando mensaje");
                TempData["Error"] = "Error al enviar el mensaje.";
                var options = await _messagingService.GetRecipientOptionsAsync(userId);
                ViewBag.RecipientOptions = options;
                return View(model);
            }
        }

        // GET: Messaging/Detail/id
        [HttpGet]
        public async Task<IActionResult> Detail(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var message = await _messagingService.GetMessageDetailAsync(id, userId);
                
                if (message == null)
                {
                    TempData["Error"] = "Mensaje no encontrado.";
                    return RedirectToAction("Inbox");
                }

                return View(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cargando detalle del mensaje");
                TempData["Error"] = "Error al cargar el mensaje.";
                return RedirectToAction("Inbox");
            }
        }

        // POST: Messaging/SendReply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendReply(Guid parentMessageId, string content)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == Guid.Empty)
                {
                    return Json(new { success = false, message = "Usuario no autenticado" });
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "El contenido es obligatorio" });
                }

                var result = await _messagingService.SendReplyAsync(parentMessageId, content, currentUserId);

                if (result)
                {
                    return Json(new { success = true, message = "Respuesta enviada correctamente" });
                }
                else
                {
                    return Json(new { success = false, message = "No se pudo enviar la respuesta" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error enviando respuesta");
                return Json(new { success = false, message = "Error al enviar la respuesta" });
            }
        }

        // POST: Messaging/MarkAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty)
                {
                    return Json(new { success = false });
                }

                var result = await _messagingService.MarkAsReadAsync(id, userId);
                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error marcando mensaje como leído");
                return Json(new { success = false });
            }
        }

        // POST: Messaging/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty)
                {
                    return Json(new { success = false, message = "Usuario no autenticado" });
                }

                var result = await _messagingService.DeleteMessageAsync(id, userId);
                
                if (result)
                {
                    return Json(new { success = true, message = "Mensaje eliminado correctamente" });
                }
                else
                {
                    return Json(new { success = false, message = "No se pudo eliminar el mensaje" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error eliminando mensaje");
                return Json(new { success = false, message = "Error al eliminar el mensaje" });
            }
        }

        // GET: Messaging/Search
        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (string.IsNullOrWhiteSpace(q))
                {
                    return RedirectToAction("Inbox");
                }

                var messages = await _messagingService.SearchMessagesAsync(userId, q);
                var stats = await _messagingService.GetStatsAsync(userId);

                ViewBag.Stats = stats;
                ViewBag.SearchTerm = q;

                return View("Inbox", messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error buscando mensajes");
                TempData["Error"] = "Error al buscar mensajes.";
                return RedirectToAction("Inbox");
            }
        }

        // GET: Messaging/GetUnreadCount (para actualizar badge en tiempo real)
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty)
                {
                    return Json(new { count = 0 });
                }

                var stats = await _messagingService.GetStatsAsync(userId);
                return Json(new { count = stats.Unread });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo contador de no leídos");
                return Json(new { count = 0 });
            }
        }

        // GET: Messaging/SearchUsers (para autocomplete)
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term, string type = "all")
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty)
                {
                    return Json(new List<object>());
                }

                var results = await _messagingService.SearchUsersForMessagingAsync(userId, term, type);
                
                return Json(results.Select(u => new
                {
                    id = u.Id,
                    text = u.Name,
                    additionalInfo = u.AdditionalInfo,
                    role = u.Role
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error buscando usuarios");
                return Json(new List<object>());
            }
        }
    }
}

