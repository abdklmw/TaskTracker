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
            return View(settings);
        }

        // POST: /Settings/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Settings settings)
        {
            if (ModelState.IsValid)
            {
                var success = await _settingsService.UpdateSettingsAsync(settings);
                if (success)
                {
                    TempData["SuccessMessage"] = "Settings updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                return View("Index", settings);
            }
            return View("Index", settings);
        }
    }
}