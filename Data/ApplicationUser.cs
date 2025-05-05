using Microsoft.AspNetCore.Identity;

namespace TaskTracker.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? TimeZoneId { get; set; } // Timezone identifier (e.g., "Central Standard Time")
    }
}