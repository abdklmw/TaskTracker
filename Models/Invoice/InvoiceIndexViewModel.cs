using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskTracker.Models
{
    public class InvoiceIndexViewModel : IPaginationViewModel
    {
        public IEnumerable<Invoice.Invoice> Invoices { get; set; } = new List<Invoice.Invoice>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;
        public int RecordLimit { get; set; } = 10;
        public int SelectedClientID { get; set; } = 0;
        public Invoice.InvoiceStatus? SelectedStatus { get; set; }
        public DateTime? PaidDateStart { get; set; }
        public DateTime? PaidDateEnd { get; set; }
        public DateTime? InvoiceDateStart { get; set; }
        public DateTime? InvoiceDateEnd { get; set; }
        public DateTime? InvoiceSentDateStart { get; set; }
        public DateTime? InvoiceSentDateEnd { get; set; }
        public decimal? TotalAmountMin { get; set; }
        public decimal? TotalAmountMax { get; set; }
        public IDictionary<string, string> RouteValues { get; set; } = new Dictionary<string, string>();
        public IEnumerable<SelectListItem> ClientFilterOptions { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> StatusFilterOptions { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> RecordLimitOptions { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "5", Text = "5" },
            new SelectListItem { Value = "10", Text = "10" },
            new SelectListItem { Value = "25", Text = "25" },
            new SelectListItem { Value = "50", Text = "50" },
            new SelectListItem { Value = "100", Text = "100" }
        };
    }
}