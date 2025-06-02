using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskTracker.Data;

namespace TaskTracker.Services
{
    public class DropdownService
    {
        private readonly AppDbContext _context;

        public DropdownService(AppDbContext context)
        {
            _context = context;
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

        public async Task<List<SelectListItem>> GetProjectDropdownAsync(int selectedProjectId = 0)
        {
            var projects = await _context.Projects
                .Where(p => p.ProjectID > 0)
                .Select(p => new { p.ProjectID, p.Name })
                .OrderBy(p => p.Name)
                .ToListAsync();

            var items = projects
                .Select(p => new SelectListItem
                {
                    Value = p.ProjectID.ToString(),
                    Text = p.Name,
                    Selected = p.ProjectID == selectedProjectId
                })
                .ToList();

            items.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "Select Project",
                Selected = selectedProjectId == 0
            });

            return items;
        }
    }
}