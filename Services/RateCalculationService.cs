using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models;

namespace TaskTracker.Services
{
    public class RateCalculationService
    {
        private readonly AppDbContext _context;

        public RateCalculationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetHourlyRateAsync(int? projectId, int? clientId)
        {
            // Try Project rate first
            if (projectId.HasValue)
            {
                var project = await _context.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProjectID == projectId.Value);
                if (project != null && project.Rate != 0m)
                {
                    return project.Rate;
                }
            }

            // Then Client rate
            if (clientId.HasValue)
            {
                var client = await _context.Clients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ClientID == clientId.Value);
                if (client != null)
                {
                    return client.DefaultRate;
                }
            }

            // Fallback to Settings default rate
            var settings = await _context.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync();
            return settings?.DefaultHourlyRate ?? 0m;
        }
    }
}