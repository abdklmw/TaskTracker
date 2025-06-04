using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using TaskTracker.Models.TimeEntries;
namespace TaskTracker.Models.Invoice
{
    public class InvoiceTimeEntry : InvoiceItemBase
    {
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
        public virtual required TimeEntry TimeEntry { get; set; }
    }
}