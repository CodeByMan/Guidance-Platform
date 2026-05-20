using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodingLanguageQuiz.Models
{
    public class QuizResult
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QuizResultId { get; set; }

        public int UserId { get; set; }

        public int TotalScore { get; set; }
        public int TotalQuestions { get; set; }
        public string DetailsJson { get; set; } // Stores user answers + correctness
    }
}
