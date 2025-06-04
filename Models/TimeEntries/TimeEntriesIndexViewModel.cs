using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using TaskTracker.Models.TimeEntries;

namespace TaskTracker.Models
{
    public class TimeEntriesIndexViewModel
    {
        public List<TimeEntry> TimeEntries { get; set; }
        public List<TimeEntry> RunningTimers { get; set; }
        public int TimezoneOffset { get; set; }
        public bool VisibleCreateForm { get; set; }
        public string ReturnTo { get; set; }
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int RecordLimit { get; set; }
        public SelectList RecordLimitOptions { get; set; }
        public SelectList ClientList { get; set; }
        public SelectList ProjectList { get; set; }
        // Filter properties
        public int SelectedClientID { get; set; }
        public List<int> SelectedProjectIDs { get; set; } = new List<int>();
        public SelectList ClientFilterOptions { get; set; }
        public MultiSelectList ProjectFilterOptions { get; set; }
    }
}