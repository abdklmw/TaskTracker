using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        // GET: /Settings
        public async Task<IActionResult> Index()
        {
            var settings = await _settingsService.GetSettingsAsync();
            var (success, error) = await _settingsService.EnsureDefaultSettingsAsync();
            if (!success)
            {
                TempData["ErrorMessage"] = error ?? "Failed to ensure default settings.";
                settings = new Settings { CompanyName = "Default Company" };
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
                var (success, error) = await _settingsService.UpdateSettingsAsync(settings);
                if (success)
                {
                    TempData["SuccessMessage"] = "Settings updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = error ?? "Error updating settings.";
            }
            return View("Index", settings);
        }
    }
}