using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CareerGuidance.Controllers
{
    [Authorize(Roles = "Mentor")]
    public class MentorEventsController : Controller
    {
        private readonly CareerDbContext _context;
        private readonly IWebHostEnvironment _env;

        public MentorEventsController(CareerDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Set profile image for _MentorLayout
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

        // Helper to get current user id
        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out int id) ? id : 0;
        }

        // =========================
        // Actions
        // =========================

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var events = await _context.Events
                .Where(e => e.Type == "Webinar" && e.UserId == userId)
                .ToListAsync();
            return View(events);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event evt, IFormFile? imageFile)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            if (ModelState.IsValid)
            {
                evt.Type = "Webinar";
                evt.UserId = userId;
                evt.CreatedAt = DateTime.UtcNow;

                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images/events");
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await imageFile.CopyToAsync(stream);
                    evt.ImageSrc = "events/" + uniqueFileName;
                }

                _context.Events.Add(evt);
                await _context.SaveChangesAsync();
                TempData["msg"] = "Webinar created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(evt);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var evt = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (evt == null || evt.Type != "Webinar" || evt.UserId != GetCurrentUserId())
                return Unauthorized();
            return View(evt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event evt, IFormFile? imageFile)
        {
            if (id != evt.EventId) return NotFound();

            var existing = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id);
            if (existing == null || existing.Type != "Webinar" || existing.UserId != GetCurrentUserId())
                return Unauthorized();

            if (ModelState.IsValid)
            {
                evt.UserId = existing.UserId;
                evt.Type = existing.Type;
                evt.CreatedAt = existing.CreatedAt;

                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images/events");
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await imageFile.CopyToAsync(stream);
                    evt.ImageSrc = "events/" + uniqueFileName;
                }
                else
                {
                    evt.ImageSrc = existing.ImageSrc;
                }

                _context.Events.Update(evt);
                await _context.SaveChangesAsync();
                TempData["msg"] = "Webinar updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(evt);
        }

        // =========================
        // Delete handled entirely in controller
        // =========================

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var evt = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id && e.Type == "Webinar" && e.UserId == GetCurrentUserId());

            if (evt == null) return Unauthorized();

            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Webinar deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
