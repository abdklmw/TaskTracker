using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskTracker.Data;
using TaskTracker.Models;
using TaskTracker.Models.Expense;
using TaskTracker.Services;

namespace TaskTracker.Controllers
{
    public class ExpensesController : Controller
    {
        private readonly ExpenseService _expenseService;
        private readonly ClientService _clientService;
        private readonly ILogger<ExpensesController> _logger;

        public ExpensesController(
            ExpenseService expenseService,
            ClientService clientService,
            ILogger<ExpensesController> logger)
        {
            _expenseService = expenseService;
            _clientService = clientService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int page = 1, int recordLimit = 10, int clientFilter = 0)
        {
            _logger.LogInformation("Index called with page={Page}, recordLimit={RecordLimit}, clientFilter={ClientFilter}", page, recordLimit, clientFilter);

            var (expenses, totalRecords, totalPages) = await _expenseService.GetExpensesAsync(page, recordLimit, clientFilter);

            var viewModel = new ExpenseIndexViewModel
            {
                Expenses = expenses,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                RecordLimit = recordLimit,
                SelectedClientID = clientFilter,
                RouteValues = new Dictionary<string, string>
                {
                    { "recordLimit", recordLimit.ToString() },
                    { "clientFilter", clientFilter.ToString() }
                },
                ClientFilterOptions = await _clientService.GetClientDropdownAsync(clientFilter),
                RecordLimitOptions = new SelectList(new[]
                {
                    new { Value = "5", Text = "5" },
                    new { Value = "10", Text = "10" },
                    new { Value = "25", Text = "25" },
                    new { Value = "50", Text = "50" },
                    new { Value = "100", Text = "100" }
                }, "Value", "Text", recordLimit.ToString())
            };

            ViewBag.ClientList = new SelectList(await _clientService.GetClientDropdownAsync(0), "Value", "Text", 0);
            ViewBag.ProductList = await _expenseService.GetProductDropdownAsync();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientID,Description,UnitAmount,Quantity,TotalAmount,ExpenseDateTime")] Expense expense)
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
            ViewBag.ProductList = await _expenseService.GetProductDropdownAsync();
            ViewBag.VisibleCreateForm = true;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ExpenseID,ClientID,Description,UnitAmount,Quantity,TotalAmount,ExpenseDateTime")] Expense expense)
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
            ViewBag.ProductList = await _expenseService.GetProductDropdownAsync();
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
            ViewBag.ProductList = await _expenseService.GetProductDropdownAsync();
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
                _logger.LogWarning("Expense {ExpenseID} not found", id);
                TempData["ErrorMessage"] = error;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}