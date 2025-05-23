using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TaskTracker.Data;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SetupService _setupService;

        public HomeController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<HomeController> logger,
            SetupService setupService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _setupService = setupService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    LoggerExtensions.LogError(_logger, "User ID could not be retrieved for authenticated user.");
                    return RedirectToAction("Login", "Account");
                }

                // Check user setup using the service
                var setupResult = await _setupService.CheckSetupAsync(userId);
                if (setupResult != null)
                {
                    return setupResult;
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    LoggerExtensions.LogError(_logger, "User not found for ID {UserId}", userId);
                    return NotFound();
                }

                // Calculate dynamic offset from user's TimeZoneId, accounting for DST
                try
                {
                    var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                    var nowUtc = DateTimeOffset.UtcNow.UtcDateTime;
                    var offset = userTimeZone.GetUtcOffset(nowUtc);
                    ViewBag.TimezoneOffset = (int)offset.TotalMinutes;
                    LoggerExtensions.LogInformation(_logger, "Dynamic TimezoneOffset for Home Index: {TimezoneOffset} minutes, DST Active: {IsDst}, TimeZoneId: {TimeZoneId}", ViewBag.TimezoneOffset, userTimeZone.IsDaylightSavingTime(nowUtc), user.TimeZoneId);
                }
                catch (TimeZoneNotFoundException ex)
                {
                    LoggerExtensions.LogError(_logger, "Invalid TimeZoneId {TimeZoneId} for user {UserId}: {Error}", user.TimeZoneId, userId, ex.Message);
                    ViewBag.TimezoneOffset = 0; // Fallback to UTC
                }

                // Populate ClientID dropdown
                var clientList = _context.Clients
                    .Select(c => new { c.ClientID, c.Name })
                    .ToList();
                clientList.Insert(0, new { ClientID = 0, Name = "Select Client" });
                ViewBag.ClientID = new SelectList(clientList, "ClientID", "Name", 0);
                // Populate ProjectID dropdown
                var projectList = _context.Projects
                    .Select(p => new { p.ProjectID, p.Name })
                    .ToList();
                projectList.Insert(0, new { ProjectID = 0, Name = "Select Project" });
                ViewBag.ProjectID = new SelectList(projectList, "ProjectID", "Name", 0);
                // Set form visibility and return target
                ViewBag.VisibleCreateForm = true;
                ViewBag.ReturnTo = "Home";
                // Fetch running timers
                var runningTimers = _context.TimeEntries
                    .Where(t => t.UserId == userId && t.EndDateTime == null)
                    .Include(t => t.Client)
                    .Include(t => t.Project)
                    .ToList();
                return View(runningTimers);
            }
            return View();
        }
        public IActionResult SetTimezone()
        {
            // Populate timezone dropdown with system timezones
            var timezones = TimeZoneInfo.GetSystemTimeZones()
                .Select(tz => new SelectListItem
                {
                    Value = tz.Id,
                    Text = $"{tz.DisplayName} (UTC{tz.BaseUtcOffset.Hours:+00;-00}:{tz.BaseUtcOffset.Minutes:00})"
                })
                .OrderBy(tz => tz.Text)
                .ToList();
            ViewBag.Timezones = new SelectList(timezones, "Value", "Text");
            return View(new SetTimezoneViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTimezone(SetTimezoneViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                // Repopulate timezone dropdown
                var timezones = TimeZoneInfo.GetSystemTimeZones()
                    .Select(tz => new SelectListItem
                    {
                        Value = tz.Id,
                        Text = $"{tz.DisplayName} (UTC{tz.BaseUtcOffset.Hours:+00;-00}:{tz.BaseUtcOffset.Minutes:00})"
                    })
                    .OrderBy(tz => tz.Text)
                    .ToList();
                ViewBag.Timezones = new SelectList(timezones, "Value", "Text", model.TimeZoneId);
                return View("SetTimezone", model);
            }

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                LoggerExtensions.LogError(_logger, "User not found for ID {UserId}", userId);
                return NotFound();
            }

            user.TimeZoneId = model.TimeZoneId;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                LoggerExtensions.LogInformation(_logger, "TimeZoneId updated for user {UserId}: {TimeZoneId}", userId, model.TimeZoneId);
                TempData["SuccessMessage"] = "Timezone updated successfully.";
            }
            else
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                LoggerExtensions.LogError(_logger, "Failed to update timezone for user {UserId}: {Errors}", userId, errors);
                TempData["ErrorMessage"] = "Failed to update timezone: " + errors;
                // Repopulate timezone dropdown
                var timezones = TimeZoneInfo.GetSystemTimeZones()
                    .Select(tz => new SelectListItem
                    {
                        Value = tz.Id,
                        Text = $"{tz.DisplayName} (UTC{tz.BaseUtcOffset.Hours:+00;-00}:{tz.BaseUtcOffset.Minutes:00})"
                    })
                    .OrderBy(tz => tz.Text)
                    .ToList();
                ViewBag.Timezones = new SelectList(timezones, "Value", "Text", model.TimeZoneId);
                return View("SetTimezone", model);
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}