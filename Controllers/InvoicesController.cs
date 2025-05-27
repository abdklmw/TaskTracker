using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
                        RateSource = t.Project != null && t.Project.Rate.HasValue ? "Project" :
                                     t.Client != null ? "Client" :
                                     "Settings",
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvoiceCreateViewModel model)
        {
            if (ModelState.IsValid && model.ClientID > 0)
            {
                var invoice = new Invoice
                {
                    ClientID = model.ClientID,
                    InvoiceDate = DateTime.Today,
                    TotalAmount = model.SelectedTimeEntryIDs
                .Select(id => _context.TimeEntries.Find(id))
                .Where(t => t != null)
                .Sum(t => (t.HourlyRate ?? 0m) * (t.HoursSpent ?? 0m)) +
                model.SelectedExpenseIDs
                .Select(id => _context.Expenses.Find(id))
                .Where(e => e != null)
                .Sum(e => e.TotalAmount),
                    Status = model.Status
                };
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
                foreach (var timeEntryId in model.SelectedTimeEntryIDs)
                {
                    var timeEntry = await _context.TimeEntries.FindAsync(timeEntryId);
                    if (timeEntry != null)
                    {
                        timeEntry.PaidDate = DateTime.Today;
                        _context.InvoiceTimeEntries.Add(new InvoiceTimeEntry
                        {
                            InvoiceID = invoice.InvoiceID,
                            TimeEntryID = timeEntryId
                        });
                    }
                }
                foreach (var expenseId in model.SelectedExpenseIDs)
                {
                    var expense = await _context.Expenses.FindAsync(expenseId);
                    if (expense != null)
                    {
                        expense.PaidDate = DateTime.Today;
                        _context.InvoiceProducts.Add(new InvoiceExpense
                        {
                            InvoiceID = invoice.InvoiceID,
                            ProductID = expense.ExpenseID,
                            ProductInvoiceDate = DateOnly.FromDateTime(DateTime.Today),
                            Quantity = expense.Quantity
                        });
                    }
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Invoice created successfully.";
                return RedirectToAction(nameof(Index));
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
            ViewData["ClientID"] = new SelectList(_context.Clients, "ClientID", "Name", invoice.ClientID);
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
            ViewData["ClientID"] = new SelectList(_context.Clients, "ClientID", "Name", invoice.ClientID);
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
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        private bool InvoiceExists(int id)
        {
            return _context.Invoices.Any(e => e.InvoiceID == id);
        }
    }
}