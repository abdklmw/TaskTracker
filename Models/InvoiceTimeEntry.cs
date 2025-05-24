using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TaskTracker.Models
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
        public virtual TimeEntry? TimeEntry { get; set; }
    }
}