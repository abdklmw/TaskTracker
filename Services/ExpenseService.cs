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

        public async Task<(List<Expense> Expenses, int TotalRecords, int TotalPages)> GetExpensesAsync(int page, int recordLimit, int clientFilter)
        {
            var query = _context.Expenses
                .Where(e => e.InvoicedDate == null)
                .Include(e => e.Client)
                .AsQueryable();

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

            var expenses = await query
                .OrderByDescending(e => e.InvoicedDate)
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
            try
            {
                _context.Update(expense);
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