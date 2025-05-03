using Microsoft.AspNetCore.Identity;

namespace TaskTracker.Data
{
    public class ApplicationUser : IdentityUser
    {
        public int? TimezoneOffset { get; set; } // Offset in minutes (e.g., -300 for EDT)
    }
}
