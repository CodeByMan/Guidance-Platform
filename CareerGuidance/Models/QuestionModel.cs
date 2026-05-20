namespace CareerGuidance.Models
{
    public class QuestionModel
    {
        public string QuestionId { get; set; }
        public string Text { get; set; }
        public string Category { get; set; }
        public int Weight { get; set; } = 1; // default weight = 1
    }
}
