using System;
using System.IO; // Added for File operations
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
        Task SendEmailAsync(string toEmail, string subject, string body, Client client, byte[]? attachment = null, string? attachmentFileName = null);
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
        public async Task SendEmailAsync(string toEmail, string subject, string body, Client client, byte[]? attachment = null, string? attachmentFileName = null)
        {
            string? tempFilePath = null;
            Attachment? attachmentItem = null;

            try
            {
                var settings = await _context.Settings.FirstOrDefaultAsync();

                // Enhanced validation for Amazon SES
                if (settings == null)
                {
                    _logger.LogWarning("No settings found in database.");
                    throw new InvalidOperationException("No settings found in database.");
                }

                if (string.IsNullOrWhiteSpace(settings.SmtpServer))
                {
                    _logger.LogWarning("SMTP Server is not configured.");
                    throw new InvalidOperationException("SMTP Server is not configured.");
                }

                if (!settings.SmtpPort.HasValue)
                {
                    _logger.LogWarning("SMTP Port is not configured.");
                    throw new InvalidOperationException("SMTP Port is not configured.");
                }

                if (string.IsNullOrWhiteSpace(settings.SmtpSenderEmail))
                {
                    _logger.LogWarning("Sender email is not configured.");
                    throw new InvalidOperationException("Sender email is not configured.");
                }

                // For Amazon SES, credentials are REQUIRED
                if (string.IsNullOrWhiteSpace(settings.SmtpUsername))
                {
                    _logger.LogWarning("SMTP Username is not configured.");
                    throw new InvalidOperationException("SMTP Username is not configured.");
                }

                if (string.IsNullOrWhiteSpace(settings.SmtpPassword))
                {
                    _logger.LogWarning("SMTP Password is not configured.");
                    throw new InvalidOperationException("SMTP Password is not configured.");
                }

                // Log configuration for debugging (without password)
                _logger.LogInformation("Attempting to send email with SMTP config - Server: {Server}, Port: {Port}, Sender: {Sender}, Username: {Username}",
                    settings.SmtpServer, settings.SmtpPort.Value, settings.SmtpSenderEmail, settings.SmtpUsername);

                using var smtpClient = new SmtpClient(settings.SmtpServer, settings.SmtpPort.Value)
                {
                    Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 30000 // 30 second timeout
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

                // Replace the attachment section with this corrected version
                if (attachment != null && !string.IsNullOrWhiteSpace(attachmentFileName))
                {
                    try
                    {
                        // ✅ Fix: Use the actual attachment filename with a temporary path
                        tempFilePath = Path.Combine(Path.GetTempPath(), attachmentFileName);

                        // Ensure the filename is safe for the filesystem
                        var safeFileName = MakeSafeFileName(attachmentFileName);
                        if (string.IsNullOrWhiteSpace(safeFileName))
                        {
                            throw new ArgumentException("Invalid attachment filename");
                        }

                        tempFilePath = Path.Combine(Path.GetTempPath(), safeFileName);

                        // Write the attachment bytes to the temporary file
                        await File.WriteAllBytesAsync(tempFilePath, attachment);

                        // Create attachment from the temporary file with the original filename
                        attachmentItem = new Attachment(tempFilePath, "application/pdf");

                        // ✅ Important: Set the display name to the original filename
                        attachmentItem.Name = attachmentFileName;

                        mailMessage.Attachments.Add(attachmentItem);

                        _logger.LogDebug("Created temporary attachment file: {TempFile} for display name: {FileName}",
                            tempFilePath, attachmentFileName);
                    }
                    catch (Exception attachEx)
                    {
                        _logger.LogError(attachEx, "Failed to create attachment {FileName}", attachmentFileName);
                        throw new InvalidOperationException($"Failed to create attachment '{attachmentFileName}': {attachEx.Message}", attachEx);
                    }
                }

                // Send the email
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {ToEmail} with subject {Subject}.", toEmail, subject);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error sending email to {ToEmail} with subject {Subject}. StatusCode: {StatusCode}",
                    toEmail, subject, ex.StatusCode);
                throw new InvalidOperationException($"Failed to send email due to SMTP server error ({ex.StatusCode}): {ex.Message}. Please verify SMTP settings.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {ToEmail} with subject {Subject}.", toEmail, subject);
                throw;
            }
            finally
            {
                // Clean up temporary file and attachment
                try
                {
                    if (attachmentItem != null)
                    {
                        attachmentItem.Dispose();
                    }
                }
                catch (Exception disposeEx)
                {
                    _logger.LogWarning(disposeEx, "Error disposing attachment");
                }

                if (!string.IsNullOrWhiteSpace(tempFilePath) && File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                        _logger.LogDebug("Cleaned up temporary file: {TempFile}", tempFilePath);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Failed to delete temporary file: {TempFile}", tempFilePath);
                    }
                }
            }
        }/// <summary>
         /// Creates a safe filename by removing or replacing invalid characters.
         /// </summary>
        private string? MakeSafeFileName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            // Remove or replace invalid filename characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = fileName;

            foreach (char invalidChar in invalidChars)
            {
                safeName = safeName.Replace(invalidChar, '_');
            }

            // Remove leading/trailing dots and spaces
            safeName = safeName.TrimStart('.', ' ');
            safeName = safeName.TrimEnd('.', ' ');

            // Ensure it has an extension if it doesn't
            if (!safeName.Contains('.'))
            {
                safeName += ".pdf"; // Default to PDF since that's what we're using
            }

            // Limit length to prevent filesystem issues
            if (safeName.Length > 255)
            {
                var extension = Path.GetExtension(safeName);
                safeName = safeName.Substring(0, 255 - extension.Length) + extension;
            }

            return safeName;
        }
    }
}