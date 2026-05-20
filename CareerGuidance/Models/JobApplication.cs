using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerGuidance.Models
{
    public class JobApplication
    {
        public int JobApplicationId { get; set; }

        [Required]
        public int UserId { get; set; } 

        [Required]
        public int JobId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Email { get; set; }

        public string Portfolio { get; set; }

        [Required]
        public string ResumePath { get; set; }

        [Required]
        public string CoverLetter { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime AppliedDate { get; set; } = DateTime.Now;

        // Navigation
        public Job Job { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
