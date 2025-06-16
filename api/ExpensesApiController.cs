using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Models.Expense;
using TaskTracker.Services;

namespace TaskTracker.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ExpensesApiController : ControllerBase
    {
        private readonly ExpenseService _expenseService;

        public ExpensesApiController(ExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int recordLimit = 10, [FromQuery] int clientFilter = 0)
        {
            var (expenses, totalRecords, totalPages) = await _expenseService.GetExpensesAsync(page, recordLimit, clientFilter);
            return Ok(new { Expenses = expenses, TotalRecords = totalRecords, TotalPages = totalPages });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var expense = await _expenseService.GetExpenseByIdAsync(id);
            if (expense == null)
            {
                return NotFound();
            }
            return Ok(expense);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Expense expense)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, error) = await _expenseService.CreateExpenseAsync(expense);
            if (!success)
            {
                return BadRequest(new { Error = error });
            }

            return CreatedAtAction(nameof(GetById), new { id = expense.ExpenseID }, expense);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] Expense expense)
        {
            if (id != expense.ExpenseID || !ModelState.IsValid)
            {
                return BadRequest();
            }

            var (success, error) = await _expenseService.UpdateExpenseAsync(expense);
            if (!success)
            {
                return BadRequest(new { Error = error });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var (success, error) = await _expenseService.DeleteExpenseAsync(id);
            if (!success)
            {
                return NotFound(new { Error = error });
            }

            return NoContent();
        }
    }
}