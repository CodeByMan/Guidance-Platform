using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CareerGuidance.Controllers
{
    [Authorize(Roles = "User")]
    public class GoalController : UserBaseController
    {
        private readonly CareerRoadmapService _roadmapService;

        public GoalController(CareerDbContext context, CareerRoadmapService roadmapService)
            : base(context)
        {
            _roadmapService = roadmapService;
        }

        // Index / Dashboard
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();
            var goals = await _context.Goals
                .Include(g => g.Steps)
                .Where(g => !g.IsDeleted && g.UserId == userId)
                .ToListAsync();
            return View("Index", goals);
        }

        // GET: Create Goal
        public IActionResult Create() => View();

        // POST: Create Goal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Goal goal)
        {
            goal.UserId = GetCurrentUserId();
            goal.CreatedAt = DateTime.UtcNow;
            _context.Goals.Add(goal);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // GET: Goal Details
        public async Task<IActionResult> Details(int id)
        {
            var goal = await _context.Goals
                .Include(g => g.Steps)
                .FirstOrDefaultAsync(g => g.GoalId == id && !g.IsDeleted && g.UserId == GetCurrentUserId());

            if (goal == null) return NotFound();
            return View(goal);
        }

        // POST: Add Step
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStep(int goalId, string stepDesc)
        {
            if (string.IsNullOrWhiteSpace(stepDesc))
                return RedirectToAction("Details", new { id = goalId });

            var goal = await _context.Goals
                .Include(g => g.Steps)
                .FirstOrDefaultAsync(g => g.GoalId == goalId && !g.IsDeleted && g.UserId == GetCurrentUserId());

            if (goal == null) return NotFound();

            _context.GoalSteps.Add(new GoalStep
            {
                GoalId = goalId,
                StepDescription = stepDesc,
                StepOrder = goal.Steps.Count + 1
            });

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = goalId });
        }

        // POST: Toggle Step Completion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStep(int stepId)
        {
            var step = await _context.GoalSteps.FindAsync(stepId);
            if (step != null)
            {
                step.IsCompleted = !step.IsCompleted;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", new { id = step?.GoalId });
        }

        // POST: Auto-suggest roadmap steps
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoSuggestSteps(int goalId)
        {
            var goal = await _context.Goals
                .Include(g => g.Steps)
                .FirstOrDefaultAsync(g => g.GoalId == goalId && !g.IsDeleted && g.UserId == GetCurrentUserId());

            if (goal == null) return NotFound();

            var suggestedSteps = _roadmapService.GetStepsForCareer(goal.Title) ?? new System.Collections.Generic.List<string>();

            foreach (var step in suggestedSteps.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                if (!goal.Steps.Any(st => st.StepDescription.Equals(step, StringComparison.OrdinalIgnoreCase)))
                {
                    _context.GoalSteps.Add(new GoalStep
                    {
                        GoalId = goalId,
                        StepDescription = step,
                        StepOrder = goal.Steps.Count + 1
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Roadmap steps loaded!";
            return RedirectToAction("Details", new { id = goalId });
        }

        // GET: Delete Goal confirmation
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var goal = await _context.Goals
                .Include(g => g.Steps)
                .FirstOrDefaultAsync(g => g.GoalId == id && !g.IsDeleted && g.UserId == GetCurrentUserId());

            if (goal == null) return NotFound();
            return View(goal);
        }

        // POST: Delete Goal
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var goal = await _context.Goals
                .Include(g => g.Steps)
                .FirstOrDefaultAsync(g => g.GoalId == id && !g.IsDeleted && g.UserId == GetCurrentUserId());

            if (goal != null)
            {
                _context.GoalSteps.RemoveRange(goal.Steps);
                _context.Goals.Remove(goal);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}
