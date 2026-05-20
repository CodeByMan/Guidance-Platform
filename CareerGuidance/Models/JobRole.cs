using System.ComponentModel.DataAnnotations;

namespace CareerGuidance.Models
{
    public class JobRole
    {
        [Key]
        public int JobRoleId { get; set; }
        public string Role { get; set; }
        public string Skills { get; set; }
        public string Responsibilities { get; set; }
        public string SalaryRange { get; set; }
        public string JobOutlook { get; set; }
    }
}
