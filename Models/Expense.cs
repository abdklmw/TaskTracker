using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskTracker.Models
{
    // Represents an expense assigned to a client with product details
    public class Expense
    {
        // Primary key for the expense
        public int ExpenseID { get; set; }

        // Foreign key to associate the expense with a client
        [Required]
        public int ClientID { get; set; }

        // Description of the expense (e.g., "SKU123 - Widget")
        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        // Unit price of the expense item (e.g., product unit price)
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitAmount { get; set; }

        // Quantity of items (default 1)
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;

        // Total amount calculated as Quantity * UnitAmount
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // Navigation property to access the associated client
        public virtual Client? Client { get; set; }
    }
}