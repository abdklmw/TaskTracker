using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskTracker.Models
{
    public class InvoiceProduct
    {
        [Required]
        public int InvoiceID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public int Quantity { get; set; }

        public decimal TotalAmount
        {
            get
            {
                return Quantity * Product.UnitPrice;
            }
        }

        public virtual required Invoice Invoice { get; set; }
        public virtual required Product Product { get; set; }
    }
}