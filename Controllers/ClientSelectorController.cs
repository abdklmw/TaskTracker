using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Services;

namespace TaskTracker.Controllers
{
    public class ClientSelectorController : Controller
    {
        private readonly IUserPreferenceService _preferenceService;

        public ClientSelectorController(IUserPreferenceService preferenceService)
        {
            _preferenceService = preferenceService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetClient(int globalClientId, string? returnUrl)
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            if (!string.IsNullOrEmpty(userId))
            {
                await _preferenceService.SetPreferredClientAsync(userId, globalClientId);
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
