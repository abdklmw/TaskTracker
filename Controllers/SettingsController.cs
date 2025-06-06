using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Data;
using TaskTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace TaskTracker.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Settings
        public async Task<IActionResult> Index()
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
            return View(settings);
        }

        // POST: /Settings/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Settings settings)
        {
            if (ModelState.IsValid)
            {
                var existingSettings = await _context.Settings.FirstOrDefaultAsync();
                if (existingSettings == null)
                {
                    // This should never happen with proper seeding, but handle it
                    _context.Settings.Add(settings);
                }
                else
                {
                    // Update existing record
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
                TempData["SuccessMessage"] = "Settings updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View("Index", settings);
        }
    }
}