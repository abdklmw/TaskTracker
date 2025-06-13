using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Data;
using TaskTracker.Services;
using TaskTracker.Models.Project;

namespace TaskTracker.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectsApiController : ControllerBase
    {
        private readonly ProjectService _projectService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectsApiController(ProjectService projectService, UserManager<ApplicationUser> userManager)
        {
            _projectService = projectService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            return Ok(projects);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            return Ok(project);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Project project)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, error) = await _projectService.CreateProjectAsync(project);
            if (!success)
            {
                return BadRequest(new { Error = error });
            }

            return CreatedAtAction(nameof(GetById), new { id = project.ProjectID }, project);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] Project project)
        {
            if (id != project.ProjectID || !ModelState.IsValid)
            {
                return BadRequest();
            }

            var (success, error) = await _projectService.UpdateProjectAsync(project);
            if (!success)
            {
                return BadRequest(new { Error = error });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var success = await _projectService.DeleteProjectAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}