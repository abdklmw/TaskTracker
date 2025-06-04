using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using TaskTracker.Models.TimeEntries;

namespace TaskTracker.Models.Project
{
    public class Project
    {
        public int ProjectID { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        [Column(TypeName = "decimal(18,2)"), DisplayName("Project Rate")]
        public decimal Rate { get; set; }
        public ICollection<TimeEntry>? TimeEntries { get; set; }
    }
}