using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace TaskTracker.Models.Invoice
{
    public class InvoiceCreateViewModel
    {
        public int ClientID { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public List<SelectListItem> Clients { get; set; } = new List<SelectListItem>();
        public List<int> SelectedTimeEntryIDs { get; set; } = new List<int>();
        public List<int> SelectedExpenseIDs { get; set; } = new List<int>();
        public List<TimeEntryViewModel> TimeEntries { get; set; } = new List<TimeEntryViewModel>();
        public List<ExpenseViewModel> Expenses { get; set; } = new List<ExpenseViewModel>();
    }

    public class TimeEntryViewModel
    {
        public int TimeEntryID { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal HoursSpent { get; set; }
        public decimal TotalAmount { get; set; }
        public string Description { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public bool IsSelected { get; set; }
        public string RateSource { get; set; } // "Project", "Client", or "Settings"
        public int? ProjectID { get; set; }    // Added
        public int? ClientID { get; set; }     // Added
    }

    public class ExpenseViewModel
    {
        public int ExpenseID { get; set; }
        public string Description { get; set; }
        public decimal UnitAmount { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsSelected { get; set; }
    }
}