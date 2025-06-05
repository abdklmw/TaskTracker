using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskTracker.Data;
using TaskTracker.Models;
using TaskTracker.Models.Expense;
using TaskTracker.Services;

namespace TaskTracker.Controllers
{
    public class ExpensesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ExpensesController> _logger;
        private readonly DropdownService _dropdownService;

        public ExpensesController(
            AppDbContext context,
            ILogger<ExpensesController> logger,
            DropdownService dropdownService)
        {
            _context = context;
            _logger = logger;
            _dropdownService = dropdownService;
        }

        public async Task<IActionResult> Index(int page = 1, int recordLimit = 10, int clientFilter = 0)
        {
            _logger.LogInformation("Index called with page={Page}, recordLimit={RecordLimit}, clientFilter={ClientFilter}", page, recordLimit, clientFilter);

            var query = _context.Expenses
                .Where(e => e.InvoicedDate == null)
                .Include(e => e.Client)
                .AsQueryable();

            if (clientFilter != 0)
            {
                query = query.Where(e => e.ClientID == clientFilter);
            }

            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalRecords / recordLimit);
            page = page < 1 ? 1 : page > totalPages ? totalPages : page;

            var expenses = await query
                .OrderByDescending(e => e.InvoicedDate)
                .Skip((page - 1) * recordLimit)
                .Take(recordLimit)
                .ToListAsync();

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
                ClientFilterOptions = await _dropdownService.GetClientDropdownAsync(clientFilter),
                RecordLimitOptions = new SelectList(new[]
                {
                    new { Value = "5", Text = "5" },
                    new { Value = "10", Text = "10" },
                    new { Value = "25", Text = "25" },
                    new { Value = "50", Text = "50" },
                    new { Value = "100", Text = "100" }
                }, "Value", "Text", recordLimit.ToString())
            };

            ViewBag.ClientList = new SelectList(await _dropdownService.GetClientDropdownAsync(0), "Value", "Text", 0);
            ViewBag.ProductList = await _context.Products
                .OrderBy(p => p.ProductSku)
                .Select(p => new
                {
                    ProductID = p.ProductID.ToString(),
                    ProductSku = p.ProductSku,
                    Name = p.Name,
                    UnitPrice = p.UnitPrice
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientID,Description,UnitAmount,Quantity,TotalAmount,ExpenseDateTime")] Expense expense)
        {
            if (ModelState.IsValid)
            {
                _context.Add(expense);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Expense created, ExpenseID: {ExpenseID}", expense.ExpenseID);
                TempData["SuccessMessage"] = "Expense created successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            ViewBag.ClientList = new SelectList(await _dropdownService.GetClientDropdownAsync(0), "Value", "Text", 0);
            ViewBag.ProductList = await _context.Products
                .OrderBy(p => p.ProductSku)
                .Select(p => new
                {
                    ProductID = p.ProductID.ToString(),
                    ProductSku = p.ProductSku,
                    Name = p.Name,
                    UnitPrice = p.UnitPrice
                })
                .ToListAsync();
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
                try
                {
                    _context.Update(expense);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Expense updated, ExpenseID: {ExpenseID}", expense.ExpenseID);
                    TempData["SuccessMessage"] = "Expense updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExpenseExists(expense.ExpenseID))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            ViewBag.ClientList = new SelectList(await _dropdownService.GetClientDropdownAsync(0), "Value", "Text", 0);
            ViewBag.ProductList = await _context.Products
                .OrderBy(p => p.ProductSku)
                .Select(p => new
                {
                    ProductID = p.ProductID.ToString(),
                    ProductSku = p.ProductSku,
                    Name = p.Name,
                    UnitPrice = p.UnitPrice
                })
                .ToListAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var expense = await _context.Expenses
                .Include(e => e.Client)
                .FirstOrDefaultAsync(e => e.ExpenseID == id);

            if (expense == null)
            {
                _logger.LogWarning("Expense {ExpenseID} not found", id);
                return NotFound();
            }

            ViewBag.ClientList = new SelectList(await _dropdownService.GetClientDropdownAsync(0), "Value", "Text", 0);
            ViewBag.ProductList = await _context.Products
                .OrderBy(p => p.ProductSku)
                .Select(p => new
                {
                    ProductID = p.ProductID.ToString(),
                    ProductSku = p.ProductSku,
                    Name = p.Name,
                    UnitPrice = p.UnitPrice
                })
                .ToListAsync();
            return View(expense);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
            {
                _logger.LogWarning("Expense {ExpenseID} not found", id);
                TempData["ErrorMessage"] = "Expense not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Expense deleted, ExpenseID: {ExpenseID}", id);
            TempData["SuccessMessage"] = "Expense deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool ExpenseExists(int id)
        {
            return _context.Expenses.Any(e => e.ExpenseID == id);
        }
    }
}