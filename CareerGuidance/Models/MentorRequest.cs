using System;

namespace CareerGuidance.Models
{
    public partial class MentorRequest
    {
        public int MentorRequestId { get; set; }
        public int MentorId { get; set; }
        public int UserId { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected, Scheduled
        public string? MeetingLink { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual User Mentor { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
