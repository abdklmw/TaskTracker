using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Data;
using TaskTracker.Models.TimeEntries;
using TaskTracker.Services;

namespace TaskTracker.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TimeEntriesApiController : ControllerBase
    {
        private readonly TimeEntryService _timeEntryService;
        private readonly UserManager<ApplicationUser> _userManager;

        public TimeEntriesApiController(TimeEntryService timeEntryService, UserManager<ApplicationUser> userManager)
        {
            _timeEntryService = timeEntryService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll(
            [FromQuery] int recordLimit = 10,
            [FromQuery] int page = 1,
            [FromQuery] int clientFilter = 0,
            [FromQuery] int[] projectFilter = null,
            [FromQuery] DateTime? invoicedDateStart = null,
            [FromQuery] DateTime? invoicedDateEnd = null,
            [FromQuery] DateTime? paidDateStart = null,
            [FromQuery] DateTime? paidDateEnd = null,
            [FromQuery] DateTime? invoiceSentDateStart = null,
            [FromQuery] DateTime? invoiceSentDateEnd = null,
            [FromQuery] bool invoicedDateAny = false,
            [FromQuery] bool paidDateAny = false,
            [FromQuery] bool invoiceSentDateAny = false)
        {
            var userId = _userManager.GetUserId(User);
            var (timeEntries, totalRecords, totalPages) = await _timeEntryService.GetTimeEntriesAsync(
                userId, recordLimit, page, clientFilter, projectFilter,
                invoicedDateStart, invoicedDateEnd, paidDateStart, paidDateEnd,
                invoiceSentDateStart, invoiceSentDateEnd,
                invoicedDateAny, paidDateAny, invoiceSentDateAny);
            return Ok(new { TimeEntries = timeEntries, TotalRecords = totalRecords, TotalPages = totalPages });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var userId = _userManager.GetUserId(User);
            var timeEntry = await _timeEntryService.GetTimeEntryByIdAsync(id, userId);
            if (timeEntry == null)
            {
                return NotFound();
            }
            return Ok(timeEntry);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] TimeEntry timeEntry)
        {
            var userId = _userManager.GetUserId(User);
            timeEntry.UserId = userId;
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, error) = await _timeEntryService.CreateTimeEntryAsync(timeEntry);
            if (!success)
            {
                return BadRequest(new { Error = error });
            }

            return CreatedAtAction(nameof(GetById), new { id = timeEntry.TimeEntryID }, timeEntry);
        }

        [HttpPost("start-timer")]
        public async Task<ActionResult> StartTimer([FromBody] TimeEntry timeEntry)
        {
            var userId = _userManager.GetUserId(User);
            timeEntry.UserId = userId;
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, error) = await _timeEntryService.StartTimerAsync(timeEntry);
            if (!success)
            {
                return BadRequest(new { Error = error });
            }

            return CreatedAtAction(nameof(GetById), new { id = timeEntry.TimeEntryID }, timeEntry);
        }

        [HttpPost("stop-timer/{id}")]
        public async Task<ActionResult> StopTimer(int id)
        {
            var userId = _userManager.GetUserId(User);
            var (success, error) = await _timeEntryService.StopTimerAsync(id, userId);
            if (!success)
            {
                return BadRequest(new { Error = error });
            }

            return Ok(new { Message = "Timer stopped successfully." });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] TimeEntry timeEntry)
        {
            if (id != timeEntry.TimeEntryID || !ModelState.IsValid)
            {
                return BadRequest();
            }

            var userId = _userManager.GetUserId(User);
            var (success, error) = await _timeEntryService.UpdateTimeEntryAsync(timeEntry, userId);
            if (!success)
            {
                return BadRequest(new { Error = error });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var (success, error) = await _timeEntryService.DeleteTimeEntryAsync(id, userId);
            if (!success)
            {
                return NotFound(new { Error = error });
            }

            return NoContent();
        }

        [HttpPost("import")]
        public async Task<ActionResult> Import([FromForm] TimeEntryImportViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (model.CsvFile == null || model.CsvFile.Length == 0)
            {
                return BadRequest(new { Error = "Please upload a valid CSV file." });
            }

            using var stream = model.CsvFile.OpenReadStream();
            var (timeEntries, errors) = await _timeEntryService.ImportFromCsvAsync(stream, userId, model.ClientID, model.ProjectID);
            if (errors.Any())
            {
                return BadRequest(new { Errors = errors });
            }

            return Ok(new { Message = $"Successfully imported {timeEntries.Count} time entries.", TimeEntries = timeEntries });
        }
    }
}