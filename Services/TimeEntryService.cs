using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models.TimeEntries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace TaskTracker.Services
{
    public class TimeEntryService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TimeEntryService> _logger;
        private readonly IUserService _userService;

        public TimeEntryService(
            AppDbContext context,
            ILogger<TimeEntryService> logger,
            IUserService userService)
        {
            _context = context;
            _logger = logger;
            _userService = userService;
        }

        public async Task<(List<TimeEntry> TimeEntries, int TotalRecords, int TotalPages)> GetTimeEntriesAsync(
            string userId,
            int recordLimit,
            int page,
            int clientFilter,
            int[] projectFilter,
            DateTime? invoicedDateStart,
            DateTime? invoicedDateEnd,
            DateTime? paidDateStart,
            DateTime? paidDateEnd,
            DateTime? invoiceSentDateStart,
            DateTime? invoiceSentDateEnd,
            bool invoicedDateAny,
            bool paidDateAny,
            bool invoiceSentDateAny)
        {
            IQueryable<TimeEntry> query = _context.TimeEntries
                .Where(t => t.UserId == userId)
                .Include(t => t.Client)
                .Include(t => t.Project);

            // Apply default filter for InvoicedDate == null if no filters are set
            bool hasFilters = clientFilter != 0 ||
                              (projectFilter != null && projectFilter.Any() && !projectFilter.Contains(0)) ||
                              invoicedDateStart.HasValue || invoicedDateEnd.HasValue ||
                              paidDateStart.HasValue || paidDateEnd.HasValue ||
                              invoiceSentDateStart.HasValue || invoiceSentDateEnd.HasValue ||
                              invoicedDateAny || paidDateAny || invoiceSentDateAny;

            if (!hasFilters)
            {
                query = query.Where(t => t.InvoicedDate == null);
            }

            // Apply explicit filters
            if (clientFilter != 0)
            {
                query = query.Where(t => t.ClientID == clientFilter);
            }

            if (projectFilter != null && projectFilter.Any() && !projectFilter.Any())
            {
                query = query.Where(t => projectFilter.Contains(t.ProjectID));
            }

            if (invoicedDateAny)
            {
                query = query.Where(t => t.InvoicedDate != null);
            }
            else
            {
                if (invoicedDateStart.HasValue)
                {
                    query = query.Where(t => t.InvoicedDate >= invoicedDateStart.Value);
                }
                if (invoicedDateEnd.HasValue)
                {
                    query = query.Where(t => t.InvoicedDate <= invoicedDateEnd.Value);
                }
            }

            if (paidDateAny)
            {
                query = query.Where(t => t.PaidDate != null);
            }
            else
            {
                if (paidDateStart.HasValue)
                {
                    query = query.Where(t => t.PaidDate >= paidDateStart.Value);
                }
                if (paidDateEnd.HasValue)
                {
                    query = query.Where(t => t.PaidDate <= paidDateEnd.Value);
                }
            }

            if (invoiceSentDateAny)
            {
                query = query.Where(t => t.InvoiceSent != null);
            }
            else
            {
                if (invoiceSentDateStart.HasValue)
                {
                    query = query.Where(t => t.InvoiceSent >= invoiceSentDateStart.Value);
                }
                if (invoiceSentDateEnd.HasValue)
                {
                    query = query.Where(t => t.InvoiceSent <= invoiceSentDateEnd.Value);
                }
            }

            // Sort the query
            query = query
                .OrderBy(t => t.Client)
                .ThenByDescending(t => t.StartDateTime);

            int totalRecords = await query.CountAsync();
            int totalPages = totalRecords > 0 ? (int)Math.Ceiling((double)totalRecords / (recordLimit == -1 ? 200 : recordLimit)) : 1;
            page = page < 1 ? 1 : page > totalPages ? totalPages : page;

            List<TimeEntry> timeEntries;
            if (recordLimit == -1)
            {
                const int pageSize = 200;
                timeEntries = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            else
            {
                timeEntries = await query
                    .Skip((page - 1) * recordLimit)
                    .Take(recordLimit)
                    .ToListAsync();
            }

            return (timeEntries, totalRecords, totalPages);
        }

        public async Task<List<TimeEntry>> GetRunningTimersAsync(string userId)
        {
            return await _context.TimeEntries
                .Where(t => t.UserId == userId && t.EndDateTime == null)
                .Include(t => t.Client)
                .Include(t => t.Project)
                .ToListAsync();
        }

        public async Task<TimeEntry?> GetTimeEntryByIdAsync(int id, string userId)
        {
            return await _context.TimeEntries
                .Include(t => t.Client)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TimeEntryID == id && t.UserId == userId);
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateTimeEntryAsync(TimeEntry timeEntry)
        {
            if (timeEntry.HourlyRate == 0)
            {
                timeEntry.HourlyRate = await GetHourlyRateAsync(timeEntry.ProjectID, timeEntry.ClientID);
            }

            // Convert dates to UTC
            int offsetMinutes = await _userService.GetUserTimezoneOffsetAsync(timeEntry.UserId);
            timeEntry.StartDateTime = ToUtc(timeEntry.StartDateTime, offsetMinutes);
            if (timeEntry.EndDateTime.HasValue)
            {
                timeEntry.EndDateTime = ToUtc(timeEntry.EndDateTime.Value, offsetMinutes);
            }
            if (timeEntry.InvoicedDate.HasValue)
            {
                timeEntry.InvoicedDate = ToUtc(timeEntry.InvoicedDate.Value, offsetMinutes);
            }
            if (timeEntry.PaidDate.HasValue)
            {
                timeEntry.PaidDate = ToUtc(timeEntry.PaidDate.Value, offsetMinutes);
            }
            if (timeEntry.InvoiceSent.HasValue)
            {
                timeEntry.InvoiceSent = ToUtc(timeEntry.InvoiceSent.Value, offsetMinutes);
            }

            _context.Add(timeEntry);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> StartTimerAsync(TimeEntry timeEntry)
        {
            int offsetMinutes = 0;
            if (timeEntry.UserId != null) offsetMinutes = await _userService.GetUserTimezoneOffsetAsync(timeEntry.UserId);

            if (timeEntry.StartDateTime != default(DateTime))
            {
                // Convert supplied StartDateTime to UTC
                timeEntry.StartDateTime = ToUtc(timeEntry.StartDateTime, offsetMinutes);
            }
            else
            {
                // Use current UTC time, rounded to the nearest 15-minute interval
                var currentUtc = DateTime.UtcNow;
                var minutes = currentUtc.Minute;
                var quarterHours = (int)Math.Floor(minutes / 15.0) * 15;
                timeEntry.StartDateTime = new DateTime(currentUtc.Year, currentUtc.Month, currentUtc.Day, currentUtc.Hour, quarterHours, 0, DateTimeKind.Utc);
            }

            timeEntry.EndDateTime = null;
            timeEntry.HoursSpent = null;
            timeEntry.HourlyRate = await GetHourlyRateAsync(timeEntry.ProjectID, timeEntry.ClientID);

            _context.Add(timeEntry);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> StopTimerAsync(int timeEntryId, string userId)
        {
            var timeEntry = await _context.TimeEntries.FindAsync(timeEntryId);
            if (timeEntry == null || timeEntry.UserId != userId)
            {
                return (false, "Time entry not found or not owned by user.");
            }

            var currentUtc = DateTime.UtcNow;
            var minutes = currentUtc.Minute;
            var quarterHours = (int)Math.Ceiling(minutes / 15.0) * 15;
            if (quarterHours >= 60)
            {
                currentUtc = currentUtc.AddHours(1).AddMinutes(-minutes);
                quarterHours = 0;
            }
            timeEntry.EndDateTime = new DateTime(currentUtc.Year, currentUtc.Month, currentUtc.Day, currentUtc.Hour, quarterHours, 0, DateTimeKind.Utc);
            timeEntry.HoursSpent = Convert.ToDecimal((timeEntry.EndDateTime.Value - timeEntry.StartDateTime).TotalHours);

            _context.Update(timeEntry);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateTimeEntryAsync(TimeEntry timeEntry, string userId)
        {
            if (timeEntry.UserId != userId)
            {
                return (false, "User does not own this time entry.");
            }

            // Convert dates to UTC
            int offsetMinutes = await _userService.GetUserTimezoneOffsetAsync(timeEntry.UserId);
            timeEntry.StartDateTime = ToUtc(timeEntry.StartDateTime, offsetMinutes);
            if (timeEntry.EndDateTime.HasValue)
            {
                timeEntry.EndDateTime = ToUtc(timeEntry.EndDateTime.Value, offsetMinutes);
            }
            if (timeEntry.InvoicedDate.HasValue)
            {
                timeEntry.InvoicedDate = ToUtc(timeEntry.InvoicedDate.Value, offsetMinutes);
            }
            if (timeEntry.PaidDate.HasValue)
            {
                timeEntry.PaidDate = ToUtc(timeEntry.PaidDate.Value, offsetMinutes);
            }
            if (timeEntry.InvoiceSent.HasValue)
            {
                timeEntry.InvoiceSent = ToUtc(timeEntry.InvoiceSent.Value, offsetMinutes);
            }

            try
            {
                _context.Update(timeEntry);
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TimeEntryExistsAsync(timeEntry.TimeEntryID))
                {
                    return (false, "Time entry not found.");
                }
                throw;
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteTimeEntryAsync(int id, string userId)
        {
            var timeEntry = await _context.TimeEntries.FindAsync(id);
            if (timeEntry == null || timeEntry.UserId != userId)
            {
                return (false, "Time entry not found or not owned by user.");
            }

            _context.TimeEntries.Remove(timeEntry);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> TimeEntryExistsAsync(int id)
        {
            return await _context.TimeEntries.AnyAsync(e => e.TimeEntryID == id);
        }

        public async Task<(List<TimeEntry> TimeEntries, List<string> Errors)> ImportFromCsvAsync(
            Stream csvStream,
            string userId,
            int clientId,
            int projectId)
        {
            var timeEntries = new List<TimeEntry>();
            var errors = new List<string>();
            int offsetMinutes = await _userService.GetUserTimezoneOffsetAsync(userId);

            try
            {
                using var reader = new StreamReader(csvStream);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                await csv.ReadAsync();
                csv.ReadHeader();
                int rowNumber = 1;

                while (await csv.ReadAsync())
                {
                    rowNumber++;
                    var timeEntry = new TimeEntry
                    {
                        UserId = userId,
                        ClientID = clientId,
                        ProjectID = projectId
                    };

                    try
                    {
                        // Parse StartDateTime (required)
                        string startDateStr = csv.GetField("StartDateTime") ?? "";
                        if (string.IsNullOrWhiteSpace(startDateStr))
                        {
                            errors.Add($"Row {rowNumber}: Skipped - StartDateTime is required.");
                            continue;
                        }

                        if (!DateTime.TryParse(startDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
                        {
                            errors.Add($"Row {rowNumber}: Skipped - Invalid StartDateTime format: {startDateStr}.");
                            continue;
                        }

                        if (!startDateStr.Contains("AM", StringComparison.OrdinalIgnoreCase) &&
                            !startDateStr.Contains("PM", StringComparison.OrdinalIgnoreCase) &&
                            !startDateStr.Contains(":"))
                        {
                            startDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, 12, 0, 0);
                        }
                        timeEntry.StartDateTime = ToUtc(startDate, offsetMinutes);

                        // Parse EndDateTime (optional)
                        if (csv.TryGetField<DateTime>("EndDateTime", out var endDate))
                        {
                            timeEntry.EndDateTime = ToUtc(endDate, offsetMinutes);
                        }

                        // Parse HoursSpent (optional)
                        bool hasHoursSpent = csv.TryGetField<decimal>("HoursSpent", out var hoursSpent);
                        if (hasHoursSpent)
                        {
                            timeEntry.HoursSpent = hoursSpent;
                        }

                        // Calculate EndDateTime if missing but StartDateTime and HoursSpent are present
                        if (!timeEntry.EndDateTime.HasValue && hasHoursSpent && timeEntry.HoursSpent.HasValue)
                        {
                            timeEntry.EndDateTime = timeEntry.StartDateTime.AddHours((double)timeEntry.HoursSpent.Value);
                        }
                        else if (timeEntry.EndDateTime.HasValue && !hasHoursSpent)
                        {
                            timeEntry.HoursSpent = Convert.ToDecimal((timeEntry.EndDateTime.Value - timeEntry.StartDateTime).TotalHours);
                        }

                        // Parse HourlyRate (optional)
                        if (csv.TryGetField<decimal>("HourlyRate", out var csvHourlyRate) && csvHourlyRate > 0m)
                        {
                            timeEntry.HourlyRate = csvHourlyRate;
                        }
                        else
                        {
                            if (csvHourlyRate != 0m)
                            {
                                _logger.LogWarning("Row {RowNumber}: Invalid or zero HourlyRate {HourlyRate}, using default rate for user {UserId}", rowNumber, csvHourlyRate, userId);
                            }
                            timeEntry.HourlyRate = await GetHourlyRateAsync(timeEntry.ProjectID, timeEntry.ClientID);
                        }

                        // Parse Description (optional)
                        timeEntry.Description = csv.GetField("Description");

                        // Parse InvoicedDate (optional)
                        if (csv.TryGetField<DateTime>("InvoicedDate", out var invoicedDate))
                        {
                            timeEntry.InvoicedDate = ToUtc(invoicedDate, offsetMinutes);
                        }

                        // Parse PaidDate (optional)
                        if (csv.TryGetField<DateTime>("PaidDate", out var paidDate))
                        {
                            timeEntry.PaidDate = ToUtc(paidDate, offsetMinutes);
                        }

                        timeEntries.Add(timeEntry);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing CSV row {RowNumber} for user {UserId}", rowNumber, userId);
                        errors.Add($"Row {rowNumber}: Error processing record: {ex.Message}");
                    }
                }

                if (timeEntries.Any())
                {
                    _context.TimeEntries.AddRange(timeEntries);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading CSV file for user {UserId}", userId);
                errors.Add("Failed to process CSV file: " + ex.Message);
            }

            return (timeEntries, errors);
        }

        private DateTime ToUtc(DateTime localDateTime, int offsetMinutes)
        {
            // Assume localDateTime is in the user's local time zone and convert to UTC
            var offset = TimeSpan.FromMinutes(-offsetMinutes); // Negate because offset is user's local time relative to UTC
            return localDateTime.Add(offset);
        }

        public async Task<decimal> GetHourlyRateAsync(int? projectId, int? clientId)
        {
            // Try Project rate first
            if (projectId.HasValue)
            {
                var project = await _context.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProjectID == projectId.Value);
                if (project != null && project.Rate != 0m)
                {
                    return project.Rate;
                }
            }

            // Then Client rate
            if (clientId.HasValue)
            {
                var client = await _context.Clients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ClientID == clientId.Value);
                if (client != null)
                {
                    return client.DefaultRate;
                }
            }

            // Fallback to Settings default rate
            var settings = await _context.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync();
            return settings?.DefaultHourlyRate ?? 0m;
        }
    }
}