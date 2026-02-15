using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskTracker.Models.Expense
{
    public class ExpenseCreateViewModel
    {
        [Required]
        public int ClientID { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [Range(0, 1000000, ErrorMessage = "Unit Amount must be between 0 and 1,000,000.")]
        public decimal UnitAmount { get; set; }

        [Required]
        [Range(0.01, 999999.99, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; } = 1;

        [Required]
        public int ProductID { get; set; }

        public List<SelectListItem> Clients { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Products { get; set; } = new List<SelectListItem>();
    }
}