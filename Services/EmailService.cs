using System.Net.Mail;
using System.Net;
using TaskTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace TaskTracker.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, byte[] attachment = null, string attachmentFileName = null);
    }

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
                if (settings == null || string.IsNullOrWhiteSpace(settings.SmtpServer) || !settings.SmtpPort.HasValue || string.IsNullOrWhiteSpace(settings.SmtpSenderEmail))
                {
                    _logger.LogWarning("SMTP settings are not configured.");
                    throw new InvalidOperationException("SMTP settings are not configured.");
                }

                using var client = new SmtpClient(settings.SmtpServer, settings.SmtpPort.Value)
                {
                    Credentials = string.IsNullOrWhiteSpace(settings.SmtpUsername) || string.IsNullOrWhiteSpace(settings.SmtpPassword)
                        ? null
                        : new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(settings.SmtpSenderEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                // Add BCC address if specified
                if (!string.IsNullOrWhiteSpace(settings.BCCAddress))
                {
                    try
                    {
                        mailMessage.Bcc.Add(settings.BCCAddress);
                        _logger.LogInformation("Added BCC address {BCCAddress} to email.", settings.BCCAddress);
                    }
                    catch (FormatException ex)
                    {
                        _logger.LogWarning(ex, "Invalid BCC address format: {BCCAddress}. Skipping BCC.", settings.BCCAddress);
                    }
                }

                MemoryStream stream = null;
                if (attachment != null && !string.IsNullOrWhiteSpace(attachmentFileName))
                {
                    stream = new MemoryStream(attachment);
                    mailMessage.Attachments.Add(new Attachment(stream, attachmentFileName, "application/pdf"));
                }

                try
                {
                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation("Email sent successfully to {ToEmail} with subject {Subject}.", toEmail, subject);
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                }
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error sending email to {ToEmail} with subject {Subject}.", toEmail, subject);
                throw new InvalidOperationException("Failed to send email due to SMTP server configuration. Please verify SMTP settings.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {ToEmail} with subject {Subject}.", toEmail, subject);
                throw;
            }
        }
    }
}