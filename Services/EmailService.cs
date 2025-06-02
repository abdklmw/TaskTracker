using Microsoft.Extensions.Logging;
using System;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using TaskTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace TaskTracker.Services
{
    public class EmailService : IEmailService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmailService> _logger;

        public EmailService(AppDbContext context, ILogger<EmailService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, byte[] attachment = null, string attachmentFileName = null)
        {
            try
            {
                var settings = await _context.Settings.FirstOrDefaultAsync();
                if (settings == null || string.IsNullOrEmpty(settings.SmtpServer) || !settings.SmtpPort.HasValue || string.IsNullOrEmpty(settings.SmtpSenderEmail))
                {
                    _logger.LogWarning("SMTP settings are not configured.");
                    throw new InvalidOperationException("SMTP settings are not configured.");
                }

                using var client = new SmtpClient(settings.SmtpServer, settings.SmtpPort.Value)
                {
                    Credentials = string.IsNullOrEmpty(settings.SmtpUsername) || string.IsNullOrEmpty(settings.SmtpPassword)
                        ? null
                        : new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(settings.SmtpSenderEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                if (attachment != null && !string.IsNullOrEmpty(attachmentFileName))
                {
                    using var stream = new System.IO.MemoryStream(attachment);
                    mailMessage.Attachments.Add(new Attachment(stream, attachmentFileName, "application/pdf"));
                }

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {ToEmail} with subject {Subject}.", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {ToEmail} with subject {Subject}.", toEmail, subject);
                throw;
            }
        }
    }
}