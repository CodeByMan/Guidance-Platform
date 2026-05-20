using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerGuidance.Models
{
    public class Resume
    {
        public int ResumeId { get; set; }

        [Required]

        public string FullName { get; set; }

        public string ResumeFor { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Objective { get; set; }

        public string Skills { get; set; }

        public string Experience { get; set; }

        public string Education { get; set; }

        public string Certifications { get; set; }

        public string Projects { get; set; }

        public string Languages { get; set; }

        public string? ProfileImageFileName { get; set; }

        public string SelectedTemplate { get; set; } = "PdfTemplate1";

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
