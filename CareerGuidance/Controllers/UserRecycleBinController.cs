using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CareerGuidance.Controllers
{
    [Authorize(Roles = "User")]
    public class UserRecycleBinController : UserBaseController
    {
        public UserRecycleBinController(CareerDbContext context) : base(context) { }

        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();

            var deletedGoals = await _context.Goals
                .Include(g => g.Steps)
                .Where(g => g.UserId == userId && g.IsDeleted)
                .ToListAsync();

            var deletedResumes = await _context.Resumes
                .Where(r => r.UserId == userId && r.IsDeleted)
                .ToListAsync();

            var deletedApplications = await _context.JobApplications
                .Where(a => a.UserId == userId && a.IsDeleted)
                .ToListAsync();

            var savedJobs = await _context.SavedJobs
                .Include(sj => sj.Job)
                .Where(sj => sj.UserId == userId)
                .ToListAsync();

            ViewBag.DeletedGoals = deletedGoals;
            ViewBag.DeletedResumes = deletedResumes;
            ViewBag.DeletedApplications = deletedApplications;
            ViewBag.SavedJobs = savedJobs;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreGoal(int id)
        {
            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.GoalId == id && g.IsDeleted && g.UserId == GetCurrentUserId());

            if (goal == null) return NotFound();

            goal.IsDeleted = false;
            goal.DeletedAt = null;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Goal restored successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreResume(int id)
        {
            var resume = await _context.Resumes
                .FirstOrDefaultAsync(r => r.ResumeId == id && r.IsDeleted && r.UserId == GetCurrentUserId());

            if (resume == null) return NotFound();

            resume.IsDeleted = false;
            resume.DeletedAt = null;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Resume restored successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreApplication(int id)
        {
            var app = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.JobApplicationId == id && a.IsDeleted && a.UserId == GetCurrentUserId());

            if (app == null) return NotFound();

            app.IsDeleted = false;
            app.DeletedAt = null;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Application restored successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGoalPermanently(int id)
        {
            var goal = await _context.Goals
                .Include(g => g.Steps)
                .FirstOrDefaultAsync(g => g.GoalId == id && g.IsDeleted && g.UserId == GetCurrentUserId());

            if (goal == null) return NotFound();

            _context.GoalSteps.RemoveRange(goal.Steps);
            _context.Goals.Remove(goal);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Goal permanently deleted.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteResumePermanently(int id)
        {
            var resume = await _context.Resumes
                .FirstOrDefaultAsync(r => r.ResumeId == id && r.IsDeleted && r.UserId == GetCurrentUserId());

            if (resume == null) return NotFound();

            _context.Resumes.Remove(resume);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Resume permanently deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteApplicationPermanently(int id)
        {
            var app = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.JobApplicationId == id && a.IsDeleted && a.UserId == GetCurrentUserId());

            if (app == null) return NotFound();

            _context.JobApplications.Remove(app);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Application permanently deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSavedJob(int id)
        {
            var saved = await _context.SavedJobs
                .FirstOrDefaultAsync(s => s.SavedJobId == id && s.UserId == GetCurrentUserId());

            if (saved == null) return NotFound();

            _context.SavedJobs.Remove(saved);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Saved job deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
