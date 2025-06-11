using Microsoft.AspNetCore.Mvc;
using TaskTracker.Data;
using TaskTracker.Services;
using Microsoft.AspNetCore.Identity;
using TaskTracker.Models.Project;

namespace TaskTracker.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly ProjectService _projectService;
        private readonly SetupService _setupService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectsController(
            ProjectService projectService,
            SetupService setupService,
            UserManager<ApplicationUser> userManager)
        {
            _projectService = projectService;
            _setupService = setupService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var setupResult = await _setupService.CheckSetupAsync(userId);
                if (setupResult != null)
                {
                    return setupResult;
                }
            }
            return View(await _projectService.GetAllProjectsAsync());
        }

        public IActionResult Create()
        {
            ViewBag.VisibleCreateForm = true;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Rate")] Project project)
        {
            if (ModelState.IsValid)
            {
                var (success, error) = await _projectService.CreateProjectAsync(project);
                if (success)
                {
                    TempData["SuccessMessage"] = "Project created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = error;
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = string.Join("; ", errors);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProjectID,Name,Description,Rate")] Project project)
        {
            if (id != project.ProjectID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var (success, error) = await _projectService.UpdateProjectAsync(project);
                if (success)
                {
                    TempData["SuccessMessage"] = "Project updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = error ?? "Project not found.";
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = string.Join("; ", errors);
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _projectService.GetProjectByIdAsync(id.Value);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _projectService.DeleteProjectAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}