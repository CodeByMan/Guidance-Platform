using System.ComponentModel.DataAnnotations;

namespace CareerGuidance.Models
{
    public class CareerPath
    {
        [Key]
        public int PathId { get; set; }
        public string Industry { get; set; }
        public string Role { get; set; }
        public string NextRole { get; set; }
        public string Skills { get; set; }
        public string Education { get; set; }
        public int ExperienceYears { get; set; }
    }
}
