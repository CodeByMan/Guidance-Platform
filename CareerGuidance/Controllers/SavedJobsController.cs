using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace CareerGuidance.Controllers
{
    [Authorize(Roles = "User")]
    public class SavedJobsController : UserBaseController
    {
        public SavedJobsController(CareerDbContext context) : base(context) { }

        [HttpPost]
        public async Task<IActionResult> Save(int jobId)
        {
            int userId = GetCurrentUserId();

            var existing = await _context.SavedJobs
                .FirstOrDefaultAsync(sj => sj.JobId == jobId && sj.UserId == userId);

            if (existing != null)
            {
                TempData["Info"] = "Already saved.";
            }
            else
            {
                _context.SavedJobs.Add(new SavedJob
                {
                    JobId = jobId,
                    UserId = userId,
                    SavedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Job saved.";
            }

            return RedirectToAction("JobList", "Home", new { id = jobId });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int jobId)
        {
            int userId = GetCurrentUserId();

            var saved = await _context.SavedJobs
                .FirstOrDefaultAsync(sj => sj.JobId == jobId && sj.UserId == userId);

            if (saved != null)
            {
                _context.SavedJobs.Remove(saved);
                await _context.SaveChangesAsync();
                TempData["SavedJobMessage"] = "Removed from saved jobs.";
            }

            return RedirectToAction("SavedJobs");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSave(int jobId)
        {
            int userId = GetCurrentUserId();

            var saved = await _context.SavedJobs
                .FirstOrDefaultAsync(sj => sj.JobId == jobId && sj.UserId == userId);

            if (saved != null)
            {
                _context.SavedJobs.Remove(saved);
                await _context.SaveChangesAsync();
                return Json(new { saved = false });
            }
            else
            {
                _context.SavedJobs.Add(new SavedJob
                {
                    JobId = jobId,
                    UserId = userId,
                    SavedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                return Json(new { saved = true });
            }
        }

        public async Task<IActionResult> SavedJobs()
        {
            int userId = GetCurrentUserId();
            var list = await _context.SavedJobs
                .Include(sj => sj.Job)
                .Where(sj => sj.UserId == userId)
                .ToListAsync();

            return View(list);
        }
    }
}
