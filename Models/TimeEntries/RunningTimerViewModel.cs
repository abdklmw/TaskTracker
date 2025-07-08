using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskTracker.Data;
namespace TaskTracker.Models.TimeEntries
{
    public class RunningTimerViewModel
    {
        public required IEnumerable<TimeEntry> TimeEntries { get; set; }
        public int TimezoneOffset { get; set; } = 0;
    }
}