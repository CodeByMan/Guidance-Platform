using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerGuidance.Models
{
    public class CareerAssessment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QuizId { get; set; }

        public int UserId { get; set; }

        [Required]
        public string AssessmentDataJson { get; set; }

        public string Category { get; set; }
        public string SuggestedCareer { get; set; }
        public int TotalScore { get; set; }
    }
}
