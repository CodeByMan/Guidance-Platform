using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerGuidance.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseAdminController
    {
        public AdminController(CareerDbContext context) : base(context) { }

        public IActionResult Index()
        {
            ViewBag.AdminsCount = _context.Users.Count(u => u.RoleId == 1);
            ViewBag.MentorsCount = _context.Users.Count(u => u.RoleId == 2);
            ViewBag.UsersCount = _context.Users.Count(u => u.RoleId == 3);
            ViewBag.JobsCount = _context.Jobs.Count();
            ViewBag.ConferencesCount = _context.Events.Count(e => e.Type == "Conference");
            ViewBag.WebinarsCount = _context.Events.Count(e => e.Type == "Webinar");
            ViewBag.CoursesCount = _context.Courses.Count();
            ViewBag.AchievementsCount = _context.Achievements.Count();

            return View();
        }

        // GET: Admin Profile
        public IActionResult Profile()
        {
            int userId = GetCurrentUserId();
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) return RedirectToAction("Index");

            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            ViewBag.ErrorMessage = TempData["ErrorMessage"];
            return View(user);
        }

        // POST: Upload Profile Image
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(IFormFile ImageFile)
        {
            int userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Profile");
            }

            if (ImageFile == null || ImageFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an image file.";
                return RedirectToAction("Profile");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(ImageFile.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["ErrorMessage"] = "Invalid file type. Use JPG, JPEG, or PNG.";
                return RedirectToAction("Profile");
            }

            if (ImageFile.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "File too large (max 5MB).";
                return RedirectToAction("Profile");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "users");
            Directory.CreateDirectory(uploadsFolder);

            var newFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, newFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await ImageFile.CopyToAsync(stream);
            }

            // Delete old image if exists and not default
            if (!string.IsNullOrEmpty(user.Imagepath) && user.Imagepath != "user.png")
            {
                var oldPath = Path.Combine(uploadsFolder, user.Imagepath);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            user.Imagepath = newFileName; // store only the filename
            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile image updated successfully!";
            return RedirectToAction("Profile");
        }

        // GET: Change Password
        [HttpGet]
        public IActionResult UpdatePassword() => View();

        // POST: Change Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(string NewPassword, string ConfirmPassword)
        {
            if (string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                TempData["ErrorMessage"] = "Both fields are required.";
                return View();
            }

            if (NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                return View();
            }

            int userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return View();
            }

            var hasher = new PasswordHasher<string>();
            user.Password = hasher.HashPassword(user.Email, NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password updated successfully!";
            return View(); // stay on the same page
        }

        public IActionResult Users()
        {
            ViewBag.RegularUsers = _context.Users.Where(u => u.RoleId == 3).ToList();
            return View();
        }

        public IActionResult Mentors()
        {
            ViewBag.Mentors = _context.Users.Where(u => u.RoleId == 2).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();

                return user.RoleId switch
                {
                    3 => RedirectToAction("Users"),
                    2 => RedirectToAction("Mentors"),
                    _ => RedirectToAction("Index")
                };
            }
            return RedirectToAction("Index");
        }
    }
}
