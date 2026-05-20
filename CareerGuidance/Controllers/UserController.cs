using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace CareerGuidance.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : UserBaseController
    {
        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger, CareerDbContext context)
            : base(context)
        {
            _logger = logger;
        }

        // Dashboard
        public async Task<IActionResult> Index()
        {
            return View();
        }

        // -------------------- User Profile --------------------

        public async Task<IActionResult> Profile()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(IFormFile? ImageFile)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return NotFound();

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "users");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var ext = Path.GetExtension(ImageFile.FileName);
                var fileName = $"{user.UserId}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await ImageFile.CopyToAsync(stream);

                if (!string.IsNullOrEmpty(user.Imagepath))
                {
                    var oldFile = Path.Combine(uploadsFolder, user.Imagepath);
                    if (System.IO.File.Exists(oldFile))
                        System.IO.File.Delete(oldFile);
                }

                user.Imagepath = fileName;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Profile image updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Please select an image file.";
            }

            return RedirectToAction("Profile");
        }

        public IActionResult UpdatePassword()
        {
            return View();
        }

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

            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return View();
            }

            var hasher = new PasswordHasher<string>();
            user.Password = hasher.HashPassword(user.Email, NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password updated successfully!";
            return RedirectToAction("Profile");
        }
    }
}
