using System.ComponentModel.DataAnnotations.Schema;

namespace TaskTracker.Models
{

    public class Invoice
    {
        public int InvoiceID { get; set; }
        public int ClientID { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public Client Client { get; set; }
        public ICollection<InvoiceItem> InvoiceItems { get; set; }
    }

}
