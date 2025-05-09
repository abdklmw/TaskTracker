using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Added for SelectList
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models;

namespace TaskTracker.Controllers
{
    // Controller for managing expenses
    public class ExpensesController : Controller
    {
        private readonly AppDbContext _context;

        // Constructor with dependency injection for database context
        public ExpensesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Expenses
        // Displays the list of all expenses with associated clients
        public async Task<IActionResult> Index()
        {
            // Populate ViewBag.ClientList for the create form
            ViewBag.ClientList = new SelectList(await _context.Clients.ToListAsync(), "ClientID", "Name");
            var expenses = _context.Expenses.Include(e => e.Client);
            return View(await expenses.ToListAsync());
        }

        // POST: Expenses/Create
        // Creates a new expense and saves it to the database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientID,Description,Amount")] Expense expense)
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
            // Repopulate ViewBag.ClientList in case of validation failure
            ViewBag.ClientList = new SelectList(await _context.Clients.ToListAsync(), "ClientID", "Name");
            return RedirectToAction(nameof(Index));
        }

        // POST: Expenses/Edit/5
        // Updates an existing expense
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ExpenseID,ClientID,Description,Amount")] Expense expense)
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
            // Repopulate ViewBag.ClientList in case of validation failure
            ViewBag.ClientList = new SelectList(await _context.Clients.ToListAsync(), "ClientID", "Name");
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
            // Populate ViewBag.ClientList for consistency, though not used in Delete view
            ViewBag.ClientList = new SelectList(await _context.Clients.ToListAsync(), "ClientID", "Name");
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