using CareerGuidance.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace CareerGuidance.Controllers
{
    [Authorize(Roles = "User")]
    public class ResumeController : UserBaseController
    {
        private readonly IWebHostEnvironment _env;

        public ResumeController(CareerDbContext context, IWebHostEnvironment env) : base(context)
        {
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();
            var resumes = await _context.Resumes
                .Where(r => r.UserId == userId && !r.IsDeleted)
                .ToListAsync();
            return View(resumes);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Resume resume, IFormFile? ProfileImageFile)
        {
            if (!ModelState.IsValid) return View(resume);

            resume.UserId = GetCurrentUserId();

            if (ProfileImageFile != null)
            {
                var path = await UploadFileAsync(ProfileImageFile, "images/profiles");
                resume.ProfileImageFileName = Path.GetFileName(path);
            }

            _context.Resumes.Add(resume);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Resume created successfully!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var resume = await _context.Resumes.FindAsync(id);
            if (resume == null || resume.IsDeleted) return NotFound();
            if (resume.UserId != GetCurrentUserId()) return Forbid();

            return View(resume);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Resume resume, IFormFile? ProfileImageFile)
        {
            if (id != resume.ResumeId) return NotFound();

            var existing = await _context.Resumes.AsNoTracking().FirstOrDefaultAsync(r => r.ResumeId == id);
            if (existing == null || existing.IsDeleted) return NotFound();
            if (existing.UserId != GetCurrentUserId()) return Forbid();

            if (ProfileImageFile != null)
            {
                var path = await UploadFileAsync(ProfileImageFile, "images/profiles");
                resume.ProfileImageFileName = Path.GetFileName(path);
            }
            else
            {
                resume.ProfileImageFileName = existing.ProfileImageFileName;
            }

            resume.UserId = existing.UserId;
            _context.Update(resume);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Resume updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Preview(int id)
        {
            var resume = await _context.Resumes.FindAsync(id);
            if (resume == null || resume.IsDeleted) return NotFound();
            if (resume.UserId != GetCurrentUserId()) return Forbid();

            var template = string.IsNullOrEmpty(resume.SelectedTemplate) ? "PdfTemplate1" : resume.SelectedTemplate;
            return View(template, resume);
        }

        public async Task<IActionResult> DownloadPdf(int id)
        {
            var resume = await _context.Resumes.FindAsync(id);
            if (resume == null || resume.IsDeleted) return NotFound();
            if (resume.UserId != GetCurrentUserId()) return Forbid();

            var template = string.IsNullOrEmpty(resume.SelectedTemplate) ? "PdfTemplate1" : resume.SelectedTemplate;
            return new ViewAsPdf(template, resume)
            {
                FileName = $"{resume.FullName}_Resume.pdf"
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var resume = await _context.Resumes.FindAsync(id);
            if (resume == null || resume.IsDeleted) return NotFound();
            if (resume.UserId != GetCurrentUserId()) return Forbid();

            resume.IsDeleted = true;
            resume.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Resume moved to Recycle Bin.";
            return RedirectToAction(nameof(Index));
        }
    }
}
