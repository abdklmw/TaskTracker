using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskTracker.Data;
using TaskTracker.Models;
using TaskTracker.Models.TimeEntries;
using TaskTracker.Services;

namespace TaskTracker.Controllers
{
    public class TimeEntriesController : Controller
    {
        private readonly TimeEntryService _timeEntryService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TimeEntriesController> _logger;
        private readonly SetupService _setupService;
        private readonly ProjectService _projectService;
        private readonly ClientService _clientService;

        public TimeEntriesController(
            TimeEntryService timeEntryService,
            UserManager<ApplicationUser> userManager,
            ILogger<TimeEntriesController> logger,
            SetupService setupService,
            ProjectService projectService,
            ClientService clientService)
        {
            _timeEntryService = timeEntryService;
            _userManager = userManager;
            _logger = logger;
            _setupService = setupService;
            _projectService = projectService;
            _clientService = clientService;
        }

        public async Task<IActionResult> Index(
            int recordLimit = 10,
            int page = 1,
            int clientFilter = 0,
            int[] projectFilter = null,
            DateTime? invoicedDateStart = null,
            DateTime? invoicedDateEnd = null,
            DateTime? paidDateStart = null,
            DateTime? paidDateEnd = null,
            DateTime? invoiceSentDateStart = null,
            DateTime? invoiceSentDateEnd = null,
            bool invoicedDateAny = false,
            bool paidDateAny = false,
            bool invoiceSentDateAny = false)
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

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("User not found for ID {UserId}", userId);
                return NotFound();
            }

            int timezoneOffset = 0;
            if (!string.IsNullOrEmpty(user.TimeZoneId))
            {
                try
                {
                    var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                    timezoneOffset = (int)userTimeZone.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
                }
                catch (TimeZoneNotFoundException)
                {
                    _logger.LogWarning("Invalid TimeZoneId {TimeZoneId} for user {UserId}", user.TimeZoneId, userId);
                }
            }

            var validLimits = new[] { 5, 10, 20, 50, 100, 200, -1 };
            if (!validLimits.Contains(recordLimit))
            {
                recordLimit = 10;
            }

            var (timeEntries, totalRecords, totalPages) = await _timeEntryService.GetTimeEntriesAsync(
                userId, recordLimit, page, clientFilter, projectFilter,
                invoicedDateStart, invoicedDateEnd, paidDateStart, paidDateEnd,
                invoiceSentDateStart, invoiceSentDateEnd,
                invoicedDateAny, paidDateAny, invoiceSentDateAny);

            var viewModel = new TimeEntriesIndexViewModel
            {
                TimeEntries = timeEntries,
                TimezoneOffset = timezoneOffset,
                ReturnTo = "TimeEntries",
                VisibleCreateForm = false,
                TotalRecords = totalRecords,
                CurrentPage = page,
                TotalPages = totalPages,
                RecordLimit = recordLimit,
                SelectedClientID = clientFilter,
                SelectedProjectIDs = projectFilter?.Where(id => id != 0).ToList() ?? new List<int>(),
                InvoicedDateStart = invoicedDateStart,
                InvoicedDateEnd = invoicedDateEnd,
                PaidDateStart = paidDateStart,
                PaidDateEnd = paidDateEnd,
                InvoiceSentDateStart = invoiceSentDateStart,
                InvoiceSentDateEnd = invoiceSentDateEnd,
                InvoicedDateAny = invoicedDateAny,
                PaidDateAny = paidDateAny,
                InvoiceSentDateAny = invoiceSentDateAny,
                RouteValues = new Dictionary<string, string>
                {
                    { "recordLimit", recordLimit.ToString() },
                    { "clientFilter", clientFilter.ToString() },
                    { "invoicedDateStart", invoicedDateStart?.ToString("yyyy-MM-dd") ?? "" },
                    { "invoicedDateEnd", invoicedDateEnd?.ToString("yyyy-MM-dd") ?? "" },
                    { "paidDateStart", paidDateStart?.ToString("yyyy-MM-dd") ?? "" },
                    { "paidDateEnd", paidDateEnd?.ToString("yyyy-MM-dd") ?? "" },
                    { "invoiceSentDateStart", invoiceSentDateStart?.ToString("yyyy-MM-dd") ?? "" },
                    { "invoiceSentDateEnd", invoiceSentDateEnd?.ToString("yyyy-MM-dd") ?? "" },
                    { "invoicedDateAny", invoicedDateAny.ToString().ToLower() },
                    { "paidDateAny", paidDateAny.ToString().ToLower() },
                    { "invoiceSentDateAny", invoiceSentDateAny.ToString().ToLower() }
                }
            };

            if (projectFilter != null && projectFilter.Any() && !projectFilter.Contains(0))
            {
                foreach (var projectId in projectFilter)
                {
                    viewModel.RouteValues.Add("projectFilter", projectId.ToString());
                }
            }

