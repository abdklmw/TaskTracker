using System.ComponentModel.DataAnnotations.Schema;

namespace TaskTracker.Models
{

    public class InvoiceItem
    {
        public int InvoiceItemID { get; set; }
        public int InvoiceID { get; set; }
        public string Description { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        public Invoice Invoice { get; set; }
    }

}
