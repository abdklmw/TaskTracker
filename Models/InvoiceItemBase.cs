using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace TaskTracker.Models
{
    public abstract class InvoiceItemBase
    {
        [Required]
        public int InvoiceID { get; set; }

        [StringLength(500)]
        [Description("Additional notes for the invoice item")]
        public string? AdditionalNotes { get; set; }

        public virtual Invoice? Invoice { get; set; }
    }
}