using iText.Html2pdf;
using iText.Kernel.Pdf;
using iText.Layout;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models.Invoice;
using TaskTracker.Models.TimeEntries;

namespace TaskTracker.Services
{
    public interface IInvoiceService
    {
        Task<(List<Invoice> Invoices, int TotalRecords, int TotalPages)> GetInvoicesAsync(
            int page, int recordLimit, int clientFilter, InvoiceStatus? statusFilter,
            DateTime? paidDateStart, DateTime? paidDateEnd,
            DateTime? invoiceDateStart, DateTime? invoiceDateEnd,
            DateTime? invoiceSentDateStart, DateTime? invoiceSentDateEnd,
            decimal? totalAmountMin, decimal? totalAmountMax);
        Task<(List<TimeEntryViewModel> TimeEntries, List<ExpenseViewModel> Expenses)> GetUnpaidItemsAsync(int clientId);
        Task<(bool Success, string? ErrorMessage)> CreateInvoiceAsync(InvoiceCreateViewModel model);
        Task<Invoice?> GetInvoiceByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage)> UpdateInvoiceAsync(Invoice invoice);
        Task<(bool Success, string? ErrorMessage)> DeleteInvoiceAsync(int id);
        Task<(bool Success, string? ErrorMessage, byte[]? PdfBytes)> SendInvoiceAsync(int id, string userId);
        Task<(bool Success, string? ErrorMessage)> MarkInvoiceAsPaidAsync(int id);
        Task<byte[]> GenerateInvoicePdfAsync(int invoiceId, string userId);
        Task<Models.Settings?> GetEmailSettingsAsync();
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<InvoiceService> _logger;
        private readonly TimeEntryService _timeEntryService;
        private readonly ClientService _clientService;

