using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models;
using Microsoft.AspNetCore.Identity;

namespace TaskTracker.Controllers
{
    public class TimeEntriesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TimeEntriesController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: TimeEntries
        public async Task<IActionResult> Index(int? clientId, int? projectId)
        {
            var userId = _userManager.GetUserId(User);
            var timeEntries = _context.TimeEntries
                .Where(t => t.UserId == userId)
                .Include(t => t.Project)
                .Include(t => t.Client);

            var clientList = _context.Clients
                .Select(c => new { c.ClientID, c.Name })
                .ToList();
            clientList.Insert(0, new { ClientID = 0, Name = "Select Client" });
            ViewBag.ClientID = new SelectList(clientList, "ClientID", "Name", clientId ?? 0);

            var projectList = _context.Projects
                .Select(p => new { p.ProjectID, p.Name })
                .ToList();
            projectList.Insert(0, new { ProjectID = 0, Name = "Select Project" });
            ViewBag.ProjectID = new SelectList(projectList, "ProjectID", "Name", projectId ?? 0);

            return View(await timeEntries.ToListAsync());
        }

        // POST: TimeEntries/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientID,ProjectID,UserId,StartDateTime,EndDateTime,HoursSpent,Description")] TimeEntry timeEntry)
        {
            if (timeEntry.ClientID == 0)
            {
                ModelState.AddModelError("ClientID", "Please select a client.");
            }
            if (timeEntry.ProjectID == 0)
            {
                ModelState.AddModelError("ProjectID", "Please select a project.");
            }

            timeEntry.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                _context.Add(timeEntry);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Time entry created successfully.";
                return RedirectToAction(nameof(Index));
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = string.Join("; ", errors);
            return RedirectToAction(nameof(Index));
        }

        // POST: TimeEntries/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TimeEntryID,ClientID,ProjectID,UserId,StartDateTime,EndDateTime,HoursSpent,Description")] TimeEntry timeEntry)
        {
            if (id != timeEntry.TimeEntryID)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (timeEntry.UserId != userId)
            {
                return Forbid();
            }

            if (timeEntry.ClientID == 0)
            {
                ModelState.AddModelError("ClientID", "Please select a client.");
            }
            if (timeEntry.ProjectID == 0)
            {
                ModelState.AddModelError("ProjectID", "Please select a project.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(timeEntry);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Time entry updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TimeEntryExists(timeEntry.TimeEntryID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = string.Join("; ", errors);
            return RedirectToAction(nameof(Index));
        }

        // GET: TimeEntries/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var timeEntry = await _context.TimeEntries
                .Include(t => t.Project)
                .Include(t => t.Client)
                .FirstOrDefaultAsync(m => m.TimeEntryID == id && m.UserId == userId);
            if (timeEntry == null)
            {
                return NotFound();
            }

            return View(timeEntry);
        }

        // POST: TimeEntries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var timeEntry = await _context.TimeEntries
                .FirstOrDefaultAsync(t => t.TimeEntryID == id && t.UserId == userId);
            if (timeEntry != null)
            {
                _context.TimeEntries.Remove(timeEntry);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TimeEntryExists(int id)
        {
            var userId = _userManager.GetUserId(User);
            return _context.TimeEntries.Any(e => e.TimeEntryID == id && e.UserId == userId);
        }
    }
}