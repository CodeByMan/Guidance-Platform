using CareerGuidance.Models;
using CodingLanguageQuiz.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CodingLanguageQuiz.Controllers
{
    [Authorize(Roles = "User")]
    public class QuizController : Controller
    {
        private readonly CareerDbContext _context;
        private readonly IWebHostEnvironment _env;

        public QuizController(CareerDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Load questions from JSON and shuffle
        private List<QuizQuestion> LoadQuestions(string category)
        {
            string path = Path.Combine(_env.WebRootPath, "data", "coding_quiz.json");
            if (!System.IO.File.Exists(path))
                return new List<QuizQuestion>();

            var json = System.IO.File.ReadAllText(path);
            var jsonDoc = JsonDocument.Parse(json);

            if (!jsonDoc.RootElement.TryGetProperty(category, out var categoryQuestions))
                return new List<QuizQuestion>();

            var questions = categoryQuestions
                .EnumerateArray()
                .Select((q, index) => new QuizQuestion
                {
                    QuizQuestionId = index + 1,
                    QuestionText = q.GetProperty("questionText").GetString(),
                    OptionA = q.GetProperty("optionA").GetString(),
                    OptionB = q.GetProperty("optionB").GetString(),
                    OptionC = q.GetProperty("optionC").GetString(),
                    OptionD = q.GetProperty("optionD").GetString(),
                    CorrectOption = q.GetProperty("correctOption").GetString(),
                    LanguageCategory = q.GetProperty("languageCategory").GetString()
                })
                .OrderBy(x => Guid.NewGuid()) // Shuffle
                .Take(10)
                .ToList();

            return questions;
        }

        // GET: /Quiz/TakeQuiz?category=HTML
        [HttpGet]
        public IActionResult TakeQuiz(string category)
        {
            if (string.IsNullOrEmpty(category))
                return NotFound("Quiz category not specified.");

            var questions = LoadQuestions(category);
            if (questions.Count == 0)
                return NotFound("No quiz available for this category.");

            ViewBag.Category = category;
            return View(questions);
        }

        // POST: /Quiz/TakeQuiz
        [HttpPost]
        public IActionResult TakeQuiz([FromForm] string category, [FromForm] Dictionary<string, string> answers)
        {
            if (string.IsNullOrEmpty(category))
                return BadRequest("Category is missing.");

            var questions = LoadQuestions(category);
            if (questions.Count == 0)
                return BadRequest("No questions found for this category.");

            int score = 0;
            var details = new List<Dictionary<string, string>>();

            foreach (var q in questions)
            {
                answers.TryGetValue(q.QuizQuestionId.ToString(), out var ans);
                bool correct = ans == q.CorrectOption;
                if (correct) score++;

                details.Add(new Dictionary<string, string>
                {
                    { "QuestionText", q.QuestionText },
                    { "Selected", ans ?? "" },
                    { "Correct", q.CorrectOption }
                });
            }

            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            var result = new QuizResult
            {
                UserId = userId,
                TotalScore = score,
                TotalQuestions = questions.Count,
                DetailsJson = JsonSerializer.Serialize(details)
            };

            _context.QuizResults.Add(result);
            _context.SaveChanges();

            return Json(new { score = score, total = questions.Count, details = details });
        }
    }
}
