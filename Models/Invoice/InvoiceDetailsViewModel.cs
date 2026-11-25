using System;
using System.Collections.Generic;

namespace TaskTracker.Models.Invoice
{
    public class InvoiceDetailsViewModel
    {
        public int InvoiceID { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public int ClientID { get; set; }
        public string ClientName { get; set; } = "";
        public InvoiceStatus Status { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime? InvoiceSentDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public decimal StoredTotalAmount { get; set; }
        public decimal TimeEntriesHoursTotal { get; set; }
        public decimal TimeEntriesSubtotal { get; set; }
        public decimal ExpensesSubtotal { get; set; }
        public decimal ComputedTotalAmount => TimeEntriesSubtotal + ExpensesSubtotal;
        public bool HasTotalMismatch => Math.Round(ComputedTotalAmount, 2) != Math.Round(StoredTotalAmount, 2);
        public List<InvoiceDetailsTimeEntryItem> TimeEntries { get; set; } = new();
        public List<InvoiceDetailsExpenseItem> Expenses { get; set; } = new();
    }

    public class InvoiceDetailsTimeEntryItem
    {
        public int TimeEntryID { get; set; }
        public DateOnly Date { get; set; }
        public string ProjectName { get; set; } = "N/A";
        public string Description { get; set; } = "N/A";
        public decimal Hours { get; set; }
        public decimal Rate { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class InvoiceDetailsExpenseItem
    {
        public int InvoiceExpenseID { get; set; }
        public DateOnly Date { get; set; }
        public string Description { get; set; } = "N/A";
        public decimal Quantity { get; set; }
        public decimal UnitAmount { get; set; }
        public decimal LineTotal { get; set; }
    }
}
