using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskTracker.Data;
using TaskTracker.Models;

namespace TaskTracker.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(AppDbContext context, ILogger<InvoicesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var invoices = await _context.Invoices.Include(i => i.Client).ToListAsync();
            var createModel = new InvoiceCreateViewModel
            {
                Clients = _context.Clients
                    .Select(c => new SelectListItem
                    {
                        Value = c.ClientID.ToString(),
                        Text = c.Name
                    })
                    .ToList()
            };
            createModel.Clients.Insert(0, new SelectListItem { Value = "0", Text = "Select Client" });
            ViewBag.CreateModel = createModel;
            return View(invoices);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnpaidItems(int clientId)
        {
            try
            {
                // Fetch the default hourly rate from Settings
                var settings = await _context.Settings.FirstOrDefaultAsync();
                var defaultHourlyRate = settings?.DefaultHourlyRate ?? 0m;
                if (settings == null)
                {
                    _logger.LogWarning("Settings not found. Using default hourly rate of 0.");
                }

                var timeEntries = await _context.TimeEntries
                    .Where(t => t.ClientID == clientId && t.PaidDate == null)
                    .Include(t => t.Project)
                    .Include(t => t.Client)
                    .Select(t => new TimeEntryViewModel
                    {
                        TimeEntryID = t.TimeEntryID,
                        HourlyRate = t.Project != null && t.Project.Rate.HasValue ? t.Project.Rate.Value :
                                     t.Client != null ? t.Client.DefaultRate :
                                     defaultHourlyRate,
                        HoursSpent = t.HoursSpent ?? 0m,
                        Description = t.Description,
                        StartDateTime = t.StartDateTime
                    })
                    .ToListAsync();

                // Calculate TotalAmount for each time entry
                foreach (var entry in timeEntries)
                {
                    entry.TotalAmount = entry.HourlyRate * entry.HoursSpent;
                }

                var expenses = await _context.Expenses
                    .Where(e => e.ClientID == clientId && e.PaidDate == null)
                    .Select(e => new ExpenseViewModel
                    {
                        ExpenseID = e.ExpenseID,
                        Description = e.Description,
                        UnitAmount = e.UnitAmount,
                        Quantity = e.Quantity,
                        TotalAmount = e.TotalAmount
                    })
                    .ToListAsync();

                return Json(new { TimeEntries = timeEntries, Expenses = expenses });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unpaid items for client ID {ClientId}", clientId);
                return StatusCode(500, new { error = "An error occurred while fetching unpaid items." });
            }
        }

        // ... (rest of the InvoicesController code remains unchanged)
    }
}