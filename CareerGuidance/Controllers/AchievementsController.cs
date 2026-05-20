using CareerGuidance.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerGuidance.Controllers
{
    public class AchievementsController : BaseAdminController
    {
        private readonly IWebHostEnvironment _env;

        public AchievementsController(CareerDbContext context, IWebHostEnvironment env)
            : base(context)
        {
            _env = env;
        }

        // GET: Achievements
        public async Task<IActionResult> Index()
        {
            var achievements = await _context.Achievements.ToListAsync();
            return View(achievements);
        }

        // GET: Achievements/Create
        public IActionResult Create() => View();

        // POST: Achievements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Achievement achievement)
        {
            if (ModelState.IsValid)
            {
                if (achievement.PhotoFile != null)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "success");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid() + Path.GetExtension(achievement.PhotoFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await achievement.PhotoFile.CopyToAsync(fileStream);
                    }

                    achievement.PhotoUrl = "/images/success/" + uniqueFileName;
                }

                _context.Add(achievement);
                await _context.SaveChangesAsync();

                TempData["msg"] = "Achievement created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(achievement);
        }

        // GET: Achievements/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var achievement = await _context.Achievements.FindAsync(id);
            if (achievement == null)
                return NotFound();

            return View(achievement);
        }

        // POST: Achievements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Achievement achievement)
        {
            if (id != achievement.AchievementId)
                return NotFound();

            if (ModelState.IsValid)
            {
                var achievementDb = await _context.Achievements.FindAsync(id);
                if (achievementDb == null)
                    return NotFound();

                // Update fields
                achievementDb.FullName = achievement.FullName;
                achievementDb.Position = achievement.Position;
                achievementDb.Organization = achievement.Organization;
                achievementDb.Sector = achievement.Sector;
                achievementDb.City = achievement.City;
                achievementDb.Description = achievement.Description;

                // Update image only if a new file was uploaded
                if (achievement.PhotoFile != null)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "success");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid() + Path.GetExtension(achievement.PhotoFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await achievement.PhotoFile.CopyToAsync(fileStream);
                    }

                    achievementDb.PhotoUrl = "/images/success/" + uniqueFileName;
                }

                await _context.SaveChangesAsync();

                TempData["msg"] = "Achievement updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(achievement);
        }

        // POST: Achievements/Delete/5
        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            var achievement = await _context.Achievements.FindAsync(id);
            if (achievement == null)
                return Json(new { success = false, message = "Achievement not found" });

            if (!string.IsNullOrEmpty(achievement.PhotoUrl))
            {
                var filePath = Path.Combine(_env.WebRootPath, achievement.PhotoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _context.Achievements.Remove(achievement);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Achievement deleted successfully!" });
        }
    }
}
