using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerGuidance.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminEventsController : BaseAdminController
    {
        private readonly IWebHostEnvironment _env;

        public AdminEventsController(CareerDbContext context, IWebHostEnvironment env)
            : base(context)
        {
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();
            var events = await _context.Events
                .Where(e => e.Type == "Conference" && e.UserId == userId)
                .ToListAsync();

            return View(events);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event evt, IFormFile? imageFile)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            if (ModelState.IsValid)
            {
                evt.Type = "Conference";
                evt.UserId = userId;
                evt.CreatedAt = DateTime.UtcNow;

                if (imageFile != null && imageFile.Length > 0)
                {
                    string folder = Path.Combine(_env.WebRootPath, "images/events");
                    Directory.CreateDirectory(folder);

                    string uniqueName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                    string filePath = Path.Combine(folder, uniqueName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await imageFile.CopyToAsync(stream);
                    evt.ImageSrc = "events/" + uniqueName;
                }

                _context.Events.Add(evt);
                await _context.SaveChangesAsync();

                TempData["msg"] = "Conference created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(evt);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var evt = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (evt == null || evt.Type != "Conference" || evt.UserId != GetCurrentUserId())
                return Unauthorized();

            return View(evt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event evt, IFormFile? imageFile)
        {
            if (id != evt.EventId) return NotFound();

            var existing = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id);
            if (existing == null || existing.Type != "Conference" || existing.UserId != GetCurrentUserId())
                return Unauthorized();

            if (ModelState.IsValid)
            {
                evt.UserId = existing.UserId;
                evt.Type = existing.Type;
                evt.CreatedAt = existing.CreatedAt;

                if (imageFile != null && imageFile.Length > 0)
                {
                    string folder = Path.Combine(_env.WebRootPath, "images/events");
                    Directory.CreateDirectory(folder);

                    string uniqueName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                    string filePath = Path.Combine(folder, uniqueName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await imageFile.CopyToAsync(stream);
                    evt.ImageSrc = "events/" + uniqueName;
                }
                else
                {
                    evt.ImageSrc = existing.ImageSrc;
                }

                _context.Events.Update(evt);
                await _context.SaveChangesAsync();

                TempData["msg"] = "Conference updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(evt);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var evt = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id && e.Type == "Conference" && e.UserId == GetCurrentUserId());

            if (evt == null) return Unauthorized();
            return View(evt);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null || evt.Type != "Conference" || evt.UserId != GetCurrentUserId())
                return Unauthorized();

            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Conference deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
