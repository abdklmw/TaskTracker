using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TaskTracker.Data;
using TaskTracker.Models;

namespace TaskTracker.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(AppDbContext context, UserManager<ApplicationUser> userManager, ILogger<HomeController> logger)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);

                // Populate ClientID dropdown
                var clientList = _context.Clients
                    .Select(c => new { c.ClientID, c.Name })
                    .ToList();
                clientList.Insert(0, new { ClientID = 0, Name = "Select Client" });
                ViewBag.ClientID = new SelectList(clientList, "ClientID", "Name", 0);

                // Populate ProjectID dropdown
                var projectList = _context.Projects
                    .Select(p => new { p.ProjectID, p.Name })
                    .ToList();
                projectList.Insert(0, new { ProjectID = 0, Name = "Select Project" });
                ViewBag.ProjectID = new SelectList(projectList, "ProjectID", "Name", 0);
                ViewBag.VisibleCreateForm = true;
                ViewBag.ReturnTo = "Home";
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
