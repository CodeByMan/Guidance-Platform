using System;
using System.ComponentModel.DataAnnotations;

namespace CareerGuidance.Models
{
    public class Job
    {
        public int JobId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public string JobType { get; set; }

        [Required]
        public string Salary { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Responsibility { get; set; }

        [Required]
        public string Qualification { get; set; }

        [Required]
        public string CompanyDetail { get; set; }

        [Required]
        public DateTime Deadline { get; set; }

        [Required]
        public DateTime PublishDate { get; set; }

        [Required]
        public int Vacancy { get; set; }

        public string Image { get; set; } = "job.png";

        public bool IsActive { get; set; } = true;
    }
}
