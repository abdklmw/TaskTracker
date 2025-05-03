using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TaskTracker.Data;
using TaskTracker.Models;
using System.Linq;
using System.Collections.Generic;
using System;

namespace TaskTracker.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(AppDbContext context, UserManager<ApplicationUser> userManager, ILogger<HomeController> logger)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("User ID could not be retrieved for authenticated user.");
                    return RedirectToAction("Login", "Account");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogError("User not found for ID {UserId}", userId);
                    return NotFound();
                }

                // Check if user has a TimezoneOffset set
                if (!user.TimezoneOffset.HasValue)
                {
                    _logger.LogInformation("No TimezoneOffset set for user {UserId}, redirecting to SetTimezone.", userId);
                    return RedirectToAction(nameof(SetTimezone));
                }
                else
                {
                    _logger.LogInformation("TimezoneOffset already set for user {UserId}: {TimezoneOffset}", userId, user.TimezoneOffset);
                    ViewBag.TimezoneOffset = user.TimezoneOffset.Value; // Pass TimezoneOffset to view
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
            // Populate timezone dropdown with common timezones and their offsets
            var timezones = new List<SelectListItem>
            {
                new SelectListItem { Value = "-720", Text = "(UTC-12:00) International Date Line West" },
                new SelectListItem { Value = "-660", Text = "(UTC-11:00) Coordinated Universal Time-11" },
                new SelectListItem { Value = "-600", Text = "(UTC-10:00) Hawaii" },
                new SelectListItem { Value = "-540", Text = "(UTC-09:00) Alaska" },
                new SelectListItem { Value = "-480", Text = "(UTC-08:00) Pacific Time (US & Canada)" },
                new SelectListItem { Value = "-420", Text = "(UTC-07:00) Mountain Time (US & Canada)" },
                new SelectListItem { Value = "-360", Text = "(UTC-06:00) Central Time (US & Canada)" },
                new SelectListItem { Value = "-300", Text = "(UTC-05:00) Eastern Time (US & Canada)" },
                new SelectListItem { Value = "-240", Text = "(UTC-04:00) Atlantic Time (Canada)" },
                new SelectListItem { Value = "-180", Text = "(UTC-03:00) Brasilia" },
                new SelectListItem { Value = "-120", Text = "(UTC-02:00) Coordinated Universal Time-02" },
                new SelectListItem { Value = "-60", Text = "(UTC-01:00) Azores" },
                new SelectListItem { Value = "0", Text = "(UTC+00:00) London, Dublin, Lisbon" },
                new SelectListItem { Value = "60", Text = "(UTC+01:00) Amsterdam, Berlin, Rome, Paris" },
                new SelectListItem { Value = "120", Text = "(UTC+02:00) Athens, Helsinki, Jerusalem" },
                new SelectListItem { Value = "180", Text = "(UTC+03:00) Moscow, St. Petersburg, Nairobi" },
                new SelectListItem { Value = "240", Text = "(UTC+04:00) Abu Dhabi, Muscat" },
                new SelectListItem { Value = "300", Text = "(UTC+05:00) Islamabad, Karachi" },
                new SelectListItem { Value = "360", Text = "(UTC+06:00) Astana, Dhaka" },
                new SelectListItem { Value = "420", Text = "(UTC+07:00) Bangkok, Hanoi, Jakarta" },
                new SelectListItem { Value = "480", Text = "(UTC+08:00) Beijing, Hong Kong, Singapore" },
                new SelectListItem { Value = "540", Text = "(UTC+09:00) Tokyo, Seoul, Osaka" },
                new SelectListItem { Value = "600", Text = "(UTC+10:00) Sydney, Melbourne" },
                new SelectListItem { Value = "660", Text = "(UTC+11:00) Solomon Islands, New Caledonia" },
                new SelectListItem { Value = "720", Text = "(UTC+12:00) Auckland, Wellington" }
            };
            ViewBag.Timezones = new SelectList(timezones, "Value", "Text");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTimezone(int TimezoneOffset)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("User not found for ID {UserId}", userId);
                return NotFound();
            }

            user.TimezoneOffset = TimezoneOffset;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("TimezoneOffset updated for user {UserId}: {TimezoneOffset}", userId, TimezoneOffset);
                TempData["SuccessMessage"] = "Timezone updated successfully.";
            }
            else
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to update timezone for user {UserId}: {Errors}", userId, errors);
                TempData["ErrorMessage"] = "Failed to update timezone: " + errors;
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