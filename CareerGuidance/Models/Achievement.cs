using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerGuidance.Models
{
    public class Achievement
    {
        public int AchievementId { get; set; }

        [Required]
        public string FullName { get; set; } = null!;

        public string Position { get; set; } = string.Empty;

        public string Organization { get; set; } = string.Empty;

        public string Sector { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string PhotoUrl { get; set; } = string.Empty;

        [NotMapped]
        public IFormFile? PhotoFile { get; set; } // For file upload
    }
}
