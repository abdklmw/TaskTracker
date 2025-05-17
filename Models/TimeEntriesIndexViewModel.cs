using System.Collections.Generic;
using TaskTracker.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskTracker.Models
{
    public class TimeEntriesIndexViewModel
    {
        public List<TimeEntry> TimeEntries { get; set; }
        public List<TimeEntry> RunningTimers { get; set; }
        public int RecordLimit { get; set; } // Selected number of records (5, 10, 20, 50, 100, 200) or -1 for ALL
        public int CurrentPage { get; set; } // Current page number (for ALL)
        public int TotalPages { get; set; } // Total pages (for ALL)
        public int TotalRecords { get; set; } // Total completed time entries
        public SelectList RecordLimitOptions { get; set; } // Dropdown options
        public SelectList ClientList { get; set; } // For create form
        public SelectList ProjectList { get; set; } // For create form
        public int TimezoneOffset { get; set; } // For time display
        public string ReturnTo { get; set; } // For form redirect
        public bool VisibleCreateForm { get; set; } // Form visibility
    }
}