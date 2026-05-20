using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CareerGuidance.Controllers
{
    [Authorize(Roles = "Admin")]
    public abstract class BaseAdminController : Controller
    {
        protected readonly CareerDbContext _context;

        protected BaseAdminController(CareerDbContext context)
        {
            _context = context;
        }

        private int? _currentUserId;

        /// <summary>
        /// Get currently logged-in admin user ID (cached per request).
        /// </summary>
        protected int GetCurrentUserId()
        {
            if (_currentUserId.HasValue)
                return _currentUserId.Value;

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int id))
            {
                _currentUserId = id;
                return id;
            }

            _currentUserId = 0;
            return 0;
        }

        /// <summary>
        /// Sets ViewBag.ProfileImage for all admin views.
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            string profileImageUrl = "/images/user.png"; // default fallback

            if (User.Identity?.IsAuthenticated == true)
            {
                int userId = GetCurrentUserId();
                if (userId > 0)
                {
                    var user = _context.Users
                        .AsNoTracking()
                        .FirstOrDefault(u => u.UserId == userId);

                    if (user != null && !string.IsNullOrEmpty(user.Imagepath))
                    {
                        // Ensure path starts with /images/
                        profileImageUrl = user.Imagepath.StartsWith("users/")
                            ? $"/images/{user.Imagepath}"
                            : $"/images/users/{user.Imagepath}";
                    }
                }
            }

            ViewBag.ProfileImage = profileImageUrl;
        }
    }
}
