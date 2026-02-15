using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskTracker.Models.Product;

namespace TaskTracker.Models.Expense
{
    public class Expense
    {
        public int ExpenseID { get; set; }

        [Required]
        public int ClientID { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitAmount { get; set; }

        [Required]
        [Range(0.01, 999999.99, ErrorMessage = "Quantity must be greater than 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; } = 1;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "date")]
        public DateTime? InvoicedDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime? InvoiceSent { get; set; }

        [Column(TypeName = "date")]
        public DateTime? PaidDate { get; set; }

        [Required]
        public int ProductID { get; set; }

        public virtual Product.Product? Product { get; set; }

        public virtual Client.Client? Client { get; set; }
    }
}