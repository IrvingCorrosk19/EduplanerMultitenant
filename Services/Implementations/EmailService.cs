using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManager.Dtos;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;
using System.Net.Mail;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SchoolManager.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private const string ResendEmailsUrl = "https://api.resend.com/emails";
        private readonly SchoolDbContext _context;
        private readonly IEmailConfigurationService _emailConfigService;
        private readonly IEmailApiConfigurationService _emailApiConfigurationService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            SchoolDbContext context,
            IEmailConfigurationService emailConfigService,
            IEmailApiConfigurationService emailApiConfigurationService,
            IHttpClientFactory httpClientFactory,
            ILogger<EmailService> logger)
        {
            _context = context;
            _emailConfigService = emailConfigService;
            _emailApiConfigurationService = emailApiConfigurationService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> SendDisciplineReportEmailAsync(Guid studentId, Guid disciplineReportId, string customMessage = "")
        {
            try
            {
                // Obtener datos del estudiante
                var student = await _context.Users
                    .Where(u => u.Id == studentId)
                    .Select(u => new { u.Email, u.Name, u.LastName })
                    .FirstOrDefaultAsync();

                if (student == null)
                {
                    _logger.LogWarning("Estudiante no encontrado: {StudentId}", studentId);
                    return false;
                }

                if (string.IsNullOrEmpty(student.Email))
                {
                    _logger.LogWarning("Estudiante no tiene email configurado: {StudentId}", studentId);
                    return false;
                }

                // Obtener datos del reporte disciplinario
                var disciplineReport = await _context.DisciplineReports
                    .Include(dr => dr.Teacher)
                    .Include(dr => dr.Subject)
                    .Include(dr => dr.Group)
                    .Include(dr => dr.GradeLevel)
                    .FirstOrDefaultAsync(dr => dr.Id == disciplineReportId);

                if (disciplineReport == null)
                {
                    _logger.LogWarning("Reporte disciplinario no encontrado: {DisciplineReportId}", disciplineReportId);
                    return false;
                }

                // Obtener configuración de email de la escuela
                var schoolId = disciplineReport.Teacher?.SchoolId;
                if (!schoolId.HasValue)
                {
                    _logger.LogWarning("No se pudo obtener SchoolId del profesor");
                    return false;
                }

                var emailConfig = await _emailConfigService.GetActiveBySchoolIdAsync(schoolId.Value);
                if (emailConfig == null)
                {
                    _logger.LogWarning("No hay configuración de email activa para la escuela: {SchoolId}", schoolId);
                    return false;
                }

                // Preparar archivos adjuntos
                var attachmentPaths = new List<string>();
                if (!string.IsNullOrEmpty(disciplineReport.Documents))
                {
                    try
                    {
                        var documents = System.Text.Json.JsonSerializer.Deserialize<List<object>>(disciplineReport.Documents);
                        if (documents != null)
                        {
                            foreach (var doc in documents)
                            {
                                var docDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(doc.ToString());
                                if (docDict != null && docDict.ContainsKey("savedName"))
                                {
                                    var savedName = docDict["savedName"].ToString();
                                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "discipline", savedName);
                                    if (File.Exists(filePath))
                                    {
                                        attachmentPaths.Add(filePath);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al procesar documentos adjuntos");
                    }
                }

                // Crear contenido del email
                var studentName = $"{student.Name} {student.LastName}";
                var teacherName = disciplineReport.Teacher != null ? 
                    $"{disciplineReport.Teacher.Name} {disciplineReport.Teacher.LastName}" : "Profesor";
                var subjectName = disciplineReport.Subject?.Name ?? "Materia";
                var groupName = disciplineReport.Group?.Name ?? "Grupo";
                var gradeName = disciplineReport.GradeLevel?.Name ?? "Grado";

                var subject = $"Reporte Disciplinario - {subjectName}";
                var body = $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin-bottom: 20px; }}
        .content {{ padding: 20px; }}
        .footer {{ background-color: #e9ecef; padding: 15px; border-radius: 5px; margin-top: 20px; font-size: 12px; }}
        .highlight {{ background-color: #fff3cd; padding: 10px; border-radius: 5px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>📋 Reporte Disciplinario</h2>
        <p><strong>Estudiante:</strong> {studentName}</p>
        <p><strong>Fecha:</strong> {disciplineReport.Date:dd/MM/yyyy}</p>
        <p><strong>Hora:</strong> {disciplineReport.Date:HH:mm}</p>
    </div>
    
    <div class='content'>
        <h3>Detalles del Reporte</h3>
        <p><strong>Materia:</strong> {subjectName}</p>
        <p><strong>Grado:</strong> {gradeName}</p>
        <p><strong>Grupo:</strong> {groupName}</p>
        <p><strong>Tipo:</strong> {disciplineReport.ReportType}</p>
        <p><strong>Categoría:</strong> {disciplineReport.Category ?? "No especificada"}</p>
        <p><strong>Estado:</strong> {disciplineReport.Status}</p>
        <p><strong>Acciones observadas:</strong> {WebUtility.HtmlEncode(FormatDisciplineActionsForEmail(disciplineReport.DisciplineActionsJson))}</p>
        
        <div class='highlight'>
            <h4>Descripción:</h4>
            <p>{disciplineReport.Description ?? "Sin descripción"}</p>
        </div>
        
        {(attachmentPaths.Any() ? "<p><strong>📎 Documentos adjuntos:</strong> Se incluyen archivos de evidencia relacionados con este reporte.</p>" : "")}
        
        {(string.IsNullOrEmpty(customMessage) ? "" : $"<div class='highlight'><h4>Mensaje adicional del profesor:</h4><p>{customMessage}</p></div>")}
        
        <p><strong>Reportado por:</strong> {teacherName}</p>
    </div>
    
    <div class='footer'>
        <p>Este es un mensaje automático del sistema EduPlanner.</p>
        <p>Por favor, mantenga este correo para sus registros.</p>
    </div>
</body>
</html>";

                // Enviar email
                return await SendEmailWithAttachmentsAsync(student.Email, subject, body, attachmentPaths, emailConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email de reporte disciplinario");
                return false;
            }
        }

        public async Task<bool> SendOrientationReportEmailAsync(Guid studentId, Guid orientationReportId, string customMessage = "")
        {
            try
            {
                // Obtener datos del estudiante
                var student = await _context.Users
                    .Where(u => u.Id == studentId)
                    .Select(u => new { u.Email, u.Name, u.LastName })
                    .FirstOrDefaultAsync();

                if (student == null)
                {
                    _logger.LogWarning("Estudiante no encontrado: {StudentId}", studentId);
                    return false;
                }

                if (string.IsNullOrEmpty(student.Email))
                {
                    _logger.LogWarning("Estudiante no tiene email configurado: {StudentId}", studentId);
                    return false;
                }

                // Obtener datos del reporte de orientación
                var orientationReport = await _context.OrientationReports
                    .Include(or => or.Teacher)
                    .Include(or => or.Subject)
                    .Include(or => or.Group)
                    .Include(or => or.GradeLevel)
                    .FirstOrDefaultAsync(or => or.Id == orientationReportId);

                if (orientationReport == null)
                {
                    _logger.LogWarning("Reporte de orientación no encontrado: {OrientationReportId}", orientationReportId);
                    return false;
                }

                // Obtener configuración de email de la escuela
                var schoolId = orientationReport.Teacher?.SchoolId;
                if (!schoolId.HasValue)
                {
                    _logger.LogWarning("No se pudo obtener SchoolId del profesor");
                    return false;
                }

                var emailConfig = await _emailConfigService.GetActiveBySchoolIdAsync(schoolId.Value);
                if (emailConfig == null)
                {
                    _logger.LogWarning("No hay configuración de email activa para la escuela: {SchoolId}", schoolId);
                    return false;
                }

                // Preparar archivos adjuntos
                var attachmentPaths = new List<string>();
                if (!string.IsNullOrEmpty(orientationReport.Documents))
                {
                    try
                    {
                        var documents = System.Text.Json.JsonSerializer.Deserialize<List<object>>(orientationReport.Documents);
                        if (documents != null)
                        {
                            foreach (var doc in documents)
                            {
                                var docDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(doc.ToString());
                                if (docDict != null && docDict.ContainsKey("savedName"))
                                {
                                    var savedName = docDict["savedName"].ToString();
                                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "orientation", savedName);
                                    if (File.Exists(filePath))
                                    {
                                        attachmentPaths.Add(filePath);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al procesar documentos adjuntos");
                    }
                }

                // Crear contenido del email
                var studentName = $"{student.Name} {student.LastName}";
                var teacherName = orientationReport.Teacher != null ? 
                    $"{orientationReport.Teacher.Name} {orientationReport.Teacher.LastName}" : "Profesor";
                var subjectName = orientationReport.Subject?.Name ?? "Materia";
                var groupName = orientationReport.Group?.Name ?? "Grupo";
                var gradeName = orientationReport.GradeLevel?.Name ?? "Grado";

                var subject = $"Reporte de Orientación - {subjectName}";
                var body = $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin-bottom: 20px; }}
        .content {{ padding: 20px; }}
        .footer {{ background-color: #e9ecef; padding: 15px; border-radius: 5px; margin-top: 20px; font-size: 12px; }}
        .highlight {{ background-color: #fff3cd; padding: 10px; border-radius: 5px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>📋 Reporte de Orientación</h2>
        <p><strong>Estudiante:</strong> {studentName}</p>
        <p><strong>Fecha:</strong> {orientationReport.Date:dd/MM/yyyy}</p>
        <p><strong>Hora:</strong> {orientationReport.Date:HH:mm}</p>
    </div>
    
    <div class='content'>
        <h3>Detalles del Reporte</h3>
        <p><strong>Materia:</strong> {subjectName}</p>
        <p><strong>Grado:</strong> {gradeName}</p>
        <p><strong>Grupo:</strong> {groupName}</p>
        <p><strong>Tipo:</strong> {orientationReport.ReportType}</p>
        <p><strong>Categoría:</strong> {orientationReport.Category ?? "No especificada"}</p>
        <p><strong>Estado:</strong> {orientationReport.Status}</p>
        
        <div class='highlight'>
            <h4>Descripción:</h4>
            <p>{orientationReport.Description ?? "Sin descripción"}</p>
        </div>
        
        {(attachmentPaths.Any() ? "<p><strong>📎 Documentos adjuntos:</strong> Se incluyen archivos de evidencia relacionados con este reporte.</p>" : "")}
        
        {(string.IsNullOrEmpty(customMessage) ? "" : $"<div class='highlight'><h4>Mensaje adicional del profesor:</h4><p>{customMessage}</p></div>")}
        
        <p><strong>Reportado por:</strong> {teacherName}</p>
    </div>
    
    <div class='footer'>
        <p>Este es un mensaje automático del sistema EduPlanner.</p>
        <p>Por favor, mantenga este correo para sus registros.</p>
    </div>
</body>
</html>";

                // Enviar email
                return await SendEmailWithAttachmentsAsync(student.Email, subject, body, attachmentPaths, emailConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email de reporte de orientación");
                return false;
            }
        }

        public async Task<bool> SendMatriculationConfirmationEmailAsync(Guid prematriculationId)
        {
            try
            {
                // Obtener datos de la prematrícula
                var prematriculation = await _context.Prematriculations
                    .Include(p => p.Student)
                    .Include(p => p.Parent)
                    .Include(p => p.Grade)
                    .Include(p => p.Group)
                    .Include(p => p.School)
                    .Include(p => p.Payments.Where(pa => pa.PaymentStatus == "Confirmado"))
                    .FirstOrDefaultAsync(p => p.Id == prematriculationId);

                if (prematriculation == null)
                {
                    _logger.LogWarning("Prematrícula no encontrada: {PrematriculationId}", prematriculationId);
                    return false;
                }

                // Obtener email del acudiente o estudiante
                string? recipientEmail = null;
                string? recipientName = null;

                if (prematriculation.Parent != null)
                {
                    recipientEmail = prematriculation.Parent.Email;
                    recipientName = $"{prematriculation.Parent.Name} {prematriculation.Parent.LastName}";
                }
                else if (prematriculation.Student != null)
                {
                    recipientEmail = prematriculation.Student.Email;
                    recipientName = $"{prematriculation.Student.Name} {prematriculation.Student.LastName}";
                }

                if (string.IsNullOrEmpty(recipientEmail))
                {
                    _logger.LogWarning("No se encontró email del acudiente o estudiante para prematrícula {PrematriculationId}", prematriculationId);
                    return false;
                }

                // Obtener configuración de email de la escuela
                var emailConfig = await _emailConfigService.GetActiveBySchoolIdAsync(prematriculation.SchoolId);
                if (emailConfig == null)
                {
                    _logger.LogWarning("No hay configuración de email activa para la escuela: {SchoolId}", prematriculation.SchoolId);
                    return false;
                }

                // Preparar datos para el email
                var studentName = prematriculation.Student != null ? 
                    $"{prematriculation.Student.Name} {prematriculation.Student.LastName}" : "Estudiante";
                var gradeName = prematriculation.Grade?.Name ?? "No asignado";
                var groupName = prematriculation.Group?.Name ?? "No asignado";
                var schoolName = prematriculation.School?.Name ?? "Institución Educativa";
                var matriculationDate = prematriculation.MatriculationDate ?? DateTime.UtcNow;
                var prematriculationCode = prematriculation.PrematriculationCode ?? "N/A";

                // Obtener información del pago confirmado
                var confirmedPayment = prematriculation.Payments.FirstOrDefault(p => p.PaymentStatus == "Confirmado");
                var receiptNumber = confirmedPayment?.ReceiptNumber ?? "N/A";
                var paymentAmount = confirmedPayment?.Amount ?? 0;
                var paymentDate = confirmedPayment?.PaymentDate ?? DateTime.UtcNow;

                var subject = $"✅ Confirmación de Matrícula - {studentName}";
                var body = $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; border-radius: 10px; margin-bottom: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f8f9fa; border-radius: 10px; }}
        .info-box {{ background-color: white; padding: 20px; border-radius: 8px; margin: 15px 0; border-left: 4px solid #667eea; }}
        .success-box {{ background-color: #d4edda; padding: 20px; border-radius: 8px; margin: 15px 0; border-left: 4px solid #28a745; }}
        .footer {{ background-color: #e9ecef; padding: 15px; border-radius: 5px; margin-top: 20px; font-size: 12px; text-align: center; }}
        .highlight {{ font-weight: bold; color: #667eea; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #667eea; color: white; text-decoration: none; border-radius: 5px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🎓 Confirmación de Matrícula</h1>
        <p style='margin: 0; font-size: 18px;'>Su matrícula ha sido confirmada exitosamente</p>
    </div>
    
    <div class='content'>
        <div class='success-box'>
            <h2 style='margin-top: 0; color: #28a745;'>✅ Matrícula Confirmada</h2>
            <p>Estimado/a <strong>{recipientName}</strong>,</p>
            <p>Nos complace informarle que la matrícula de <strong>{studentName}</strong> ha sido confirmada exitosamente.</p>
        </div>

        <div class='info-box'>
            <h3 style='margin-top: 0; color: #667eea;'>📋 Información de la Matrícula</h3>
            <p><strong>Código de Prematrícula:</strong> <span class='highlight'>{prematriculationCode}</span></p>
            <p><strong>Estudiante:</strong> {studentName}</p>
            <p><strong>Grado:</strong> {gradeName}</p>
            <p><strong>Grupo:</strong> {groupName}</p>
            <p><strong>Fecha de Matrícula:</strong> {matriculationDate:dd/MM/yyyy HH:mm}</p>
            <p><strong>Institución:</strong> {schoolName}</p>
        </div>

        <div class='info-box'>
            <h3 style='margin-top: 0; color: #667eea;'>💳 Información de Pago</h3>
            <p><strong>Número de Recibo:</strong> <span class='highlight'>{receiptNumber}</span></p>
            <p><strong>Monto Pagado:</strong> {paymentAmount:C}</p>
            <p><strong>Fecha de Pago:</strong> {paymentDate:dd/MM/yyyy}</p>
            <p><strong>Estado del Pago:</strong> <span style='color: #28a745; font-weight: bold;'>Confirmado</span></p>
        </div>

        <div class='info-box' style='background-color: #fff3cd; border-left-color: #ffc107;'>
            <h4 style='margin-top: 0;'>📝 Importante</h4>
            <ul>
                <li>Guarde este correo como comprobante de matrícula.</li>
                <li>El código de prematrícula y número de recibo son únicos e importantes para futuras consultas.</li>
                <li>Puede consultar el estado de la matrícula en cualquier momento desde la plataforma.</li>
                <li>Para cualquier consulta, comuníquese con la institución educativa.</li>
            </ul>
        </div>

        <div style='text-align: center;'>
            <p>Puede acceder a la plataforma para ver más detalles de la matrícula.</p>
            <a href='https://eduplaner.net' class='button'>Acceder a la Plataforma</a>
        </div>
    </div>
    
    <div class='footer'>
        <p><strong>{schoolName}</strong></p>
        <p>Este es un mensaje automático del sistema EduPlanner. Por favor, no responda a este email.</p>
        <p>Para consultas, comuníquese con la institución educativa.</p>
    </div>
</body>
</html>";

                // Enviar email sin adjuntos
                return await SendEmailWithAttachmentsAsync(recipientEmail, subject, body, new List<string>(), emailConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email de confirmación de matrícula");
                return false;
            }
        }

        public async Task<bool> SendEmailWithAttachmentsAsync(string toEmail, string subject, string body, List<string> attachmentPaths, EmailConfigurationDto emailConfig)
        {
            try
            {
                _logger.LogInformation("Iniciando envío de email a: {ToEmail}", toEmail);

                // Limpiar credenciales de espacios ocultos
                var cleanUsername = emailConfig.SmtpUsername?.Trim() ?? string.Empty;
                var cleanPassword = emailConfig.SmtpPassword?.Trim() ?? string.Empty;

                using var client = new SmtpClient(emailConfig.SmtpServer, emailConfig.SmtpPort);
                
                // Configurar SSL/TLS
                bool enableSsl = emailConfig.SmtpUseSsl;
                if (emailConfig.SmtpServer.ToLower().Contains("gmail") && emailConfig.SmtpPort == 587)
                {
                    enableSsl = true;
                }
                
                client.EnableSsl = enableSsl;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(cleanUsername, cleanPassword);
                client.DeliveryMethod = SmtpDeliveryMethod.Network;

                // Crear mensaje
                using var message = new MailMessage();
                message.From = new MailAddress(emailConfig.FromEmail, emailConfig.FromName);
                message.To.Add(toEmail);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                // Agregar archivos adjuntos
                foreach (var attachmentPath in attachmentPaths)
                {
                    if (File.Exists(attachmentPath))
                    {
                        var fileName = Path.GetFileName(attachmentPath);
                        var attachment = new Attachment(attachmentPath);
                        attachment.Name = fileName;
                        message.Attachments.Add(attachment);
                        _logger.LogInformation("Archivo adjunto agregado: {FileName}", fileName);
                    }
                }

                _logger.LogInformation("Enviando email desde {FromEmail} a {ToEmail} con {AttachmentCount} archivos adjuntos", 
                    emailConfig.FromEmail, toEmail, message.Attachments.Count);

                await client.SendMailAsync(message);
                _logger.LogInformation("Email enviado exitosamente");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email con adjuntos: {ErrorMessage}", ex.Message);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<(bool Success, string? Message)> SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                return (false, "Correo destino vacío.");

            var cfg = await _emailApiConfigurationService.GetActiveAsync(cancellationToken);
            if (cfg == null)
                return (false, "No hay configuración API de correo activa (tabla email_api_configurations, IsActive=true).");
            if (string.IsNullOrWhiteSpace(cfg.ApiKey))
                return (false, "La API key de correo no está configurada.");
            if (string.IsNullOrWhiteSpace(cfg.FromEmail))
                return (false, "FromEmail no configurado.");

            var provider = (cfg.Provider ?? "").Trim();
            if (!provider.Equals("Resend", StringComparison.OrdinalIgnoreCase))
                return (false, $"Proveedor no soportado para SendEmailAsync: {provider}.");

            var fromDisplay = $"{cfg.FromName} <{cfg.FromEmail.Trim()}>";
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(45);
            using var request = new HttpRequestMessage(HttpMethod.Post, ResendEmailsUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ApiKey.Trim());

            var payload = new
            {
                from = fromDisplay,
                to = new[] { toEmail.Trim() },
                subject,
                html = htmlBody
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            try
            {
                using var response = await client.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Resend API {Code}: {Body}", (int)response.StatusCode, body);
                    return (false, string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendEmailAsync Resend falló para {Email}", toEmail);
                return (false, ex.Message);
            }
        }

        private static string FormatDisciplineActionsForEmail(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return "No especificadas";
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(json);
                if (list == null || list.Count == 0) return "No especificadas";
                return string.Join(", ", list.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            catch
            {
                return "No especificadas";
            }
        }
    }
}