        public InvoiceService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<InvoiceService> logger,
            TimeEntryService timeEntryService,
            ClientService clientService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _timeEntryService = timeEntryService;
            _clientService = clientService;
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateInvoiceAsync(InvoiceCreateViewModel model)
        {
            if (model.ClientID <= 0 || (model.SelectedTimeEntryIDs == null || !model.SelectedTimeEntryIDs.Any()) && (model.SelectedExpenseIDs == null || !model.SelectedExpenseIDs.Any()))
            {
                return (false, "Invalid client or no items selected.");
            }

            try
            {
                var timeEntries = await _context.TimeEntries
                    .Where(t => model.SelectedTimeEntryIDs.Contains(t.TimeEntryID))
                    .Include(t => t.Project)
                    .Include(t => t.Client)
                    .ToListAsync();

                var expenses = await _context.Expenses
                    .Where(e => model.SelectedExpenseIDs.Contains(e.ExpenseID))
                    .Include(e => e.Product)
                    .ToListAsync();

                decimal totalAmount = 0m;

                // Calculate total amount for time entries
                foreach (var timeEntry in timeEntries)
                {
                    var hourlyRate = await _timeEntryService.GetHourlyRateAsync(timeEntry.ProjectID, timeEntry.ClientID);
                    timeEntry.HourlyRate = hourlyRate;
                    totalAmount += (hourlyRate * (timeEntry.HoursSpent ?? 0m));
                }

                // Validate and calculate total amount for expenses
                foreach (var expense in expenses)
                {
                    if (expense.Product == null || !await _context.Products.AnyAsync(p => p.ProductID == expense.ProductID))
                    {
                        _logger.LogError("Expense ID {ExpenseId} has invalid or missing ProductID {ProductId}.", expense.ExpenseID, expense.ProductID);
                        return (false, $"Expense ID {expense.ExpenseID} is linked to an invalid or missing product.");
                    }

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

                // Add time entries to invoice
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

                // Add expenses to invoice
                foreach (var expense in expenses)
                {
                    expense.InvoicedDate = DateTime.Today;
                    _context.InvoiceExpenses.Add(new InvoiceExpense
                    {
                        InvoiceID = invoice.InvoiceID,
                        ProductID = expense.ProductID,
                        ProductInvoiceDate = DateOnly.FromDateTime(DateTime.Today),
                        Quantity = expense.Quantity,
                        UnitAmount = expense.UnitAmount,
                        Description = expense.Description
                    });
                }

                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice for client ID {ClientId}", model.ClientID);
                return (false, "An error occurred while creating the invoice.");
            }
        }

        public async Task<(List<TimeEntryViewModel> TimeEntries, List<ExpenseViewModel> Expenses)> GetUnpaidItemsAsync(int clientId)
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
                        entry.HourlyRate = await _timeEntryService.GetHourlyRateAsync(entry.ProjectID, entry.ClientID);
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

                return (timeEntries, expenses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unpaid items for client ID {ClientId}", clientId);
                throw;
            }
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            return await _context.Invoices
                .Include(i => i.Client)
                .FirstOrDefaultAsync(i => i.InvoiceID == id);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateInvoiceAsync(Invoice invoice)
        {
            try
            {
                // Fetch the invoice with its associated time entries and expenses
                var existingInvoice = await _context.Invoices
                    .Include(i => i.InvoiceTimeEntries)
                    .ThenInclude(ite => ite.TimeEntry)
                    .Include(i => i.InvoiceExpenses)
                    .FirstOrDefaultAsync(i => i.InvoiceID == invoice.InvoiceID);

                if (existingInvoice == null)
                {
                    return (false, "Invoice not found.");
                }

                // Update invoice properties
                existingInvoice.ClientID = invoice.ClientID;
                existingInvoice.InvoiceDate = invoice.InvoiceDate;
                existingInvoice.InvoiceSentDate = invoice.InvoiceSentDate;
                existingInvoice.PaidDate = invoice.PaidDate;
                existingInvoice.TotalAmount = invoice.TotalAmount;
                existingInvoice.Status = invoice.Status;

                // Update associated time entries' date fields
                foreach (var invoiceTimeEntry in existingInvoice.InvoiceTimeEntries)
                {
                    var timeEntry = invoiceTimeEntry.TimeEntry;
                    timeEntry.InvoicedDate = invoice.InvoiceDate;
                    timeEntry.InvoiceSent = invoice.InvoiceSentDate;
                    timeEntry.PaidDate = invoice.PaidDate;
                }

                _context.Update(existingInvoice);
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await InvoiceExistsAsync(invoice.InvoiceID))
                {
                    return (false, "Invoice not found.");
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice ID {InvoiceId}", invoice.InvoiceID);
                return (false, "An error occurred while updating the invoice.");
            }
        }

        public async Task<(List<Invoice> Invoices, int TotalRecords, int TotalPages)> GetInvoicesAsync(
            int page, int recordLimit, int clientFilter, InvoiceStatus? statusFilter,
            DateTime? paidDateStart, DateTime? paidDateEnd,
            DateTime? invoiceDateStart, DateTime? invoiceDateEnd,
            DateTime? invoiceSentDateStart, DateTime? invoiceSentDateEnd,
            decimal? totalAmountMin, decimal? totalAmountMax)
        {
            try
            {
                var query = _context.Invoices
                    .Include(i => i.Client)
                    .AsQueryable();

                // Apply filters
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

                // Calculate total records
                int totalRecords = await query.CountAsync();

                // Calculate total pages
                int totalPages = (int)Math.Ceiling((double)totalRecords / recordLimit);

                // Ensure page is within valid range
                page = page < 1 ? 1 : page > totalPages ? totalPages : page;

                // Ensure skippages is not negative
                int skipPages = page > 0 ? (page - 1) * recordLimit : 0;

                // Retrieve paginated invoices
                var invoices = await query
                    .OrderByDescending(i => i.InvoiceDate)
                    .Skip(skipPages)
                    .Take(recordLimit)
                    .ToListAsync();

                return (invoices, totalRecords, totalPages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices with page={Page}, recordLimit={RecordLimit}, clientFilter={ClientFilter}", page, recordLimit, clientFilter);
                throw;
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteInvoiceAsync(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.InvoiceTimeEntries)
                    .Include(i => i.InvoiceExpenses)
                    .FirstOrDefaultAsync(i => i.InvoiceID == id);

                if (invoice == null)
                {
                    return (false, "Invoice not found.");
                }

                var timeEntryIds = invoice.InvoiceTimeEntries.Select(ite => ite.TimeEntryID).ToList();
                var timeEntries = await _context.TimeEntries
                    .Where(t => timeEntryIds.Contains(t.TimeEntryID))
                    .ToListAsync();

                foreach (var timeEntry in timeEntries)
                {
                    timeEntry.InvoicedDate = null;
                    timeEntry.InvoiceSent = null;
                    timeEntry.PaidDate = null;
                }

                var productIds = invoice.InvoiceExpenses.Select(ie => ie.ProductID).ToList();
                var expenses = await _context.Expenses
                    .Where(e => productIds.Contains(e.ProductID) && e.InvoicedDate.HasValue)
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
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice ID {InvoiceId}", id);
                return (false, "An error occurred while deleting the invoice.");
            }
        }

        public async Task<(bool Success, string? ErrorMessage, byte[]? PdfBytes)> SendInvoiceAsync(int id, string userId)
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
                    return (false, "Invoice not found.", null);
                }

                var pdfBytes = await GenerateInvoicePdfAsync(id, userId);
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

                var productIds = invoice.InvoiceExpenses.Select(ie => ie.ProductID).ToList();
                var expenses = await _context.Expenses
                    .Where(e => productIds.Contains(e.ProductID) && e.InvoicedDate.HasValue)
                    .ToListAsync();

                foreach (var expense in expenses)
                {
                    expense.InvoiceSent = DateTime.Today;
                }

                await _context.SaveChangesAsync();
                return (true, null, pdfBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invoice ID {InvoiceId}", id);
                return (false, "An error occurred while processing the invoice.", null);
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> MarkInvoiceAsPaidAsync(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.InvoiceTimeEntries)
                    .Include(i => i.InvoiceExpenses)
                    .FirstOrDefaultAsync(i => i.InvoiceID == id);

                if (invoice == null)
                {
                    return (false, "Invoice not found.");
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

                var productIds = invoice.InvoiceExpenses.Select(ie => ie.ProductID).ToList();
                var expenses = await _context.Expenses
                    .Where(e => productIds.Contains(e.ProductID) && e.InvoicedDate.HasValue)
                    .ToListAsync();

                foreach (var expense in expenses)
                {
                    expense.PaidDate = DateTime.Today;
                }

                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking invoice ID {InvoiceId} as paid", id);
                return (false, "An error occurred while marking the invoice as paid.");
            }
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(int invoiceId,string userId)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Client)
                    .Include(i => i.InvoiceTimeEntries).ThenInclude(ite => ite.TimeEntry).ThenInclude(te => te.Project)
                    .Include(i => i.InvoiceExpenses).ThenInclude(ie => ie.Product)
                    .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId);

