using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskTracker.Data;
using TaskTracker.Models;

namespace TaskTracker.Services
{
    public class SetupService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SetupService> _logger;

        public SetupService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<SetupService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> CheckSetupAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                LoggerExtensions.LogError(_logger, "User ID is null or empty.");
                return new RedirectToActionResult("Login", "Account", null);
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                LoggerExtensions.LogError(_logger, "User not found for ID {UserId}", userId);
                return new NotFoundResult();
            }

            // Check if user has a TimeZoneId set
            if (string.IsNullOrEmpty(user.TimeZoneId))
            {
                LoggerExtensions.LogInformation(_logger, "No TimeZoneId set for user {UserId}, redirecting to SetTimezone.", userId);
                return new RedirectToActionResult("SetTimezone", "Home", null);
            }

            // Check if there are any clients
            var hasClients = await _context.Clients.AnyAsync();
            if (!hasClients)
            {
                LoggerExtensions.LogInformation(_logger, "No clients found for user {UserId}, redirecting to Create Client.", userId);
                return new RedirectToActionResult("Create", "Clients", null);
            }

            // Check if there are any projects
            var hasProjects = await _context.Projects.AnyAsync();
            if (!hasProjects)
            {
                LoggerExtensions.LogInformation(_logger, "No projects found for user {UserId}, redirecting to Create Project.", userId);
                return new RedirectToActionResult("Create", "Projects", null);
            }

            // No redirects needed
            return null;
        }
    }
}