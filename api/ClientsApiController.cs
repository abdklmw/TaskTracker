using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Data;
using TaskTracker.Models.Client;
using TaskTracker.Services;

namespace TaskTracker.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClientsApiController : ControllerBase
    {
        private readonly ClientService _clientService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClientsApiController(ClientService clientService, UserManager<ApplicationUser> userManager)
        {
            _clientService = clientService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var clients = await _clientService.GetAllClientsAsync();
            return Ok(clients);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var client = await _clientService.GetClientByIdAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            return Ok(client);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Client client)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, error) = await _clientService.CreateClientAsync(client);
            if (!success)
            {
                return BadRequest(new { Error = error });
            }

            return CreatedAtAction(nameof(GetById), new { id = client.ClientID }, client);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] Client client)
        {
            if (id != client.ClientID || !ModelState.IsValid)
            {
                return BadRequest();
            }

            var (success, error) = await _clientService.UpdateClientAsync(client);
            if (!success)
            {
                return BadRequest(new { Error = error });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var success = await _clientService.DeleteClientAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}