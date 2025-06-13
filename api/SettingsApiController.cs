using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Models;
using TaskTracker.Services;
using System.Threading.Tasks;

namespace TaskTracker.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SettingsApiController : ControllerBase
    {
        private readonly ISettingsService _settingsService;

        public SettingsApiController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet]
        public async Task<ActionResult<Settings>> GetSettings()
        {
            var settings = await _settingsService.GetSettingsAsync();
            if (settings == null)
            {
                return NotFound(new { Error = "Settings not found." });
            }
            return Ok(settings);
        }

        [HttpPut]
        public async Task<ActionResult> Update([FromBody] Settings settings)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _settingsService.UpdateSettingsAsync(settings);
            if (!success)
            {
                return BadRequest(new { Error = "Failed to update settings." });
            }

            return NoContent();
        }
    }
}