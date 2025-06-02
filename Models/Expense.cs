using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace TaskTracker.Models
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
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        [Column(TypeName = "date")]
        public DateTime? InvoicedDate { get; set; }
        [Column(TypeName = "date")]
        public DateTime? InvoiceSent { get; set; }
        [Column(TypeName = "date")]
        public DateTime? PaidDate { get; set; }
        public virtual Client? Client { get; set; }
    }
}