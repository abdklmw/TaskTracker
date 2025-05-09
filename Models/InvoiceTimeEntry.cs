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
        [StringLength(500)]
        [Description("Additional notes for the invoice time entry")]
        public virtual required TimeEntry TimeEntry { get; set; }
    }
}