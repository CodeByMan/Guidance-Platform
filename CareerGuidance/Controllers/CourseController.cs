using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerGuidance.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CourseController : BaseAdminController
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CourseController(CareerDbContext context, IWebHostEnvironment webHostEnvironment) : base(context)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        // ----------------- Manage -----------------
        public async Task<IActionResult> Manage()
        {
            var courses = await _context.Courses.ToListAsync();
            foreach (var course in courses)
            {
                course.ImageUrl = string.IsNullOrEmpty(course.ImageUrl) ? "/images/course/course.jpg" : "/images/course/" + course.ImageUrl;
                course.TrainerProfileImageUrl = string.IsNullOrEmpty(course.TrainerProfileImageUrl) ? "/images/mentor/mentor.jpg" : "/images/mentor/" + course.TrainerProfileImageUrl;
            }
            return View(courses);
        }

        // ----------------- Details -----------------
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == id);
            if (course == null) return NotFound();

            course.ImageUrl = string.IsNullOrEmpty(course.ImageUrl) ? "/images/course/course.jpg" : "/images/course/" + course.ImageUrl;
            course.TrainerProfileImageUrl = string.IsNullOrEmpty(course.TrainerProfileImageUrl) ? "/images/mentor/mentor.jpg" : "/images/mentor/" + course.TrainerProfileImageUrl;

            return View(course);
        }

        // ----------------- Create -----------------
        public IActionResult Create() => View(new Course());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Course course, IFormFile? CourseImage, IFormFile? TrainerImage, IFormFile? CourseVideo)
        {
            if (CourseImage != null)
                course.ImageUrl = await SaveFileAsync(CourseImage, Path.Combine(_webHostEnvironment.WebRootPath, "images/course"));
            if (TrainerImage != null)
                course.TrainerProfileImageUrl = await SaveFileAsync(TrainerImage, Path.Combine(_webHostEnvironment.WebRootPath, "images/mentor"));
            if (CourseVideo != null)
                course.VideoUrl = await SaveFileAsync(CourseVideo, Path.Combine(_webHostEnvironment.WebRootPath, "videos/course"));

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Course created successfully!";
            return RedirectToAction(nameof(Manage));
        }

        // ----------------- Edit -----------------
        public async Task<IActionResult> Edit(int id)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == id);
            return course == null ? NotFound() : View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Course course, IFormFile? CourseImage, IFormFile? TrainerImage, IFormFile? CourseVideo)
        {
            var existing = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == id);
            if (existing == null) return NotFound();

            existing.Title = course.Title;
            existing.Category = course.Category;
            existing.Description = course.Description;
            existing.Fee = course.Fee;
            existing.TrainerName = course.TrainerName;
            existing.QuizCategory = course.QuizCategory;
            existing.ReferenceBooks = course.ReferenceBooks;
            existing.ReferenceArticles = course.ReferenceArticles;

            if (CourseImage != null)
                existing.ImageUrl = await ReplaceFileAsync(existing.ImageUrl, CourseImage, Path.Combine(_webHostEnvironment.WebRootPath, "images/course"));
            if (TrainerImage != null)
                existing.TrainerProfileImageUrl = await ReplaceFileAsync(existing.TrainerProfileImageUrl, TrainerImage, Path.Combine(_webHostEnvironment.WebRootPath, "images/mentor"));
            if (CourseVideo != null)
                existing.VideoUrl = await ReplaceFileAsync(existing.VideoUrl, CourseVideo, Path.Combine(_webHostEnvironment.WebRootPath, "videos/course"));

            _context.Courses.Update(existing);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Course updated successfully!";
            return RedirectToAction(nameof(Manage));
        }

        // ----------------- Delete -----------------
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            DeleteFile(course.ImageUrl, Path.Combine(_webHostEnvironment.WebRootPath, "images/course"));
            DeleteFile(course.TrainerProfileImageUrl, Path.Combine(_webHostEnvironment.WebRootPath, "images/mentor"));
            DeleteFile(course.VideoUrl, Path.Combine(_webHostEnvironment.WebRootPath, "videos/course"));

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Course deleted successfully!";
            return RedirectToAction(nameof(Manage));
        }

        // ----------------- Helper Methods -----------------
        private async Task<string> SaveFileAsync(IFormFile file, string folderPath)
        {
            Directory.CreateDirectory(folderPath);
            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            string fullPath = Path.Combine(folderPath, fileName);
            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);
            return fileName;
        }

        private async Task<string> ReplaceFileAsync(string? oldFileName, IFormFile newFile, string folderPath)
        {
            DeleteFile(oldFileName, folderPath);
            return await SaveFileAsync(newFile, folderPath);
        }

        private void DeleteFile(string? fileName, string folderPath)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            string fullPath = Path.Combine(folderPath, fileName);
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
        }
    }
}
