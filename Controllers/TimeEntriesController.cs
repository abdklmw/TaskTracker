using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace TaskTracker.Controllers
{
    public class TimeEntriesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TimeEntriesController> _logger;

        public TimeEntriesController(AppDbContext context, UserManager<ApplicationUser> userManager, ILogger<TimeEntriesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? clientId, int? projectId)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("User not found for ID {UserId}", userId);
                return NotFound();
            }

            if (!user.TimezoneOffset.HasValue)
            {
                _logger.LogInformation("No TimezoneOffset set for user {UserId}, redirecting to SetTimezone.", userId);
                return RedirectToAction("SetTimezone", "Home");
            }

            ViewBag.TimezoneOffset = user.TimezoneOffset.Value;

            var timeEntries = _context.TimeEntries
                .Where(t => t.UserId == userId)
                .Include(t => t.Project)
                .Include(t => t.Client);

            var clientList = _context.Clients
                .Select(c => new { c.ClientID, c.Name })
                .ToList();
            clientList.Insert(0, new { ClientID = 0, Name = "Select Client" });
            ViewBag.ClientID = new SelectList(clientList, "ClientID", "Name", clientId ?? 0);

            var projectList = _context.Projects
                .Select(p => new { p.ProjectID, p.Name })
                .ToList();
            projectList.Insert(0, new { ProjectID = 0, Name = "Select Project" });
            ViewBag.ProjectID = new SelectList(projectList, "ProjectID", "Name", projectId ?? 0);

            ViewBag.ReturnTo = "TimeEntries";

            return View(await timeEntries.ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientID,ProjectID,StartDateTime,EndDateTime,HoursSpent,Description,UserId")] TimeEntry timeEntry, string ReturnTo, string action)
        {
            if (!User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Unauthorized access attempt to Create action.");
                return RedirectToAction("Login", "Account");
            }

            var userId = _userManager.GetUserId(User);
            timeEntry.UserId = userId;
            ModelState.Remove("UserId");
            ModelState.Remove("Client");
            ModelState.Remove("Project");
            ModelState.Remove("User");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("User not found for ID {UserId}", userId);
                return NotFound();
            }

            if (!user.TimezoneOffset.HasValue)
            {
                _logger.LogInformation("No TimezoneOffset set for user {UserId}, redirecting to SetTimezone.", userId);
                return RedirectToAction("SetTimezone", "Home");
            }

            int effectiveOffset = user.TimezoneOffset.Value;
            ViewBag.TimezoneOffset = effectiveOffset;

            if (action == "StartTimer")
            {
                // Set StartDateTime to current UTC time, rounded back to nearest quarter hour
                var nowUtc = DateTimeOffset.UtcNow.UtcDateTime;
                var minutes = nowUtc.Minute;
                var remainder = minutes % 15;
                var roundedMinutes = minutes - remainder;
                timeEntry.StartDateTime = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, roundedMinutes, 0, 0, DateTimeKind.Utc);
                timeEntry.EndDateTime = null; // Running timer
                timeEntry.HoursSpent = null; // No hours yet
                ModelState.Remove("StartDateTime");
                ModelState.Remove("EndDateTime");
                ModelState.Remove("HoursSpent");
            }
            else
            {
                // Regular create: Convert local times to UTC
                if (timeEntry.StartDateTime != default)
                {
                    timeEntry.StartDateTime = DateTimeOffset.Parse(timeEntry.StartDateTime.ToString()).ToOffset(TimeSpan.FromMinutes(-effectiveOffset)).UtcDateTime;
                }
                if (timeEntry.EndDateTime.HasValue)
                {
                    timeEntry.EndDateTime = DateTimeOffset.Parse(timeEntry.EndDateTime.Value.ToString()).ToOffset(TimeSpan.FromMinutes(-effectiveOffset)).UtcDateTime;
                }
            }

            _logger.LogInformation("Form data: Action={Action}, ClientID={ClientID}, ProjectID={ProjectID}, UserId={UserId}, StartDateTime={StartDateTime}, EndDateTime={EndDateTime}, EffectiveOffset={EffectiveOffset}",
                action, timeEntry.ClientID, timeEntry.ProjectID, timeEntry.UserId, timeEntry.StartDateTime, timeEntry.EndDateTime, effectiveOffset);

            if (timeEntry.ClientID == 0)
            {
                ModelState.AddModelError("ClientID", "Please select a client.");
            }
            if (timeEntry.ProjectID == 0)
            {
                ModelState.AddModelError("ProjectID", "Please select a project.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(timeEntry);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Time entry created successfully: TimeEntryID={TimeEntryID}", timeEntry.TimeEntryID);
                    TempData["SuccessMessage"] = "Time entry created successfully.";
                    return ReturnTo == "Home" ? RedirectToAction("Index", "Home") : RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving time entry: ClientID={ClientID}, ProjectID={ProjectID}", timeEntry.ClientID, timeEntry.ProjectID);
                    ModelState.AddModelError("", "An error occurred while saving the time entry.");
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            _logger.LogWarning("Validation errors in Create: {Errors}", string.Join("; ", errors));

            var clientList = _context.Clients
                .Select(c => new { c.ClientID, c.Name })
                .ToList();
            clientList.Insert(0, new { ClientID = 0, Name = "Select Client" });
            ViewBag.ClientID = new SelectList(clientList, "ClientID", "Name", timeEntry.ClientID);

            var projectList = _context.Projects
                .Select(p => new { p.ProjectID, p.Name })
                .ToList();
            projectList.Insert(0, new { ProjectID = 0, Name = "Select Project" });
            ViewBag.ProjectID = new SelectList(projectList, "ProjectID", "Name", timeEntry.ProjectID);

            ViewBag.ReturnTo = ReturnTo;

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StopTimer(int TimeEntryID)
        {
            if (!User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Unauthorized access attempt to StopTimer action.");
                return RedirectToAction("Login", "Account");
            }

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("User not found for ID {UserId}", userId);
                return NotFound();
            }

            if (!user.TimezoneOffset.HasValue)
            {
                _logger.LogInformation("No TimezoneOffset set for user {UserId}, redirecting to SetTimezone.", userId);
                return RedirectToAction("SetTimezone", "Home");
            }

            var timeEntry = await _context.TimeEntries
                .FirstOrDefaultAsync(t => t.TimeEntryID == TimeEntryID && t.UserId == userId && t.EndDateTime == null);

            if (timeEntry == null)
            {
                _logger.LogWarning("Time entry not found or already stopped: TimeEntryID={TimeEntryID}", TimeEntryID);
                TempData["ErrorMessage"] = "Time entry not found or already stopped.";
                return RedirectToAction("Index", "Home");
            }

            int effectiveOffset = user.TimezoneOffset.Value;

            var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromMinutes(effectiveOffset)).DateTime;
            var minutes = now.Minute;
            var nextQuarterHour = minutes % 15 == 0 ? now : now.AddMinutes(15 - (minutes % 15)).AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
            timeEntry.EndDateTime = DateTimeOffset.Parse(nextQuarterHour.ToString()).ToOffset(TimeSpan.FromMinutes(-effectiveOffset)).UtcDateTime;

            try
            {
                _context.Update(timeEntry);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Timer stopped successfully: TimeEntryID={TimeEntryID}", TimeEntryID);
                TempData["SuccessMessage"] = "Timer stopped successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping timer for TimeEntryID={TimeEntryID}", TimeEntryID);
                TempData["ErrorMessage"] = "Error stopping timer.";
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TimeEntryID,ClientID,ProjectID,StartDateTime,EndDateTime,HoursSpent,Description")] TimeEntry timeEntry)
        {
            if (id != timeEntry.TimeEntryID)
            {
                _logger.LogWarning("Mismatch between ID {Id} and TimeEntryID {TimeEntryID}", id, timeEntry.TimeEntryID);
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            timeEntry.UserId = userId;
            ModelState.Remove("UserId");
            ModelState.Remove("Client");
            ModelState.Remove("Project");
            ModelState.Remove("User");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("User not found for ID {UserId}", userId);
                return NotFound();
            }

            if (!user.TimezoneOffset.HasValue)
            {
                _logger.LogInformation("No TimezoneOffset set for user {UserId}, redirecting to SetTimezone.", userId);
                return RedirectToAction("SetTimezone", "Home");
            }

            int effectiveOffset = user.TimezoneOffset.Value;
            ViewBag.TimezoneOffset = effectiveOffset;

            if (timeEntry.StartDateTime != default)
            {
                timeEntry.StartDateTime = DateTimeOffset.Parse(timeEntry.StartDateTime.ToString()).ToOffset(TimeSpan.FromMinutes(-effectiveOffset)).UtcDateTime;
            }
            if (timeEntry.EndDateTime.HasValue)
            {
                timeEntry.EndDateTime = DateTimeOffset.Parse(timeEntry.EndDateTime.Value.ToString()).ToOffset(TimeSpan.FromMinutes(-effectiveOffset)).UtcDateTime;
            }

            if (timeEntry.ClientID == 0)
            {
                ModelState.AddModelError("ClientID", "Please select a client.");
            }
            if (timeEntry.ProjectID == 0)
            {
                ModelState.AddModelError("ProjectID", "Please select a project.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(timeEntry);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Time entry updated successfully: TimeEntryID={TimeEntryID}", timeEntry.TimeEntryID);
                    TempData["SuccessMessage"] = "Time entry updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TimeEntryExists(timeEntry.TimeEntryID))
                    {
                        _logger.LogWarning("Time entry not found for TimeEntryID={TimeEntryID}", timeEntry.TimeEntryID);
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            _logger.LogWarning("Validation errors in Edit: {Errors}", string.Join("; ", errors));
            TempData["ErrorMessage"] = string.Join("; ", errors);

            var clientList = _context.Clients
                .Select(c => new { c.ClientID, c.Name })
                .ToList();
            clientList.Insert(0, new { ClientID = 0, Name = "Select Client" });
            ViewBag.ClientID = new SelectList(clientList, "ClientID", "Name", timeEntry.ClientID);

            var projectList = _context.Projects
                .Select(p => new { p.ProjectID, p.Name })
                .ToList();
            projectList.Insert(0, new { ProjectID = 0, Name = "Select Project" });
            ViewBag.ProjectID = new SelectList(projectList, "ProjectID", "Name", timeEntry.ProjectID);

            ViewBag.ReturnTo = "TimeEntries";

            return View("Index", await _context.TimeEntries.Where(t => t.UserId == userId).Include(t => t.Project).Include(t => t.Client).ToListAsync());
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete called with null ID");
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("User not found for ID {UserId}", userId);
                return NotFound();
            }

            if (!user.TimezoneOffset.HasValue)
            {
                _logger.LogInformation("No TimezoneOffset set for user {UserId}, redirecting to SetTimezone.", userId);
                return RedirectToAction("SetTimezone", "Home");
            }

            var timeEntry = await _context.TimeEntries
                .Include(t => t.Project)
                .Include(t => t.Client)
                .FirstOrDefaultAsync(m => m.TimeEntryID == id && m.UserId == userId);
            if (timeEntry == null)
            {
                _logger.LogWarning("Time entry not found for TimeEntryID={TimeEntryID}", id);
                return NotFound();
            }

            ViewBag.TimezoneOffset = user.TimezoneOffset.Value;

            return View(timeEntry);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("User not found for ID {UserId}", userId);
                return NotFound();
            }

            if (!user.TimezoneOffset.HasValue)
            {
                _logger.LogInformation("No TimezoneOffset set for user {UserId}, redirecting to SetTimezone.", userId);
                return RedirectToAction("SetTimezone", "Home");
            }

            var timeEntry = await _context.TimeEntries
                .FirstOrDefaultAsync(t => t.TimeEntryID == id && t.UserId == userId);
            if (timeEntry != null)
            {
                _context.TimeEntries.Remove(timeEntry);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Time entry deleted successfully: TimeEntryID={TimeEntryID}", id);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TimeEntryExists(int id)
        {
            var userId = _userManager.GetUserId(User);
            return _context.TimeEntries.Any(e => e.TimeEntryID == id && e.UserId == userId);
        }
    }
}