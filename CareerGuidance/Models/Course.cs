using System.Collections.Generic;

namespace CareerGuidance.Models
{
    public class Course
    {
        public int CourseId { get; set; } // Primary key
        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public string Fee { get; set; } = "Free";
        public string TrainerName { get; set; }
        public string? TrainerProfileImageUrl { get; set; }
        public string? QuizCategory { get; set; } // e.g., "HTML", "CSS", "JavaScript"

        // New columns for skill development resources
        public string? ReferenceBooks { get; set; } // Comma-separated book titles
        public string? ReferenceArticles { get; set; } // Comma-separated article URLs

        public ICollection<CourseComment> CourseComments { get; set; } = new List<CourseComment>();
    }
}
