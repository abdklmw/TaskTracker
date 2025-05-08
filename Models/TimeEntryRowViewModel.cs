namespace TaskTracker.Models
{
    public class TimeEntryRowViewModel
    {
        public TimeEntry? TimeEntry { get; set; }
        public string Parity { get; set; }
        public TimeEntryRowViewModel()
        {
            this.Parity = "odd";
        }
        public TimeEntryRowViewModel(TimeEntry TimeEntry, int index)
        {
            this.Parity = (index % 2 == 0) ? "even" : "odd";
            this.TimeEntry = TimeEntry;
        }
    }
}
