using System.ComponentModel.DataAnnotations;

namespace CareerGuidance.Models
{
    public class GoalStep
    {
        [Key]
        public int GoalStepId { get; set; }

        [Required]
        public int GoalId { get; set; }

        [Required]
        public string StepDescription { get; set; }

        public bool IsCompleted { get; set; } = false;

        public int StepOrder { get; set; }

        public Goal Goal { get; set; }
    }
}
