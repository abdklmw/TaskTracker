using System;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using TaskTracker.Data;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Models.Client;

namespace TaskTracker.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, Client client, byte[] attachment = null, string attachmentFileName = null);
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

        // <summary>
        // Sends an email with the specified parameters.
        // </summary>
        public async Task SendEmailAsync(string toEmail, string subject, string body, Client client, byte[] attachment = null, string attachmentFileName = null)
        {
            try
            {
                var settings = await _context.Settings.FirstOrDefaultAsync();
                if (settings == null || string.IsNullOrWhiteSpace(settings.SmtpServer) || !settings.SmtpPort.HasValue || string.IsNullOrWhiteSpace(settings.SmtpSenderEmail))
                {
                    _logger.LogWarning("SMTP settings are not configured.");
                    throw new InvalidOperationException("SMTP settings are not configured.");
                }

                using var smtpClient = new SmtpClient(settings.SmtpServer, settings.SmtpPort.Value)
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

                // Add client CC address(es) if specified
                if (client != null && !string.IsNullOrWhiteSpace(client.CC))
                {
                    var ccAddresses = client.CC.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var ccAddress in ccAddresses)
                    {
                        try
                        {
                            var trimmedCc = ccAddress.Trim();
                            if (!string.IsNullOrWhiteSpace(trimmedCc))
                            {
                                mailMessage.CC.Add(trimmedCc);
                                _logger.LogInformation("Added CC address {CCAddress} to email.", trimmedCc);
                            }
                        }
                        catch (FormatException ex)
                        {
                            _logger.LogWarning(ex, "Invalid CC address format: {CCAddress}. Skipping CC.", ccAddress);
                        }
                    }
                }

                // Add client BCC addresses if specified
                if (client != null && !string.IsNullOrWhiteSpace(client.BCC))
                {
                    var bccAddresses = client.BCC.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var bccAddress in bccAddresses)
                    {
                        try
                        {
                            var trimmedBcc = bccAddress.Trim();
                            if (!string.IsNullOrWhiteSpace(trimmedBcc))
                            {
                                mailMessage.Bcc.Add(trimmedBcc);
                                _logger.LogInformation("Added BCC address {BCCAddress} to email.", trimmedBcc);
                            }
                        }
                        catch (FormatException ex)
                        {
                            _logger.LogWarning(ex, "Invalid BCC address format: {BCCAddress}. Skipping BCC.", bccAddress);
                        }
                    }
                }

                // Add global settings BCC address if specified
                if (!string.IsNullOrWhiteSpace(settings.BCCAddress))
                {
                    try
                    {
                        mailMessage.Bcc.Add(settings.BCCAddress);
                        _logger.LogInformation("Added global BCC address {BCCAddress} to email.", settings.BCCAddress);
                    }
                    catch (FormatException ex)
                    {
                        _logger.LogWarning(ex, "Invalid global BCC address format: {BCCAddress}. Skipping BCC.", settings.BCCAddress);
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
                    await smtpClient.SendMailAsync(mailMessage);
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