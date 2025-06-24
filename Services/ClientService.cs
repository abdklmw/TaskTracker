using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models.Client;

namespace TaskTracker.Services
{
    public class ClientService
    {
        private readonly AppDbContext _context;

        public ClientService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Client>> GetAllClientsAsync()
        {
            return await _context.Clients.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<Client?> GetClientByIdAsync(int id)
        {
            return await _context.Clients.FirstOrDefaultAsync(c => c.ClientID == id);
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateClientAsync(Client client)
        {
            _context.Add(client);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateClientAsync(Client client)
        {
            try
            {
                _context.Update(client);
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ClientExistsAsync(client.ClientID))
                {
                    return (false, "Client not found.");
                }
                throw;
            }
        }

        public async Task<bool> DeleteClientAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return false;
            }
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ClientExistsAsync(int id)
        {
            return await _context.Clients.AnyAsync(e => e.ClientID == id);
        }

        public async Task<List<SelectListItem>> GetClientDropdownAsync(int selectedClientId = 0)
        {
            var clients = await _context.Clients
                .Where(c => c.ClientID > 0)
                .Select(c => new { c.ClientID, c.Name })
                .OrderBy(c => c.Name)
                .ToListAsync();

            var items = clients
                .Select(c => new SelectListItem
                {
                    Value = c.ClientID.ToString(),
                    Text = c.Name,
                    Selected = c.ClientID == selectedClientId
                })
                .ToList();

            items.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "Select Client",
                Selected = selectedClientId == 0
            });

            return items;
        }
    }
}