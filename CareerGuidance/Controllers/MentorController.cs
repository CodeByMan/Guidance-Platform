using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CareerGuidance.Controllers
{
    [Authorize(Roles = "Mentor")]
    public class MentorController : Controller
    {
        private readonly CareerDbContext _context;

        public MentorController(CareerDbContext context)
        {
            _context = context;
        }

        // Run before each action to set profile image for logged-in mentor
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = _context.Users.AsNoTracking().FirstOrDefault(u => u.UserId.ToString() == userId);
                    // Keep only the basic profile image for navbar/avatar display
                    ViewBag.ProfileImage = user?.Imagepath ?? "user.png";
                }
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: Mentor/Requests
        public async Task<IActionResult> Requests()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int mentorId))
                return Unauthorized();

            var requests = await _context.MentorRequests
                                         .Include(r => r.User)
                                         .Where(r => r.MentorId == mentorId)
                                         .OrderByDescending(r => r.CreatedAt)
                                         .ToListAsync();

            return View(requests);
        }

        // ViewModel for responding to requests
        public class RespondRequestViewModel
        {
            public int MentorRequestId { get; set; }
            public string Action { get; set; } = null!;
            public string? MeetingLink { get; set; }
            public DateTime? ScheduledDate { get; set; }
        }

        // POST: Mentor/RespondRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondRequest(RespondRequestViewModel model)
        {
            var mentorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(mentorIdStr, out int mentorId))
                return Unauthorized();

            var request = await _context.MentorRequests
                                        .FirstOrDefaultAsync(r => r.MentorRequestId == model.MentorRequestId && r.MentorId == mentorId);
            if (request == null) return NotFound();

            switch (model.Action)
            {
                case "Accept":
                    request.Status = "Accepted";
                    request.MeetingLink = model.MeetingLink;
                    request.ScheduledDate = null;
                    break;

                case "Reject":
                    request.Status = "Rejected";
                    request.MeetingLink = null;
                    request.ScheduledDate = null;
                    break;

                case "Schedule":
                    if (model.ScheduledDate == null || string.IsNullOrEmpty(model.MeetingLink))
                    {
                        TempData["ErrorMessage"] = "Please provide meeting link and scheduled date for scheduling.";
                        return RedirectToAction("Requests");
                    }
                    request.Status = "Scheduled";
                    request.MeetingLink = model.MeetingLink;
                    request.ScheduledDate = model.ScheduledDate;
                    break;

                default:
                    TempData["ErrorMessage"] = "Invalid action.";
                    return RedirectToAction("Requests");
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Request updated successfully.";
            return RedirectToAction("Requests");
        }

        // ===============================
        // Personal Info Actions
        // ===============================

        // GET: Mentor/PersonalInfo
        [HttpGet]
        public IActionResult PersonalInfo()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var mentorProfile = _context.MentorProfiles
                                        .Include(m => m.User)
                                        .FirstOrDefault(m => m.UserId == userId);

            if (mentorProfile == null)
            {
                mentorProfile = new MentorProfile { UserId = userId };
                _context.MentorProfiles.Add(mentorProfile);
                _context.SaveChanges();
            }

            return View(mentorProfile);
        }

        // GET: Mentor/UpdatePassword
        [HttpGet]
        public IActionResult UpdatePassword()
        {
            return View();
        }

        // POST: Mentor/UpdatePassword
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

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                TempData["ErrorMessage"] = "Invalid user.";
                return View();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return View();
            }

            var hasher = new PasswordHasher<string>();
            user.Password = hasher.HashPassword(user.Email, NewPassword);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password Updated Successfully!";
            return View();
        }

        // POST: Mentor/PersonalInfo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PersonalInfo(MentorProfile model, IFormFile? ImageFile)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var existingProfile = _context.MentorProfiles.FirstOrDefault(m => m.UserId == userId);
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            if (existingProfile == null || user == null) return NotFound();

            // Handle image upload
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/users");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(ImageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }

                // **Do NOT delete old image**, just update the database
                user.Imagepath = fileName;
            }

            // Update profile fields
            existingProfile.Specialization = model.Specialization;
            existingProfile.Description = model.Description;
            existingProfile.Degree = model.Degree;
            existingProfile.Experiences = model.Experiences;
            existingProfile.CurrentCompany = model.CurrentCompany;
            existingProfile.CurrentPost = model.CurrentPost;
            existingProfile.WorkStatus = model.WorkStatus;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Profile Successfully Updated!";
            return RedirectToAction("PersonalInfo", new { success = true });
        }
    }
}
