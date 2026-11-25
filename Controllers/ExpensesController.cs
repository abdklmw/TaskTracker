using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskTracker.Models;
using TaskTracker.Models.Expense;
using TaskTracker.Services;

namespace TaskTracker.Controllers
{
    public class ExpensesController : Controller
    {
        private static readonly int[] ValidRecordLimits = { 5, 10, 20, 25, 50, 100, 200, -1 };

        private readonly ExpenseService _expenseService;
        private readonly ClientService _clientService;
        private readonly ILogger<ExpensesController> _logger;
        private readonly ProductService _productService;
        private readonly IUserPreferenceService _preferenceService;

        public ExpensesController(
            ExpenseService expenseService,
            ClientService clientService,
            ProductService productService,
            ILogger<ExpensesController> logger,
            IUserPreferenceService preferenceService)
        {
            _expenseService = expenseService;
            _clientService = clientService;
            _productService = productService;
            _logger = logger;
            _preferenceService = preferenceService;
        }

        public async Task<IActionResult> Index(
            int page = 1,
            int recordLimit = 0,
            int clientFilter = 0,
            string status = "Unbilled",
            string[]? productFilter = null,
            DateTime? expenseDateStart = null,
            DateTime? expenseDateEnd = null,
            DateTime? invoicedDateStart = null,
            DateTime? invoicedDateEnd = null,
            DateTime? paidDateStart = null,
            DateTime? paidDateEnd = null,
            DateTime? invoiceSentDateStart = null,
            DateTime? invoiceSentDateEnd = null,
            bool expenseDateAny = false,
            bool invoicedDateAny = false,
            bool paidDateAny = false,
            bool invoiceSentDateAny = false)
        {
            status = ExpenseService.NormalizeStatus(status);
            var productIds = ParseIdList(productFilter);

            int globalClientId = (int)(ViewData["GlobalClientId"] ?? 0);
            if (globalClientId != 0)
            {
                clientFilter = globalClientId;
            }

            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            if ((recordLimit > 0 || recordLimit == -1) && !string.IsNullOrEmpty(userId))
            {
                await _preferenceService.SetRecordLimitAsync(userId, "expenses", recordLimit);
            }
            else
            {
                recordLimit = (int)(ViewData["UserExpensesRecordLimit"] ?? 10);
            }

            if (!ValidRecordLimits.Contains(recordLimit))
            {
                recordLimit = 10;
            }

            _logger.LogInformation(
                "Index called with page={Page}, recordLimit={RecordLimit}, clientFilter={ClientFilter}, status={Status}",
                page, recordLimit, clientFilter, status);

            var (expenses, totalRecords, totalPages) = await _expenseService.GetExpensesAsync(
                page, recordLimit, clientFilter, status, productIds,
                expenseDateStart, expenseDateEnd, invoicedDateStart, invoicedDateEnd,
                paidDateStart, paidDateEnd, invoiceSentDateStart, invoiceSentDateEnd,
                expenseDateAny, invoicedDateAny, paidDateAny, invoiceSentDateAny);

            int clientExpenseCount = await _expenseService.CountExpensesForClientAsync(clientFilter);

            string? clientName = null;
            if (globalClientId != 0)
            {
                clientName = ViewData["GlobalClientName"] as string;
            }

            var clientOptions = await _clientService.GetClientDropdownAsync(clientFilter);
            if (string.IsNullOrEmpty(clientName) && clientFilter != 0)
            {
                clientName = clientOptions.FirstOrDefault(c => c.Value == clientFilter.ToString())?.Text;
            }

            var viewModel = new ExpenseIndexViewModel
            {
                Expenses = expenses,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                ClientExpenseCount = clientExpenseCount,
                ClientName = clientName,
                RecordLimit = recordLimit,
                SelectedClientID = clientFilter,
                Status = status,
                SelectedProductIDs = productIds.ToList(),
                ExpenseDateStart = expenseDateStart,
                ExpenseDateEnd = expenseDateEnd,
                InvoicedDateStart = invoicedDateStart,
                InvoicedDateEnd = invoicedDateEnd,
                PaidDateStart = paidDateStart,
                PaidDateEnd = paidDateEnd,
                InvoiceSentDateStart = invoiceSentDateStart,
                InvoiceSentDateEnd = invoiceSentDateEnd,
                ExpenseDateAny = expenseDateAny,
                InvoicedDateAny = invoicedDateAny,
                PaidDateAny = paidDateAny,
                InvoiceSentDateAny = invoiceSentDateAny,
                ClientFilterOptions = clientOptions,
                StatusFilterOptions = new SelectList(new[]
                {
                    new { Value = "Unbilled", Text = "Unbilled" },
                    new { Value = "Invoiced", Text = "Invoiced" },
                    new { Value = "Paid", Text = "Paid" },
                    new { Value = "All", Text = "All" }
                }, "Value", "Text", status),
                ProductFilterOptions = new MultiSelectList(
                    await _productService.GetProductFilterDropdownAsync(productIds),
                    "Value", "Text", productIds.Select(id => id.ToString())),
                RecordLimitOptions = new SelectList(new[]
                {
                    new { Value = "5", Text = "5" },
                    new { Value = "10", Text = "10" },
                    new { Value = "20", Text = "20" },
                    new { Value = "50", Text = "50" },
                    new { Value = "100", Text = "100" },
                    new { Value = "200", Text = "200" },
                    new { Value = "-1", Text = "ALL" }
                }, "Value", "Text", recordLimit.ToString())
            };

            viewModel.RouteValues = BuildRouteValues(viewModel);

            int createClientId = globalClientId != 0 ? globalClientId : 0;
            ViewBag.ClientList = new SelectList(await _clientService.GetClientDropdownAsync(createClientId), "Value", "Text", createClientId);
            ViewBag.ProductList = await _productService.GetProductDropdownAsync();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientID,Description,UnitAmount,Quantity,TotalAmount,ProductID,ExpenseDate")] Expense expense)
        {
            if (ModelState.IsValid)
            {
                var (success, error) = await _expenseService.CreateExpenseAsync(expense);
                if (success)
                {
                    _logger.LogInformation("Expense created, ExpenseID: {ExpenseID}", expense.ExpenseID);
                    TempData["SuccessMessage"] = "Expense created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = error;
            }
            else
            {
                TempData["ErrorMessage"] = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }

            ViewBag.ClientList = new SelectList(await _clientService.GetClientDropdownAsync(0), "Value", "Text", 0);
            ViewBag.ProductList = await _productService.GetProductDropdownAsync();
            ViewBag.VisibleCreateForm = true;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ExpenseID,ClientID,Description,UnitAmount,Quantity,TotalAmount,ProductID,ExpenseDate")] Expense expense)
        {
            if (id != expense.ExpenseID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var (success, error) = await _expenseService.UpdateExpenseAsync(expense);
                if (success)
                {
                    _logger.LogInformation("Expense updated, ExpenseID: {ExpenseID}", expense.ExpenseID);
                    TempData["SuccessMessage"] = "Expense updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = error ?? "Expense not found.";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }

            ViewBag.ClientList = new SelectList(await _clientService.GetClientDropdownAsync(0), "Value", "Text", 0);
            ViewBag.ProductList = await _productService.GetProductDropdownAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var expense = await _expenseService.GetExpenseByIdAsync(id.Value);
            if (expense == null)
            {
                _logger.LogWarning("Expense {ExpenseID} not found", id);
                return NotFound();
            }

            ViewBag.ClientList = new SelectList(await _clientService.GetClientDropdownAsync(0), "Value", "Text", 0);
            ViewBag.ProductList = await _productService.GetProductDropdownAsync();
            return View(expense);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (success, error) = await _expenseService.DeleteExpenseAsync(id);
            if (success)
            {
                _logger.LogInformation("Expense deleted, ExpenseID: {ExpenseID}", id);
                TempData["SuccessMessage"] = "Expense deleted successfully.";
            }
            else
            {
                _logger.LogWarning("Expense {ExpenseID} delete rejected or not found: {Error}", id, error);
                TempData["ErrorMessage"] = error;
            }
            return RedirectToAction(nameof(Index));
        }

        private static int[] ParseIdList(string[]? values)
        {
            if (values == null || values.Length == 0)
            {
                return Array.Empty<int>();
            }

            return values
                .SelectMany(v => (v ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(v => int.TryParse(v, out var id) ? id : 0)
                .Where(id => id != 0)
                .Distinct()
                .ToArray();
        }

        private static Dictionary<string, string> BuildRouteValues(ExpenseIndexViewModel model)
        {
            return new Dictionary<string, string>
            {
                { "recordLimit", model.RecordLimit.ToString() },
                { "clientFilter", model.SelectedClientID.ToString() },
                { "status", model.Status },
                { "productFilter", string.Join(",", model.SelectedProductIDs) },
                { "expenseDateStart", model.ExpenseDateStart?.ToString("yyyy-MM-dd") ?? "" },
                { "expenseDateEnd", model.ExpenseDateEnd?.ToString("yyyy-MM-dd") ?? "" },
                { "invoicedDateStart", model.InvoicedDateStart?.ToString("yyyy-MM-dd") ?? "" },
                { "invoicedDateEnd", model.InvoicedDateEnd?.ToString("yyyy-MM-dd") ?? "" },
                { "paidDateStart", model.PaidDateStart?.ToString("yyyy-MM-dd") ?? "" },
                { "paidDateEnd", model.PaidDateEnd?.ToString("yyyy-MM-dd") ?? "" },
                { "invoiceSentDateStart", model.InvoiceSentDateStart?.ToString("yyyy-MM-dd") ?? "" },
                { "invoiceSentDateEnd", model.InvoiceSentDateEnd?.ToString("yyyy-MM-dd") ?? "" },
                { "expenseDateAny", model.ExpenseDateAny.ToString().ToLowerInvariant() },
                { "invoicedDateAny", model.InvoicedDateAny.ToString().ToLowerInvariant() },
                { "paidDateAny", model.PaidDateAny.ToString().ToLowerInvariant() },
                { "invoiceSentDateAny", model.InvoiceSentDateAny.ToString().ToLowerInvariant() }
            };
        }
    }
}
