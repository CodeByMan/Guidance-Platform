using System;
using System.ComponentModel.DataAnnotations;

namespace CareerGuidance.Models
{
    public class ContactMessage
    {
        [Key]
        public int MessageId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, StringLength(200)]
        public string Subject { get; set; }

        [Required]
        public string Message { get; set; }

        public string? AdminReply { get; set; }

        public bool IsReplied { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? RepliedAt { get; set; }
    }
}
