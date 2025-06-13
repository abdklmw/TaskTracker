using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;
using TaskTracker.Models;
using TaskTracker.Models.Invoice;
using TaskTracker.Services;

namespace TaskTracker.Controllers
{
	public class InvoicesController : Controller
	{
		private readonly IInvoiceService _invoiceService;
		private readonly ILogger<InvoicesController> _logger;
		private readonly ClientService _clientService;
		private readonly IEmailService _emailService;

		public InvoicesController(
			IInvoiceService invoiceService,
			ILogger<InvoicesController> logger,
			ClientService clientService,
			IEmailService emailService)
		{
			_invoiceService = invoiceService;
			_logger = logger;
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
			var (invoices, totalRecords, totalPages) = await _invoiceService.GetInvoicesAsync(
				page, recordLimit, clientFilter, statusFilter,
				paidDateStart, paidDateEnd, invoiceDateStart, invoiceDateEnd,
				invoiceSentDateStart, invoiceSentDateEnd, totalAmountMin, totalAmountMax);

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
				var (timeEntries, expenses) = await _invoiceService.GetUnpaidItemsAsync(clientId);
				return Json(new { TimeEntries = timeEntries, Expenses = expenses });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving unpaid items for client ID {ClientId}", clientId);
				return StatusCode(500, new { error = "An error occurred while retrieving unpaid items." });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(InvoiceCreateViewModel model)
		{
			var (success, error, createdInvoice) = await _invoiceService.CreateInvoiceAsync(model);
			if (success)
			{
				TempData["SuccessMessage"] = "Invoice created successfully.";
			}
			else
			{
				TempData["ErrorMessage"] = error ?? "Error creating invoice.";
			}
			return RedirectToAction(nameof(Index));
		}

		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var invoice = await _invoiceService.GetInvoiceByIdAsync(id.Value);
			if (invoice == null)
			{
				return NotFound();
			}

			ViewData["ClientID"] = new SelectList(await _clientService.GetClientDropdownAsync(invoice.ClientID), "Value", "Text", invoice.ClientID);
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
				var (success, error) = await _invoiceService.UpdateInvoiceAsync(invoice);
				if (success)
				{
					TempData["SuccessMessage"] = "Invoice updated successfully.";
					return RedirectToAction(nameof(Index));
				}
				TempData["ErrorMessage"] = error;
			}

			ViewData["ClientID"] = new SelectList(await _clientService.GetClientDropdownAsync(invoice.ClientID), "Value", "Text", invoice.ClientID);
			return View(invoice);
		}

		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var invoice = await _invoiceService.GetInvoiceByIdAsync(id.Value);
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
			var (success, error) = await _invoiceService.DeleteInvoiceAsync(id);
			if (success)
			{
				TempData["SuccessMessage"] = "Invoice deleted successfully.";
			}
			else
			{
				TempData["ErrorMessage"] = error;
			}
			return RedirectToAction(nameof(Index));
		}

		[HttpPost, ActionName("Send")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Send(int id)
		{
			var (success, error, pdfBytes) = await _invoiceService.SendInvoiceAsync(id);
			if (!success)
			{
				TempData["ErrorMessage"] = error;
				return RedirectToAction(nameof(Index));
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

			return File(pdfBytes, "application/pdf", $"Invoice_{invoice.InvoiceDate:yyyyMMdd}.{invoice.InvoiceID}.pdf");
		}

		[HttpPost, ActionName("Paid")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Paid(int id)
		{
			var (success, error) = await _invoiceService.MarkInvoiceAsPaidAsync(id);
			if (success)
			{
				TempData["SuccessMessage"] = "Invoice marked as paid successfully.";
			}
			else
			{
				TempData["ErrorMessage"] = error;
			}
			return RedirectToAction(nameof(Index));
		}
	}
}