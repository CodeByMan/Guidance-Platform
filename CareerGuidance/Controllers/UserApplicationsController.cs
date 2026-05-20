using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CareerGuidance.Controllers
{
    [Authorize(Roles = "User")]
    public class UserApplicationsController : UserBaseController
    {
        private readonly IWebHostEnvironment _env;

        public UserApplicationsController(CareerDbContext context, IWebHostEnvironment env)
            : base(context)
        {
            _env = env;
        }

        // ---------------- JOB APPLICATIONS ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyJob(JobApplication app, IFormFile? Resume)
        {
            int userId = GetCurrentUserId();
            app.UserId = userId;

            bool alreadyApplied = await _context.JobApplications
                .AnyAsync(a => a.JobId == app.JobId && a.UserId == userId && !a.IsDeleted);

            if (alreadyApplied)
            {
                TempData["Error"] = "You have already applied for this job.";
                return RedirectToAction("JobDetails", "Home", new { id = app.JobId });
            }

            if (Resume != null)
                app.ResumePath = await UploadFileAsync(Resume, "resumes");

            app.AppliedDate = DateTime.UtcNow;
            app.Status = "Pending";

            _context.JobApplications.Add(app);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Job application submitted successfully!";
            return RedirectToAction(nameof(MyJobApplications));
        }

        public async Task<IActionResult> MyJobApplications()
        {
            int userId = GetCurrentUserId();
            var apps = await _context.JobApplications
                .Include(a => a.Job)
                .Where(a => a.UserId == userId && !a.IsDeleted)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            return View(apps);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJobApplication(int id)
        {
            var app = await _context.JobApplications.FindAsync(id);
            if (app == null || app.UserId != GetCurrentUserId())
                return NotFound();

            app.IsDeleted = true;
            app.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Job application moved to recycle bin.";

            return RedirectToAction(nameof(MyJobApplications));
        }

        // ---------------- EVENT APPLICATIONS ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyEvent(int eventId, string name, string email)
        {
            int userId = GetCurrentUserId();

            var existing = await _context.EventApplications
                .FirstOrDefaultAsync(a => a.EventId == eventId && a.UserId == userId);

            if (existing != null)
            {
                TempData["msg"] = "You have already applied for this event.";
                return RedirectToAction("Events", "Home");
            }

            var app = new EventApplication
            {
                EventId = eventId,
                UserId = userId,
                Message = $"Applied by {name}, Email: {email}",
                Status = "Pending",
                AppliedAt = DateTime.UtcNow
            };

            _context.EventApplications.Add(app);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Event application submitted successfully!";
            return RedirectToAction(nameof(MyEventApplications));
        }

        public async Task<IActionResult> MyEventApplications()
        {
            int userId = GetCurrentUserId();

            var apps = await _context.EventApplications
                .Include(a => a.Event)
                .ThenInclude(e => e.User)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return View(apps);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEventApplication(int id)
        {
            int userId = GetCurrentUserId();
            var app = await _context.EventApplications
                .FirstOrDefaultAsync(a => a.EventApplicationId == id && a.UserId == userId);

            if (app == null)
                return NotFound();

            _context.EventApplications.Remove(app);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Event application deleted successfully.";
            return RedirectToAction(nameof(MyEventApplications));
        }

        // ---------------- FILE UPLOAD HELPER ----------------

        private async Task<string> UploadFileAsync(IFormFile file, string folderName)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", folderName);
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            return $"{folderName}/{fileName}";
        }
        // -------------------- Mentor Requests --------------------

        public async Task<IActionResult> MyMentorRequests()
        {
            int userId = GetCurrentUserId();

            var requests = await _context.MentorRequests
                .Include(r => r.Mentor)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMentorRequest(int requestId)
        {
            int userId = GetCurrentUserId();

            var request = await _context.MentorRequests
                .FirstOrDefaultAsync(r => r.MentorRequestId == requestId && r.UserId == userId);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Mentor request not found or you cannot delete it.";
                return RedirectToAction(nameof(MyMentorRequests));
            }

            _context.MentorRequests.Remove(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Mentor request deleted successfully!";
            return RedirectToAction(nameof(MyMentorRequests));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
