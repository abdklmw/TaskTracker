using Microsoft.AspNetCore.Identity;
using TaskTracker.Data;

namespace TaskTracker.Services
{
    public interface IUserPreferenceService
    {
        Task<int> GetPreferredClientIdAsync(string userId);
        Task SetPreferredClientAsync(string userId, int clientId);
        Task<int> GetRecordLimitAsync(string userId, string page);
        Task SetRecordLimitAsync(string userId, string page, int recordLimit);
    }

    public class UserPreferenceService : IUserPreferenceService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserPreferenceService> _logger;

        public UserPreferenceService(
            UserManager<ApplicationUser> userManager,
            ILogger<UserPreferenceService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<int> GetPreferredClientIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.PreferredClientId ?? 0;
        }

        public async Task SetPreferredClientAsync(string userId, int clientId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Cannot set preferred client: user {UserId} not found", userId);
                return;
            }

            user.PreferredClientId = clientId;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to save preferred client for user {UserId}: {Errors}",
                    userId, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task<int> GetRecordLimitAsync(string userId, string page)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return 10;

            return page.ToLowerInvariant() switch
            {
                "invoices" => user.InvoicesRecordLimit,
                "timeentries" => user.TimeEntriesRecordLimit,
                "expenses" => user.ExpensesRecordLimit,
                _ => 10
            };
        }

        public async Task SetRecordLimitAsync(string userId, string page, int recordLimit)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Cannot set record limit: user {UserId} not found", userId);
                return;
            }

            switch (page.ToLowerInvariant())
            {
                case "invoices":
                    user.InvoicesRecordLimit = recordLimit;
                    break;
                case "timeentries":
                    user.TimeEntriesRecordLimit = recordLimit;
                    break;
                case "expenses":
                    user.ExpensesRecordLimit = recordLimit;
                    break;
                default:
                    _logger.LogWarning("Unknown page '{Page}' for record limit", page);
                    return;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to save record limit for user {UserId}, page {Page}: {Errors}",
                    userId, page, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
