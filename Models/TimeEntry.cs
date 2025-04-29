using System.ComponentModel.DataAnnotations.Schema;

namespace TaskTracker.Models
{
    public class TimeEntry
    {
        public int TimeEntryID { get; set; }
        public int ProjectID { get; set; }
        public DateTime Date { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal HoursSpent { get; set; }
        public string Description { get; set; }
        public Project Project { get; set; }

    }
}
