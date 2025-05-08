using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Models
{
    public class InvoiceTimeEntry
    {
        [Required]
        public int InvoiceID { get; set; }

        [Required]
        public int TimeEntryID { get; set; }

        public decimal? TotalAmount
        {
            get
            {
                if (TimeEntry?.HourlyRate != null && TimeEntry?.HoursSpent != null)
                {
                    return TimeEntry.HourlyRate * TimeEntry.HoursSpent;
                }
                return 0;
            }
        }

        public virtual required Invoice Invoice { get; set; }
        public virtual required TimeEntry TimeEntry { get; set; }
    }
}