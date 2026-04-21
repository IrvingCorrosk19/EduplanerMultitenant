using SchoolManager.Dtos;
using System.Threading;

namespace SchoolManager.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendDisciplineReportEmailAsync(Guid studentId, Guid disciplineReportId, string customMessage = "");
        Task<bool> SendOrientationReportEmailAsync(Guid studentId, Guid orientationReportId, string customMessage = "");
        Task<bool> SendMatriculationConfirmationEmailAsync(Guid prematriculationId);
        Task<bool> SendEmailWithAttachmentsAsync(string toEmail, string subject, string body, List<string> attachmentPaths, EmailConfigurationDto emailConfig);

        /// <summary>
        /// Envío transaccional vía API (Resend) usando <see cref="EmailApiConfiguration"/> activa.
        /// No usa SMTP por escuela; distinto de los demás métodos de este servicio.
        /// </summary>
        Task<(bool Success, string? Message)> SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default);
    }
}
