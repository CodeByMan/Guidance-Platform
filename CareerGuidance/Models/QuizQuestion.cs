using System.ComponentModel.DataAnnotations;

namespace CodingLanguageQuiz.Models
{
    public class QuizQuestion
    {
        [Key]
        public int QuizQuestionId { get; set; }

        public string QuestionText { get; set; }

        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }

        public string CorrectOption { get; set; } // A, B, C, D
        public string LanguageCategory { get; set; } // HTML, CSS, JS, etc.
    }
}
