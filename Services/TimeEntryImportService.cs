using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using TaskTracker.Data;
using TaskTracker.Models;
using Microsoft.Extensions.Logging;

namespace TaskTracker.Services
{
    public class TimeEntryImportService
    {
        private readonly AppDbContext _context;
        private readonly RateCalculationService _rateService;
        private readonly ILogger<TimeEntryImportService> _logger;

        public TimeEntryImportService(
            AppDbContext context,
            RateCalculationService rateService,
            ILogger<TimeEntryImportService> logger)
        {
            _context = context;
            _rateService = rateService;
            _logger = logger;
        }

        public async Task<(List<TimeEntry> TimeEntries, List<string> Errors)> ImportFromCsvAsync(
            Stream csvStream,
            string userId,
            int clientId,
            int projectId)
        {
            var timeEntries = new List<TimeEntry>();
            var errors = new List<string>();

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
                        // Parse StartDateTime (default to noon if no time)
                        string startDateStr = csv.GetField("StartDateTime") ?? "";
                        if (string.IsNullOrWhiteSpace(startDateStr))
                        {
                            errors.Add($"Row {rowNumber}: StartDateTime is required.");
                            continue;
                        }

                        if (DateTime.TryParse(startDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
                        {
                            // Check if time was provided (e.g., contains "AM", "PM", or ":")
                            if (!startDateStr.Contains("AM", StringComparison.OrdinalIgnoreCase) &&
                                !startDateStr.Contains("PM", StringComparison.OrdinalIgnoreCase) &&
                                !startDateStr.Contains(":"))
                            {
                                startDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, 12, 0, 0, DateTimeKind.Utc);
                            }
                            timeEntry.StartDateTime = startDate.ToUniversalTime();
                        }
                        else
                        {
                            errors.Add($"Row {rowNumber}: Invalid StartDateTime format: {startDateStr}");
                            continue;
                        }

                        // Parse EndDateTime (optional)
                        if (csv.TryGetField<DateTime>("EndDateTime", out var endDate))
                        {
                            timeEntry.EndDateTime = endDate.ToUniversalTime();
                        }

                        // Parse HoursSpent (optional)
                        if (csv.TryGetField<decimal>("HoursSpent", out var hoursSpent))
                        {
                            timeEntry.HoursSpent = hoursSpent;
                        }
                        else if (timeEntry.EndDateTime.HasValue)
                        {
                            timeEntry.HoursSpent = Convert.ToDecimal((timeEntry.EndDateTime.Value - timeEntry.StartDateTime).TotalHours);
                        }

                        // Parse HourlyRate (use CSV value if valid and non-zero, otherwise use RateCalculationService)
                        if (csv.TryGetField<decimal>("HourlyRate", out var csvHourlyRate) && csvHourlyRate > 0m)
                        {
                            timeEntry.HourlyRate = csvHourlyRate;
                        }
                        else
                        {
                            if (csvHourlyRate != 0m)
                            {
                                _logger.LogWarning("Row {RowNumber}: Invalid or zero HourlyRate {HourlyRate}, using RateCalculationService for user {UserId}", rowNumber, csvHourlyRate, userId);
                            }
                            timeEntry.HourlyRate = await _rateService.GetHourlyRateAsync(timeEntry.ProjectID, timeEntry.ClientID);
                        }

                        // Parse Description (optional)
                        timeEntry.Description = csv.GetField("Description");

                        // Parse InvoicedDate (optional)
                        if (csv.TryGetField<DateTime>("InvoicedDate", out var invoicedDate))
                        {
                            timeEntry.InvoicedDate = invoicedDate.Date;
                        }

                        // Parse PaidDate (optional)
                        if (csv.TryGetField<DateTime>("PaidDate", out var paidDate))
                        {
                            timeEntry.PaidDate = paidDate.Date;
                        }

                        timeEntries.Add(timeEntry);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing CSV row {RowNumber} for user {UserId}", rowNumber, userId);
                        errors.Add($"Row {rowNumber}: Error processing record: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading CSV file for user {UserId}", userId);
                errors.Add("Failed to process CSV file: " + ex.Message);
            }

            return (timeEntries, errors);
        }
    }
}