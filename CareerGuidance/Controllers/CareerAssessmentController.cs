using CareerGuidance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Collections.Generic;

namespace CareerGuidance.Controllers
{
    [Authorize(Roles = "User")]
    public class CareerAssessmentController : UserBaseController
    {
        private readonly IWebHostEnvironment _env;

        public CareerAssessmentController(CareerDbContext context, IWebHostEnvironment env)
            : base(context)
        {
            _env = env;
        }

        private List<QuestionModel> LoadQuestions()
        {
            string path = Path.Combine(_env.WebRootPath, "data", "quiz_questions.json");
            if (!System.IO.File.Exists(path))
                return new List<QuestionModel>();

            string json = System.IO.File.ReadAllText(path);
            var jsonDoc = JsonDocument.Parse(json);

            return jsonDoc.RootElement
                          .GetProperty("questions")
                          .EnumerateArray()
                          .Select(q => new QuestionModel
                          {
                              QuestionId = q.GetProperty("id").GetString() ?? "",
                              Text = q.GetProperty("text").GetString() ?? "",
                              Category = q.GetProperty("category").GetString() ?? "",
                              Weight = q.TryGetProperty("weight", out var w) ? w.GetInt32() : 1
                          })
                          .ToList();
        }

        private string GetSuggestedCareer(string category)
        {
            return category switch
            {
                "Mathematical Inclination" => "Data Analyst, Software Developer, Statistician",
                "Creativity and Design" => "UI/UX Designer, Graphic Designer, Product Designer",
                "Problem-Solving & Logic" => "Software Engineer, QA Engineer, Algorithm Specialist",
                "Communication Skills" => "Technical Writer, Trainer, IT Consultant",
                "Attention to Detail" => "QA Engineer, Database Administrator, Network Specialist",
                "Interest in Emerging Technologies" => "AI Engineer, Blockchain Developer, Researcher",
                "Team vs Individual Work Preference" => "Project Manager, Scrum Master, Developer",
                "Learning Style & Patience" => "Researcher, Analyst, Developer",
                "Hands-on vs Conceptual Work" => "Systems Engineer, Architect, Developer",
                "Interest in People/Business vs Technology" => "Business Analyst, Product Manager, IT Consultant",
                _ => ""
            };
        }

        [HttpGet]
        public IActionResult TakeQuiz()
        {
            int userId = GetCurrentUserId();
            var existing = _context.CareerAssessments.FirstOrDefault(a => a.UserId == userId);
            if (existing != null)
            {
                TempData["Message"] = "You already took the quiz. You can retake it.";
                return RedirectToAction("Result");
            }
            return View(LoadQuestions());
        }

        [HttpPost]
        public IActionResult TakeQuiz(Dictionary<string, string> answers)
        {
            int userId = GetCurrentUserId();
            if (answers == null || answers.Count == 0)
                return BadRequest("No answers submitted.");

            var questions = LoadQuestions();
            var scores = new Dictionary<string, int>();

            // Initialize scores
            foreach (var q in questions)
                if (!string.IsNullOrEmpty(q.Category))
                    scores[q.Category] = 0;

            // Calculate scores
            foreach (var q in questions)
            {
                answers.TryGetValue(q.QuestionId, out var ans);
                int val = ans switch
                {
                    "NoInterest" => 0,
                    "Low" => 1,
                    "Normal" => 2,
                    "High" => 4,
                    _ => 0
                };
                if (!string.IsNullOrEmpty(q.Category))
                    scores[q.Category] += val * q.Weight;
            }

            string topCategory = scores.OrderByDescending(x => x.Value).FirstOrDefault().Key ?? "";
            int totalScore = scores.Values.Sum();
            string suggestedCareer = GetSuggestedCareer(topCategory);

            var jsonData = JsonSerializer.Serialize(new
            {
                CategoryScores = scores,
                TotalScore = totalScore
            });

            // Check if assessment already exists
            var existing = _context.CareerAssessments.FirstOrDefault(a => a.UserId == userId);
            if (existing == null)
            {
                _context.CareerAssessments.Add(new CareerAssessment
                {
                    UserId = userId,
                    Category = topCategory,
                    SuggestedCareer = suggestedCareer,
                    AssessmentDataJson = jsonData,
                    TotalScore = totalScore
                });
            }
            else
            {
                existing.Category = topCategory;
                existing.SuggestedCareer = suggestedCareer;
                existing.AssessmentDataJson = jsonData;
                existing.TotalScore = totalScore;
                _context.Entry(existing).State = EntityState.Modified;
            }

            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest("Error saving quiz results: " + msg);
            }

            return RedirectToAction("Result");
        }

        [HttpGet]
        public IActionResult Result()
        {
            int userId = GetCurrentUserId();
            var assessment = _context.CareerAssessments.FirstOrDefault(a => a.UserId == userId);
            if (assessment == null) return RedirectToAction("TakeQuiz");

            ViewBag.JsonData = JsonSerializer.Deserialize<JsonElement>(assessment.AssessmentDataJson);
            return View(assessment);
        }

        [HttpGet]
        public IActionResult RetakeQuiz()
        {
            int userId = GetCurrentUserId();
            var existing = _context.CareerAssessments.FirstOrDefault(a => a.UserId == userId);
            if (existing != null)
            {
                _context.CareerAssessments.Remove(existing);
                _context.SaveChanges();
            }
            return RedirectToAction("TakeQuiz");
        }
    }
}
