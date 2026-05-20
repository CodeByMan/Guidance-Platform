using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerGuidance.Models
{
    public class MentorProfile
    {
        [Key]
        public int MentorProfileId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }   // Linked to the User table

        public string? Specialization { get; set; }

        public string? Description { get; set; }

        public string? Degree { get; set; }

        public string? Experiences { get; set; } // e.g. "8 years in Software Engineering"

        public string? CurrentCompany { get; set; }

        public string? CurrentPost { get; set; }

        public string? WorkStatus { get; set; } = "Working";
        // Options: "Working", "Retired", "Owner"

        // Navigation property
        public User User { get; set; }
    }
}
