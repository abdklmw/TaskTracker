using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskTracker.Models
{
    public class ExpenseIndexViewModel : IPaginationViewModel
    {
        public IEnumerable<Expense.Expense> Expenses { get; set; } = new List<Expense.Expense>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;
        public int ClientExpenseCount { get; set; } = 0;
        public string? ClientName { get; set; }
        public int RecordLimit { get; set; } = 10;
        public int SelectedClientID { get; set; } = 0;
        public string Status { get; set; } = "Unbilled";
        public List<int> SelectedProductIDs { get; set; } = new();
        public DateTime? ExpenseDateStart { get; set; }
        public DateTime? ExpenseDateEnd { get; set; }
        public DateTime? InvoicedDateStart { get; set; }
        public DateTime? InvoicedDateEnd { get; set; }
        public DateTime? PaidDateStart { get; set; }
        public DateTime? PaidDateEnd { get; set; }
        public DateTime? InvoiceSentDateStart { get; set; }
        public DateTime? InvoiceSentDateEnd { get; set; }
        public bool ExpenseDateAny { get; set; }
        public bool InvoicedDateAny { get; set; }
        public bool PaidDateAny { get; set; }
        public bool InvoiceSentDateAny { get; set; }
        public IDictionary<string, string> RouteValues { get; set; } = new Dictionary<string, string>();
        public IEnumerable<SelectListItem> ClientFilterOptions { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> StatusFilterOptions { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> ProductFilterOptions { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> RecordLimitOptions { get; set; } = new List<SelectListItem>();

        public bool HasFilters =>
            SelectedClientID != 0
            || !string.Equals(Status, "Unbilled", StringComparison.OrdinalIgnoreCase)
            || SelectedProductIDs.Any()
            || ExpenseDateStart.HasValue || ExpenseDateEnd.HasValue
            || InvoicedDateStart.HasValue || InvoicedDateEnd.HasValue
            || PaidDateStart.HasValue || PaidDateEnd.HasValue
            || InvoiceSentDateStart.HasValue || InvoiceSentDateEnd.HasValue
            || ExpenseDateAny || InvoicedDateAny || PaidDateAny || InvoiceSentDateAny;
    }
}
