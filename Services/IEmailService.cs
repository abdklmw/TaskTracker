using System.Threading.Tasks;

namespace TaskTracker.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, byte[] attachment = null, string attachmentFileName = null);
    }
}