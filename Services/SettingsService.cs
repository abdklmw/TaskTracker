using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models;
using System.Threading.Tasks;

namespace TaskTracker.Services
{
    public interface ISettingsService
    {
        Task<Settings> GetSettingsAsync();
        Task<bool> UpdateSettingsAsync(Settings settings);
    }

    public class SettingsService : ISettingsService
    {
        private readonly AppDbContext _context;

        public SettingsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Settings> GetSettingsAsync()
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
            return settings;
        }

        public async Task<bool> UpdateSettingsAsync(Settings settings)
        {
            if (settings == null)
            {
                return false;
            }

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

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
    }
}