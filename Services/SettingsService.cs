using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models;

namespace TaskTracker.Services
{
    public interface ISettingsService
    {
        Task<Settings?> GetSettingsAsync();
        Task<(bool Success, string? ErrorMessage)> UpdateSettingsAsync(Settings settings);
        Task<(bool Success, string? ErrorMessage)> EnsureDefaultSettingsAsync();
    }

    public class SettingsService : ISettingsService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(AppDbContext context, ILogger<SettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Settings?> GetSettingsAsync()
        {
            try
            {
                return await _context.Settings.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings.");
                throw;
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateSettingsAsync(Settings settings)
        {
            try
            {
                var existingSettings = await _context.Settings.FirstOrDefaultAsync();
                if (existingSettings == null)
                {
                    _context.Settings.Add(settings);
                }
                else
                {
                    existingSettings.CompanyName = settings.CompanyName;
                    existingSettings.AccountsReceivableAddress = settings.AccountsReceivableAddress;
                    existingSettings.AccountsReceivablePhone = settings.AccountsReceivablePhone;
                    existingSettings.AccountsReceivableEmail = settings.AccountsReceivableEmail;
                    existingSettings.PaymentInformation = settings.PaymentInformation;
                    existingSettings.ThankYouMessage = settings.ThankYouMessage;
                    existingSettings.DefaultHourlyRate = settings.DefaultHourlyRate;
                    existingSettings.SmtpServer = settings.SmtpServer;
                    existingSettings.SmtpPort = settings.SmtpPort;
                    existingSettings.SmtpSenderEmail = settings.SmtpSenderEmail;
                    existingSettings.SmtpUsername = settings.SmtpUsername;
                    existingSettings.SmtpPassword = settings.SmtpPassword;
                    existingSettings.InvoiceTemplate = settings.InvoiceTemplate;
                    _context.Update(existingSettings);
                }
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SettingsExistsAsync())
                {
                    return (false, "Settings not found.");
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating settings.");
                return (false, "An error occurred while updating settings.");
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> EnsureDefaultSettingsAsync()
        {
            try
            {
                var settings = await _context.Settings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    settings = new Settings
                    {
                        CompanyName = "Default Company"
                    };
                    _context.Settings.Add(settings);
                    await _context.SaveChangesAsync();
                }
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring default settings.");
                return (false, "An error occurred while ensuring default settings.");
            }
        }

        private async Task<bool> SettingsExistsAsync()
        {
            return await _context.Settings.AnyAsync();
        }
    }
}