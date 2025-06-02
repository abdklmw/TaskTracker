using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models;
using TaskTracker.Services;

namespace TaskTracker.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<InvoicesController> _logger;
        private readonly IInvoicePdfService _pdfService;
        private readonly RateCalculationService _rateService;
        private readonly DropdownService _dropdownService;
        private readonly TimeEntryImportService _importService;

        public InvoicesController(
            AppDbContext context,
            ILogger<InvoicesController> logger,
            IInvoicePdfService pdfService,
            RateCalculationService rateService,
            DropdownService dropdownService,
            TimeEntryImportService importService)
        {
            _context = context;
            _logger = logger;
            _pdfService = pdfService;
            _rateService = rateService;
            _dropdownService = dropdownService;
            _importService = importService;
        }

        public async Task<IActionResult> Index()
        {
            var invoices = await _context.Invoices.Include(i => i.Client).ToListAsync();
            var createModel = new InvoiceCreateViewModel
            {
                Clients = await _dropdownService.GetClientDropdownAsync(0)
            };
            ViewBag.CreateModel = createModel;
            var clientList = await _dropdownService.GetClientDropdownAsync(0);
            ViewData["ClientID"] = new SelectList(clientList, "Value", "Text");
            return View(invoices);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnpaidItems(int clientId)
        {
            try
            {
                var timeEntries = await _context.TimeEntries
                    .Where(t => t.ClientID == clientId && t.InvoicedDate == null)
                    .Include(t => t.Project)
                    .Include(t => t.Client)
                    .Select(t => new TimeEntryViewModel
                    {
                        TimeEntryID = t.TimeEntryID,
                        HourlyRate = t.HourlyRate ?? 0m, // Use TimeEntry.HourlyRate if set
                        RateSource = "", // Will be set below
                        HoursSpent = t.HoursSpent ?? 0m,
                        Description = t.Description ?? "",
                        StartDateTime = t.StartDateTime,
                        EndDateTime = t.EndDateTime,
                        ProjectID = t.ProjectID,
                        ClientID = t.ClientID
                    })
                    .ToListAsync();

                foreach (var entry in timeEntries)
                {
                    // If TimeEntry.HourlyRate is set and non-zero, use it and mark source as "TimeEntry"
                    if (entry.HourlyRate > 0m)
                    {
                        entry.RateSource = "TimeEntry";
                    }
                    else
                    {
                        // Otherwise, use RateCalculationService
                        entry.HourlyRate = await _rateService.GetHourlyRateAsync(entry.ProjectID, entry.ClientID);

                        // Determine RateSource
                        if (entry.ProjectID.HasValue)
                        {
                            var project = await _context.Projects
                                .AsNoTracking()
                                .FirstOrDefaultAsync(p => p.ProjectID == entry.ProjectID.Value);
                            if (project != null && project.Rate != 0m && project.Rate == entry.HourlyRate)
                            {
                                entry.RateSource = "Project";
                                entry.TotalAmount = entry.HourlyRate * entry.HoursSpent;
                                continue;
                            }
                        }

                        if (entry.ClientID.HasValue)
                        {
                            var client = await _context.Clients
                                .AsNoTracking()
                                .FirstOrDefaultAsync(c => c.ClientID == entry.ClientID.Value);
                            if (client != null && client.DefaultRate != 0m && client.DefaultRate == entry.HourlyRate)
                            {
                                entry.RateSource = "Client";
                                entry.TotalAmount = entry.HourlyRate * entry.HoursSpent;
                                continue;
                            }
                        }

                        entry.RateSource = "Settings";
                    }
                    entry.TotalAmount = entry.HourlyRate * entry.HoursSpent;
                }

                var expenses = await _context.Expenses
                    .Where(e => e.ClientID == clientId && e.InvoicedDate == null)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvoiceCreateViewModel model)
        {
            if (ModelState.IsValid && model.ClientID > 0)
            {
                try
                {
                    var timeEntries = await _context.TimeEntries
                        .Where(t => model.SelectedTimeEntryIDs.Contains(t.TimeEntryID))
                        .Include(t => t.Project)
                        .Include(t => t.Client)
                        .ToListAsync();

                    var expenses = await _context.Expenses
                        .Where(e => model.SelectedExpenseIDs.Contains(e.ExpenseID))
                        .ToListAsync();

                    decimal totalAmount = 0m;
                    foreach (var timeEntry in timeEntries)
                    {
                        // Calculate and update HourlyRate using RateCalculationService
                        var hourlyRate = await _rateService.GetHourlyRateAsync(timeEntry.ProjectID, timeEntry.ClientID);
                        timeEntry.HourlyRate = hourlyRate;
                        totalAmount += (hourlyRate * (timeEntry.HoursSpent ?? 0m));
                    }

                    foreach (var expense in expenses)
                    {
                        if (expense.TotalAmount != expense.UnitAmount * expense.Quantity)
                        {
                            _logger.LogWarning("Expense ID {ExpenseId} has inconsistent TotalAmount. Expected {Expected}, found {Actual}",
                                expense.ExpenseID, expense.UnitAmount * expense.Quantity, expense.TotalAmount);
                            expense.TotalAmount = expense.UnitAmount * expense.Quantity;
                        }
                        totalAmount += expense.TotalAmount;
                    }

                    var invoice = new Invoice
                    {
                        ClientID = model.ClientID,
                        InvoiceDate = DateTime.Today,
                        TotalAmount = totalAmount,
                        Status = model.Status
                    };
                    _context.Invoices.Add(invoice);
                    await _context.SaveChangesAsync();

                    foreach (var timeEntry in timeEntries)
                    {
                        timeEntry.InvoicedDate = DateTime.Today;
                        _context.InvoiceTimeEntries.Add(new InvoiceTimeEntry
                        {
                            InvoiceID = invoice.InvoiceID,
                            TimeEntryID = timeEntry.TimeEntryID,
                            TimeEntry = timeEntry
                        });
                    }

                    foreach (var expense in expenses)
                    {
                        expense.InvoicedDate = DateTime.Today;
                        _context.InvoiceExpenses.Add(new InvoiceExpense
                        {
                            InvoiceID = invoice.InvoiceID,
                            ProductID = expense.ExpenseID,
                            ProductInvoiceDate = DateOnly.FromDateTime(DateTime.Today),
                            Quantity = expense.Quantity
                        });
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Invoice created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating invoice for client ID {ClientId}", model.ClientID);
                    TempData["ErrorMessage"] = "An error occurred while creating the invoice.";
                    return RedirectToAction(nameof(Index));
                }
            }
            TempData["ErrorMessage"] = "Error creating invoice: " + string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }
            var clientList = await _dropdownService.GetClientDropdownAsync(invoice.ClientID);
            ViewData["ClientID"] = new SelectList(clientList, "Value", "Text", invoice.ClientID);
            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("InvoiceID,ClientID,InvoiceDate,DueDate,TotalAmount,Status")] Invoice invoice)
        {
            if (id != invoice.InvoiceID)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(invoice);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InvoiceExists(invoice.InvoiceID))
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
            var clientList = await _dropdownService.GetClientDropdownAsync(invoice.ClientID);
            ViewData["ClientID"] = new SelectList(clientList, "Value", "Text", invoice.ClientID);
            return View(invoice);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .FirstOrDefaultAsync(m => m.InvoiceID == id);
            if (invoice == null)
            {
                return NotFound();
            }
            return View(invoice);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.InvoiceTimeEntries)
                    .Include(i => i.InvoiceExpenses)
                    .FirstOrDefaultAsync(i => i.InvoiceID == id);
                if (invoice == null)
                {
                    return NotFound();
                }
                var timeEntryIds = invoice.InvoiceTimeEntries.Select(ite => ite.TimeEntryID).ToList();
                var timeEntries = await _context.TimeEntries
                    .Where(t => timeEntryIds.Contains(t.TimeEntryID))
                    .ToListAsync();
                foreach (var timeEntry in timeEntries)
                {
                    timeEntry.InvoicedDate = null;
                }
                var expenseIds = invoice.InvoiceExpenses.Select(ie => ie.ProductID).ToList();
                var expenses = await _context.Expenses
                    .Where(e => expenseIds.Contains(e.ExpenseID))
                    .ToListAsync();
                foreach (var expense in expenses)
                {
                    expense.InvoicedDate = null;
                }
                _context.InvoiceTimeEntries.RemoveRange(invoice.InvoiceTimeEntries);
                _context.InvoiceExpenses.RemoveRange(invoice.InvoiceExpenses);
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Invoice deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice ID {InvoiceId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the invoice.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Send")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.InvoiceID == id);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice ID {InvoiceId} not found for sending.", id);
                    TempData["ErrorMessage"] = "Invoice not found.";
                    return RedirectToAction(nameof(Index));
                }
                if (invoice.Status == InvoiceStatus.Sent || invoice.Status == InvoiceStatus.Paid)
                {
                    _logger.LogWarning("Invoice ID {InvoiceId} already sent or paid.", id);
                    TempData["ErrorMessage"] = "Invoice has already been sent or paid.";
                    return RedirectToAction(nameof(Index));
                }
                var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(id);
                invoice.InvoiceSentDate = DateTime.Today;
                invoice.Status = InvoiceStatus.Sent;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Invoice PDF generated and marked as sent.";
                return File(pdfBytes, "application/pdf", $"Invoice_{invoice.InvoiceID}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invoice ID {InvoiceId}", id);
                TempData["ErrorMessage"] = "An error occurred while generating the invoice PDF.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Paid")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Paid(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.InvoiceTimeEntries)
                    .Include(i => i.InvoiceExpenses)
                    .FirstOrDefaultAsync(i => i.InvoiceID == id);
                if (invoice == null)
                {
                    return NotFound();
                }
                invoice.PaidDate = DateTime.Today;
                invoice.Status = InvoiceStatus.Paid;
                var timeEntryIds = invoice.InvoiceTimeEntries.Select(ite => ite.TimeEntryID).ToList();
                var timeEntries = await _context.TimeEntries
                    .Where(t => timeEntryIds.Contains(t.TimeEntryID))
                    .ToListAsync();
                foreach (var timeEntry in timeEntries)
                {
                    timeEntry.PaidDate = DateTime.Today;
                }
                var expenseIds = invoice.InvoiceExpenses.Select(ie => ie.ProductID).ToList();
                var expenses = await _context.Expenses
                    .Where(e => expenseIds.Contains(e.ExpenseID))
                    .ToListAsync();
                foreach (var expense in expenses)
                {
                    expense.PaidDate = DateTime.Today;
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Invoice marked as paid successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking invoice ID {InvoiceId} as paid", id);
                TempData["ErrorMessage"] = "An error occurred while marking the invoice as paid.";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool InvoiceExists(int id)
        {
            return _context.Invoices.Any(e => e.InvoiceID == id);
        }
    }
}