using iText.Html2pdf;
using iText.Kernel.Pdf;
using iText.Layout;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;
using TaskTracker.Data;
using TaskTracker.Models;

namespace TaskTracker.Services
{
    public interface IInvoicePdfService
    {
        Task<byte[]> GenerateInvoicePdfAsync(int invoiceId);
    }

    public class InvoicePdfService : IInvoicePdfService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<InvoicePdfService> _logger;

        public InvoicePdfService(AppDbContext context, ILogger<InvoicePdfService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(int invoiceId)
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
                string timeEntriesTable = "";
                if (invoice.InvoiceTimeEntries.Any())
                {
                    timeEntriesTable = "<table><tr><th>Project</th><th>Description</th><th>Hours</th><th>Rate</th><th>Total</th></tr>";
                    foreach (var entry in invoice.InvoiceTimeEntries)
                    {
                        var timeEntry = entry.TimeEntry;
                        var rate = timeEntry.HourlyRate.HasValue ? timeEntry.HourlyRate.Value :
                                   timeEntry.Project != null ? timeEntry.Project.Rate :
                                   invoice.Client.DefaultRate;
                        timeEntriesTable += $"<tr><td>{(timeEntry.Project?.Name ?? "N/A")}</td><td>{(timeEntry.Description ?? "N/A")}</td><td>{timeEntry.HoursSpent:F2}</td><td>${rate:N2}</td><td>${(timeEntry.HoursSpent * rate):N2}</td></tr>";
                    }
                    timeEntriesTable += "</table>";
                }

                // Build Expenses Table HTML
                string expensesTable = "";
                if (invoice.InvoiceExpenses.Any())
                {
                    expensesTable = "<table><tr><th>Description</th><th>Quantity</th><th>Unit Price</th><th>Total</th></tr>";
                    foreach (var expense in invoice.InvoiceExpenses)
                    {
                        var product = expense.Product;
                        expensesTable += $"<tr><td>{(product?.Description ?? "N/A")}</td><td>{expense.Quantity}</td><td>${(product?.UnitPrice ?? 0):N2}</td><td>${((product?.UnitPrice ?? 0) * expense.Quantity):N2}</td></tr>";
                    }
                    expensesTable += "</table>";
                }

                // Replace placeholders in template
                string htmlContent = settings.InvoiceTemplate
                    .Replace("{{CompanyName}}", settings.CompanyName ?? "")
                    .Replace("{{AccountsReceivableAddress}}", settings.AccountsReceivableAddress ?? "")
                    .Replace("{{AccountsReceivablePhone}}", settings.AccountsReceivablePhone ?? "")
                    .Replace("{{AccountsReceivableEmail}}", settings.AccountsReceivableEmail ?? "")
                    .Replace("{{InvoiceID}}", $"{invoice.InvoiceDate:yyyyMMdd}.{invoice.InvoiceID}")
                    .Replace("{{ClientName}}", invoice.Client?.Name ?? "")
                    .Replace("{{InvoiceDate}}", invoice.InvoiceDate.ToString("M dd, yyyy"))
                    .Replace("{{TotalAmount}}", invoice.TotalAmount.ToString("N2"))
                    .Replace("{{Status}}", invoice.Status.ToString())
                    .Replace("{{TimeEntriesTable}}", timeEntriesTable)
                    .Replace("{{ExpensesTable}}", expensesTable)
                    .Replace("{{PaymentInformation}}", settings.PaymentInformation ?? "")
                    .Replace("{{ThankYouMessage}}", settings.ThankYouMessage ?? "");

                using (var stream = new MemoryStream())
                {
                    var writer = new PdfWriter(stream);
                    var pdf = new PdfDocument(writer);
                    var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);

                    // Convert HTML to PDF
                    HtmlConverter.ConvertToPdf(htmlContent, pdf, new ConverterProperties());

                    document.Close();
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for invoice ID {invoiceId}", invoiceId);
                throw;
            }
        }
    }
}