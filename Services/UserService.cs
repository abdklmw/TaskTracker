using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TaskTracker.Data;

namespace TaskTracker.Services
{
    public interface IUserService
    {
        Task<int> GetUserTimezoneOffsetAsync(string userId);
    }

    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserService> _logger;

        public UserService(UserManager<ApplicationUser> userManager, ILogger<UserService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<int> GetUserTimezoneOffsetAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID is null or empty.");
                return 0;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.TimeZoneId))
            {
                _logger.LogInformation("User {UserId} not found or TimeZoneId not set.", userId);
                return 0;
            }

            try
            {
                var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                var nowUtc = DateTimeOffset.UtcNow.UtcDateTime;
                int offset = (int)userTimeZone.GetUtcOffset(nowUtc).TotalMinutes;
                _logger.LogInformation("Timezone offset for user {UserId}: {Offset} minutes, TimeZoneId: {TimeZoneId}", userId, offset, user.TimeZoneId);
                return offset;
            }
            catch (TimeZoneNotFoundException ex)
            {
                _logger.LogError(ex, "Invalid TimeZoneId {TimeZoneId} for user {UserId}", user.TimeZoneId, userId);
                return 0;
            }
        }
    }
}