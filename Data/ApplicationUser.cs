using Microsoft.AspNetCore.Identity;

namespace TaskTracker.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? TimeZoneId { get; set; } // Timezone identifier (e.g., "Central Standard Time")

        // User preferences — persisted so they survive browser/device changes and app pool recycles
        public int PreferredClientId { get; set; } // 0 = "All Clients"
        public int InvoicesRecordLimit { get; set; } = 10;
        public int TimeEntriesRecordLimit { get; set; } = 10;
        public int ExpensesRecordLimit { get; set; } = 10;
    }
}