            viewModel.RecordLimitOptions = new SelectList(new[]
            {
                new { Value = 5, Text = "5" },
                new { Value = 10, Text = "10" },
                new { Value = 20, Text = "20" },
                new { Value = 50, Text = "50" },
                new { Value = 100, Text = "100" },
                new { Value = 200, Text = "200" },
                new { Value = -1, Text = "ALL" }
            }, "Value", "Text", recordLimit);

            var clientDropdown = await _clientService.GetClientDropdownAsync(clientFilter);
            viewModel.ClientFilterOptions = new SelectList(clientDropdown, "Value", "Text", clientFilter);

            var projectDropdown = await _projectService.GetProjectDropdownAsync(0);
            projectDropdown.RemoveAt(0);
            viewModel.ProjectFilterOptions = new MultiSelectList(projectDropdown, "Value", "Text", viewModel.SelectedProjectIDs);

            viewModel.ClientList = new SelectList(
                await _clientService.GetClientDropdownAsync(0),
                "Value",
                "Text",
                0);
            viewModel.ProjectList = new SelectList(
                await _projectService.GetProjectDropdownAsync(0),
                "Value",
                "Text",
                0);
            ViewBag.ClientID = viewModel.ClientList;
            ViewBag.ProjectID = viewModel.ProjectList;

