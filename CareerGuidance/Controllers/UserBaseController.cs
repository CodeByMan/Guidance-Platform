using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace CareerGuidance.Controllers
{
    [Authorize]
    public class UserBaseController : Controller
    {
        protected readonly CareerDbContext _context;

        public UserBaseController(CareerDbContext context)
        {
            _context = context;
        }

        // -------------------- Current User Helpers --------------------

        protected int GetCurrentUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idStr, out var id) ? id : 0;
        }

        protected string GetCurrentUserRole()
        {
            return User.FindFirstValue(ClaimTypes.Role) ?? "";
        }

        /// <summary>
        /// Fetch the currently logged-in user from the database
        /// </summary>
        protected async Task<User?> GetCurrentUserAsync()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return null;

            return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        }

        // -------------------- Load upcoming job expirations --------------------

        protected async Task LoadUpcomingExpirationsAsync()
        {
            if (!User.Identity.IsAuthenticated)
            {
                ViewBag.UpcomingExpirations = new List<SavedJob>();
                return;
            }

            int userId = GetCurrentUserId();
            var today = DateTime.Today;
            var upcomingLimit = today.AddDays(7);

            var upcomingExpirations = await _context.SavedJobs
                .Include(sj => sj.Job)
                .Where(sj => sj.UserId == userId && sj.Job.Deadline >= today && sj.Job.Deadline <= upcomingLimit)
                .OrderBy(sj => sj.Job.Deadline)
                .ToListAsync();

            ViewBag.UpcomingExpirations = upcomingExpirations;
        }

        // -------------------- Action Execution Hook --------------------

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await LoadUpcomingExpirationsAsync();

            if (User.Identity.IsAuthenticated)
            {
                int userId = GetCurrentUserId();
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
                ViewBag.ProfileImage = string.IsNullOrEmpty(user?.Imagepath) ? "user.png" : user.Imagepath;
            }

            await next();
        }

        // -------------------- File Upload Helper --------------------

        protected async Task<string?> UploadFileAsync(IFormFile file, string subfolder)
        {
            if (file == null || file.Length == 0)
                return null;

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", subfolder);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"{subfolder}/{fileName}";
        }
    }
}
