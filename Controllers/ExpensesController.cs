using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TaskTracker.Data;
using TaskTracker.Models.Expense;
using TaskTracker.Services;

namespace TaskTracker.Controllers
{
    // Controller for managing expenses
    public class ExpensesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly DropdownService _dropdownService;

        // Constructor with dependency injection for database context
        public ExpensesController(AppDbContext context, DropdownService dropdownService)
        {
            _context = context;
            _dropdownService = dropdownService;
        }

        // GET: Expenses
        // Displays the list of all expenses with associated clients
        public async Task<IActionResult> Index()
        {
            // Populate ViewBag for client and product dropdowns
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
            var expenses = _context.Expenses.Include(e => e.Client);
            return View(await expenses.ToListAsync());
        }

        // POST: Expenses/Create
        // Creates a new expense and saves it to the database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientID,Description,UnitAmount,Quantity,TotalAmount")] Expense expense)
        {
            if (ModelState.IsValid)
            {
                _context.Add(expense);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Expense created successfully.";
                return RedirectToAction(nameof(Index));
            }
            // Collect validation errors for display
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = string.Join("; ", errors);
            // Repopulate ViewBag for form redisplay
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

        // POST: Expenses/Edit/5
        // Updates an existing expense
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ExpenseID,ClientID,Description,UnitAmount,Quantity,TotalAmount")] Expense expense)
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
            // Collect validation errors for display
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = string.Join("; ", errors);
            // Repopulate ViewBag for form redisplay
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

        // GET: Expenses/Delete/5
        // Displays the confirmation page for deleting an expense
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var expense = await _context.Expenses
                .Include(e => e.Client)
                .FirstOrDefaultAsync(m => m.ExpenseID == id);
            if (expense == null)
            {
                return NotFound();
            }
            // Populate ViewBag for consistency
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

        // POST: Expenses/Delete/5
        // Deletes the specified expense
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense != null)
            {
                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Expense deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper method to check if an expense exists
        private bool ExpenseExists(int id)
        {
            return _context.Expenses.Any(e => e.ExpenseID == id);
        }
    }
}