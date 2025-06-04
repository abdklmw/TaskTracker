using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskTracker.Models
{
    public class ExpenseIndexViewModel
    {
        public IEnumerable<Expense.Expense> Expenses { get; set; } = new List<Expense.Expense>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;
        public int RecordLimit { get; set; } = 10;
        public int SelectedClientID { get; set; } = 0;
        public IEnumerable<SelectListItem> ClientFilterOptions { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> RecordLimitOptions { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "10", Text = "10" },
            new SelectListItem { Value = "25", Text = "25" },
            new SelectListItem { Value = "50", Text = "50" },
            new SelectListItem { Value = "100", Text = "100" }
        };
    }
}