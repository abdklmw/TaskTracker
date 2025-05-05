using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Models
{
    public class SetTimezoneViewModel
    {
        [Required(ErrorMessage = "Please select a timezone.")]
        public string TimeZoneId { get; set; }
    }
}