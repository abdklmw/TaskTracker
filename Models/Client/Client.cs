using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace TaskTracker.Models.Client
{
    public class Client
    {
        public int ClientID { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }

        [StringLength(100)]
        [DisplayName("Accounts Receivable Name")]
        public string? AccountsReceivableName { get; set; }

        [Column(TypeName = "decimal(18,2)"), DisplayName("Rate"), Description("Default rate for client. Project specific rate, where provided, will override.")]
        public decimal DefaultRate { get; set; }

        public ICollection<Project.Project>? Projects { get; set; }
    }
}