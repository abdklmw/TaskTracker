using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace TaskTracker.Models
{
    public class Client
    {
        public int ClientID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; } // Made nullable
        public string? Address { get; set; } // Made nullable
        [Column(TypeName = "decimal(18,2)"), DisplayName("Rate"), Description("Default rate for client. Project specific rate, where provided, will override.")]
        public decimal DefaultRate { get; set; }

        public ICollection<Project>? Projects { get; set; }
    }
}