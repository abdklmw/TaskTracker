using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskTracker.Models.Invoice;

namespace TaskTracker.Models.Product
{
    public class Product
    {
        public int ProductID { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [Required]
        [StringLength(100)]
        [Description("Product Sku")]
        public required string ProductSku { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public virtual ICollection<InvoiceExpense> InvoiceProducts { get; set; } = new List<InvoiceExpense>();
    }
}