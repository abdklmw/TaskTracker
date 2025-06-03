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

        [StringLength(100)]
        [DisplayName("SMTP Server")]
        public string? SmtpServer { get; set; }

        [Range(1, 65535, ErrorMessage = "SMTP Port must be between 1 and 65535.")]
        [DisplayName("SMTP Port")]
        public int? SmtpPort { get; set; }

        [EmailAddress]
        [StringLength(100)]
        [DisplayName("SMTP Sender Email")]
        public string? SmtpSenderEmail { get; set; }

        [StringLength(100)]
        [DisplayName("SMTP Username")]
        public string? SmtpUsername { get; set; }

        [StringLength(100)]
        [DataType(DataType.Password)]
        [DisplayName("SMTP Password")]
        public string? SmtpPassword { get; set; }

        [DataType(DataType.Html)]
        [DisplayName("Invoice Template")]
        public string InvoiceTemplate { get; set; } = @"
            <!DOCTYPE html>
            <html>
            <head>
            <style>
            body { font-family: Helvetica, Arial, sans-serif; font-size: 12px; color: #333; }
            .header { overflow: auto; margin-bottom: 20px; }
            .company-info { float: left; text-align: left; }
            .payment-info { float: right; text-align: right; }
            .title { text-align: center; font-size: 18px; font-weight: bold; margin-bottom: 20px; clear: both; }
            .company-name { font-size: 1.2em; font-weight: bold; }
            .client-info { margin-bottom: 20px; }
            table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
            th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
            th { background-color: #f4f4f4; font-weight: bold; }
            .total { text-align: right; font-size: 14px; font-weight: bold; }
            .footer { margin-top: 20px; }
            </style>
            </head>
            <body>
            <div class='header'>
            <div class='company-info'>
            <p class='company-name'>{{CompanyName}}</p>
            <p>{{AccountsReceivableAddress}}</p>
            <p>{{AccountsReceivablePhone}}</p>
            <p>{{AccountsReceivableEmail}}</p>
            </div>
            <div class='payment-info'>
            <p>Payment Information</p>
            <p>{{PaymentInformation}}</p>
            </div>
            </div>
            <div class='title'>Invoice #{{InvoiceID}}</div>
            <div class='client-info'>
            <p>Billed To: {{ClientName}}</p>
            <p>Invoice Date: {{InvoiceDate}}</p>
            <p>Total Amount: ${{TotalAmount}}</p>
            </div>
            {{TimeEntriesTable}}
            {{ExpensesTable}}
            <div class='total'>Total: ${{TotalAmount}}</div>
            <div class='footer'>
            </div>
            </body>
            </html>";

        public int SingletonGuard { get; set; } = 0;
    }
}