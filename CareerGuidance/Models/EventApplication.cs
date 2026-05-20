using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerGuidance.Models
{
    public class EventApplication
    {
        [Key]
        public int EventApplicationId { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public int EventId { get; set; }
        [ForeignKey("EventId")]
        public Event Event { get; set; } = null!;

        public string Status { get; set; } = "Pending";
        public string? Message { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? MeetingLink { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    }
}
