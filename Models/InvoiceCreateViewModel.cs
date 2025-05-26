using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskTracker.Models
{
    public class InvoiceCreateViewModel
    {
        public int ClientID { get; set; }
        [Required]
        public InvoiceStatus Status { get; set; }
        public List<SelectListItem> Clients { get; set; } = new List<SelectListItem>();
        public List<TimeEntryViewModel> TimeEntries { get; set; } = new List<TimeEntryViewModel>();
        public List<ExpenseViewModel> Expenses { get; set; } = new List<ExpenseViewModel>();
        public List<int> SelectedTimeEntryIDs { get; set; } = new List<int>();
        public List<int> SelectedExpenseIDs { get; set; } = new List<int>();
    }

    public class TimeEntryViewModel
    {
        public int TimeEntryID { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal HoursSpent { get; set; }
        public decimal TotalAmount { get; set; }
        public string Description { get; set; }
        public DateTime StartDateTime { get; set; }
        public bool IsSelected { get; set; }
    }

    public class ExpenseViewModel
    {
        public int ExpenseID { get; set; }
        public string Description { get; set; }
        public decimal UnitAmount { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsSelected { get; set; }
    }
}