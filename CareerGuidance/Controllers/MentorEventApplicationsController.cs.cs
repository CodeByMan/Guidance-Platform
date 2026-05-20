using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CareerGuidance.Controllers
{
    [Authorize] // Only require authentication
    public class MentorEventApplicationsController : Controller
    {
        private readonly CareerDbContext _context;

        public MentorEventApplicationsController(CareerDbContext context)
        {
            _context = context;
        }

        // ---------------- COMMON: Get Mentor Profile Image ----------------
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = _context.Users.AsNoTracking()
                        .FirstOrDefault(u => u.UserId.ToString() == userId);
                    ViewBag.ProfileImage = user?.Imagepath ?? "user.png";
                }
            }
        }

        // ---------------- LIST EVENT APPLICATIONS (Mentor Only) ----------------
        public async Task<IActionResult> ByEvent(int eventId)
        {
            if (User.FindFirstValue(ClaimTypes.Role) != "Mentor")
                return Forbid();

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (eventItem == null) return NotFound();

            ViewBag.EventTitle = eventItem.Title;
            ViewBag.EventId = eventId;

            var applications = await _context.EventApplications
                .Include(a => a.User)
                .Where(a => a.EventId == eventId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return View(applications);
        }

        // ---------------- RESPOND TO AN APPLICATION (Mentor Only) ----------------
        public async Task<IActionResult> Respond(int applicationId)
        {
            if (User.FindFirstValue(ClaimTypes.Role) != "Mentor")
                return Forbid();

            var app = await _context.EventApplications
                .Include(a => a.User)
                .Include(a => a.Event)
                .FirstOrDefaultAsync(a => a.EventApplicationId == applicationId);

            if (app == null) return NotFound();

            ViewBag.Application = app;
            return View(app);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(int applicationId, string actionStatus, string? meetingLink)
        {
            if (User.FindFirstValue(ClaimTypes.Role) != "Mentor")
                return Forbid();

            var app = await _context.EventApplications
                .Include(a => a.User)
                .Include(a => a.Event)
                .FirstOrDefaultAsync(a => a.EventApplicationId == applicationId);

            if (app == null) return NotFound();

            if (actionStatus == "Accepted")
            {
                app.Status = "Accepted";
                app.MeetingLink = meetingLink;
                app.Message = $"Congratulations! Join the {(app.Event.Type == "Webinar" ? "webinar" : "conference")} using this link: {meetingLink}";
            }
            else if (actionStatus == "Rejected")
            {
                app.Status = "Rejected";
                app.Message = "Sorry, your application was not selected.";
            }
            else
            {
                ModelState.AddModelError("", "Unknown action");
                return View(app);
            }

            _context.Update(app);
            await _context.SaveChangesAsync();

            TempData["msg"] = $"Application has been {app.Status.ToLower()}.";
            return RedirectToAction(nameof(ByEvent), new { eventId = app.EventId });
        }

        // ---------------- REMOVE AN APPLICATION (Mentor Only) ----------------
        [HttpPost]
        public async Task<IActionResult> Remove(int applicationId)
        {
            if (User.FindFirstValue(ClaimTypes.Role) != "Mentor")
                return Forbid();

            var app = await _context.EventApplications.FindAsync(applicationId);
            if (app == null)
            {
                TempData["msg"] = "Application not found.";
                return RedirectToAction("Index", "Home");
            }

            int eventId = app.EventId;
            _context.EventApplications.Remove(app);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Application removed successfully.";
            return RedirectToAction(nameof(ByEvent), new { eventId });
        }

        // ---------------- APPLY REQUEST FOR MENTOR EVENTS (User Only) ----------------
        [HttpPost]
        public async Task<IActionResult> ApplyRequest(int eventId)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Auth");

            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role != "User") // Only students can apply
            {
                TempData["msg"] = "Only students can apply for mentor events.";
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