                if (invoice == null)
                {
                    throw new ArgumentException($"Invoice with ID {invoiceId} not found.");
                }

                var settings = await _context.Settings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    _logger.LogWarning("Settings not found for invoice ID {InvoiceId}", invoiceId);
                    throw new InvalidOperationException("Settings not configured.");
                }

                // Build Time Entries Table HTML
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogError("User not found for ID {UserId}", userId);
                }

                int timezoneOffset = 0;
                if (!string.IsNullOrEmpty(user?.TimeZoneId))
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

                string timeEntriesTable = "";
                decimal hoursTotal = 0, timeEntriesSubtotal = 0;
                if (invoice.InvoiceTimeEntries.Any())
                {
                    timeEntriesTable = @"
                        Time Entries
                        <table>
                            <tr>
                                <th>Date</th>
                                <th>Project</th>
                                <th>Description</th>
                                <th>Hours</th>
                                <th>Rate</th>
                                <th>Total</th>
                            </tr>";
                    foreach (var entry in invoice.InvoiceTimeEntries)
                    {
                        var timeEntry = entry.TimeEntry;
                        var rate = timeEntry.HourlyRate.HasValue ? timeEntry.HourlyRate.Value :
                            timeEntry.Project != null ? timeEntry.Project.Rate :
                            invoice.Client.DefaultRate;
                        var timeentryCharge = timeEntry.HoursSpent * rate;
                        hoursTotal += timeEntry.HoursSpent ?? 0;
                        timeEntriesSubtotal += timeentryCharge ?? 0;
                        timeEntriesTable += @$"
                            <tr>
                                <td>{DateOnly.FromDateTime(timeEntry.StartDateTime.AddMinutes(timezoneOffset))}</td>
                                <td>{(timeEntry.Project?.Name ?? "N/A")}</td>
                                <td>{(timeEntry.Description ?? "N/A")}</td>
                                <td>{timeEntry.HoursSpent:F2}</td>
                                <td>${rate:N2}</td>
                                <td>${timeentryCharge:N2}</td>
                            </tr>";
                    }
                    timeEntriesTable += @$"
                        <tr>
                            <td colspan='3'><b>Time Entries Sub total</b></td>
                            <td>{hoursTotal:F2}</td>
                            <td>-</td>
                            <td>${(timeEntriesSubtotal):N2}</td>
                        </tr>";
                    timeEntriesTable += "</table>";
                }

                // Build Expenses Table HTML
                string expensesTable = "";
                decimal expensesSubtotal = 0;
                if (invoice.InvoiceExpenses.Any())
                {
                    expensesTable = @"
                        Expenses
                        <table>
                            <tr>
                                <th>Date</th>
                                <th>Description</th>
                                <th>Quantity</th>
                                <th>Unit Price</th>
                                <th>Total</th>
                            </tr>";
                    foreach (var expense in invoice.InvoiceExpenses)
                    {
                        var expenseCharge = expense.UnitAmount * expense.Quantity;
                        var description = expense.Description ?? "N/A";
                        expensesTable += @$"
                            <tr>
                                <td>{expense.ProductInvoiceDate}</td>
                                <td>{description}</td>
                                <td>{expense.Quantity}</td>
                                <td>${(expense.UnitAmount):N2}</td>
                                <td>${(expenseCharge):N2}</td>
                            </tr>";
                        expensesSubtotal += expenseCharge;
                    }
                    expensesTable += @$"
                            <tr>
                                <td colspan='4'><b>Expenses Sub total</b></td>
                                <td>${(expensesSubtotal):N2}</td>
                            </tr>";
                    expensesTable += "</table>";
                }

                // Replace placeholders in template
                string htmlContent = settings.InvoiceTemplate
                    .Replace("{{CompanyName}}", settings.CompanyName ?? "")
                    .Replace("{{AccountsReceivableAddress}}", settings.AccountsReceivableAddress ?? "")
                    .Replace("{{AccountsReceivablePhone}}", settings.AccountsReceivablePhone ?? "")
                    .Replace("{{AccountsReceivableEmail}}", settings.AccountsReceivableEmail ?? "")
                    .Replace("{{InvoiceID}}", invoiceId.ToString())
                    .Replace("{{AccountsReceivableName}}", invoice.Client?.AccountsReceivableName ?? "")
                    .Replace("{{ClientName}}", invoice.Client?.Name ?? "")
                    .Replace("{{InvoiceDate}}", invoice.InvoiceDate.ToString("MMM dd, yyyy"))
                    .Replace("{{TotalAmount}}", invoice.TotalAmount.ToString("N2"))
                    .Replace("{{Status}}", invoice.Status.ToString() ?? "")
                    .Replace("{{TimeEntriesTable}}", timeEntriesTable)
                    .Replace("{{ExpensesTable}}", expensesTable)
                    .Replace("{{PaymentInformation}}", settings.PaymentInformation ?? "")
                    .Replace("{{ThankYouMessage}}", settings.ThankYouMessage ?? "");

                using (var stream = new MemoryStream())
                {
                    var writer = new PdfWriter(stream);
                    var pdf = new PdfDocument(writer);
                    var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);
                    HtmlConverter.ConvertToPdf(htmlContent, pdf, new ConverterProperties());
                    document.Close();
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for invoice ID {InvoiceId}", invoiceId);
                throw;
            }
        }

        public async Task<Models.Settings?> GetEmailSettingsAsync()
        {
            try
            {
                return await _context.Settings.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve email settings.");
                throw;
            }
        }

        private async Task<bool> InvoiceExistsAsync(int id)
        {
            return await _context.Invoices.AnyAsync(e => e.InvoiceID == id);
        }
    }
}