using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerGuidance.Models
{
    public class SavedJob
    {
        public int SavedJobId { get; set; }

        [Required]
        public int JobId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime SavedDate { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("JobId")]
        public Job Job { get; set; }

    }
}
