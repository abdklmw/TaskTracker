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

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                LoggerExtensions.LogError(_logger, "User not found for ID {UserId}", userId);
                return NotFound();
            }

            // Calculate dynamic offset from user's TimeZoneId
            try
            {
                var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                var nowUtc = DateTimeOffset.UtcNow.UtcDateTime;
                var offset = userTimeZone.GetUtcOffset(nowUtc);
                ViewBag.TimezoneOffset = (int)offset.TotalMinutes;
                LoggerExtensions.LogInformation(_logger, "Dynamic TimezoneOffset for TimeEntries Index: {TimezoneOffset} minutes, DST Active: {IsDst}, TimeZoneId: {TimeZoneId}", ViewBag.TimezoneOffset, userTimeZone.IsDaylightSavingTime(nowUtc), user.TimeZoneId);
            }
            catch (TimeZoneNotFoundException ex)
            {
                LoggerExtensions.LogError(_logger, "Invalid TimeZoneId {TimeZoneId} for user {UserId}: {Error}", user.TimeZoneId, userId, ex.Message);
                ViewBag.TimezoneOffset = 0; // Fallback to UTC
            }

            var timeEntries = await _context.TimeEntries
                .Where(t => t.UserId == userId)
                .Include(t => t.Client)
                .Include(t => t.Project)
                .ToListAsync();

            // Populate dropdowns for create form
            var clientList = _context.Clients
                .Select(c => new { c.ClientID, c.Name })
                .ToList();
            clientList.Insert(0, new { ClientID = 0, Name = "Select Client" });
            ViewBag.ClientID = new SelectList(clientList, "ClientID", "Name", 0);

            var projectList = _context.Projects
                .Select(p => new { p.ProjectID, p.Name })
                .ToList();
            projectList.Insert(0, new { ProjectID = 0, Name = "Select Project" });
            ViewBag.ProjectID = new SelectList(projectList, "ProjectID", "Name", 0);

            ViewBag.VisibleCreateForm = false;
            ViewBag.ReturnTo = "TimeEntries";

            return View(timeEntries);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientID,ProjectID,Description,StartDateTime,EndDateTime,HoursSpent")] TimeEntry timeEntry, string action)
        {
            var userId = _userManager.GetUserId(User);
            timeEntry.UserId = userId;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                LoggerExtensions.LogError(_logger, "User not found for ID {UserId}", userId);
                return NotFound();
            }

            // Calculate dynamic offset from user's TimeZoneId
            int timezoneOffset;
            try
            {
                var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                var nowUtc = DateTimeOffset.UtcNow.UtcDateTime;
                timezoneOffset = (int)userTimeZone.GetUtcOffset(nowUtc).TotalMinutes;
                LoggerExtensions.LogInformation(_logger, "Dynamic TimezoneOffset for Create: {TimezoneOffset} minutes, TimeZoneId: {TimeZoneId}", timezoneOffset, user.TimeZoneId);
            }
            catch (TimeZoneNotFoundException ex)
            {
                LoggerExtensions.LogError(_logger, "Invalid TimeZoneId {TimeZoneId} for user {UserId}: {Error}", user.TimeZoneId, userId, ex.Message);
                timezoneOffset = 0; // Fallback to UTC
            }

            if (action == "StartTimer")
            {
                // Set StartDateTime to current UTC time, rounded to the most recent quarter hour
                var nowUtc = DateTime.UtcNow;
                var minutes = nowUtc.Minute;
                var quarterHours = (int)Math.Floor(minutes / 15.0) * 15; // Round down to nearest 15-minute interval
                timeEntry.StartDateTime = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, quarterHours, 0, DateTimeKind.Utc);
                timeEntry.EndDateTime = null;
                timeEntry.HoursSpent = null;

                // Remove validation errors for StartDateTime, EndDateTime, and HoursSpent
                ModelState.Remove("StartDateTime");
                ModelState.Remove("EndDateTime");
                ModelState.Remove("HoursSpent");
                ModelState.Remove("UserId");
                ModelState.Remove("Client");
                ModelState.Remove("Project");
                ModelState.Remove("User");

                if (ModelState.IsValid)
                {
                    _context.Add(timeEntry);
                    await _context.SaveChangesAsync();
                    LoggerExtensions.LogInformation(_logger, "Timer started for user {UserId}, TimeEntryID: {TimeEntryID}, StartDateTime: {StartDateTime}", userId, timeEntry.TimeEntryID, timeEntry.StartDateTime);
                    TempData["SuccessMessage"] = "Timer started successfully.";
                    return RedirectToAction(nameof(Index), "Home");
                }
            }
            else
            {
                if (ModelState.IsValid)
                {
                    _context.Add(timeEntry);
                    await _context.SaveChangesAsync();
                    LoggerExtensions.LogInformation(_logger, "Time entry created for user {UserId}, TimeEntryID: {TimeEntryID}", userId, timeEntry.TimeEntryID);
                    TempData["SuccessMessage"] = "Time entry created successfully.";
                    return RedirectToAction(nameof(Index), ViewBag.ReturnTo == "Home" ? "Home" : "TimeEntries");
                }
            }

            // Repopulate dropdowns and return to view
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

            ViewBag.TimezoneOffset = timezoneOffset;
            ViewBag.VisibleCreateForm = true;
            ViewBag.ReturnTo = ViewBag.ReturnTo ?? "TimeEntries";

            return View("Index", await _context.TimeEntries.Where(t => t.UserId == userId).Include(t => t.Client).Include(t => t.Project).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTimer([Bind("ClientID,ProjectID,Description")] TimeEntry timeEntry)
        {
            var userId = _userManager.GetUserId(User);
            timeEntry.UserId = userId;
            // Set StartDateTime to current UTC time, rounded to the most recent quarter hour
            var nowUtc = DateTime.UtcNow;
            var minutes = nowUtc.Minute;
            var quarterHours = (int)Math.Floor(minutes / 15.0) * 15; // Round down to nearest 15-minute interval
            timeEntry.StartDateTime = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, quarterHours, 0, DateTimeKind.Utc);
            timeEntry.EndDateTime = null;
            timeEntry.HoursSpent = null;

            // Remove validation errors for StartDateTime, EndDateTime, and HoursSpent
            ModelState.Remove("StartDateTime");
            ModelState.Remove("EndDateTime");
            ModelState.Remove("HoursSpent");

            if (ModelState.IsValid)
            {
                _context.Add(timeEntry);
                await _context.SaveChangesAsync();
                LoggerExtensions.LogInformation(_logger, "Timer started for user {UserId}, TimeEntryID: {TimeEntryID}, StartDateTime: {StartDateTime}", userId, timeEntry.TimeEntryID, timeEntry.StartDateTime);
                TempData["SuccessMessage"] = "Timer started successfully.";
                return RedirectToAction(nameof(Index));
            }

            // Repopulate dropdowns
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

            // Calculate dynamic offset for view
            var user = await _userManager.FindByIdAsync(userId);
            try
            {
                var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                var nowUtcAlso = DateTimeOffset.UtcNow.UtcDateTime;
                ViewBag.TimezoneOffset = (int)userTimeZone.GetUtcOffset(nowUtcAlso).TotalMinutes;
            }
            catch (TimeZoneNotFoundException)
            {
                ViewBag.TimezoneOffset = 0; // Fallback to UTC
            }

            ViewBag.VisibleCreateForm = true;
            return View("Index", await _context.TimeEntries.Where(t => t.UserId == userId).Include(t => t.Client).Include(t => t.Project).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StopTimer(int TimeEntryID)
        {
            var userId = _userManager.GetUserId(User);
            var timeEntry = await _context.TimeEntries.FindAsync(TimeEntryID);

            if (timeEntry == null || timeEntry.UserId != userId)
            {
                LoggerExtensions.LogWarning(_logger, "Time entry {TimeEntryID} not found or not owned by user {UserId}", TimeEntryID, userId);
                TempData["ErrorMessage"] = "Time entry not found.";
                return RedirectToAction(nameof(Index));
            }

            timeEntry.EndDateTime = DateTime.UtcNow;
            var duration = timeEntry.EndDateTime.Value - timeEntry.StartDateTime;
            timeEntry.HoursSpent = Convert.ToDecimal(duration.TotalHours);

            if (ModelState.IsValid)
            {
                _context.Update(timeEntry);
                await _context.SaveChangesAsync();
                LoggerExtensions.LogInformation(_logger, "Timer stopped for user {UserId}, TimeEntryID: {TimeEntryID}, HoursSpent: {HoursSpent}", userId, timeEntry.TimeEntryID, timeEntry.HoursSpent);
                TempData["SuccessMessage"] = "Timer stopped successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to stop timer.";
            }

            return RedirectToAction(nameof(Index), "Home");
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var timeEntry = await _context.TimeEntries
                .Include(t => t.Client)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TimeEntryID == id && t.UserId == userId);

            if (timeEntry == null)
            {
                LoggerExtensions.LogWarning(_logger, "Time entry {TimeEntryID} not found for user {UserId}", id, userId);
                return NotFound();
            }

            // Calculate dynamic offset for view
            var user = await _userManager.FindByIdAsync(userId);
            try
            {
                var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                var nowUtc = DateTimeOffset.UtcNow.UtcDateTime;
                ViewBag.TimezoneOffset = (int)userTimeZone.GetUtcOffset(nowUtc).TotalMinutes;
            }
            catch (TimeZoneNotFoundException)
            {
                ViewBag.TimezoneOffset = 0; // Fallback to UTC
            }

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

            return View(timeEntry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TimeEntryID,UserId,ClientID,ProjectID,Description,StartDateTime,EndDateTime,HoursSpent")] TimeEntry timeEntry)
        {
            if (id != timeEntry.TimeEntryID)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (timeEntry.UserId != userId)
            {
                LoggerExtensions.LogWarning(_logger, "User {UserId} attempted to edit time entry {TimeEntryID} not owned", userId, timeEntry.TimeEntryID);
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(timeEntry);
                    await _context.SaveChangesAsync();
                    LoggerExtensions.LogInformation(_logger, "Time entry updated for user {UserId}, TimeEntryID: {TimeEntryID}", userId, timeEntry.TimeEntryID);
                    TempData["SuccessMessage"] = "Time entry updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TimeEntryExists(timeEntry.TimeEntryID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // Repopulate dropdowns
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

            // Calculate dynamic offset for view
            var user = await _userManager.FindByIdAsync(userId);
            try
            {
                var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                var nowUtc = DateTimeOffset.UtcNow.UtcDateTime;
                ViewBag.TimezoneOffset = (int)userTimeZone.GetUtcOffset(nowUtc).TotalMinutes;
            }
            catch (TimeZoneNotFoundException)
            {
                ViewBag.TimezoneOffset = 0; // Fallback to UTC
            }

            return View(timeEntry);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var timeEntry = await _context.TimeEntries
                .Include(t => t.Client)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TimeEntryID == id && t.UserId == userId);

            if (timeEntry == null)
            {
                LoggerExtensions.LogWarning(_logger, "Time entry {TimeEntryID} not found for user {UserId}", id, userId);
                return NotFound();
            }

            return View(timeEntry);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var timeEntry = await _context.TimeEntries.FindAsync(id);
            if (timeEntry == null || timeEntry.UserId != userId)
            {
                LoggerExtensions.LogWarning(_logger, "Time entry {TimeEntryID} not found or not owned by user {UserId}", id, userId);
                TempData["ErrorMessage"] = "Time entry not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.TimeEntries.Remove(timeEntry);
            await _context.SaveChangesAsync();
            LoggerExtensions.LogInformation(_logger, "Time entry deleted for user {UserId}, TimeEntryID: {TimeEntryID}", userId, id);
            TempData["SuccessMessage"] = "Time entry deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool TimeEntryExists(int id)
        {
            return _context.TimeEntries.Any(e => e.TimeEntryID == id);
        }
    }
}