            viewModel.RunningTimers = await _timeEntryService.GetRunningTimersAsync(userId);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientID,ProjectID,Description,StartDateTime,EndDateTime,HoursSpent")] TimeEntry timeEntry, string? action)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User ID could not be retrieved for authenticated user.");
                return RedirectToAction("Login", "Account");
            }
            timeEntry.UserId = userId;

            int timezoneOffset = 0;
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !string.IsNullOrEmpty(user.TimeZoneId))
            {
                try
                {
                    var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                    timezoneOffset = (int)userTimeZone.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
                }
                catch (TimeZoneNotFoundException)
                {
                    _logger.LogWarning("Invalid TimeZoneId {TimeZoneId} for user {UserId}", user.TimeZoneId, userId);
                }
            }

            ModelState.Remove("UserId");

            if (action == "StartTimer")
            {
                ModelState.Remove("StartDateTime");
                ModelState.Remove("EndDateTime");
                ModelState.Remove("HoursSpent");
                ModelState.Remove("Client");
                ModelState.Remove("Project");
                ModelState.Remove("User");

                if (ModelState.IsValid)
                {
                    var (success, error) = await _timeEntryService.StartTimerAsync(timeEntry);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Timer started successfully.";
                        return RedirectToAction(nameof(Index), "Home");
                    }
                    TempData["ErrorMessage"] = error;
                }
            }
            else
            {
                if (ModelState.IsValid)
                {
                    var (success, error) = await _timeEntryService.CreateTimeEntryAsync(timeEntry);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Time entry created successfully.";
                        var returnTo = ViewBag.ReturnTo as string ?? "TimeEntries";
                        return RedirectToAction(nameof(Index), returnTo == "Home" ? "Home" : "TimeEntries");
                    }
                    TempData["ErrorMessage"] = error;
                }
            }

            var clientList = await _clientService.GetClientDropdownAsync(timeEntry.ClientID);
            var projectList = await _projectService.GetProjectDropdownAsync(timeEntry.ProjectID);

            var viewModel = new TimeEntriesIndexViewModel
            {
                TimeEntries = (await _timeEntryService.GetTimeEntriesAsync(userId, 10, 1, 0, null, null, null, null, null, null, null, false, false, false)).TimeEntries,
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
                ReturnTo = ViewBag.ReturnTo as string ?? "TimeEntries",
                CurrentPage = 1,
                TotalPages = 1,
                TotalRecords = (await _timeEntryService.GetTimeEntriesAsync(userId, -1, 1, 0, null, null, null, null, null, null, null, false, false, false)).TotalRecords
            };

            ViewBag.ClientID = viewModel.ClientList;
            ViewBag.ProjectID = viewModel.ProjectList;
            ViewBag.VisibleCreateForm = true;
            ViewBag.ReturnTo = viewModel.ReturnTo;

            return View("Index", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTimer([Bind("ClientID,ProjectID,Description")] TimeEntry timeEntry)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User ID could not be retrieved for authenticated user.");
                return RedirectToAction("Login", "Account");
            }
            timeEntry.UserId = userId;

            ModelState.Remove("UserId");
            ModelState.Remove("StartDateTime");
            ModelState.Remove("EndDateTime");
            ModelState.Remove("HoursSpent");

            if (ModelState.IsValid)
            {
                var (success, error) = await _timeEntryService.StartTimerAsync(timeEntry);
                if (success)
                {
                    TempData["SuccessMessage"] = "Timer started successfully.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = error;
            }

            int timezoneOffset = 0;
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !string.IsNullOrEmpty(user.TimeZoneId))
            {
                try
                {
                    var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                    timezoneOffset = (int)userTimeZone.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
                }
                catch (TimeZoneNotFoundException)
                {
                    _logger.LogWarning("Invalid TimeZoneId {TimeZoneId} for user {UserId}", user.TimeZoneId, userId);
                }
            }

            var clientList = await _clientService.GetClientDropdownAsync(timeEntry.ClientID);
            var projectList = await _projectService.GetProjectDropdownAsync(timeEntry.ProjectID);

            var viewModel = new TimeEntriesIndexViewModel
            {
                TimeEntries = (await _timeEntryService.GetTimeEntriesAsync(userId, 10, 1, 0, null, null, null, null, null, null, null, false, false, false)).TimeEntries,
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
                TotalRecords = (await _timeEntryService.GetTimeEntriesAsync(userId, -1, 1, 0, null, null, null, null, null, null, null, false, false, false)).TotalRecords
            };

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
            var (success, error) = await _timeEntryService.StopTimerAsync(TimeEntryID, userId);
            if (success)
            {
                TempData["SuccessMessage"] = "Timer stopped successfully.";
            }
            else
            {
                _logger.LogWarning("Time entry {TimeEntryID} not found or not owned by user {UserId}", TimeEntryID, userId);
                TempData["ErrorMessage"] = error;
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
            var timeEntry = await _timeEntryService.GetTimeEntryByIdAsync(id.Value, userId);
            if (timeEntry == null)
            {
                _logger.LogWarning("Time entry {TimeEntryID} not found for user {UserId}", id, userId);
                return NotFound();
            }

            int timezoneOffset = 0;
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !string.IsNullOrEmpty(user.TimeZoneId))
            {
                try
                {
                    var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                    timezoneOffset = (int)userTimeZone.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
                }
                catch (TimeZoneNotFoundException)
                {
                    _logger.LogWarning("Invalid TimeZoneId {TimeZoneId} for user {UserId}", user.TimeZoneId, userId);
                }
            }

            var clientList = await _clientService.GetClientDropdownAsync(timeEntry.ClientID);
            ViewBag.ClientID = new SelectList(clientList, "Value", "Text", timeEntry.ClientID);

            var projectList = await _projectService.GetProjectDropdownAsync(timeEntry.ProjectID);
            ViewBag.ProjectID = new SelectList(projectList, "Value", "Text", timeEntry.ProjectID);

            ViewBag.TimezoneOffset = timezoneOffset;

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
            if (ModelState.IsValid)
            {
                var (success, error) = await _timeEntryService.UpdateTimeEntryAsync(timeEntry, userId);
                if (success)
                {
                    TempData["SuccessMessage"] = "Time entry updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = error;
            }

            int timezoneOffset = 0;
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !string.IsNullOrEmpty(user.TimeZoneId))
            {
                try
                {
                    var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                    timezoneOffset = (int)userTimeZone.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
                }
                catch (TimeZoneNotFoundException)
                {
                    _logger.LogWarning("Invalid TimeZoneId {TimeZoneId} for user {UserId}", user.TimeZoneId, userId);
                }
            }

            var clientList = await _clientService.GetClientDropdownAsync(timeEntry.ClientID);
            ViewBag.ClientID = new SelectList(clientList, "Value", "Text", timeEntry.ClientID);

            var projectList = await _projectService.GetProjectDropdownAsync(timeEntry.ProjectID);
            ViewBag.ProjectID = new SelectList(projectList, "Value", "Text", timeEntry.ProjectID);

            ViewBag.TimezoneOffset = timezoneOffset;

            return View(timeEntry);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var timeEntry = await _timeEntryService.GetTimeEntryByIdAsync(id.Value, userId);
            if (timeEntry == null)
            {
                _logger.LogWarning("Time entry {TimeEntryID} not found for user {UserId}", id, userId);
                return NotFound();
            }

            return View(timeEntry);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var (success, error) = await _timeEntryService.DeleteTimeEntryAsync(id, userId);
            if (success)
            {
                TempData["SuccessMessage"] = "Time entry deleted successfully.";
            }
            else
            {
                _logger.LogWarning("Time entry {TimeEntryID} not found or not owned by user {UserId}", id, userId);
                TempData["ErrorMessage"] = error;
            }
            return RedirectToAction(nameof(Index));
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
                var (timeEntries, errors) = await _timeEntryService.ImportFromCsvAsync(stream, userId, model.ClientID, model.ProjectID);

                if (errors.Any())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (timeEntries.Any())
                {
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
            model.Clients = await _clientService.GetClientDropdownAsync(model.ClientID);
            model.Projects = await _projectService.GetProjectDropdownAsync(model.ProjectID);
            return model;
        }
    }
}