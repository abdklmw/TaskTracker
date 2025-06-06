using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace TaskTracker.Models
{
    public class TimeEntriesIndexViewModel : IPaginationViewModel
    {
        public IEnumerable<TimeEntries.TimeEntry> TimeEntries { get; set; } = new List<TimeEntries.TimeEntry>();
        public IEnumerable<TimeEntries.TimeEntry> RunningTimers { get; set; } = new List<TimeEntries.TimeEntry>();
        public int TimezoneOffset { get; set; } = 0;
        public bool VisibleCreateForm { get; set; } = false;
        public string ReturnTo { get; set; } = "TimeEntries";
        public int TotalRecords { get; set; } = 0;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int RecordLimit { get; set; } = 10;
        public IEnumerable<SelectListItem> RecordLimitOptions { get; set; } = new List<SelectListItem>();
        public SelectList ClientList { get; set; } = new SelectList(new List<SelectListItem>());
        public SelectList ProjectList { get; set; } = new SelectList(new List<SelectListItem>());
        public IDictionary<string, string> RouteValues { get; set; } = new Dictionary<string, string>();
        public int SelectedClientID { get; set; } = 0;
        public List<int> SelectedProjectIDs { get; set; } = new List<int>();
        public IEnumerable<SelectListItem> ClientFilterOptions { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> ProjectFilterOptions { get; set; } = new List<SelectListItem>();
        public DateTime? InvoicedDateStart { get; set; }
        public DateTime? InvoicedDateEnd { get; set; }
        public DateTime? PaidDateStart { get; set; }
        public DateTime? PaidDateEnd { get; set; }
        public DateTime? InvoiceSentDateStart { get; set; }
        public DateTime? InvoiceSentDateEnd { get; set; }
    }
}