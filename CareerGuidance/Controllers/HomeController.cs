using Microsoft.AspNetCore.Mvc;
using CareerGuidance.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Text.Json;

namespace CareerGuidance.Controllers
{
    public class HomeController : Controller
    {
        private readonly CareerDbContext _context;
        private readonly string[] _jobTypeOptions = new[] { "Full Time", "Part Time", "Internship", "Permanent", "Remote", "Contract" };

        public HomeController(CareerDbContext context)
        {
            _context = context;
        }

        // ✅ Automatically set profile image for all actions
        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            if (User.Identity.IsAuthenticated)
            {
                var username = User.Identity.Name;
                var user = _context.Users
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Username == username);
                ViewBag.ProfileImage = user?.Imagepath ?? "user.png";
            }
            else
            {
                ViewBag.ProfileImage = "user.png";
            }
        }

        public IActionResult Index(string category = null)
        {
            // --- Counts ---
            ViewBag.StudentsCount = _context.Users.Count(u => u.RoleId == 3);
            ViewBag.MentorsCount = _context.Users.Count(u => u.RoleId == 2);
            ViewBag.CoursesCount = _context.Courses.Count();
            ViewBag.EventsCount = _context.Events.Count();

            // --- Get courses, limit to 4 distinct categories ---
            var courses = _context.Courses
                .GroupBy(c => c.Category)       // Group by category
                .Select(g => g.OrderByDescending(c => c.CourseId).First()) // Pick latest course in each category
                .Take(4)                        // Only 4 courses
                .ToList();

            // Set default images if null
            foreach (var course in courses)
            {
                course.ImageUrl = string.IsNullOrEmpty(course.ImageUrl) ? "/images/course/course.jpg" : "/images/course/" + course.ImageUrl;
                course.TrainerProfileImageUrl = string.IsNullOrEmpty(course.TrainerProfileImageUrl) ? "/images/mentor/mentor.jpg" : "/images/mentor/" + course.TrainerProfileImageUrl;
            }

            // --- Mentors (as before) ---
            var mentors = (from u in _context.Users
                           where u.RoleId == 2
                           join p in _context.MentorProfiles on u.UserId equals p.UserId into profileGroup
                           from profile in profileGroup.DefaultIfEmpty()
                           select new
                           {
                               UserId = u.UserId,
                               Username = u.Username,
                               ImagePath = string.IsNullOrEmpty(u.Imagepath) ? "user.png" : u.Imagepath,
                               Degree = profile != null ? profile.Degree : "N/A",
                               CurrentPost = profile != null ? profile.CurrentPost : "Not specified"
                           })
                           .Take(4)
                           .ToList<dynamic>();

            ViewBag.Mentors = mentors;

            // --- Testimonials ---
            var testimonials = _context.Achievements
                 .Select(a => new
                 {
                     a.FullName,
                     a.Position,
                     a.Organization,
                     PhotoUrl = string.IsNullOrEmpty(a.PhotoUrl) ? "/images/users/user.png" : a.PhotoUrl,
                     TestimonialText = a.Sector
                 })
                 .Take(5)
                 .ToList();

            ViewBag.Testimonials = testimonials;


            return View(courses);
        }


        public async Task<IActionResult> Courses()
        {
            var courses = await _context.Courses.ToListAsync();

            foreach (var course in courses)
            {
                // Course image
                if (string.IsNullOrEmpty(course.ImageUrl) || !System.IO.File.Exists(Path.Combine("wwwroot/images/course", course.ImageUrl)))
                {
                    course.ImageUrl = "/images/course/course.jpg"; // default image
                }
                else
                {
                    course.ImageUrl = "/images/course/" + course.ImageUrl;
                }

                // Trainer image
                if (string.IsNullOrEmpty(course.TrainerProfileImageUrl) || !System.IO.File.Exists(Path.Combine("wwwroot/images/mentor", course.TrainerProfileImageUrl)))
                {
                    course.TrainerProfileImageUrl = "/images/mentor/mentor.jpg"; // default trainer image
                }
                else
                {
                    course.TrainerProfileImageUrl = "/images/mentor/" + course.TrainerProfileImageUrl;
                }
            }

            return View(courses);
        }


        // Course details with comments (User only)
        [Authorize(Roles = "User")]
        public async Task<IActionResult> CourseDetails(int id)
        {
            var course = await _context.Courses
                .Include(c => c.CourseComments)
                .ThenInclude(cc => cc.User)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null) return NotFound();

            // Set trainer image
            if (string.IsNullOrEmpty(course.TrainerProfileImageUrl) ||
                !System.IO.File.Exists(Path.Combine("wwwroot/images/mentor", course.TrainerProfileImageUrl)))
            {
                course.TrainerProfileImageUrl = "/images/mentor/mentor.jpg"; // default trainer image
            }
            else
            {
                course.TrainerProfileImageUrl = "/images/mentor/" + course.TrainerProfileImageUrl;
            }

            // ----------------- Quiz Logic -----------------
            string quizCategory = course.QuizCategory; // use QuizCategory from DB
            bool quizAvailable = false;

            if (!string.IsNullOrEmpty(quizCategory))
            {
                string quizJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/data/coding_quiz.json");
                if (System.IO.File.Exists(quizJsonPath))
                {
                    var json = System.IO.File.ReadAllText(quizJsonPath);
                    var jsonDoc = JsonDocument.Parse(json);
                    quizAvailable = jsonDoc.RootElement.TryGetProperty(quizCategory, out var _);
                }
            }

            ViewBag.QuizAvailable = quizAvailable;

            return View(course);
        }

        // Add comment to course (User only)
        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> AddComment(int courseId, string content, int? parentCommentId = null)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["CommentError"] = "Comment cannot be empty.";
                return RedirectToAction(nameof(CourseDetails), new { id = courseId });
            }

            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Unauthorized();

            var comment = new CourseComment
            {
                CourseId = courseId,
                UserId = user.UserId,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow,
                ParentCommentId = parentCommentId
            };

            _context.CourseComments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["CommentSuccess"] = "Your comment was posted successfully.";
            return RedirectToAction(nameof(CourseDetails), new { id = courseId });
        }

        // Edit comment (owner only)
        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> EditComment(int commentId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["CommentError"] = "Comment cannot be empty.";
                var courseIdErr = await _context.CourseComments
                    .Where(c => c.CourseCommentId == commentId)
                    .Select(c => c.CourseId)
                    .FirstOrDefaultAsync();
                return RedirectToAction(nameof(CourseDetails), new { id = courseIdErr });
            }

            var comment = await _context.CourseComments.FindAsync(commentId);
            if (comment == null) return NotFound();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (comment.UserId != userId) return Forbid();

            comment.Content = content.Trim();
            comment.UpdatedAt = DateTime.UtcNow;

            _context.CourseComments.Update(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(CourseDetails), new { id = comment.CourseId });
        }

        // Delete comment (owner or admin)
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var comment = await _context.CourseComments
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.CourseCommentId == commentId);

            if (comment == null) return NotFound();

            var role = User.FindFirstValue(ClaimTypes.Role);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (role != "Admin" && comment.UserId != userId) return Forbid();

            _context.CourseComments.Remove(comment);
            await _context.SaveChangesAsync();

            TempData["CommentSuccess"] = "Comment deleted successfully.";
            return RedirectToAction(nameof(CourseDetails), new { id = comment.CourseId });
        }

        // Course video playback (single video)
        [Authorize(Roles = "User")]
        public async Task<IActionResult> CourseVideo(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            if (string.IsNullOrEmpty(course.VideoUrl))
            {
                TempData["Error"] = "No video available for this course.";
                return RedirectToAction("CourseDetails", new { id = courseId });
            }

            return View(course);
        }

        // Events listing with optional type filter
        public async Task<IActionResult> Events(string type = null)
        {
            var eventsQuery = _context.Events
                .Include(e => e.User)
                .AsQueryable();

            if (string.IsNullOrEmpty(type) || type == "Conference")
            {
                eventsQuery = eventsQuery
                    .Where(e => e.Type == "Conference" && e.User.RoleId == 1);
            }
            else if (type == "Webinar")
            {
                eventsQuery = eventsQuery
                    .Where(e => e.Type == "Webinar" && e.User.RoleId == 2);
            }

            var events = await eventsQuery
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            var roleStr = User.FindFirstValue(ClaimTypes.Role);
            int.TryParse(roleStr, out int roleId);
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int userId);

            ViewBag.UserRoleId = roleId;
            ViewBag.UserId = userId;

            if (userId > 0 && roleId == 3)
            {
                var appliedEventIds = await _context.EventApplications
                    .Where(a => a.UserId == userId)
                    .Select(a => a.EventId)
                    .ToListAsync();
                ViewBag.AppliedEventIds = appliedEventIds;
            }
            else
            {
                ViewBag.AppliedEventIds = new List<int>();
            }

            return View(events);
        }

        // Job listings with filters and paging
        public async Task<IActionResult> JobList(string title, string location, string JobType, int page = 1)
        {
            int pageSize = 10;
            ViewBag.JobTypes = new SelectList(_jobTypeOptions, JobType);

            var jobs = _context.Jobs.AsQueryable();

            if (!string.IsNullOrEmpty(title)) jobs = jobs.Where(j => j.Title.Contains(title));
            if (!string.IsNullOrEmpty(location)) jobs = jobs.Where(j => j.Location.Contains(location));
            if (!string.IsNullOrEmpty(JobType)) jobs = jobs.Where(j => j.JobType == JobType);

            int totalJobs = await jobs.CountAsync();
            var pagedJobs = await jobs
                .OrderBy(j => j.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalJobs / pageSize);
            ViewBag.TitleFilter = title;
            ViewBag.LocationFilter = location;
            ViewBag.JobTypeFilter = JobType;

            return View(pagedJobs);
        }

        public async Task<IActionResult> JobDetails(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.JobId == id.Value);
            if (job == null) return NotFound();

            return View(job);
        }

        // Mentors listing with Send Request button (User only for request)
        public async Task<IActionResult> Mentors()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            List<int> sentMentorIds = new List<int>();

            if (int.TryParse(userIdStr, out int userId))
            {
                sentMentorIds = await _context.MentorRequests
                    .Where(r => r.UserId == userId &&
                                (r.Status == "Pending" || r.Status == "Accepted" || r.Status == "Scheduled"))
                    .Select(r => r.MentorId)
                    .ToListAsync();
            }

            // Left join Users and MentorProfiles, no limit
            var mentors = (from u in _context.Users
                           where u.RoleId == 2
                           join p in _context.MentorProfiles on u.UserId equals p.UserId into profileGroup
                           from profile in profileGroup.DefaultIfEmpty()
                           select new
                           {
                               UserId = u.UserId,
                               Username = u.Username,
                               ImagePath = string.IsNullOrEmpty(u.Imagepath) ? "user.png" : u.Imagepath,
                               Degree = profile != null ? profile.Degree : "N/A",
                               CurrentPost = profile != null ? profile.CurrentPost : "Not specified",
                               Specialization = profile != null ? profile.Specialization : "N/A",
                               Description = profile != null ? profile.Description : "",
                               Experiences = profile != null ? profile.Experiences : "",
                               CurrentCompany = profile != null ? profile.CurrentCompany : ""
                           })
                           .ToList<dynamic>();

            ViewBag.Mentors = mentors;           // pass to view
            ViewBag.SentMentorIds = sentMentorIds;

            return View();                        // no model
        }

        // POST: Send mentor request (Users only)
        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> SendMentorRequest(int mentorId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var mentor = await _context.Users.FirstOrDefaultAsync(u => u.UserId == mentorId && u.RoleId == 2);
            if (mentor == null) return NotFound("Mentor not found");

            // Check if request already exists
            var existingRequest = await _context.MentorRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.MentorId == mentorId && r.UserId == userId &&
                                          (r.Status == "Pending" || r.Status == "Accepted" || r.Status == "Scheduled"));

            if (existingRequest != null)
            {
                TempData["ErrorMessage"] = "You already have a pending or active request with this mentor.";
                return RedirectToAction("Mentors");
            }

            // Create new mentor request
            var request = new MentorRequest
            {
                MentorId = mentorId,
                UserId = userId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.MentorRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Mentor request sent successfully.";
            return RedirectToAction("Mentors");
        }

        // GET: /Achievements/
        public IActionResult Achievements()
        {
            var achievements = _context.Achievements.ToList();
            return View(achievements);
        }

        // GET: /Achievements/Details/5
        public IActionResult AchievementDetails(int id)
        {
            var achievement = _context.Achievements.FirstOrDefault(a => a.AchievementId == id);
            if (achievement == null)
            {
                return NotFound();
            }
            return View(achievement);
        }
        public IActionResult Pricing() => View();
    }
}
