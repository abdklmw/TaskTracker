using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models.Expense;

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

        public async Task<int> CountExpensesForClientAsync(int clientFilter)
        {
            var query = _context.Expenses.AsQueryable();
            if (clientFilter != 0)
            {
                query = query.Where(e => e.ClientID == clientFilter);
            }
            return await query.CountAsync();
        }

        public async Task<(List<Expense> Expenses, int TotalRecords, int TotalPages)> GetExpensesAsync(
            int page,
            int recordLimit,
            int clientFilter,
            string status = "Unbilled",
            int[]? productFilter = null,
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
            status = NormalizeStatus(status);

            var query = _context.Expenses
                .Include(e => e.Client)
                .Include(e => e.Product)
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

            var productIds = productFilter?.Where(id => id != 0).Distinct().ToArray() ?? Array.Empty<int>();
            if (productIds.Length > 0)
            {
                query = query.Where(e => productIds.Contains(e.ProductID));
            }

            if (expenseDateAny)
            {
                query = query.Where(e => e.ExpenseDate != null);
            }
            else
            {
                if (expenseDateStart.HasValue)
                {
                    query = query.Where(e => e.ExpenseDate >= expenseDateStart.Value.Date);
                }
                if (expenseDateEnd.HasValue)
                {
                    query = query.Where(e => e.ExpenseDate <= expenseDateEnd.Value.Date);
                }
            }

            if (invoicedDateAny)
            {
                query = query.Where(e => e.InvoicedDate != null);
            }
            else
            {
                if (invoicedDateStart.HasValue)
                {
                    query = query.Where(e => e.InvoicedDate >= invoicedDateStart.Value.Date);
                }
                if (invoicedDateEnd.HasValue)
                {
                    query = query.Where(e => e.InvoicedDate <= invoicedDateEnd.Value.Date);
                }
            }

            if (invoiceSentDateAny)
            {
                query = query.Where(e => e.InvoiceSent != null);
            }
            else
            {
                if (invoiceSentDateStart.HasValue)
                {
                    query = query.Where(e => e.InvoiceSent >= invoiceSentDateStart.Value.Date);
                }
                if (invoiceSentDateEnd.HasValue)
                {
                    query = query.Where(e => e.InvoiceSent <= invoiceSentDateEnd.Value.Date);
                }
            }

            if (paidDateAny)
            {
                query = query.Where(e => e.PaidDate != null);
            }
            else
            {
                if (paidDateStart.HasValue)
                {
                    query = query.Where(e => e.PaidDate >= paidDateStart.Value.Date);
                }
                if (paidDateEnd.HasValue)
                {
                    query = query.Where(e => e.PaidDate <= paidDateEnd.Value.Date);
                }
            }

            int totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return (new List<Expense>(), 0, 0);
            }

            int pageSize = recordLimit == -1 ? 200 : (recordLimit > 0 ? recordLimit : 10);
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            page = page < 1 ? 1 : page > totalPages ? totalPages : page;

            IOrderedQueryable<Expense> orderedQuery = status == "Unbilled"
                ? query.OrderByDescending(e => e.ExpenseDate).ThenByDescending(e => e.ExpenseID)
                : query.OrderByDescending(e => e.InvoicedDate).ThenByDescending(e => e.ExpenseID);

            var expenses = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
            if (!expense.ExpenseDate.HasValue || expense.ExpenseDate.Value == default)
            {
                expense.ExpenseDate = DateTime.Today;
            }
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
            existing.ExpenseDate = expense.ExpenseDate;

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
