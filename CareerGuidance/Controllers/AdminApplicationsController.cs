using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CareerGuidance.Controllers
{
    [Authorize] // Only require authentication
    public class AdminApplicationsController : Controller
    {
        private readonly CareerDbContext _context;

        public AdminApplicationsController(CareerDbContext context)
        {
            _context = context;
        }

        // ---------------- JOB APPLICATIONS (Admin) ----------------
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> JobApplicationsByJob()
        {
            var apps = await _context.JobApplications
                .Include(a => a.User)
                .Include(a => a.Job)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            return View(apps);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateJobStatus(int id, string actionStatus)
        {
            var app = await _context.JobApplications
                .Include(a => a.Job)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.JobApplicationId == id);

            if (app == null) return NotFound();

            app.Status = actionStatus switch
            {
                "Accept" => "Accepted",
                "Reject" => "Rejected",
                _ => app.Status
            };

            _context.Update(app);
            await _context.SaveChangesAsync();

            TempData["msg"] = $"Job application {app.Status.ToLower()} successfully.";
            return RedirectToAction(nameof(JobApplicationsByJob));
        }

        // ---------------- EVENT APPLICATIONS (Admin) ----------------
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EventApplicationsByEvent(int? eventId = null)
        {
            var apps = _context.EventApplications
                .Include(a => a.User)
                .Include(a => a.Event)
                .AsQueryable();

            if (eventId.HasValue)
                apps = apps.Where(a => a.EventId == eventId);

            var list = await apps.OrderByDescending(a => a.AppliedAt).ToListAsync();
            ViewBag.EventTitle = list.FirstOrDefault()?.Event?.Title ?? "All Events";

            return View(list);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEventStatus(int id, string actionStatus)
        {
            var app = await _context.EventApplications
                .Include(a => a.Event)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.EventApplicationId == id);

            if (app == null) return NotFound();

            if (actionStatus == "Accept")
            {
                app.Status = "Accepted";
                app.RegistrationNumber = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                app.Message = $"Congratulations! You are selected for {app.Event.Title}. Reg No: {app.RegistrationNumber}";
            }
            else if (actionStatus == "Reject")
            {
                app.Status = "Rejected";
                app.Message = "Sorry, your application was not selected.";
            }

            _context.Update(app);
            await _context.SaveChangesAsync();

            TempData["msg"] = $"Event application {app.Status.ToLower()} successfully.";
            return RedirectToAction(nameof(EventApplicationsByEvent), new { eventId = app.EventId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteApplication(int id, string type)
        {
            if (type == "job")
            {
                var jobApp = await _context.JobApplications.FindAsync(id);
                if (jobApp != null)
                {
                    _context.JobApplications.Remove(jobApp);
                    await _context.SaveChangesAsync();
                    TempData["msg"] = "Job application removed.";
                }
            }
            else if (type == "event")
            {
                var eventApp = await _context.EventApplications.FindAsync(id);
                if (eventApp != null)
                {
                    _context.EventApplications.Remove(eventApp);
                    await _context.SaveChangesAsync();
                    TempData["msg"] = "Event application removed.";
                }
            }

            return RedirectToAction(nameof(EventApplicationsByEvent));
        }

        // ---------------- APPLY REQUEST FOR EVENTS (User) ----------------
        [HttpPost]
        public async Task<IActionResult> ApplyRequest(int eventId)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Auth");

            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role != "User") // Only users can apply
            {
                TempData["msg"] = "Only students can apply for events.";
                return RedirectToAction("Events", "Home");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Check if already applied
            bool alreadyApplied = await _context.EventApplications
                .AnyAsync(a => a.EventId == eventId && a.UserId.ToString() == userId);

            if (alreadyApplied)
            {
                TempData["msg"] = "You have already applied for this event.";
                return RedirectToAction("Events", "Home");
            }

            var app = new EventApplication
            {
                EventId = eventId,
                UserId = int.Parse(userId),
                AppliedAt = DateTime.Now,
                Status = "Pending"
            };

            _context.EventApplications.Add(app);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Applied successfully!";
            return RedirectToAction("Events", "Home");
        }
    }
}
