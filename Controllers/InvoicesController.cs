using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models;
using TaskTracker.Models.Invoice;
using TaskTracker.Services;

namespace TaskTracker.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<InvoicesController> _logger;
        private readonly IInvoicePdfService _pdfService;
        private readonly RateCalculationService _rateService;
        private readonly ProjectService _projectService;
        private readonly TimeEntryImportService _importService;
		private readonly ClientService _clientService;
		private readonly IEmailService _emailService;

        public InvoicesController(
            AppDbContext context,
            ILogger<InvoicesController> logger,
            IInvoicePdfService pdfService,
            RateCalculationService rateService,
            ProjectService projectService,
            TimeEntryImportService importService,
			ClientService clientService,
            IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _pdfService = pdfService;
            _rateService = rateService;
            _projectService = projectService;
            _importService = importService;
			_clientService = clientService;
			_emailService = emailService;
        }

        public async Task<IActionResult> Index(
            int page = 1,
            int recordLimit = 10,
            int clientFilter = 0,
            InvoiceStatus? statusFilter = null,
            DateTime? paidDateStart = null,
            DateTime? paidDateEnd = null,
            DateTime? invoiceDateStart = null,
            DateTime? invoiceDateEnd = null,
            DateTime? invoiceSentDateStart = null,
            DateTime? invoiceSentDateEnd = null,
            decimal? totalAmountMin = null,
            decimal? totalAmountMax = null)
        {
            _logger.LogInformation("Index called with page={Page}, recordLimit={RecordLimit}, clientFilter={ClientFilter}, statusFilter={StatusFilter}",
                page, recordLimit, clientFilter, statusFilter);

            var query = _context.Invoices
                .Include(i => i.Client)
                .AsQueryable();

            if (clientFilter != 0)
            {
                query = query.Where(i => i.ClientID == clientFilter);
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(i => i.Status == statusFilter.Value);
            }

            if (paidDateStart.HasValue)
            {
                query = query.Where(i => i.PaidDate >= paidDateStart.Value);
            }

            if (paidDateEnd.HasValue)
            {
                query = query.Where(i => i.PaidDate <= paidDateEnd.Value);
            }

            if (invoiceDateStart.HasValue)
            {
                query = query.Where(i => i.InvoiceDate >= invoiceDateStart.Value);
            }

            if (invoiceDateEnd.HasValue)
            {
                query = query.Where(i => i.InvoiceDate <= invoiceDateEnd.Value);
            }

            if (invoiceSentDateStart.HasValue)
            {
                query = query.Where(i => i.InvoiceSentDate >= invoiceSentDateStart.Value);
            }

            if (invoiceSentDateEnd.HasValue)
            {
                query = query.Where(i => i.InvoiceSentDate <= invoiceSentDateEnd.Value);
            }

            if (totalAmountMin.HasValue)
            {
                query = query.Where(i => i.TotalAmount >= totalAmountMin.Value);
            }

            if (totalAmountMax.HasValue)
            {
                query = query.Where(i => i.TotalAmount <= totalAmountMax.Value);
            }

            int totalRecords = await query.CountAsync();
            int totalPages = totalRecords > 0 ? (int)Math.Ceiling((double)totalRecords / recordLimit) : 1;
            page = page < 1 ? 1 : page > totalPages ? totalPages : page;

            var invoices = totalRecords > 0
                ? await query
                    .OrderByDescending(i => i.InvoiceDate)
                    .Skip((page - 1) * recordLimit)
                    .Take(recordLimit)
                    .ToListAsync()
                : new List<Invoice>();

            var statusOptions = Enum.GetValues(typeof(InvoiceStatus))
                .Cast<InvoiceStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.ToString()
                })
                .Prepend(new SelectListItem { Value = "", Text = "All Statuses" });

            var viewModel = new InvoiceIndexViewModel
            {
                Invoices = invoices,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                RecordLimit = recordLimit,
                SelectedClientID = clientFilter,
                SelectedStatus = statusFilter,
                PaidDateStart = paidDateStart,
                PaidDateEnd = paidDateEnd,
                InvoiceDateStart = invoiceDateStart,
                InvoiceDateEnd = invoiceDateEnd,
                InvoiceSentDateStart = invoiceSentDateStart,
                InvoiceSentDateEnd = invoiceSentDateEnd,
                TotalAmountMin = totalAmountMin,
                TotalAmountMax = totalAmountMax,
                RouteValues = new Dictionary<string, string>
                {
                    { "recordLimit", recordLimit.ToString() },
                    { "clientFilter", clientFilter.ToString() },
                    { "statusFilter", statusFilter?.ToString() ?? "" },
                    { "paidDateStart", paidDateStart?.ToString("yyyy-MM-dd") ?? "" },
                    { "paidDateEnd", paidDateEnd?.ToString("yyyy-MM-dd") ?? "" },
                    { "invoiceDateStart", invoiceDateStart?.ToString("yyyy-MM-dd") ?? "" },
                    { "invoiceDateEnd", invoiceDateEnd?.ToString("yyyy-MM-dd") ?? "" },
                    { "invoiceSentDateStart", invoiceSentDateStart?.ToString("yyyy-MM-dd") ?? "" },
                    { "invoiceSentDateEnd", invoiceSentDateEnd?.ToString("yyyy-MM-dd") ?? "" },
                    { "totalAmountMin", totalAmountMin?.ToString() ?? "" },
                    { "totalAmountMax", totalAmountMax?.ToString() ?? "" }
                },
                ClientFilterOptions = await _clientService.GetClientDropdownAsync(clientFilter),
                StatusFilterOptions = statusOptions
            };

            var createModel = new InvoiceCreateViewModel
            {
                Clients = await _clientService.GetClientDropdownAsync(0)
            };
            ViewBag.CreateModel = createModel;
            ViewData["ClientID"] = new SelectList(await _clientService.GetClientDropdownAsync(0), "Value", "Text");

            return View(viewModel);
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
                        HourlyRate = t.HourlyRate ?? 0m,
                        RateSource = "",
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
                    if (entry.HourlyRate > 0m)
                    {
                        entry.RateSource = "TimeEntry";
                    }
                    else
                    {
                        entry.HourlyRate = await _rateService.GetHourlyRateAsync(entry.ProjectID, entry.ClientID);

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

            var clientList = await _clientService.GetClientDropdownAsync(invoice.ClientID);
            ViewData["ClientID"] = new SelectList(clientList, "Value", "Text", invoice.ClientID);
            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("InvoiceID,ClientID,InvoiceDate,InvoiceSentDate,PaidDate,TotalAmount,Status")] Invoice invoice)
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

            var clientList = await _clientService.GetClientDropdownAsync(invoice.ClientID);
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
                return NotFound();

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
                    return NotFound();

                var timeEntryIds = invoice.InvoiceTimeEntries.Select(ite => ite.TimeEntryID).ToList();
                var timeEntries = await _context.TimeEntries
                    .Where(t => timeEntryIds.Contains(t.TimeEntryID))
                    .ToListAsync();

                foreach (var timeEntry in timeEntries)
                {
                    timeEntry.InvoicedDate = null;
                    timeEntry.InvoiceSent = null;
                }

                var expenseIds = invoice.InvoiceExpenses.Select(ie => ie.ProductID).ToList();
                var expenses = await _context.Expenses
                    .Where(e => expenseIds.Contains(e.ExpenseID))
                    .ToListAsync();

                foreach (var expense in expenses)
                {
                    expense.InvoicedDate = null;
                    expense.InvoiceSent = null;
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
                    .Include(i => i.Client)
                    .Include(i => i.InvoiceTimeEntries)
                    .Include(i => i.InvoiceExpenses)
                    .FirstOrDefaultAsync(i => i.InvoiceID == id);

                if (invoice == null)
                {
                    _logger.LogWarning("Invoice ID {InvoiceId} not found for sending.", id);
                    TempData["ErrorMessage"] = "Invoice not found.";
                    return RedirectToAction(nameof(Index));
                }

                var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(id);
                var clientEmail = invoice.Client?.Email;
                var settings = await _context.Settings.FirstOrDefaultAsync();
                bool canSendEmail = !string.IsNullOrWhiteSpace(clientEmail) &&
                                    !string.IsNullOrWhiteSpace(settings?.SmtpServer) &&
                                    settings?.SmtpPort.HasValue == true &&
                                    !string.IsNullOrWhiteSpace(settings?.SmtpSenderEmail);

                if (canSendEmail)
                {
                    try
                    {
                        var subject = $"Invoice {invoice.InvoiceDate:yyyyMMdd}.{invoice.InvoiceID} from {settings?.CompanyName}";
                        var recipientName = string.IsNullOrWhiteSpace(invoice.Client?.AccountsReceivableName)
                            ? invoice.Client?.Name
                            : invoice.Client?.AccountsReceivableName;
                        var body = $"<p>Dear {recipientName},</p>" +
                                   $"<p>Please find your invoice attached. The total amount due is ${invoice.TotalAmount:N2}.</p>" +
                                   "<p>Please let me know if you have any questions. Thank you for your business!</p>" +
                                   $"<p>Best regards,<br>{settings?.CompanyName}</p>";

                        await _emailService.SendEmailAsync(
                            clientEmail,
                            subject,
                            body,
                            pdfBytes,
                            $"Invoice_{invoice.InvoiceDate:yyyyMMdd}.{invoice.InvoiceID}.pdf");

                        invoice.InvoiceSentDate = DateTime.Today;
                        invoice.Status = invoice.Status == InvoiceStatus.Draft ? InvoiceStatus.Sent : invoice.Status;

                        var timeEntryIds = invoice.InvoiceTimeEntries.Select(ite => ite.TimeEntryID).ToList();
                        var timeEntries = await _context.TimeEntries
                            .Where(t => timeEntryIds.Contains(t.TimeEntryID))
                            .ToListAsync();

                        foreach (var timeEntry in timeEntries)
                        {
                            timeEntry.InvoiceSent = DateTime.Today;
                        }

                        var expenseIds = invoice.InvoiceExpenses.Select(ie => ie.ProductID).ToList();
                        var expenses = await _context.Expenses
                            .Where(e => expenseIds.Contains(e.ExpenseID))
                            .ToListAsync();

                        foreach (var expense in expenses)
                        {
                            expense.InvoiceSent = DateTime.Today;
                        }

                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Invoice sent successfully via email.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send email for invoice ID {InvoiceId}. Falling back to download.", id);
                        TempData["WarningMessage"] = "Failed to send email. The invoice PDF will be downloaded instead.";
                    }
                }
                else
                {
                    var warningMessage = string.IsNullOrWhiteSpace(clientEmail)
                        ? "Client email address is missing. The invoice PDF will be downloaded instead."
                        : "SMTP settings are missing or incomplete. The invoice PDF will be downloaded instead.";
                    _logger.LogWarning(warningMessage + " Invoice ID: {InvoiceId}", id);
                    TempData["WarningMessage"] = warningMessage;
                }

                // Fallback to download
                invoice.InvoiceSentDate = DateTime.Today;
                invoice.Status = InvoiceStatus.Sent;

                var timeEntryIdsFallback = invoice.InvoiceTimeEntries.Select(ite => ite.TimeEntryID).ToList();
                var timeEntriesFallback = await _context.TimeEntries
                    .Where(t => timeEntryIdsFallback.Contains(t.TimeEntryID))
                    .ToListAsync();

                foreach (var timeEntry in timeEntriesFallback)
                {
                    timeEntry.InvoiceSent = DateTime.Today;
                }

                var expenseIdsFallback = invoice.InvoiceExpenses.Select(ie => ie.ProductID).ToList();
                var expensesFallback = await _context.Expenses
                    .Where(e => expenseIdsFallback.Contains(e.ExpenseID))
                    .ToListAsync();

                foreach (var expense in expensesFallback)
                {
                    expense.InvoiceSent = DateTime.Today;
                }

                await _context.SaveChangesAsync();
                return File(pdfBytes, "application/pdf", $"Invoice_{invoice.InvoiceDate:yyyyMMdd}.{invoice.InvoiceID}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invoice ID {InvoiceId}", id);
                TempData["ErrorMessage"] = "An error occurred while processing the invoice.";
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
                    return NotFound();

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