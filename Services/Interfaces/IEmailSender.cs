using System.Threading;
using System.Threading.Tasks;

namespace SchoolManager.Services.Interfaces;

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
