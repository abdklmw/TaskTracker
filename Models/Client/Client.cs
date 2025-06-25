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

        [StringLength(500)]
        [DisplayName("CC Addresses")]
        [RegularExpression(@"^(([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})(\s*;\s*([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}))*)?$", ErrorMessage = "CC must be a semi-colon separated list of valid email addresses.")]
        public string? CC { get; set; }

        [StringLength(500)]
        [DisplayName("BCC Addresses")]
        [RegularExpression(@"^(([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})(\s*;\s*([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}))*)?$", ErrorMessage = "BCC must be a semi-colon separated list of valid email addresses.")]
        public string? BCC { get; set; }

        public ICollection<Project.Project>? Projects { get; set; }
    }
}