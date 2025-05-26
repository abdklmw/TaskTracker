using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace TaskTracker.Models
{
    public class Settings
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        [DisplayName("Company Name")]
        public string CompanyName { get; set; }
        [DataType(DataType.MultilineText)]
        [DisplayName("Accounts Receivable Address")]
        public string? AccountsReceivableAddress { get; set; }
        [StringLength(15)]
        [DisplayName("Accounts Receivable Phone")]
        public string? AccountsReceivablePhone { get; set; }
        [EmailAddress]
        [DisplayName("Accounts Receivable Email")]
        public string? AccountsReceivableEmail { get; set; }
        [DataType(DataType.MultilineText)]
        [DisplayName("Payment Information Message")]
        public string? PaymentInformation { get; set; }
        [DataType(DataType.MultilineText)]
        [DisplayName("Thank You Message")]
        public string? ThankYouMessage { get; set; }
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("Default Hourly Rate")]
        [Range(0, 10000, ErrorMessage = "Default Hourly Rate must be between 0 and 10,000.")]
        public decimal DefaultHourlyRate { get; set; }
        public int SingletonGuard { get; set; } = 0;
    }
}