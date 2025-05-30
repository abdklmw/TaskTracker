using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
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

                using (var stream = new MemoryStream())
                {
                    var writer = new PdfWriter(stream);
                    var pdf = new PdfDocument(writer);
                    var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);

                    // Fonts
                    var titleFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
                    var headerFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
                    var normalFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

                    // Header: Company Info
                    var companyHeader = new Paragraph(settings.CompanyName)
                        .SetFont(headerFont)
                        .SetFontSize(12);
                    if (!string.IsNullOrEmpty(settings.AccountsReceivableAddress))
                        companyHeader.Add($"\n{settings.AccountsReceivableAddress}");
                    if (!string.IsNullOrEmpty(settings.AccountsReceivablePhone))
                        companyHeader.Add($"\n{settings.AccountsReceivablePhone}");
                    if (!string.IsNullOrEmpty(settings.AccountsReceivableEmail))
                        companyHeader.Add($"\n{settings.AccountsReceivableEmail}");
                    companyHeader.SetTextAlignment(TextAlignment.LEFT);
                    document.Add(companyHeader);

                    // Invoice Title
                    var title = new Paragraph($"Invoice #{invoice.InvoiceID}")
                        .SetFont(titleFont)
                        .SetFontSize(18)
                        .SetTextAlignment(TextAlignment.CENTER);
                    document.Add(title);
                    document.Add(new Paragraph("\n"));

                    // Client Info
                    var clientInfo = new Paragraph($"Billed To:\n{invoice?.Client?.Name}\n")
                        .SetFont(normalFont)
                        .SetFontSize(10);
                    clientInfo.Add($"Invoice Date: {invoice?.InvoiceDate:yyyy-MM-dd}\n");
                    clientInfo.Add($"Total Amount: ${invoice?.TotalAmount:F2}\n");
                    clientInfo.Add($"Status: {invoice?.Status}\n");
                    clientInfo.SetTextAlignment(TextAlignment.LEFT);
                    document.Add(clientInfo);
                    document.Add(new Paragraph("\n"));

                    // Time Entries Table
                    if (invoice!= null && invoice.InvoiceTimeEntries.Count != 0)
                    {
                        var timeTable = new Table(new float[] { 3, 2, 2, 2 }).UseAllAvailableWidth();
                        timeTable.AddHeaderCell(new Cell().Add(new Paragraph("Description").SetFont(headerFont).SetFontSize(12)));
                        timeTable.AddHeaderCell(new Cell().Add(new Paragraph("Hours").SetFont(headerFont).SetFontSize(12)));
                        timeTable.AddHeaderCell(new Cell().Add(new Paragraph("Rate").SetFont(headerFont).SetFontSize(12)));
                        timeTable.AddHeaderCell(new Cell().Add(new Paragraph("Total").SetFont(headerFont).SetFontSize(12)));

                        foreach (var entry in invoice.InvoiceTimeEntries)
                        {
                            var timeEntry = entry.TimeEntry;
                            var rate = timeEntry.HourlyRate.HasValue ? timeEntry.HourlyRate.Value :
                                       timeEntry.Project != null ? timeEntry.Project.Rate :
                                       invoice?.Client?.DefaultRate != 0 ? invoice?.Client?.DefaultRate :
                                       settings.DefaultHourlyRate;
                            timeTable.AddCell(new Cell().Add(new Paragraph(timeEntry.Description ?? "N/A").SetFont(normalFont).SetFontSize(10)));
                            timeTable.AddCell(new Cell().Add(new Paragraph($"{timeEntry.HoursSpent:F2}").SetFont(normalFont).SetFontSize(10)));
                            timeTable.AddCell(new Cell().Add(new Paragraph($"${rate:F2}").SetFont(normalFont).SetFontSize(10)));
                            timeTable.AddCell(new Cell().Add(new Paragraph($"${(timeEntry.HoursSpent * rate):F2}").SetFont(normalFont).SetFontSize(10)));
                        }

                        document.Add(timeTable);
                        document.Add(new Paragraph("\n"));
                    }

                    // Expenses Table
                    if (invoice != null && invoice.InvoiceExpenses.Count != 0)
                    {
                        var expenseTable = new Table(new float[] { 3, 2, 2, 2 }).UseAllAvailableWidth();
                        expenseTable.AddHeaderCell(new Cell().Add(new Paragraph("Description").SetFont(headerFont).SetFontSize(12)));
                        expenseTable.AddHeaderCell(new Cell().Add(new Paragraph("Quantity").SetFont(headerFont).SetFontSize(12)));
                        expenseTable.AddHeaderCell(new Cell().Add(new Paragraph("Unit Price").SetFont(headerFont).SetFontSize(12)));
                        expenseTable.AddHeaderCell(new Cell().Add(new Paragraph("Total").SetFont(headerFont).SetFontSize(12)));

                        foreach (var expense in invoice.InvoiceExpenses)
                        {
                            var product = expense.Product;
                            expenseTable.AddCell(new Cell().Add(new Paragraph(product?.Description ?? "N/A").SetFont(normalFont).SetFontSize(10)));
                            expenseTable.AddCell(new Cell().Add(new Paragraph($"{expense.Quantity}").SetFont(normalFont).SetFontSize(10)));
                            expenseTable.AddCell(new Cell().Add(new Paragraph($"${product?.UnitPrice:F2}").SetFont(normalFont).SetFontSize(10)));
                            expenseTable.AddCell(new Cell().Add(new Paragraph($"${(product?.UnitPrice * expense.Quantity):F2}").SetFont(normalFont).SetFontSize(10)));
                        }

                        document.Add(expenseTable);
                        document.Add(new Paragraph("\n"));
                    }

                    // Total
                    var total = new Paragraph($"Total: ${invoice?.TotalAmount:F2}")
                        .SetFont(headerFont)
                        .SetFontSize(12)
                        .SetTextAlignment(TextAlignment.RIGHT);
                    document.Add(total);

                    // Footer: Payment Info and Thank You
                    document.Add(new Paragraph("\n"));
                    if (!string.IsNullOrEmpty(settings.PaymentInformation))
                    {
                        var paymentInfo = new Paragraph($"Payment Information:\n{settings.PaymentInformation}\n")
                            .SetFont(normalFont)
                            .SetFontSize(10);
                        document.Add(paymentInfo);
                    }
                    if (!string.IsNullOrEmpty(settings.ThankYouMessage))
                    {
                        var thankYou = new Paragraph(settings.ThankYouMessage)
                            .SetFont(normalFont)
                            .SetFontSize(10);
                        document.Add(thankYou);
                    }

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
    }
}