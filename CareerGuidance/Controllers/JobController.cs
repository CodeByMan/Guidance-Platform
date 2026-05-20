using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CareerGuidance.Controllers
{
    [Authorize(Roles = "Admin")]
    public class JobsController : BaseAdminController
    {
        private readonly string[] jobTypes =
            { "Full Time", "Part Time", "Internship", "Permanent", "Remote", "Contract" };
        private const string DefaultImage = "job.jpg";

        public JobsController(CareerDbContext context) : base(context) { }

        // GET: Jobs
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            // Automatically close expired jobs
            await _context.Jobs
                .Where(j => j.IsActive && j.Deadline < today)
                .ExecuteUpdateAsync(j => j.SetProperty(x => x.IsActive, false));

            var jobs = await _context.Jobs
                .Where(j => j.IsActive)
                .OrderByDescending(j => j.PublishDate)
                .ToListAsync();

            return View(jobs);
        }

        // GET: Closed Jobs
        public async Task<IActionResult> ClosedJobs()
        {
            var closedJobs = await _context.Jobs
                .Where(j => !j.IsActive)
                .OrderByDescending(j => j.Deadline)
                .ToListAsync();

            return View(closedJobs);
        }

        // GET: Jobs/Create
        public IActionResult Create()
        {
            ViewBag.JobTypes = new SelectList(jobTypes);
            return View();
        }

        // POST: Jobs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Job job, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                job.PublishDate = DateTime.Today;
                job.Deadline = job.Deadline.Date;
                job.Image = await SaveImage(imageFile, DefaultImage);

                _context.Add(job);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Job created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.JobTypes = new SelectList(jobTypes);
            return View(job);
        }

        // GET: Jobs/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Jobs.FindAsync(id.Value);
            if (job == null) return NotFound();

            ViewBag.JobTypes = new SelectList(jobTypes);
            return View(job);
        }

        // POST: Jobs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Job job, IFormFile? imageFile)
        {
            if (id != job.JobId) return NotFound();

            if (ModelState.IsValid)
            {
                var existingJob = await _context.Jobs.AsNoTracking()
                    .FirstOrDefaultAsync(j => j.JobId == id);
                if (existingJob == null) return NotFound();

                job.PublishDate = existingJob.PublishDate;
                job.Deadline = job.Deadline.Date;
                job.Image = await SaveImage(imageFile, existingJob.Image);

                _context.Update(job);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Job updated successfully.";
                return RedirectToAction(nameof(Edit), new { id = job.JobId });
            }

            ViewBag.JobTypes = new SelectList(jobTypes);
            return View(job);
        }

        // POST: Jobs/Delete/5 (for both Active & Closed)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? redirectTo = "Index")
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job != null)
            {
                _context.Jobs.Remove(job);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Job deleted successfully.";
            }

            // redirectTo = "Index" (Active jobs) or "ClosedJobs"
            return RedirectToAction(redirectTo ?? "Index");
        }

        // POST: Jobs/DeleteAllClosed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllClosed()
        {
            var closedJobs = await _context.Jobs
                .Where(j => !j.IsActive)
                .ToListAsync();

            _context.Jobs.RemoveRange(closedJobs);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "All closed jobs deleted successfully.";
            return RedirectToAction(nameof(ClosedJobs));
        }

        // GET: Jobs/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Jobs
                .FirstOrDefaultAsync(j => j.JobId == id.Value);

            if (job == null) return NotFound();

            return View(job);
        }

        #region Helper Methods

        private async Task<string> SaveImage(IFormFile? imageFile, string defaultImage)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/jobs");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                var filePath = Path.Combine(uploadDir, fileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await imageFile.CopyToAsync(stream);

                return fileName;
            }

            return defaultImage;
        }

        #endregion
    }
}
