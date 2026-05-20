using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerGuidance.Models
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = "Webinar";

        [Required]
        public string DateTimeText { get; set; }  // Use proper DateTime

        [Required]
        public string Venue { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? ImageSrc { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [ValidateNever]
        public virtual User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
