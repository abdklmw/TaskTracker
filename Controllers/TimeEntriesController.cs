using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskTracker.Data;
using TaskTracker.Models;
using TaskTracker.Models.TimeEntries;
using TaskTracker.Services;

namespace TaskTracker.Controllers
{
    public class TimeEntriesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TimeEntriesController> _logger;
        private readonly SetupService _setupService;
        private readonly RateCalculationService _rateService;
        private readonly TimeEntryImportService _importService;
        private readonly DropdownService _dropdownService;

        public TimeEntriesController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<TimeEntriesController> logger,
            SetupService setupService,
            RateCalculationService rateService,
            TimeEntryImportService importService,
            DropdownService dropdownService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _setupService = setupService;
            _rateService = rateService;
            _importService = importService;
            _dropdownService = dropdownService;
        }

        public async Task<IActionResult> Index(int recordLimit = 10, int page = 1, int clientFilter = 0, int[] projectFilter = null)
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

            // Get completed time entries (EndDateTime != null and InvoicedDate == null)
            IQueryable<TimeEntry> completedTimeEntriesQuery = _context.TimeEntries
                .Where(t => t.UserId == userId && t.EndDateTime != null && t.InvoicedDate == null)
                .Include(t => t.Client)
                .Include(t => t.Project);

            // Apply client filter
            if (clientFilter > 0)
            {
                completedTimeEntriesQuery = completedTimeEntriesQuery.Where(t => t.ClientID == clientFilter);
            }

            // Apply project filter
            if (projectFilter != null && projectFilter.Any() && !projectFilter.Contains(0))
            {
                completedTimeEntriesQuery = completedTimeEntriesQuery.Where(t => projectFilter.Contains(t.ProjectID));
            }

            // Sort the query
            completedTimeEntriesQuery = completedTimeEntriesQuery
                .OrderBy(t => t.Client)
                .ThenByDescending(t => t.StartDateTime);

            var totalRecords = await completedTimeEntriesQuery.CountAsync();
            var viewModel = new TimeEntriesIndexViewModel
            {
                TimezoneOffset = timezoneOffset,
                ReturnTo = "TimeEntries",
                VisibleCreateForm = false,
                TotalRecords = totalRecords,
                SelectedClientID = clientFilter,
                SelectedProjectIDs = projectFilter?.Where(id => id != 0).ToList() ?? new List<int>()
            };

            // Validate recordLimit
            var validLimits = new[] { 5, 10, 20, 50, 100, 200, -1 }; // -1 represents ALL
            if (!validLimits.Contains(recordLimit))
            {
                recordLimit = 10; // Default to 10 if invalid
            }
            viewModel.RecordLimit = recordLimit;

            // Populate dropdown options for records per page
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

            // Populate filter dropdowns using DropdownService
            var clientDropdown = await _dropdownService.GetClientDropdownAsync(clientFilter);
            clientDropdown.RemoveAt(0); // Remove "Select Client" option for filter
            viewModel.ClientFilterOptions = new SelectList(clientDropdown, "Value", "Text", clientFilter);

            var projectDropdown = await _dropdownService.GetProjectDropdownAsync(0);
            projectDropdown.RemoveAt(0); // Remove "Select Project" option for filter
            viewModel.ProjectFilterOptions = new MultiSelectList(projectDropdown, "Value", "Text", viewModel.SelectedProjectIDs);

            // Populate create form dropdowns using DropdownService
            viewModel.ClientList = new SelectList(
                await _dropdownService.GetClientDropdownAsync(0),
                "Value",
                "Text",
                0
            );
            viewModel.ProjectList = new SelectList(
                await _dropdownService.GetProjectDropdownAsync(0),
                "Value",
                "Text",
                0
            );
            ViewBag.ClientID = viewModel.ClientList;
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

            ViewBag.VisibleCreateForm = false;
            ViewBag.ReturnTo = "TimeEntries";
            ViewBag.TimezoneOffset = timezoneOffset;

            // Fetch running timers
            var runningTimers = await _context.TimeEntries
                .Where(t => t.UserId == userId && t.EndDateTime == null)
                .Include(t => t.Client)
                .Include(t => t.Project)
                .ToListAsync();

            viewModel.RunningTimers = runningTimers;

            return View(viewModel);
        }

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
            var clientList = await _dropdownService.GetClientDropdownAsync(timeEntry.ClientID);
            var projectList = await _dropdownService.GetProjectDropdownAsync(timeEntry.ProjectID);

            var viewModel = new TimeEntriesIndexViewModel
            {
                TimeEntries = await _context.TimeEntries
                    .Where(t => t.UserId == userId && t.EndDateTime != null)
                    .Include(t => t.Client)
                    .Include(t => t.Project)
                    .OrderBy(t => t.Client)
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
                ClientList = new SelectList(clientList, "Value", "Text", timeEntry.ClientID),
                ProjectList = new SelectList(projectList, "Value", "Text", timeEntry.ProjectID),
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

            var clientList = await _dropdownService.GetClientDropdownAsync(timeEntry.ClientID);
            var projectList = await _dropdownService.GetProjectDropdownAsync(timeEntry.ProjectID);

            var viewModel = new TimeEntriesIndexViewModel
            {
                TimeEntries = await _context.TimeEntries
                    .Where(t => t.UserId == userId && t.EndDateTime != null)
                    .Include(t => t.Client)
                    .Include(t => t.Project)
                    .OrderBy(t => t.Client)
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
                ClientList = new SelectList(clientList, "Value", "Text", timeEntry.ClientID),
                ProjectList = new SelectList(projectList, "Value", "Text", timeEntry.ProjectID),
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

            var clientList = await _dropdownService.GetClientDropdownAsync(timeEntry.ClientID);
            ViewBag.ClientID = new SelectList(clientList, "Value", "Text", timeEntry.ClientID);

            var projectList = await _dropdownService.GetProjectDropdownAsync(timeEntry.ProjectID);
            ViewBag.ProjectID = new SelectList(projectList, "Value", "Text", timeEntry.ProjectID);

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
            var clientList = await _dropdownService.GetClientDropdownAsync(timeEntry.ClientID);
            ViewBag.ClientID = new SelectList(clientList, "Value", "Text", timeEntry.ClientID);

            var projectList = await _dropdownService.GetProjectDropdownAsync(timeEntry.ProjectID);
            ViewBag.ProjectID = new SelectList(projectList, "Value", "Text", timeEntry.ProjectID);

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

        [HttpGet]
        public async Task<IActionResult> Import()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User ID could not be retrieved for authenticated user.");
                return RedirectToAction("Login", "Account");
            }

            var setupResult = await _setupService.CheckSetupAsync(userId);
            if (setupResult != null)
            {
                return setupResult;
            }

            var viewModel = await PopulateImportViewModelAsync(new TimeEntryImportViewModel());
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(TimeEntryImportViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User ID could not be retrieved for authenticated user.");
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid || model.CsvFile == null || model.CsvFile.Length == 0)
            {
                ModelState.AddModelError("CsvFile", "Please upload a valid CSV file.");
                var viewModel = await PopulateImportViewModelAsync(model);
                return View(viewModel);
            }

            try
            {
                using var stream = model.CsvFile.OpenReadStream();
                var (timeEntries, errors) = await _importService.ImportFromCsvAsync(stream, userId, model.ClientID, model.ProjectID);

                if (errors.Any())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (timeEntries.Any() && ModelState.IsValid)
                {
                    _context.TimeEntries.AddRange(timeEntries);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Imported {Count} time entries for user {UserId}", timeEntries.Count, userId);
                    TempData["SuccessMessage"] = $"Successfully imported {timeEntries.Count} time entries.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing CSV for user {UserId}", userId);
                ModelState.AddModelError("", "An error occurred while processing the CSV file.");
            }

            var errorViewModel = await PopulateImportViewModelAsync(model);
            return View(errorViewModel);
        }

        private async Task<TimeEntryImportViewModel> PopulateImportViewModelAsync(TimeEntryImportViewModel model)
        {
            model.Clients = await _dropdownService.GetClientDropdownAsync(model.ClientID);
            model.Projects = await _dropdownService.GetProjectDropdownAsync(model.ProjectID);
            return model;
        }
    }
}