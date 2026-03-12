using Microsoft.AspNetCore.Mvc;

namespace TaskTracker.Controllers
{
    public class ClientSelectorController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetClient(int globalClientId, string? returnUrl)
        {
            HttpContext.Session.SetInt32("GlobalClientId", globalClientId);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
