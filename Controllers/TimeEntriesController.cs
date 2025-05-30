﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models;
using Microsoft.AspNetCore.Identity;
using TaskTracker.Services;

namespace TaskTracker.Controllers
{
    public class TimeEntriesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TimeEntriesController> _logger;
        private readonly SetupService _setupService;

        public TimeEntriesController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<TimeEntriesController> logger,
            SetupService setupService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _setupService = setupService;
        }

        public async Task<IActionResult> Index(int recordLimit = 10, int page = 1)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                LoggerExtensions.LogError(_logger, "User ID could not be retrieved for authenticated user.");
                return RedirectToAction("Login", "Account");
            }

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

            // Calculate dynamic offset from user's TimeZoneId
            int timezoneOffset;
            try
            {
                var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                var nowUtc = DateTimeOffset.UtcNow.UtcDateTime;
                timezoneOffset = (int)userTimeZone.GetUtcOffset(nowUtc).TotalMinutes;
                LoggerExtensions.LogInformation(_logger, "Dynamic TimezoneOffset for TimeEntries Index: {TimezoneOffset} minutes, DST Active: {IsDst}, TimeZoneId: {TimeZoneId}", timezoneOffset, userTimeZone.IsDaylightSavingTime(nowUtc), user.TimeZoneId);
            }
            catch (TimeZoneNotFoundException ex)
            {
                LoggerExtensions.LogError(_logger, "Invalid TimeZoneId {TimeZoneId} for user {UserId}: {Error}", user.TimeZoneId, userId, ex.Message);
                timezoneOffset = 0; // Fallback to UTC
            }

            // Get completed time entries (EndDateTime != null)
            var completedTimeEntriesQuery = _context.TimeEntries
                .Where(t => t.UserId == userId && t.EndDateTime != null)
                .Include(t => t.Client)
                .Include(t => t.Project)
                .OrderBy(t => t.Client.Name ?? "") // Sort by Client.Name ascending, handle nulls
                .ThenByDescending(t => t.StartDateTime); // Then sort by StartDateTime descending

            var totalRecords = await completedTimeEntriesQuery.CountAsync();
            var viewModel = new TimeEntriesIndexViewModel
            {
                TimezoneOffset = timezoneOffset,
                ReturnTo = "TimeEntries",
                VisibleCreateForm = false,
                TotalRecords = totalRecords
            };

            // Validate recordLimit
            var validLimits = new[] { 5, 10, 20, 50, 100, 200, -1 }; // -1 represents ALL
            if (!validLimits.Contains(recordLimit))
            {
                recordLimit = 10; // Default to 10 if invalid
            }
            viewModel.RecordLimit = recordLimit;

            // Populate dropdown options
            var limitOptions = new[]
            {
                new { Value = 5, Text = "5" },
                new { Value = 10, Text = "10" },
                new { Value = 20, Text = "20" },
                new { Value = 50, Text = "50" },
                new { Value = 100, Text = "100" },
                new { Value = 200, Text = "200" },
                new { Value = -1, Text = "ALL" }
            };
            viewModel.RecordLimitOptions = new SelectList(limitOptions, "Value", "Text", recordLimit);

            // Populate create form dropdowns
            var clientList = _context.Clients
                .Select(c => new { c.ClientID, c.Name })
                .ToList();
            clientList.Insert(0, new { ClientID = 0, Name = "Select Client" });
            viewModel.ClientList = new SelectList(clientList, "ClientID", "Name", 0);
            ViewBag.ClientID = viewModel.ClientList;

            var projectList = _context.Projects
                .Select(p => new { p.ProjectID, p.Name })
                .ToList();
            projectList.Insert(0, new { ProjectID = 0, Name = "Select Project" });
            viewModel.ProjectList = new SelectList(projectList, "ProjectID", "Name", 0);
            ViewBag.ProjectID = viewModel.ProjectList;

            // Apply pagination
            viewModel.CurrentPage = page < 1 ? 1 : page;
            if (recordLimit == -1)
            {
                const int pageSize = 200;
                viewModel.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
                viewModel.TimeEntries = await completedTimeEntriesQuery
                    .Skip((viewModel.CurrentPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            else
            {
                viewModel.TotalPages = (int)Math.Ceiling((double)totalRecords / recordLimit);
                viewModel.TimeEntries = await completedTimeEntriesQuery
                    .Skip((viewModel.CurrentPage - 1) * recordLimit)
                    .Take(recordLimit)
                    .ToListAsync();
            }

            ViewBag.VisibleCreateForm = false; // Set form to hidden by default
            ViewBag.ReturnTo = "TimeEntries"; // Set form redirect target
            ViewBag.TimezoneOffset = timezoneOffset;

            // Fetch running timers
            var runningTimers = _context.TimeEntries
                .Where(t => t.UserId == userId && t.EndDateTime == null)
                .Include(t => t.Client)
                .Include(t => t.Project)
                .ToList();

            viewModel.RunningTimers = runningTimers;

            return View(viewModel);
        }

        // Other actions (Create, StartTimer, StopTimer, Edit, Delete, DeleteConfirmed, TimeEntryExists) remain unchanged
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientID,ProjectID,Description,StartDateTime,EndDateTime,HoursSpent")] TimeEntry timeEntry, string? action)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                LoggerExtensions.LogError(_logger, "User ID could not be retrieved for authenticated user.");
                return RedirectToAction("Login", "Account");
            }
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

            // Remove UserId from ModelState validation
            ModelState.Remove("UserId");

            if (action == "StartTimer")
            {
                // Set StartDateTime to current UTC time, rounded to the most recent quarter hour
                var currentUtc = DateTime.UtcNow; // Renamed to avoid conflict
                var minutes = currentUtc.Minute;
                var quarterHours = (int)Math.Floor(minutes / 15.0) * 15;
                timeEntry.StartDateTime = new DateTime(currentUtc.Year, currentUtc.Month, currentUtc.Day, currentUtc.Hour, quarterHours, 0, DateTimeKind.Utc);
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
                    var returnTo = ViewBag.ReturnTo as string ?? "TimeEntries";
                    return RedirectToAction(nameof(Index), returnTo == "Home" ? "Home" : "TimeEntries");
                }
            }

            // Repopulate view model for error case
            var clientList = _context.Clients
                .Select(c => new { c.ClientID, c.Name })
                .ToList();
            clientList.Insert(0, new { ClientID = 0, Name = "Select Client" });
            var projectList = _context.Projects
                .Select(p => new { p.ProjectID, p.Name })
                .ToList();
            projectList.Insert(0, new { ProjectID = 0, Name = "Select Project" });

            var viewModel = new TimeEntriesIndexViewModel
            {
                TimeEntries = await _context.TimeEntries
                    .Where(t => t.UserId == userId && t.EndDateTime != null)
                    .Include(t => t.Client)
                    .Include(t => t.Project)
                    .OrderBy(t => t.Client.Name ?? "")
                    .ThenByDescending(t => t.StartDateTime)
                    .Take(10)
                    .ToListAsync(),
                RecordLimit = 10,
                RecordLimitOptions = new SelectList(new[]
                {
                    new { Value = 5, Text = "5" },
                    new { Value = 10, Text = "10" },
                    new { Value = 20, Text = "20" },
                    new { Value = 50, Text = "50" },
                    new { Value = 100, Text = "100" },
                    new { Value = 200, Text = "200" },
                    new { Value = -1, Text = "ALL" }
                }, "Value", "Text", 10),
                ClientList = new SelectList(clientList, "ClientID", "Name", timeEntry.ClientID),
                ProjectList = new SelectList(projectList, "ProjectID", "Name", timeEntry.ProjectID),
                TimezoneOffset = timezoneOffset,
                VisibleCreateForm = true,
                ReturnTo = ViewBag.ReturnTo ?? "TimeEntries",
                CurrentPage = 1,
                TotalPages = 1,
                TotalRecords = await _context.TimeEntries.CountAsync(t => t.UserId == userId && t.EndDateTime != null)
            };

            // Set ViewBag for _CreateForm.cshtml
            ViewBag.ClientID = viewModel.ClientList;
            ViewBag.ProjectID = viewModel.ProjectList;
            ViewBag.VisibleCreateForm = true;
            ViewBag.ReturnTo = ViewBag.ReturnTo ?? "TimeEntries";
            ViewBag.TimezoneOffset = timezoneOffset;

            return View("Index", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTimer([Bind("ClientID,ProjectID,Description")] TimeEntry timeEntry)
        {
            var userId = _userManager.GetUserId(User);
            timeEntry.UserId = userId;
            // Set StartDateTime to current UTC time, rounded to the most recent quarter hour
            var currentUtc = DateTime.UtcNow; // Renamed to avoid conflict
            var minutes = currentUtc.Minute;
            var quarterHours = (int)Math.Floor(minutes / 15.0) * 15;
            timeEntry.StartDateTime = new DateTime(currentUtc.Year, currentUtc.Month, currentUtc.Day, currentUtc.Hour, quarterHours, 0, DateTimeKind.Utc);
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

            // Repopulate view model for error case
            var user = await _userManager.FindByIdAsync(userId);
            int timezoneOffset;
            try
            {
                var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                var nowUtc = DateTimeOffset.UtcNow.UtcDateTime;
                timezoneOffset = (int)userTimeZone.GetUtcOffset(nowUtc).TotalMinutes;
            }
            catch (TimeZoneNotFoundException)
            {
                timezoneOffset = 0;
            }

            var clientList = _context.Clients
                .Select(c => new { c.ClientID, c.Name })
                .ToList();
            clientList.Insert(0, new { ClientID = 0, Name = "Select Client" });
            var projectList = _context.Projects
                .Select(p => new { p.ProjectID, p.Name })
                .ToList();
            projectList.Insert(0, new { ProjectID = 0, Name = "Select Project" });

            var viewModel = new TimeEntriesIndexViewModel
            {
                TimeEntries = await _context.TimeEntries
                    .Where(t => t.UserId == userId && t.EndDateTime != null)
                    .Include(t => t.Client)
                    .Include(t => t.Project)
                    .OrderBy(t => t.Client.Name ?? "")
                    .ThenByDescending(t => t.StartDateTime)
                    .Take(10)
                    .ToListAsync(),
                RecordLimit = 10,
                RecordLimitOptions = new SelectList(new[]
                {
                    new { Value = 5, Text = "5" },
                    new { Value = 10, Text = "10" },
                    new { Value = 20, Text = "20" },
                    new { Value = 50, Text = "50" },
                    new { Value = 100, Text = "100" },
                    new { Value = 200, Text = "200" },
                    new { Value = -1, Text = "ALL" }
                }, "Value", "Text", 10),
                ClientList = new SelectList(clientList, "ClientID", "Name", timeEntry.ClientID),
                ProjectList = new SelectList(projectList, "ProjectID", "Name", timeEntry.ProjectID),
                TimezoneOffset = timezoneOffset,
                VisibleCreateForm = true,
                ReturnTo = "TimeEntries",
                CurrentPage = 1,
                TotalPages = 1,
                TotalRecords = await _context.TimeEntries.CountAsync(t => t.UserId == userId && t.EndDateTime != null)
            };

            // Set ViewBag for _CreateForm.cshtml
            ViewBag.ClientID = viewModel.ClientList;
            ViewBag.ProjectID = viewModel.ProjectList;
            ViewBag.VisibleCreateForm = true;
            ViewBag.ReturnTo = "TimeEntries";
            ViewBag.TimezoneOffset = timezoneOffset;

            return View("Index", viewModel);
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

            // Get current UTC time and round up to the nearest quarter hour
            var currentUtc = DateTime.UtcNow;
            var minutes = currentUtc.Minute;
            var quarterHours = (int)Math.Ceiling(minutes / 15.0) * 15;
            if (quarterHours >= 60)
            {
                currentUtc = currentUtc.AddHours(1).AddMinutes(-minutes);
                quarterHours = 0;
            }
            timeEntry.EndDateTime = new DateTime(currentUtc.Year, currentUtc.Month, currentUtc.Day, currentUtc.Hour, quarterHours, 0, DateTimeKind.Utc);

            // Calculate HoursSpent based on rounded EndDateTime
            var duration = timeEntry.EndDateTime.Value - timeEntry.StartDateTime;
            timeEntry.HoursSpent = Convert.ToDecimal(duration.TotalHours);

            if (ModelState.IsValid)
            {
                _context.Update(timeEntry);
                await _context.SaveChangesAsync();
                LoggerExtensions.LogInformation(_logger, "Timer stopped for user {UserId}, TimeEntryID: {TimeEntryID}, EndDateTime: {EndDateTime}, HoursSpent: {HoursSpent}", userId, timeEntry.TimeEntryID, timeEntry.EndDateTime, timeEntry.HoursSpent);
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