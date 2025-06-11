using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskTracker.Data;
using TaskTracker.Models.Project;

namespace TaskTracker.Services
{
    public class ProjectService
    {
        private readonly AppDbContext _context;

        public ProjectService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            return await _context.Projects.OrderBy(p => p.Name).ToListAsync();
        }

        public async Task<Project?> GetProjectByIdAsync(int id)
        {
            return await _context.Projects.FirstOrDefaultAsync(p => p.ProjectID == id);
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateProjectAsync(Project project)
        {
            _context.Add(project);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateProjectAsync(Project project)
        {
            try
            {
                _context.Update(project);
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ProjectExistsAsync(project.ProjectID))
                {
                    return (false, "Project not found.");
                }
                throw;
            }
        }

        public async Task<bool> DeleteProjectAsync(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return false;
            }
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ProjectExistsAsync(int id)
        {
            return await _context.Projects.AnyAsync(e => e.ProjectID == id);
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