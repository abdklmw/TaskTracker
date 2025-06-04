using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskTracker.Data;
namespace TaskTracker.Models.TimeEntries
{
    public class TimeEntry
    {
        public int TimeEntryID { get; set; }
        [Required]
        public int ProjectID { get; set; }
        public Project? Project { get; set; }
        [Required]
        public int ClientID { get; set; }
        public Client? Client { get; set; }
        [Required]
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        [Required]
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? HoursSpent { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? HourlyRate { get; set; }
        public string? Description { get; set; }
        [Column(TypeName = "date")]
        public DateTime? InvoicedDate { get; set; }
        [Column(TypeName = "date")]
        public DateTime? InvoiceSent { get; set; }
        [Column(TypeName = "date")]
        public DateTime? PaidDate { get; set; }
    }
}