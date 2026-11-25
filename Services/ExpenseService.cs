using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models.Expense;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace TaskTracker.Services
{
    public class ExpenseService
    {
        private readonly AppDbContext _context;

        public ExpenseService(AppDbContext context)
        {
            _context = context;
        }

        public static string NormalizeStatus(string? status)
        {
            return status?.Trim().ToLowerInvariant() switch
            {
                "invoiced" => "Invoiced",
                "paid" => "Paid",
                "all" => "All",
                _ => "Unbilled"
            };
        }

        public async Task<(List<Expense> Expenses, int TotalRecords, int TotalPages)> GetExpensesAsync(
            int page,
            int recordLimit,
            int clientFilter,
            string status = "Unbilled")
        {
            status = NormalizeStatus(status);

            var query = _context.Expenses
                .Include(e => e.Client)
                .AsQueryable();

            query = status switch
            {
                "Invoiced" => query.Where(e => e.InvoicedDate != null && e.PaidDate == null),
                "Paid" => query.Where(e => e.PaidDate != null),
                "All" => query,
                _ => query.Where(e => e.InvoicedDate == null)
            };

            if (clientFilter != 0)
            {
                query = query.Where(e => e.ClientID == clientFilter);
            }

            int totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return (new List<Expense>(), 0, 0);
            }

            int totalPages = (int)Math.Ceiling((double)totalRecords / recordLimit);
            page = page < 1 ? 1 : page > totalPages ? totalPages : page;

            IOrderedQueryable<Expense> orderedQuery = status == "Unbilled"
                ? query.OrderByDescending(e => e.ExpenseID)
                : query.OrderByDescending(e => e.InvoicedDate).ThenByDescending(e => e.ExpenseID);

            var expenses = await orderedQuery
                .Skip((page - 1) * recordLimit)
                .Take(recordLimit)
                .ToListAsync();

            return (expenses, totalRecords, totalPages);
        }

        public async Task<Expense?> GetExpenseByIdAsync(int id)
        {
            return await _context.Expenses
                .Include(e => e.Client)
                .FirstOrDefaultAsync(e => e.ExpenseID == id);
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateExpenseAsync(Expense expense)
        {
            _context.Add(expense);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateExpenseAsync(Expense expense)
        {
            var existing = await _context.Expenses.FirstOrDefaultAsync(e => e.ExpenseID == expense.ExpenseID);
            if (existing == null)
            {
                return (false, "Expense not found.");
            }

            if (existing.InvoicedDate.HasValue)
            {
                return (false, "This expense has been invoiced and cannot be edited.");
            }

            existing.ClientID = expense.ClientID;
            existing.ProductID = expense.ProductID;
            existing.Description = expense.Description;
            existing.UnitAmount = expense.UnitAmount;
            existing.Quantity = expense.Quantity;
            existing.TotalAmount = expense.TotalAmount;

            try
            {
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ExpenseExistsAsync(expense.ExpenseID))
                {
                    return (false, "Expense not found.");
                }
                throw;
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteExpenseAsync(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
            {
                return (false, "Expense not found.");
            }

            if (expense.InvoicedDate.HasValue)
            {
                return (false, "This expense has been invoiced and cannot be deleted.");
            }

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> ExpenseExistsAsync(int id)
        {
            return await _context.Expenses.AnyAsync(e => e.ExpenseID == id);
        }
    }
}
