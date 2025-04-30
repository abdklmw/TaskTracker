using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskTracker.Models
{
    public class TimeEntry
    {
        public int TimeEntryID { get; set; }

        [Required]
        public int ProjectID { get; set; }
        public Project Project { get; set; }

        [Required]
        public int ClientID { get; set; }
        public Client Client { get; set; }

        [Required]
        public DateTime StartDateTime { get; set; }

        public DateTime? EndDateTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? HoursSpent { get; set; }

        public string? Description { get; set; }
    }
}