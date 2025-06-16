using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Models.Invoice;
using TaskTracker.Services;

namespace TaskTracker.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class InvoicesApiController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IEmailService _emailService;
        private readonly ClientService _clientService;

        public InvoicesApiController(IInvoiceService invoiceService, IEmailService emailService, ClientService clientService)
        {
            _invoiceService = invoiceService;
            _emailService = emailService;
            _clientService = clientService;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int recordLimit = 10,
            [FromQuery] int clientFilter = 0,
            [FromQuery] InvoiceStatus? statusFilter = null,
            [FromQuery] DateTime? paidDateStart = null,
            [FromQuery] DateTime? paidDateEnd = null,
            [FromQuery] DateTime? invoiceDateStart = null,
            [FromQuery] DateTime? invoiceDateEnd = null,
            [FromQuery] DateTime? invoiceSentDateStart = null,
            [FromQuery] DateTime? invoiceSentDateEnd = null,
            [FromQuery] decimal? totalAmountMin = null,
            [FromQuery] decimal? totalAmountMax = null)
        {
            var (invoices, totalRecords, totalPages) = await _invoiceService.GetInvoicesAsync(
                page, recordLimit, clientFilter, statusFilter, paidDateStart, paidDateEnd,
                invoiceDateStart, invoiceDateEnd, invoiceSentDateStart, invoiceSentDateEnd,
                totalAmountMin, totalAmountMax);
            return Ok(new { Invoices = invoices, TotalRecords = totalRecords, TotalPages = totalPages });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }
            return Ok(invoice);
        }

        [HttpGet("unpaid-items/{clientId}")]
        public async Task<ActionResult> GetUnpaidItems(int clientId)
        {
            var (timeEntries, expenses) = await _invoiceService.GetUnpaidItemsAsync(clientId);
            return Ok(new { TimeEntries = timeEntries, Expenses = expenses });
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] InvoiceCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, error, createdInvoice) = await _invoiceService.CreateInvoiceAsync(model);
            if (!success)
            {
                return BadRequest(new { Error = error });
            }
            return CreatedAtAction(nameof(GetById), new { id = createdInvoice?.InvoiceID }, createdInvoice);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] Invoice invoice)
        {
            if (id != invoice.InvoiceID || !ModelState.IsValid)
            {
                return BadRequest();
            }

            var (success, error) = await _invoiceService.UpdateInvoiceAsync(invoice);
            if (!success)
            {
                return BadRequest(new { Error = error });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var (success, error) = await _invoiceService.DeleteInvoiceAsync(id);
            if (!success)
            {
                return NotFound(new { Error = error });
            }

            return NoContent();
        }

        [HttpPost("{id}/send")]
        public async Task<ActionResult> Send(int id)
        {
            var (success, error, pdfBytes) = await _invoiceService.SendInvoiceAsync(id);
            if (!success)
            {
                return BadRequest(new { Error = error });
            }

            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            var clientEmail = invoice?.Client?.Email;
            var settings = await _invoiceService.GetEmailSettingsAsync();
            bool canSendEmail = !string.IsNullOrWhiteSpace(clientEmail) &&
                !string.IsNullOrWhiteSpace(settings?.SmtpServer) &&
                settings?.SmtpPort.HasValue == true &&
                !string.IsNullOrWhiteSpace(settings?.SmtpSenderEmail);

            if (canSendEmail)
            {
                var subject = $"Invoice {invoice?.InvoiceDate:yyyyMMdd}.{invoice?.InvoiceID} from {settings?.CompanyName}";
                var recipientName = string.IsNullOrWhiteSpace(invoice?.Client?.AccountsReceivableName)
                    ? invoice?.Client?.Name
                    : invoice.Client?.AccountsReceivableName;
                var body = $"<p>Dear {recipientName},</p>" +
                           $"<p>Please find your invoice attached. The total amount due is ${invoice?.TotalAmount:N2}.</p>" +
                           "<p>Please let me know if you have any questions. Thank you for your business!</p>" +
                           $"<p>Best regards,<br>{settings?.CompanyName}</p>";

                await _emailService.SendEmailAsync(
                    clientEmail,
                    subject,
                    body,
                    pdfBytes,
                    $"Invoice_{invoice?.InvoiceDate:yyyyMMdd}.{invoice?.InvoiceID}.pdf");

                return Ok(new { Message = "Invoice sent successfully via email." });
            }

            return File(pdfBytes, "application/pdf", $"Invoice_{invoice?.InvoiceDate:yyyyMMdd}.{invoice?.InvoiceID}.pdf");
        }

        [HttpPost("{id}/paid")]
        public async Task<ActionResult> MarkAsPaid(int id)
        {
            var (success, error) = await _invoiceService.MarkInvoiceAsPaidAsync(id);
            if (!success)
            {
                return BadRequest(new { Error = error });
            }

            return Ok(new { Message = "Invoice marked as paid successfully." });
        }
    }
}