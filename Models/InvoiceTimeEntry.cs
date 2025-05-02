using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Models
{
    public class InvoiceTimeEntry
    {
        [Required]
        public int InvoiceID { get; set; }

        [Required]
        public int TimeEntryID { get; set; }

        public virtual Invoice Invoice { get; set; }
        public virtual TimeEntry TimeEntry { get; set; }
    }
}