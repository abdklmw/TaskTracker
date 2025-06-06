namespace TaskTracker.Models.TimeEntries
{
    public class TimeEntryRowViewModel
    {
        public TimeEntry? TimeEntry { get; set; }
        public string Parity { get; set; }
        public int TimezoneOffset { get; set; } = 0;
        public TimeEntryRowViewModel()
        {
            Parity = "odd";
        }
        public TimeEntryRowViewModel(TimeEntry TimeEntry, int index, int timezoneOffest = 0)
        {
            Parity = index % 2 == 0 ? "even" : "odd";
            this.TimeEntry = TimeEntry;
            this.TimezoneOffset = timezoneOffest;
        }
    }
}
