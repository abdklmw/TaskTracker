namespace TaskTracker.Models.TimeEntries
{
    public class TimeEntryRowViewModel
    {
        public TimeEntry? TimeEntry { get; set; }
        public string Parity { get; set; }
        public TimeEntryRowViewModel()
        {
            Parity = "odd";
        }
        public TimeEntryRowViewModel(TimeEntry TimeEntry, int index)
        {
            Parity = index % 2 == 0 ? "even" : "odd";
            this.TimeEntry = TimeEntry;
        }
    }
}
